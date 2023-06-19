#pragma once

#include <memory>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/classes/sub_viewport.hpp>
#include <godot_cpp/classes/input_event.hpp>

namespace ImGui::Godot {
class Input
{
public:
    Input(godot::Window* mainWindow);
    ~Input();

    void Update();
    bool ProcessInput(const godot::Ref<godot::InputEvent>& evt, godot::Window* window);
    void ProcessNotification(int what);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};
} // namespace ImGui::Godot
