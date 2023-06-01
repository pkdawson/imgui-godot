#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/object.hpp>
#pragma warning(pop)

#include <memory>

using godot::Object;
using godot::String;
using godot::Array;

namespace ImGui::Godot {

class ImGui : public Object
{
    GDCLASS(ImGui, Object);

protected:
    static void _bind_methods();

public:
    ImGui();
    ~ImGui();

    static bool Begin(String name, Array p_open);
    static void End();

    static void Text(String text);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
