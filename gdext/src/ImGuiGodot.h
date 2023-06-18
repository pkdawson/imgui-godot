#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/node.hpp>
#pragma warning(pop)

#include <memory>

using godot::InputEvent;
using godot::Node;
using godot::Ref;
using godot::PackedInt64Array;
using godot::String;

namespace ImGui::Godot {

class ImGuiGodot : public Node
{
    GDCLASS(ImGuiGodot, Node);

protected:
    static void _bind_methods();

public:
    void _ready() override;
    void _enter_tree() override;
    void _exit_tree() override;
    void _process(double delta) override;

    ImGuiGodot();
    ~ImGuiGodot();

    PackedInt64Array GetImGuiPtrs(String version, int ioSize, int vertSize, int idxSize);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
