#pragma once
#include <godot_cpp/classes/node.hpp>
#include <memory>

using godot::InputEvent;
using godot::Node;
using godot::Ref;

namespace ImGui::Godot {

class ImGuiLayerHelper : public Node
{
    GDCLASS(ImGuiLayerHelper, Node);

protected:
    static void _bind_methods();

public:
    void _ready() override;
    void _enter_tree() override;
    void _exit_tree() override;
    void _process(double delta) override;

    ImGuiLayerHelper();
    ~ImGuiLayerHelper();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
