#pragma once

#ifdef _WIN32
#ifdef IGN_EXPORT
#define IGN_API __declspec(dllexport)
#else
#define IGN_API __declspec(dllimport)
#endif
#endif

#pragma warning(push, 0)
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/classes/canvas_layer.hpp>
#pragma warning(pop)

#ifdef __cplusplus
static_assert(sizeof(godot::RID) == 8);
static_assert(sizeof(void*) == 8);
#endif

extern "C" {
void IGN_API ImGuiGodot_Init(godot::Window* window, godot::CanvasLayer* layer);
void IGN_API ImGuiGodot_Update(double delta);
void IGN_API ImGuiGodot_Render();
void IGN_API ImGuiGodot_Shutdown();
}
