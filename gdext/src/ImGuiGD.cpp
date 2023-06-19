#include "ImGuiGD.h"
#include "common.h"
#include "imgui-godot.h"

namespace ImGui::Godot {

void ImGuiGD::_bind_methods()
{
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("Connect", "callable"), &ImGuiGD::Connect);
    ClassDB::bind_static_method("ImGuiGD",
                                D_METHOD("Image", "tex", "size", "uv0", "uv1", "tint_col", "border_col"),
                                &ImGuiGD::Image,
                                DEFVAL(Vector2(0, 0)),
                                DEFVAL(Vector2(1, 1)),
                                DEFVAL(Color(1, 1, 1, 1)),
                                DEFVAL(Color(0, 0, 0, 0)));

    ClassDB::bind_static_method(
        "ImGuiGD",
        D_METHOD("ImageButton", "str_id", "tex", "size", "uv0", "uv1", "tint_col", "border_col"),
        &ImGuiGD::ImageButton,
        DEFVAL(Vector2(0, 0)),
        DEFVAL(Vector2(1, 1)),
        DEFVAL(Color(0, 0, 0, 0)),
        DEFVAL(Color(1, 1, 1, 1)));
}

void ImGuiGD::Connect(const Callable& callable)
{
    ImGui::Godot::Connect(callable);
}

void ImGuiGD::Image(Texture2D* tex, const Vector2& size, const Vector2& uv0, const Vector2& uv1, const Color& tint_col,
                    const Color& border_col)
{
    ImGui::Godot::Image(tex, size, uv0, uv1, tint_col, border_col);
}

bool ImGuiGD::ImageButton(const String& str_id, Texture2D* tex, const Vector2& size, const Vector2& uv0,
                          const Vector2& uv1, const Color& bg_col, const Color& tint_col)

{
    return ImGui::Godot::ImageButton(str_id, tex, size, uv0, uv1, bg_col, tint_col);
}

} // namespace ImGui::Godot
