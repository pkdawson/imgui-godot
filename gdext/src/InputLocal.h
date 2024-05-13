#pragma once
#include "Input.h"

using namespace godot;

namespace ImGui::Godot {

class InputLocal : public Input
{
protected:
    void UpdateMousePos() override;
    bool ProcessInput(const Ref<InputEvent>& evt) override;
};

} // namespace ImGui::Godot
