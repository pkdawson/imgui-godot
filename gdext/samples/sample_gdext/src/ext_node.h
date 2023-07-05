#pragma once

#include <godot_cpp/classes/node.hpp>

using namespace godot;

class ExtNode : public Node
{
    GDCLASS(ExtNode, Node);

protected:
    static void _bind_methods();

public:
    void _ready() override;
    void _process(double delta) override;
    void imgui_layout();
};
