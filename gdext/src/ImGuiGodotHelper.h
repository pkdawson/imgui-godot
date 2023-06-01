#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/node.hpp>
#pragma warning(pop)

#include <memory>

using godot::InputEvent;
using godot::Node;
using godot::Ref;

namespace ImGui::Godot {

class ImGuiGodotHelper : public Node
{
    GDCLASS(ImGuiGodotHelper, Node);

protected:
    static void _bind_methods();

public:
    void _ready() override;
    void _enter_tree() override;
    void _exit_tree() override;
    void _process(double delta) override;

    ImGuiGodotHelper();
    ~ImGuiGodotHelper();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
