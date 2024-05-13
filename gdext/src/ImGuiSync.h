#pragma once
#include <godot_cpp/classes/object.hpp>

using namespace godot;

namespace ImGui::Godot {

class ImGuiSync : public Object
{
    GDCLASS(ImGuiSync, Object);

protected:
    static void _bind_methods();

public:
    static PackedInt64Array GetImGuiPtrs(String version, int ioSize, int vertSize, int idxSize, int charSize);
};

} // namespace ImGui::Godot
