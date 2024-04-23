#pragma once
#include <godot_cpp/classes/node.hpp>

using namespace godot;

class Example : public Node
{
    GDCLASS(Example, Node);

protected:
    static void _bind_methods();

public:
    Example();
    ~Example();

    void _ready() override;
    void _process(double delta) override;
};
