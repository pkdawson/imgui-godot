#pragma once
#include "imgui-godot.h"
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
    }

public:
    void init(ImGuiViewport* vp)
    {
        _vp = vp;
        // TODO: connect signals
    }

    void _input(const Ref<InputEvent>& evt) override
    {
        ImGui::Godot::ProcessInput(evt, this);
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
