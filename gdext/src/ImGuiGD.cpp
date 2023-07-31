#include "ImGuiGD.h"
#include "ImGuiLayer.h"
#include "ImGuiRoot.h"
#include "common.h"
#include "imgui-godot.h"
#include <godot_cpp/classes/main_loop.hpp>
#include <godot_cpp/classes/packed_scene.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/classes/scene_tree.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace ImGui::Godot {

void ImGuiGD::_bind_methods()
{
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("InitEditor", "parent"), &ImGuiGD::InitEditor);
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("ToolInit"), &ImGuiGD::ToolInit);
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("Connect", "callable"), &ImGuiGD::Connect);
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("ResetFonts"), &ImGuiGD::ResetFonts);
    ClassDB::bind_static_method("ImGuiGD",
                                D_METHOD("AddFont", "font_file", "font_size", "merge"),
                                &ImGuiGD::AddFont,
                                DEFVAL(false));
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("AddFontDefault"), &ImGuiGD::AddFontDefault);
    ClassDB::bind_static_method("ImGuiGD",
                                D_METHOD("RebuildFontAtlas", "scale"),
                                &ImGuiGD::RebuildFontAtlas,
                                DEFVAL(1.0f));

    ClassDB::bind_static_method("ImGuiGD", D_METHOD("SetVisible", "visible"), &ImGuiGD::SetVisible);

    ClassDB::bind_static_method("ImGuiGD", D_METHOD("SubViewport", "svp"), &ImGuiGD::SubViewport);

    ClassDB::bind_static_method("ImGuiGD", D_METHOD("GetFontPtrs"), &ImGuiGD::GetFontPtrs);
    ClassDB::bind_static_method("ImGuiGD",
                                D_METHOD("GetImGuiPtrs", "version", "ioSize", "vertSize", "idxSize", "charSize"),
                                &ImGuiGD::GetImGuiPtrs);
}

void ImGuiGD::InitEditor(Node* parent)
{
#ifdef DEBUG_ENABLED
    if (!Engine::get_singleton()->is_editor_hint())
        return;

    if (!Engine::get_singleton()->has_singleton("ImGuiRoot"))
    {
        String resPath = "res://addons/imgui-godot-native/ImGuiGodot.tscn";
        if (ResourceLoader::get_singleton()->exists(resPath))
        {
            Ref<PackedScene> scene = ResourceLoader::get_singleton()->load(resPath);
            if (scene.is_valid())
                parent->add_child(scene->instantiate());
        }
    }
#endif
}

void ImGuiGD::ToolInit()
{
#ifdef DEBUG_ENABLED
    if (!Engine::get_singleton()->is_editor_hint())
        return;
    ImGuiLayer* igl = Object::cast_to<ImGuiLayer>(Engine::get_singleton()->get_singleton("ImGuiLayer"));
    if (igl)
        igl->ToolInit();
#endif
}

void ImGuiGD::Connect(const Callable& callable)
{
    ImGui::Godot::Connect(callable);
}

void ImGuiGD::ResetFonts()
{
    ImGui::Godot::ResetFonts();
}

void ImGuiGD::AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge)
{
    ImGui::Godot::AddFont(fontFile, fontSize, merge);
}

void ImGuiGD::AddFontDefault()
{
    ImGui::Godot::AddFontDefault();
}

void ImGuiGD::RebuildFontAtlas(float scale)
{
    ImGui::Godot::RebuildFontAtlas(scale);
}

void ImGuiGD::SetVisible(bool visible)
{
    ImGui::Godot::SetVisible(visible);
}

PackedInt64Array ImGuiGD::GetFontPtrs()
{
    ImGuiIO& io = ImGui::GetIO();
    PackedInt64Array rv;
    rv.resize(io.Fonts->Fonts.Size);
    for (int i = 0; i < io.Fonts->Fonts.Size; ++i)
    {
        rv[i] = (int64_t)io.Fonts->Fonts[i];
    }
    return rv;
}

PackedInt64Array ImGuiGD::GetImGuiPtrs(String version, int ioSize, int vertSize, int idxSize, int charSize)
{
    if (version != String(ImGui::GetVersion()) || ioSize != sizeof(ImGuiIO) || vertSize != sizeof(ImDrawVert) ||
        idxSize != sizeof(ImDrawIdx) || charSize != sizeof(ImWchar))
    {
        UtilityFunctions::push_error("ImGui version mismatch, use ", ImGui::GetVersion(), "-docking");
        return {};
    }

    ImGuiMemAllocFunc alloc_func = nullptr;
    ImGuiMemFreeFunc free_func = nullptr;
    void* user_data = nullptr;

    ImGui::GetAllocatorFunctions(&alloc_func, &free_func, &user_data);

    PackedInt64Array rv;
    rv.resize(3);
    rv[0] = reinterpret_cast<int64_t>(ImGui::GetCurrentContext());
    rv[1] = reinterpret_cast<int64_t>(alloc_func);
    rv[2] = reinterpret_cast<int64_t>(free_func);
    return rv;
}

bool ImGuiGD::SubViewport(godot::SubViewport* svp)
{
    return ImGui::Godot::SubViewport(svp);
}

} // namespace ImGui::Godot
