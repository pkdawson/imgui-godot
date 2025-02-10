#include "Context.h"
#include "CanvasRenderer.h"
#include "common.h"
#include <imgui.h>

using namespace godot;

namespace ImGui::Godot {

namespace {
std::unique_ptr<Context> ctx;

const char* PlatformName = "godot4";

void SetImeData(ImGuiContext* ctx, ImGuiViewport* vp, ImGuiPlatformImeData* data)
{
    DisplayServer* DS = DisplayServer::get_singleton();
    const int32_t windowID = (int32_t)(intptr_t)vp->PlatformHandle;

    DS->window_set_ime_active(data->WantVisible, windowID);
    if (data->WantVisible)
    {
        Vector2i pos;
        pos.x = data->InputPos.x - vp->Pos.x;
        pos.y = data->InputPos.y - vp->Pos.y + data->InputLineHeight;
        DS->window_set_ime_position(pos, windowID);
    }
}

void SetClipboardText(ImGuiContext* c, const char* text)
{
    DisplayServer::get_singleton()->clipboard_set(String::utf8(text));
}

const char* GetClipboardText(ImGuiContext* c)
{
    static std::vector<char> clipbuf;

    CharString cbtext = DisplayServer::get_singleton()->clipboard_get().utf8();
    const std::string_view sv(cbtext.get_data(), cbtext.length());

    clipbuf.resize(sv.size() + 1);
    std::copy(sv.begin(), sv.end(), clipbuf.begin());
    clipbuf[sv.size()] = '\0';

    return clipbuf.data();
}
} // namespace

Context* GetContext()
{
    return ctx.get();
}

Context::Context(std::unique_ptr<Renderer> r)
{
    renderer = std::move(r);
    input = std::make_unique<Input>();
    fonts = std::make_unique<Fonts>();

    ImGuiIO& io = ImGui::GetIO();
    io.BackendFlags = ImGuiBackendFlags_HasGamepad | ImGuiBackendFlags_HasSetMousePos |
                      ImGuiBackendFlags_HasMouseCursors | ImGuiBackendFlags_RendererHasVtxOffset |
                      ImGuiBackendFlags_RendererHasViewports;

    io.BackendPlatformName = PlatformName;
    io.BackendRendererName = renderer->Name();

    ImGuiPlatformIO& pio = ImGui::GetPlatformIO();
    pio.Platform_SetImeDataFn = SetImeData;
    pio.Platform_SetClipboardTextFn = SetClipboardText;
    pio.Platform_GetClipboardTextFn = GetClipboardText;

    viewports = std::make_unique<Viewports>();
}

Context::~Context()
{
    if (ImGui::GetCurrentContext())
        ImGui::DestroyContext();
}

void Init(const Ref<Resource>& cfg)
{
    // re-init not allowed
    ERR_FAIL_COND(ctx);

    DisplayServer* DS = DisplayServer::get_singleton();
    RenderingServer* RS = RenderingServer::get_singleton();
    ProjectSettings* PS = ProjectSettings::get_singleton();

    RendererType rendererType = RendererType::Dummy;

    String rendererName = cfg->get("Renderer");
    if (rendererName == "Dummy")
        rendererType = RendererType::Dummy;
    else if (rendererName == "Canvas")
        rendererType = RendererType::Canvas;
    else
        rendererType = RendererType::RenderingDevice;

    if (DS->get_name() == "headless")
        rendererType = RendererType::Dummy;

    // fall back to Canvas in OpenGL compatibility mode
    if (rendererType == RendererType::RenderingDevice && !RS->get_rendering_device())
        rendererType = RendererType::Canvas;

    // there's no way to get the actual current thread model, eg if --render-thread is used
    const int threadModel = PS->get_setting("rendering/driver/threads/thread_model");

    std::unique_ptr<Renderer> renderer;
    switch (rendererType)
    {
    case RendererType::Dummy:
        renderer = std::make_unique<DummyRenderer>();
        break;
    case RendererType::Canvas:
        renderer = std::make_unique<CanvasRenderer>();
        break;
    case RendererType::RenderingDevice:
        renderer = threadModel == 2 ? std::make_unique<RdRendererThreadSafe>() : std::make_unique<RdRenderer>();
        break;
    }

    if (!renderer->Init())
    {
        if (rendererType == RendererType::RenderingDevice)
        {
            UtilityFunctions::push_warning("imgui-godot: falling back to Canvas renderer");
            renderer = std::make_unique<CanvasRenderer>();
        }
        else
        {
            UtilityFunctions::push_error("imgui-godot: failed to init renderer");
            renderer = std::make_unique<DummyRenderer>();
        }
        renderer->Init();
    }

    ctx = std::make_unique<Context>(std::move(renderer));
    ctx->scale = cfg->get("Scale");

    String iniFilename = cfg->get("IniFilename");
    SetIniFilename(iniFilename);

    Array fonts = cfg->get("Fonts");
    for (int i = 0; i < fonts.size(); ++i)
    {
        Ref<Resource> fontres = fonts[i];
        Ref<FontFile> fontData = fontres->get("FontData");
        const int fontSize = fontres->get("FontSize");
        const bool merge = fontres->get("Merge");
        if (i == 0)
            AddFont(fontData, fontSize);
        else
            AddFont(fontData, fontSize, merge);
    }
    if (cfg->get("AddDefaultFont"))
        AddFontDefault();
    RebuildFontAtlas();
}

void Context::Update(double delta, Vector2 displaySize)
{
    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = displaySize;
    io.DeltaTime = static_cast<float>(delta);

    input->Update();

    gdscache->OnNewFrame();
    ImGui::NewFrame();
}

void Context::Render()
{
    ImGui::Render();
    ImGui::UpdatePlatformWindows();
    renderer->Render();
}

void Shutdown()
{
    if (ImGui::GetCurrentContext())
        ImGui::DestroyContext();
    ctx.reset();
}

void Connect(const godot::Callable& callable)
{
    Object* igc = Engine::get_singleton()->get_singleton("ImGuiController");
    ERR_FAIL_COND(!igc);
    igc->connect("imgui_layout", callable);
}

void ResetFonts()
{
    ERR_FAIL_COND(!ctx);
    ctx->fonts->Reset();
}

void AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge, const ImVector<ImWchar>& glyphRanges)
{
    ERR_FAIL_COND(!ctx);
    ctx->fonts->Add(fontFile, fontSize, merge, glyphRanges);
}

