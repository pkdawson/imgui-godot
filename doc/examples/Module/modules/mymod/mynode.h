#pragma once
#include "scene/main/node.h"
#include "scene/resources/texture.h"

class MyNode : public Node
{
    GDCLASS(MyNode, Node);

protected:
    static void _bind_methods();

    void _notification(int what);

public:
    MyNode();
    ~MyNode();

private:
    Ref<Texture2D> _img;
    float _iconSize = 64.f;
};
