#pragma once

#if !__has_include("godot_cpp/classes/engine.hpp")
// TODO: support Godot modules
#error this header is only usable from a C++ GDExtension
#endif

#ifdef _WIN32
#ifdef IGN_EXPORT
#define IGN_API __declspec(dllexport)
#else
#define IGN_API __declspec(dllimport)
#endif
#else
#define IGN_API
#endif

#include <imgui.h>

#pragma warning(push, 0)
#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/sub_viewport.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/variant/callable.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

static_assert(sizeof(godot::RID) == 8);
static_assert(sizeof(void*) == 8);

namespace ImGui::Godot {
#ifdef IGN_EXPORT
void Init(godot::Window* mainWindow, godot::RID canvasItem);
void Update(double delta);
void Render();
void Shutdown();
void Connect(godot::Callable& callable);

bool SubViewport(godot::SubViewport* svp);
#else
namespace detail {
static godot::Object* ImGuiGD = nullptr;
inline bool GET_IMGUIGD()
{
    if (ImGuiGD)
        return true;
    ImGuiGD = godot::Engine::get_singleton()->get_singleton("ImGuiGodot");
    return ImGuiGD != nullptr;
}
} // namespace detail

inline void Init(godot::Window* window, godot::RID canvasItem)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Init", canvasItem);
}

inline void Update(double delta)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Update", delta);
}

inline void Render()
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Render");
}

inline void Shutdown()
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Shutdown");
}

inline void Connect(godot::Callable& callable)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Connect", callable);
}

inline bool SubViewport(godot::SubViewport* svp)
{
    ERR_FAIL_COND_V(!detail::GET_IMGUIGD(), false);
    return detail::ImGuiGD->call("SubViewport", svp);
}
#endif
inline ImTextureID BindTexture(godot::Texture2D* tex)
{
    return reinterpret_cast<ImTextureID>(tex->get_rid().get_id());
}

inline void Image(godot::Texture2D* tex, const godot::Vector2& size, const godot::Vector2& uv0 = {0, 0},
                  const godot::Vector2& uv1 = {1, 1}, const godot::Color& tint_col = {1, 1, 1, 1},
                  const godot::Color& border_col = {0, 0, 0, 0})
{
    ImGui::Image(BindTexture(tex), size, uv0, uv1, tint_col, border_col);
}

inline bool ImageButton(const char* str_id, godot::Texture2D* tex, const godot::Vector2& size,
                        const godot::Vector2& uv0 = {0, 0}, const godot::Vector2& uv1 = {1, 1},
                        const godot::Color& bg_col = {0, 0, 0, 0}, const godot::Color& tint_col = {1, 1, 1, 1})
{
    ImGui::ImageButton(str_id, BindTexture(tex), size, uv0, uv1, bg_col, tint_col);
}

inline bool ImageButton(godot::CharString str_id, godot::Texture2D* tex, const godot::Vector2& size,
                        const godot::Vector2& uv0 = {0, 0}, const godot::Vector2& uv1 = {1, 1},
                        const godot::Color& bg_col = {0, 0, 0, 0}, const godot::Color& tint_col = {1, 1, 1, 1})
{
    ImGui::ImageButton(str_id.get_data(), BindTexture(tex), size, uv0, uv1, bg_col, tint_col);
}
} // namespace ImGui::Godot
