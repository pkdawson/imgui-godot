#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/node.hpp>
#pragma warning(pop)

#include <memory>

using godot::InputEvent;
using godot::Node;
using godot::Ref;

namespace ImGui::Godot {

class ImGuiGodot : public Node
{
    GDCLASS(ImGuiGodot, Node);

protected:
    static void _bind_methods();

public:
    void _ready() override;
    void _enter_tree() override;
    void _exit_tree() override;
    void _process(double delta) override;

    ImGuiGodot();
    ~ImGuiGodot();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
