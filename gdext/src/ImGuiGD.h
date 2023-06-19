#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/object.hpp>
#pragma warning(pop)

#include <memory>

using godot::Object;

namespace ImGui::Godot {

class ImGuiGD : public Object
{
    GDCLASS(ImGuiGD, Object);

protected:
    static void _bind_methods();

public:
    static void Connect(const godot::Callable& cb);
};

} // namespace ImGui::Godot
