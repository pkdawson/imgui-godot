#pragma once

#include "scene/main/node.h"

class MyNode : public Node
{
    GDCLASS(MyNode, Node);

protected:
    static void _bind_methods();

    void _notification(int what);

public:
    MyNode();
    ~MyNode();
};
