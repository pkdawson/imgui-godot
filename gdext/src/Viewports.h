#pragma once
#include "Context.h"
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/window.hpp>
#include <imgui.h>
#include <memory>

using namespace godot;

namespace ImGui::Godot {

class ImGuiWindow : public Window
{
    GDCLASS(ImGuiWindow, Window);

protected:
    static void _bind_methods()
    {
        ClassDB::bind_method(D_METHOD("_close_requested"), &ImGuiWindow::_close_requested);
        ClassDB::bind_method(D_METHOD("_size_changed"), &ImGuiWindow::_size_changed);
    }

public:
    void init(ImGuiViewport* vp)
    {
        _vp = vp;
        connect("close_requested", Callable(this, "_close_requested"));
        connect("size_changed", Callable(this, "_size_changed"));
    }

    void _input(const Ref<InputEvent>& evt) override
    {
        ImGui::Godot::ProcessInput(evt, this);
    }

    void _close_requested()
    {
        _vp->PlatformRequestClose = true;
    }

    void _size_changed()
    {
        _vp->PlatformRequestResize = true;
    }

private:
    ImGuiViewport* _vp = nullptr;
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
