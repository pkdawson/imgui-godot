#include "ImGuiController.h"
#include "Context.h"
#include "ImGuiControllerHelper.h"
#include "ImGuiLayer.h"
#include "Input.h"
#include "InputLocal.h"
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/gd_script.hpp>
#include <godot_cpp/classes/packed_scene.hpp>
#include <godot_cpp/classes/resource_loader.hpp>

namespace ImGui::Godot {

namespace {
ImGuiController* instance = nullptr;
}

struct ImGuiController::Impl
{
    Window* window;
    ImGuiControllerHelper* helper = nullptr;

    void CheckContentScale() const;
};

void ImGuiController::Impl::CheckContentScale() const
{
    if (window->get_content_scale_mode() == Window::CONTENT_SCALE_MODE_VIEWPORT)
    {
        UtilityFunctions::printerr("imgui-godot: scale mode `viewport` is unsupported");
    }
}


ImGuiController* ImGuiController::Instance()
{
    return instance;
}

void ImGuiController::_bind_methods()
{
    ADD_SIGNAL(MethodInfo("imgui_layout"));
    ClassDB::bind_method(D_METHOD("on_frame_pre_draw"), &ImGuiController::on_frame_pre_draw);
    ClassDB::bind_method(D_METHOD("OnLayerExiting"), &ImGuiController::OnLayerExiting);
}

ImGuiController::ImGuiController() : impl(std::make_unique<Impl>())
{
}

ImGuiController::~ImGuiController()
{
}

void ImGuiController::_enter_tree()
{
    instance = this;

    set_name("ImGuiController");
    Engine::get_singleton()->register_singleton("ImGuiController", this);
    impl->window = get_window();

    impl->CheckContentScale();

    Ref<PackedScene> cfgPackedScene = ResourceLoader::get_singleton()->load("res://addons/imgui-godot/Config.tscn");
    Node* cfgScene = cfgPackedScene->instantiate();
    Ref<Resource> cfg = cfgScene->get("Config");
    memdelete(cfgScene);

    if (cfg.is_null())
    {
        Ref<GDScript> script = ResourceLoader::get_singleton()->load("res://addons/imgui-godot/scripts/ImGuiConfig.gd");
        cfg = script->new_();
    }

    ImGui::Godot::Init(cfg);

    impl->helper = memnew(ImGuiControllerHelper);
    add_child(impl->helper);

    SetMainViewport(impl->window);
}

void ImGuiController::_ready()
{
    set_process_priority(std::numeric_limits<int>::max());
    set_process_mode(Node::PROCESS_MODE_ALWAYS);
}

void ImGuiController::_exit_tree()
{
    ImGui::Godot::Shutdown();
}

void ImGuiController::_process(double delta)
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
    {
        // verify signal connections
        auto conns = get_signal_connection_list("imgui_layout");
        for (int i = 0; i < conns.size(); ++i)
        {
            const Dictionary& conn = conns[i];
            const Callable& cb = conn["callable"];
            if (!cb.is_valid())
                disconnect("imgui_layout", cb);
        }
    }
#endif

    Context* ctx = GetContext();
    ctx->layer->UpdateViewport();
    emit_signal("imgui_layout");
    ctx->Render();
    ctx->inProcessFrame = false;
}

void ImGuiController::_notification(int p_what)
{
    Context* ctx = GetContext();
    if (ctx)
        ctx->input->ProcessNotification(p_what);
}

void ImGuiController::OnLayerExiting()
{
    // an ImGuiLayer is being destroyed without calling SetMainViewport
    if (GetContext()->layer->get_viewport() != impl->window)
    {
        // revert to main window
        SetMainViewport(impl->window);
    }
}

void ImGuiController::SetMainViewport(Viewport* vp)
{
    Context* ctx = GetContext();
    ImGuiLayer* oldLayer = ctx->layer;
    if (oldLayer)
    {
        oldLayer->disconnect("tree_exiting", Callable(this, "OnLayerExiting"));
        oldLayer->queue_free();
    }

    ImGuiLayer* newLayer = memnew(ImGuiLayer);
    newLayer->connect("tree_exiting", Callable(this, "OnLayerExiting"));

    if (vp->get_class() == "Window")
    {
        ctx->input = std::make_unique<Input>();
        if (vp == impl->window)
            add_child(newLayer);
        else
            vp->add_child(newLayer);
        ImGui::GetIO().BackendFlags |= ImGuiBackendFlags_PlatformHasViewports;
    }
    else
    {
        ctx->input = std::make_unique<InputLocal>();
        vp->add_child(newLayer);
        ImGui::GetIO().BackendFlags &= ~ImGuiBackendFlags_PlatformHasViewports;
    }
    ctx->layer = newLayer;
}

void ImGuiController::on_frame_pre_draw()
{
    GetContext()->renderer->OnFramePreDraw();
}

} // namespace ImGui::Godot
