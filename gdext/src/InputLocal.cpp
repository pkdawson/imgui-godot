#include "InputLocal.h"
#include <godot_cpp/classes/input_event_mouse_motion.hpp>
#include <imgui.h>

namespace ImGui::Godot {

void InputLocal::UpdateMousePos()
{
    // do not use global mouse position
}

bool InputLocal::ProcessInput(const Ref<InputEvent>& evt)
{
    // no support for SubViewport widgets

    if (Ref<InputEventMouseMotion> mm = evt; mm.is_valid())
    {
        ImGuiIO& io = ImGui::GetIO();
        Vector2 mousePos = mm->get_position();
        io.AddMousePosEvent(mousePos.x, mousePos.y);
        return io.WantCaptureMouse;
    }
    return HandleEvent(evt);
}

} // namespace ImGui::Godot
