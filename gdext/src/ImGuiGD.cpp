#include "ImGuiGD.h"
#include "Context.h"
#include "ImGuiController.h"
#include "ImGuiLayer.h"
#include "common.h"
#include <godot_cpp/classes/main_loop.hpp>
#include <godot_cpp/classes/packed_scene.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/classes/scene_tree.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace ImGui::Godot {

void ImGuiGD::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("_SetJoyAxisDeadZone", "deadZone"), &ImGuiGD::_SetJoyAxisDeadZone);
    ClassDB::bind_method(D_METHOD("_GetJoyAxisDeadZone"), &ImGuiGD::_GetJoyAxisDeadZone);
    ADD_PROPERTY(PropertyInfo(Variant::FLOAT, "JoyAxisDeadZone"), "_SetJoyAxisDeadZone", "_GetJoyAxisDeadZone");

    ClassDB::bind_method(D_METHOD("_SetVisible", "visible"), &ImGuiGD::_SetVisible);
    ClassDB::bind_method(D_METHOD("_GetVisible"), &ImGuiGD::_GetVisible);
    ADD_PROPERTY(PropertyInfo(Variant::BOOL, "Visible"), "_SetVisible", "_GetVisible");

    ClassDB::bind_method(D_METHOD("_SetScale", "scale"), &ImGuiGD::_SetScale);
    ClassDB::bind_method(D_METHOD("_GetScale"), &ImGuiGD::_GetScale);
    ADD_PROPERTY(PropertyInfo(Variant::FLOAT, "Scale"), "_SetScale", "_GetScale");

    ClassDB::bind_method(D_METHOD("AddFont", "font_file", "font_size", "merge", "glyph_ranges"),
                         &ImGuiGD::AddFont,
                         DEFVAL(false),
                         DEFVAL(PackedInt32Array()));
    ClassDB::bind_method(D_METHOD("AddFontDefault"), &ImGuiGD::AddFontDefault);
    ClassDB::bind_method(D_METHOD("Connect", "callable"), &ImGuiGD::Connect);
    ClassDB::bind_method(D_METHOD("RebuildFontAtlas"), &ImGuiGD::RebuildFontAtlas);
    ClassDB::bind_method(D_METHOD("ResetFonts"), &ImGuiGD::ResetFonts);
    ClassDB::bind_method(D_METHOD("SetMainViewport", "vp"), &ImGuiGD::SetMainViewport);

    ClassDB::bind_method(D_METHOD("SubViewport", "svp"), &ImGuiGD::SubViewport);
    ClassDB::bind_method(D_METHOD("GetImGuiPtrs", "version", "ioSize", "vertSize", "idxSize", "charSize"),
                         &ImGuiGD::GetImGuiPtrs);
    ClassDB::bind_method(D_METHOD("ToolInit"), &ImGuiGD::ToolInit);
    ClassDB::bind_method(D_METHOD("SetIniFilename", "filename"), &ImGuiGD::SetIniFilename);

    ClassDB::bind_method(D_METHOD("GetFontPtrs"), &ImGuiGD::GetFontPtrs);
}

bool ImGuiGD::ToolInit()
{
#ifdef DEBUG_ENABLED
    if (!Engine::get_singleton()->is_editor_hint())
        return false;

    Node* plugin = Object::cast_to<Node>(Engine::get_singleton()->get_singleton("ImGuiPlugin"));
    ERR_FAIL_COND_V(!plugin, false);
    if (!plugin->get_node_or_null("ImGuiController"))
    {
        plugin->add_child(memnew(ImGuiController));
    }
    return true;
#else
    return false;
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

void ImGuiGD::AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge, const PackedInt32Array& glyphRanges)
{
    if (glyphRanges.size() > 0)
    {
        ImVector<ImWchar> gr;
        gr.resize(glyphRanges.size() + 1, 0);
        for (int i = 0; i < glyphRanges.size(); ++i)
            gr[i] = glyphRanges[i];
        ImGui::Godot::AddFont(fontFile, fontSize, merge, gr);
    }
    else
    {
        ImGui::Godot::AddFont(fontFile, fontSize, merge);
    }
}

void ImGuiGD::AddFontDefault()
{
    ImGui::Godot::AddFontDefault();
}

void ImGuiGD::RebuildFontAtlas()
{
    ImGui::Godot::RebuildFontAtlas();
}

void ImGuiGD::SetMainViewport(Viewport* vp)
{
    ImGuiController* igc = Object::cast_to<ImGuiController>(Engine::get_singleton()->get_singleton("ImGuiController"));
    ERR_FAIL_COND(!igc);
    igc->SetMainViewport(vp);
}

void ImGuiGD::_SetVisible(bool visible)
{
    Context* ctx = GetContext();
    ERR_FAIL_COND(!ctx);
    ERR_FAIL_COND(!ctx->layer);
    ctx->layer->set_visible(visible);
}

bool ImGuiGD::_GetVisible()
{
    Context* ctx = GetContext();
    if (ctx && ctx->layer)
        return ctx->layer->is_visible();
    return false;
}

void ImGuiGD::_SetJoyAxisDeadZone(float zone)
{
    Context* ctx = ImGui::Godot::GetContext();
    ERR_FAIL_COND(!ctx);
    ctx->joyAxisDeadZone = zone;
}

float ImGuiGD::_GetJoyAxisDeadZone()
{
    Context* ctx = ImGui::Godot::GetContext();
    if (ctx)
        return ctx->joyAxisDeadZone;
    return 0.15f;
}

void ImGuiGD::_SetScale(float scale)
{
    Context* ctx = ImGui::Godot::GetContext();
    ERR_FAIL_COND(!ctx);
    ctx->scale = scale;
}

float ImGuiGD::_GetScale()
{
    Context* ctx = ImGui::Godot::GetContext();
    if (ctx)
        return ctx->scale;
    return 1.0f;
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
        UtilityFunctions::push_error("ImGui version mismatch, use v", ImGui::GetVersion(), "-docking");
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
    return ImGui::Godot::SubViewportWidget(svp);
}

void ImGuiGD::SetIniFilename(String fn)
{
    ImGui::Godot::SetIniFilename(fn);
}

} // namespace ImGui::Godot
