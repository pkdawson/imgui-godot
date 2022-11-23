#pragma once
#include <godot_cpp/classes/node.hpp>
#include <memory>

using godot::InputEvent;
using godot::Node;
using godot::Ref;

class MyCppNode : public Node
{
    GDCLASS(MyCppNode, Node);

protected:
    static void _bind_methods();

public:
    void _ready() override;
    void _exit_tree() override;
    void _process(double delta) override;

    MyCppNode();
    ~MyCppNode();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};
