#include "Viewports.h"
#include <imgui.h>

#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/variant/callable.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace ImGui::Godot {

struct Godot_ViewportData
{
    Window* window = nullptr;
};

struct Viewports::Impl
{
    void InitPlatformInterface();
    void UpdateMonitors();
};

static void Godot_CreateWindow(ImGuiViewport* vp)
{
    Godot_ViewportData* vd = IM_NEW(Godot_ViewportData);
    vp->PlatformUserData = vd;

    {
        Godot_ViewportData* mainvd = (Godot_ViewportData*)ImGui::GetMainViewport()->PlatformUserData;
        Window* mainWindow = mainvd->window;
        if (mainWindow->is_embedding_subwindows())
        {
            UtilityFunctions::push_warning(
                "ImGui Viewports: 'display/window/subwindows/embed_subwindows' needs to be disabled");
            mainWindow->set_embedding_subwindows(false);
        }
    }

    Rect2i winRect = Rect2i(vp->Pos, vp->Size);

    ImGuiWindow* igwin = memnew(ImGuiWindow);
    igwin->init(vp);
    vd->window = Object::cast_to<Window>(igwin);
    vd->window->set_flag(Window::FLAG_BORDERLESS, true);
    vd->window->set_position(winRect.position);
    vd->window->set_size(winRect.size);
    vd->window->set_transparent_background(true);
    vd->window->set_flag(Window::FLAG_TRANSPARENT, true);

    // Callable::bind is not yet implemented...
    // vd->window->connect("window_input", Callable(vd->signalProxy, "window_input"));
    // vd->window->connect("close_requested", Callable(vd->signalProxy, "close_requested"));
    // vd->window->connect("size_changed", Callable(vd->signalProxy, "size_changed"));

    Node* root = Object::cast_to<Node>(Engine::get_singleton()->get_singleton("ImGuiRoot"));
    root->add_child(vd->window);

    // need to do this after add_child
    vd->window->set_flag(Window::FLAG_TRANSPARENT, true);

    // it's our window, so just draw directly to the root viewport
    RID vprid = vd->window->get_viewport_rid();
    vp->RendererUserData = (void*)vprid.get_id();

    RenderingServer* RS = RenderingServer::get_singleton();
    RS->viewport_set_clear_mode(vprid, RenderingServer::VIEWPORT_CLEAR_NEVER);
    RS->viewport_set_transparent_background(vprid, true);
}

static void Godot_DestroyWindow(ImGuiViewport* vp)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    if (vd)
    {
        if (vd->window && vp != ImGui::GetMainViewport())
        {
            vd->window->queue_free();
        }
        IM_DELETE(vd);
        vp->PlatformUserData = nullptr;
        vp->RendererUserData = nullptr;
    }
}

static void Godot_ShowWindow(ImGuiViewport* vp)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    vd->window->show();
}

static void Godot_SetWindowPos(ImGuiViewport* vp, ImVec2 pos)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    vd->window->set_position(pos);
}

ImVec2 Godot_GetWindowPos(ImGuiViewport* vp)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    return vd->window->get_position();
}

void Godot_SetWindowSize(ImGuiViewport* vp, ImVec2 size)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    vd->window->set_size(size);
}

ImVec2 Godot_GetWindowSize(ImGuiViewport* vp)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    return vd->window->get_size();
}

void Godot_SetWindowFocus(ImGuiViewport* vp)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    vd->window->grab_focus();
}

bool Godot_GetWindowFocus(ImGuiViewport* vp)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    return vd->window->has_focus();
}

bool Godot_GetWindowMinimized(ImGuiViewport* vp)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    return vd->window->get_mode() & Window::MODE_MINIMIZED;
}

void Godot_SetWindowTitle(ImGuiViewport* vp, const char* str)
{
    Godot_ViewportData* vd = (Godot_ViewportData*)vp->PlatformUserData;
    vd->window->set_title(String(str));
}

void Viewports::Impl::InitPlatformInterface()
{
    auto& pio = ImGui::GetPlatformIO();
    pio.Platform_CreateWindow = Godot_CreateWindow;
    pio.Platform_DestroyWindow = Godot_DestroyWindow;
    pio.Platform_ShowWindow = Godot_ShowWindow;
    pio.Platform_SetWindowPos = Godot_SetWindowPos;
    pio.Platform_GetWindowPos = Godot_GetWindowPos;
    pio.Platform_SetWindowSize = Godot_SetWindowSize;
    pio.Platform_GetWindowSize = Godot_GetWindowSize;
    pio.Platform_SetWindowFocus = Godot_SetWindowFocus;
    pio.Platform_GetWindowFocus = Godot_GetWindowFocus;
    pio.Platform_GetWindowMinimized = Godot_GetWindowMinimized;
    pio.Platform_SetWindowTitle = Godot_SetWindowTitle;
}

void Viewports::Impl::UpdateMonitors()
{
    auto& pio = ImGui::GetPlatformIO();
    DisplayServer* DS = DisplayServer::get_singleton();
    int screenCount = DS->get_screen_count();

    pio.Monitors.resize(0);
    for (int i = 0; i < screenCount; ++i)
    {
        ImGuiPlatformMonitor monitor;
        monitor.MainPos = DS->screen_get_position(i);
        monitor.MainSize = DS->screen_get_size(i);
        monitor.DpiScale = DS->screen_get_scale(i);

        Rect2i rect = DS->screen_get_usable_rect(i);
        monitor.WorkPos = rect.position;
        monitor.WorkSize = rect.size;

        pio.Monitors.push_back(monitor);
    }
}

Viewports::Viewports(Window* mainWindow, RID mainSubViewport) : impl(std::make_unique<Impl>())
{
    auto& io = ImGui::GetIO();
    io.BackendFlags |= ImGuiBackendFlags_PlatformHasViewports;
    impl->InitPlatformInterface();
    impl->UpdateMonitors();

    Godot_ViewportData* mainvd = IM_NEW(Godot_ViewportData);
    mainvd->window = mainWindow;
    ImGuiViewport* vp = ImGui::GetMainViewport();
    vp->PlatformUserData = mainvd;
    vp->RendererUserData = (void*)mainSubViewport.get_id();
}

Viewports::~Viewports()
{
}

} // namespace ImGui::Godot
