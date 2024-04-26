#include "Context.h"
#include "CanvasRenderer.h"
#include "common.h"
#include <imgui.h>

using namespace godot;

namespace ImGui::Godot {

namespace {
std::unique_ptr<Context> ctx;

const char* PlatformName = "godot4";
} // namespace

Context* GetContext()
{
    return ctx.get();
}

Context::Context(Window* mainWindow, RID mainSubViewport, std::unique_ptr<Renderer> r)
{
    renderer = std::move(r);
    input = std::make_unique<Input>(mainWindow);
    fonts = std::make_unique<Fonts>();

    ImGuiIO& io = ImGui::GetIO();
    io.BackendFlags = ImGuiBackendFlags_HasGamepad | ImGuiBackendFlags_HasSetMousePos |
                      ImGuiBackendFlags_HasMouseCursors | ImGuiBackendFlags_RendererHasVtxOffset |
                      ImGuiBackendFlags_RendererHasViewports;

    io.BackendPlatformName = PlatformName;
    io.BackendRendererName = renderer->Name();

    viewports = std::make_unique<Viewports>(mainWindow, mainSubViewport);
}

Context::~Context()
{
    if (ImGui::GetCurrentContext())
        ImGui::DestroyContext();
}

void Init(godot::Window* mainWindow, RID mainSubViewport, const Ref<Resource>& cfg)
{
    // re-init not allowed
    ERR_FAIL_COND(ctx);

    DisplayServer* DS = DisplayServer::get_singleton();
    RenderingServer* RS = RenderingServer::get_singleton();
    ProjectSettings* PS = ProjectSettings::get_singleton();

    RendererType rendererType;

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
    int threadModel = PS->get_setting("rendering/driver/threads/thread_model");

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

    ctx = std::make_unique<Context>(mainWindow, mainSubViewport, std::move(renderer));
    ctx->scale = cfg->get("Scale");
    ctx->renderer->InitViewport(mainSubViewport);

    String iniFilename = cfg->get("IniFilename");
    if (iniFilename.length() > 0)
        SetIniFilename(iniFilename);

    Array fonts = cfg->get("Fonts");
    for (int i = 0; i < fonts.size(); ++i)
    {
        Ref<Resource> fontres = fonts[i];
        Ref<FontFile> fontData = fontres->get("FontData");
        int fontSize = fontres->get("FontSize");
        bool merge = fontres->get("Merge");
        if (i == 0)
            AddFont(fontData, fontSize);
        else
            AddFont(fontData, fontSize, merge);
    }
    if (cfg->get("AddDefaultFont"))
        AddFontDefault();
    RebuildFontAtlas();
}

void Update(double delta, Vector2 displaySize)
{
    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = displaySize;
    io.DeltaTime = static_cast<float>(delta);

    ctx->input->Update();

    gdscache->OnNewFrame();
    ImGui::NewFrame();
}

bool ProcessInput(const Ref<InputEvent>& evt, Window* window)
{
    return ctx->input->ProcessInput(evt, window);
}

void ProcessNotification(int what)
{
    if (ctx)
        ctx->input->ProcessNotification(what);
}

void Render()
{
    ImGui::Render();
    ImGui::UpdatePlatformWindows();
    ctx->renderer->Render();
}

void Shutdown()
{
    if (ImGui::GetCurrentContext())
        ImGui::DestroyContext();
    ctx.reset();
}

void Connect(const godot::Callable& callable)
{
    Object* igl = Engine::get_singleton()->get_singleton("ImGuiLayer");
    ERR_FAIL_COND(!igl);
    igl->connect("imgui_layout", callable);
}

void ResetFonts()
{
    ctx->fonts->Reset();
}

void AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge, const ImVector<ImWchar>& glyphRanges)
{
    ctx->fonts->Add(fontFile, fontSize, merge, glyphRanges);
}

void AddFontDefault()
{
    ctx->fonts->Add(nullptr, 13, false);
}

void RebuildFontAtlas(float scale)
{
    bool scaleToDpi = ProjectSettings::get_singleton()->get_setting("display/window/dpi/allow_hidpi");
    int dpiFactor = std::max(1, DisplayServer::get_singleton()->screen_get_dpi() / 96);
    ctx->fonts->RebuildFontAtlas(scaleToDpi ? dpiFactor * scale : scale);
}

void SetIniFilename(const String& fn)
{
    ImGuiIO& io = ImGui::GetIO();
    static std::vector<char> iniFilename;
    if (fn.length() > 0)
    {
        std::string globalfn = ProjectSettings::get_singleton()->globalize_path(fn).utf8().get_data();
        iniFilename.resize(globalfn.length() + 1);
        std::copy(globalfn.begin(), globalfn.end(), iniFilename.begin());
        iniFilename.back() = '\0';
        io.IniFilename = iniFilename.data();
    }
    else
        io.IniFilename = nullptr;
}

void OnFramePreDraw()
{
    ctx->renderer->OnFramePreDraw();
}

bool SubViewportWidget(SubViewport* svp)
{
    ImVec2 vpSize = svp->get_size();
    ImVec2 pos = ImGui::GetCursorScreenPos();
    ImVec2 pos_max = {pos.x + vpSize.x, pos.y + vpSize.y};
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
