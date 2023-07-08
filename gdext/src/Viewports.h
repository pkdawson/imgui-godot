#pragma once
#include "imgui-godot.h"
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/window.hpp>
#include <imgui.h>
#include <memory>

using namespace godot;

namespace ImGui::Godot {

class WindowSignalProxy : public Object
{
    GDCLASS(WindowSignalProxy, Object);

protected:
    static void _bind_methods()
    {
        ClassDB::bind_method(D_METHOD("window_input", "evt"), &WindowSignalProxy::window_input);
    }

public:
    void init(ImGuiViewport* vp, Window* window)
    {
        _vp = vp;
        _window = window;
    }

    void window_input(const Ref<InputEvent>& evt)
    {
        ImGui::Godot::ProcessInput(evt, _window);
    }

    void close_requested()
    {
        _vp->PlatformRequestClose = true;
    }

    void size_changed()
    {
        _vp->PlatformRequestResize = true;
    }

private:
    ImGuiViewport* _vp = nullptr;
    Window* _window = nullptr;
};

class Viewports
{
public:
    Viewports(Window* mainWindow, RID mainSubViewport);
    ~Viewports();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