void AddFontDefault()
{
    ERR_FAIL_COND(!ctx);
    ctx->fonts->Add(nullptr, 13, false);
}

void RebuildFontAtlas()
{
    ERR_FAIL_COND(!ctx);
    ERR_FAIL_COND(ctx->inProcessFrame);

    const bool scaleToDpi = ProjectSettings::get_singleton()->get_setting("display/window/dpi/allow_hidpi");
    const int dpiFactor = std::max(1, DisplayServer::get_singleton()->screen_get_dpi() / 96);
    ctx->fonts->RebuildFontAtlas(scaleToDpi ? dpiFactor * ctx->scale : ctx->scale);
}

void SetIniFilename(const String& fn)
{
    ImGuiIO& io = ImGui::GetIO();
    if (fn.length() > 0)
    {
        std::string globalfn = ProjectSettings::get_singleton()->globalize_path(fn).utf8().get_data();
        ctx->iniFilename.resize(globalfn.length() + 1);
        std::copy(globalfn.begin(), globalfn.end(), ctx->iniFilename.begin());
        ctx->iniFilename.back() = '\0';
        io.IniFilename = ctx->iniFilename.data();
    }
    else
        io.IniFilename = nullptr;
}

bool SubViewportWidget(SubViewport* svp)
{
    const ImVec2 vpSize = svp->get_size();
    const ImVec2 pos = ImGui::GetCursorScreenPos();
    const ImVec2 pos_max = {pos.x + vpSize.x, pos.y + vpSize.y};
    ImGui::GetWindowDrawList()->AddImage((ImTextureID)svp->get_texture()->get_rid().get_id(), pos, pos_max);

    ImGui::PushID(svp->get_instance_id());
    ImGui::InvisibleButton("godot_subviewport", vpSize);
    ImGui::PopID();

    if (ImGui::IsItemHovered())
    {
        ctx->input->SetActiveSubViewport(svp, pos);
        return true;
    }
    return false;
}

} // namespace ImGui::Godot
