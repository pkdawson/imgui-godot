#pragma once
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/atlas_texture.hpp>

using godot::Node;
using godot::Ref;
using godot::AtlasTexture;

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

private:
    Ref<AtlasTexture> _img;
};
