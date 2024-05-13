#pragma once

#include <godot_cpp/classes/input_event.hpp>
#include <godot_cpp/classes/sub_viewport.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/variant/vector2.hpp>
#include <memory>

using namespace godot;

namespace ImGui::Godot {
class Input
{
public:
    Input();
    virtual ~Input();

    void Update();
    virtual bool ProcessInput(const Ref<InputEvent>& evt);
    void ProcessNotification(int what);
    void SetActiveSubViewport(godot::SubViewport* svp, Vector2 pos);

protected:
    virtual void UpdateMousePos();
    void ProcessSubViewportWidget(const Ref<InputEvent>& evt);
    bool HandleEvent(const Ref<InputEvent>& evt);

private:
    void UpdateMouse();

    struct Impl;
    std::unique_ptr<Impl> impl;
};
} // namespace ImGui::Godot
