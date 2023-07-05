#include "ImGuiGD.h"
#include "ImGuiLayer.h"
#include "ImGuiRoot.h"
#include "common.h"
#include "imgui-godot.h"
#include <godot_cpp/variant/utility_functions.hpp>

namespace ImGui::Godot {

void ImGuiGD::_bind_methods()
{
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("InitEditor", "root"), &ImGuiGD::InitEditor);
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("Connect", "callable"), &ImGuiGD::Connect);
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("ResetFonts"), &ImGuiGD::ResetFonts);
    ClassDB::bind_static_method("ImGuiGD",
                                D_METHOD("AddFont", "font_file", "font_size", "merge"),
                                &ImGuiGD::AddFont,
                                DEFVAL(false));
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("AddFontDefault"), &ImGuiGD::AddFontDefault);
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("RebuildFontAtlas"), &ImGuiGD::RebuildFontAtlas);
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("GetFontPtrs"), &ImGuiGD::GetFontPtrs);

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

    ClassDB::bind_static_method("ImGuiGD",
                                D_METHOD("GetImGuiPtrs", "version", "ioSize", "vertSize", "idxSize", "charSize"),
                                &ImGuiGD::GetImGuiPtrs);
}

void ImGuiGD::InitEditor(Node* root)
{
#ifdef DEBUG_ENABLED
    if (!Engine::get_singleton()->is_editor_hint())
        return;

    UtilityFunctions::print("ie ", root);
    ImGuiRoot* igr = memnew(ImGuiRoot);
    root->call_deferred("add_child", igr);
#endif
}

void ImGuiGD::Connect(const Callable& callable)
{
    ImGui::Godot::Connect(callable);
}

void ImGuiGD::ResetFonts()
{
}

void ImGuiGD::AddFont(FontFile* fontFile, int fontSize, bool merge)
{
}

void ImGuiGD::AddFontDefault()
{
}

void ImGuiGD::RebuildFontAtlas()
{
}

TypedArray<int64_t> ImGuiGD::GetFontPtrs()
{
    TypedArray<int64_t> rv;
    ImGuiIO& io = ImGui::GetIO();
    rv.resize(io.Fonts->Fonts.Size);
    for (int i = 0; i < io.Fonts->Fonts.Size; ++i)
    {
        rv[i] = (int64_t)io.Fonts->Fonts[i];
    }
    return rv;
}

TypedArray<int64_t> ImGuiGD::GetImGuiPtrs(String version, int ioSize, int vertSize, int idxSize, int charSize)
{
    UtilityFunctions::print("GetImGuiPtrs");
    bool ok = version == ImGui::GetVersion() && ioSize == sizeof(ImGuiIO) && vertSize == sizeof(ImDrawVert) &&
              idxSize == sizeof(ImDrawIdx) && charSize == sizeof(ImWchar);

    if (!ok)
    {
        UtilityFunctions::printerr("ImGui version mismatch");
        return {};
    }

    TypedArray<int64_t> rv;
    rv.resize(3);

    ImGuiMemAllocFunc p_alloc_func;
    ImGuiMemFreeFunc p_free_func;
    void* p_user_data;

    ImGui::GetAllocatorFunctions(&p_alloc_func, &p_free_func, &p_user_data);
    rv[0] = reinterpret_cast<int64_t>(ImGui::GetCurrentContext());
    rv[1] = reinterpret_cast<int64_t>(p_alloc_func);
    rv[2] = reinterpret_cast<int64_t>(p_free_func);
    return rv;
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
