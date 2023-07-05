#pragma once
#include "scene/main/node.h"

class SampleNode : public Node
{
    GDCLASS(SampleNode, Node);

protected:
    static void _bind_methods();
    void _notification(int p_what);
};
