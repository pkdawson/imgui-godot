#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/font_file.hpp>
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/sub_viewport.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/variant/variant.hpp>
#pragma warning(pop)

#include <memory>

using namespace godot;

namespace ImGui::Godot {

class ImGuiGD : public Object
{
    GDCLASS(ImGuiGD, Object);

protected:
    static void _bind_methods();

public:
    static void InitEditor(Node* parent);
    static void ToolInit();

    static void Connect(const Callable& cb);

    static void ResetFonts();
    static void AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge = false);
    static void AddFontDefault();
    static void RebuildFontAtlas(float scale);

    static void SetVisible(bool visible);

    static PackedInt64Array GetFontPtrs();
    static PackedInt64Array GetImGuiPtrs(String version, int ioSize, int vertSize, int idxSize, int charSize);

    static bool SubViewport(godot::SubViewport* svp);
};

} // namespace ImGui::Godot
