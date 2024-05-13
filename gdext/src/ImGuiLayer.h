#pragma once
#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/input_event.hpp>
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
    void _input(const Ref<InputEvent>& event) override;

    void on_visibility_changed();
    void UpdateViewport();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
