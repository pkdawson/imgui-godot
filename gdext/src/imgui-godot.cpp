#include "imgui-godot.h"
#include "DummyRenderer.h"
#include "Fonts.h"
#include "ImGuiGD.h"
#include "Input.h"
#include "RdRenderer.h"
#include "RdRendererThreadSafe.h"
#include "Renderer.h"
#include "ShortTermCache.h"
#include "Viewports.h"
#include "common.h"
#include <imgui.h>

#pragma warning(push, 0)
#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/image.hpp>
#include <godot_cpp/classes/image_texture.hpp>
#include <godot_cpp/classes/project_settings.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/classes/sub_viewport.hpp>
#include <godot_cpp/classes/viewport_texture.hpp>
#include <godot_cpp/variant/packed_byte_array.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

using namespace godot;

namespace ImGui::Godot {

namespace {
struct Context
{
    Window* mainWindow = nullptr;
    std::unique_ptr<Renderer> renderer;
    std::unique_ptr<Input> input;
    std::unique_ptr<Fonts> fonts;
    std::unique_ptr<Viewports> viewports;
    RID svp;
    RID ci;
    Ref<ImageTexture> fontTexture;
    bool headless = false;
    int dpiFactor = 1;
    bool scaleToDPI = false;
    std::vector<char> iniFilename;

    ~Context()
    {
        RenderingServer::get_singleton()->free_rid(ci);
        RenderingServer::get_singleton()->free_rid(svp);
    }
};

std::unique_ptr<Context> ctx;

const char* PlatformName = "godot4";
} // namespace

void Init(godot::Window* mainWindow, RID canvasItem, const Ref<Resource>& cfg)
{
    // re-init not allowed
    if (ctx)
        return;

    ctx = std::make_unique<Context>();
    ctx->mainWindow = mainWindow;
    ctx->ci = canvasItem;
    ctx->input = std::make_unique<Input>(ctx->mainWindow);

    int32_t screenDPI = DisplayServer::get_singleton()->screen_get_dpi();
    ctx->dpiFactor = std::max(1, screenDPI / 96);
    ctx->scaleToDPI = ProjectSettings::get_singleton()->get_setting("display/window/dpi/allow_hidpi");

    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = ctx->mainWindow->get_size();

    io.BackendPlatformName = PlatformName;

    io.BackendFlags |= ImGuiBackendFlags_HasGamepad;
    io.BackendFlags |= ImGuiBackendFlags_HasMouseCursors;
    io.BackendFlags |= ImGuiBackendFlags_HasSetMousePos;
    io.BackendFlags |= ImGuiBackendFlags_RendererHasVtxOffset;
    io.BackendFlags |= ImGuiBackendFlags_PlatformHasViewports;
    io.BackendFlags |= ImGuiBackendFlags_RendererHasViewports;

    Array fonts = cfg->get("Fonts");
    bool addDefaultFont = cfg->get("AddDefaultFont");
    float scale = cfg->get("Scale");
    String iniFilename = cfg->get("IniFilename");
    String rendererName = cfg->get("Renderer");

    SetIniFilename(iniFilename);

    RenderingServer* RS = RenderingServer::get_singleton();

    ctx->headless = DisplayServer::get_singleton()->get_name() == "headless";

    if (!ctx->headless && !RS->get_rendering_device())
    {
        ctx->headless = true;
        UtilityFunctions::printerr("imgui-godot requires RenderingDevice");
    }

    if (ctx->headless || rendererName == "Dummy")
    {
        ctx->renderer = std::make_unique<DummyRenderer>();
    }
    else
    {
        int threadModel = ProjectSettings::get_singleton()->get_setting("rendering/driver/threads/thread_model");
#ifdef DEBUG_ENABLED
        if (Engine::get_singleton()->is_editor_hint())
            threadModel = 0;
#endif
        if (threadModel == 2)
            ctx->renderer = std::make_unique<RdRendererThreadSafe>();
        else
            ctx->renderer = std::make_unique<RdRenderer>();
    }
    io.BackendRendererName = ctx->renderer->Name();

    Object* igl = Engine::get_singleton()->get_singleton("ImGuiLayer");
    RS->connect("frame_pre_draw", Callable(igl, "on_frame_pre_draw"));

    ctx->svp = RS->viewport_create();
    RS->viewport_set_transparent_background(ctx->svp, true);
    RS->viewport_set_update_mode(ctx->svp, RenderingServer::VIEWPORT_UPDATE_ALWAYS);
    RS->viewport_set_clear_mode(ctx->svp, RenderingServer::VIEWPORT_CLEAR_NEVER);
    RS->viewport_set_active(ctx->svp, true);
    RS->viewport_set_parent_viewport(ctx->svp, ctx->mainWindow->get_viewport_rid());

    ctx->fonts = std::make_unique<Fonts>();

    for (int i = 0; i < fonts.size(); ++i)
    {
        Ref<Resource> fontres = fonts[i];
        Ref<FontFile> font = fontres->get("FontData");
        int fontSize = fontres->get("FontSize");
        bool merge = fontres->get("Merge");
        AddFont(font, fontSize, i > 0 && merge);
    }
    if (addDefaultFont)
        AddFontDefault();
    RebuildFontAtlas(scale);

    ctx->viewports = std::make_unique<Viewports>(ctx->mainWindow, ctx->svp);
}

void Update(double delta)
{
    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = ctx->mainWindow->get_size();
    io.DeltaTime = static_cast<float>(delta);

    if (!ctx->headless)
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
    RenderingServer* RS = RenderingServer::get_singleton();
    godot::Vector2i winSize = ctx->mainWindow->get_size();
    RS->viewport_set_size(ctx->svp, winSize.x, winSize.y);
    RID vptex = RS->viewport_get_texture(ctx->svp);
    RS->canvas_item_clear(ctx->ci);
    RS->canvas_item_set_transform(ctx->ci, ctx->mainWindow->get_final_transform().affine_inverse());
    RS->canvas_item_add_texture_rect(ctx->ci, godot::Rect2(0, 0, winSize.x, winSize.y), vptex);

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

void AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge)
{
    ctx->fonts->Add(fontFile, fontSize, merge);
}

void AddFontDefault()
{
    ctx->fonts->Add(nullptr, 13, false);
}

void RebuildFontAtlas(float scale)
{
    ctx->fonts->RebuildFontAtlas(ctx->scaleToDPI ? ctx->dpiFactor * scale : scale);
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

void SetVisible(bool visible)
{
    CanvasLayer* igl = Object::cast_to<CanvasLayer>(Engine::get_singleton()->get_singleton("ImGuiLayer"));
    ERR_FAIL_COND(!igl);
    igl->set_visible(visible);
}

void OnFramePreDraw()
{
    ctx->renderer->OnFramePreDraw();
}

bool SubViewport(godot::SubViewport* svp)
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
