#pragma once
#include <godot_cpp/classes/node.hpp>
#include <memory>

using namespace godot;

namespace ImGui::Godot {

class ImGuiController : public Node
{
    GDCLASS(ImGuiController, Node);

protected:
    static void _bind_methods();

public:
    static ImGuiController* Instance();

    ImGuiController();
    ~ImGuiController();

    void _ready() override;
    void _enter_tree() override;
    void _exit_tree() override;
    void _process(double delta) override;
    void _notification(int p_what);
    void SetMainViewport(Viewport* vp);

    void OnLayerExiting();
    void on_frame_pre_draw();
    void window_input_callback(Ref<InputEvent> evt);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
