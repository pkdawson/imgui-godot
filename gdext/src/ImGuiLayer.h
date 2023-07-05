#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/input_event.hpp>
#include <godot_cpp/classes/node.hpp>
#pragma warning(pop)

#include <memory>

using godot::InputEvent;
using godot::Node;
using godot::PackedInt64Array;
using godot::Ref;
using godot::String;

namespace ImGui::Godot {

class ImGuiLayer : public Node
{
    GDCLASS(ImGuiLayer, Node);

protected:
    static void _bind_methods();

public:
    ImGuiLayer();
    ~ImGuiLayer();

    void _ready() override;
    void _enter_tree() override;
    void _exit_tree() override;
    void _process(double delta) override;
    void _input(const Ref<InputEvent>& event) override;
    void _notification(int p_what);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
