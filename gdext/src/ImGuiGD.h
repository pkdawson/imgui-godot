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
    bool ToolInit();

    void Connect(const Callable& cb);

    void ResetFonts();
    void AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge = false);
    void AddFontDefault();
    void RebuildFontAtlas(float scale);

    void _SetJoyAxisDeadZone(float zone);
    float _GetJoyAxisDeadZone();

    void _SetVisible(bool visible);
    bool _GetVisible();

    void _SetScale(float scale);
    float _GetScale();

    PackedInt64Array GetFontPtrs();
    PackedInt64Array GetImGuiPtrs(String version, int ioSize, int vertSize, int idxSize, int charSize);

    bool SubViewport(godot::SubViewport* svp);
};

} // namespace ImGui::Godot
