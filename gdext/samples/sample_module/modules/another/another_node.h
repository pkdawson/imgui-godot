#pragma once
#include "scene/main/node.h"

class AnotherNode : public Node
{
    GDCLASS(AnotherNode, Node);

protected:
    static void _bind_methods();
    void _notification(int p_what);
};
