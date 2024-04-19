#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/input_event.hpp>
#pragma warning(pop)

#include <memory>

using namespace godot;

namespace ImGui::Godot {

class ImGuiLayer : public CanvasLayer
{
    GDCLASS(ImGuiLayer, CanvasLayer);

protected:
    static void _bind_methods();

public:
    ImGuiLayer();
    ~ImGuiLayer();

    void _ready() override;
    void _enter_tree() override;
    void _exit_tree() override;
    void _process(double delta) override;
    void _input(const Ref<InputEvent>& event) override;
    void _notification(int p_what);
    void on_visibility_changed();
    void on_frame_pre_draw();

    void ToolInit();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
