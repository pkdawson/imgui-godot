#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/window.hpp>
#pragma warning(pop)

using godot::Window;

namespace ImGui::Godot {

void Init(Window* window);
void Update(double delta);
void Render();
void Shutdown();

} // namespace ImGui::Godot

extern "C" {

// void GDE_EXPORT ImGuiGodot_Init(Window* window);
}
