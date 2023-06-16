#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/ref_counted.hpp>
#pragma warning(pop)

#include <imgui.h>
#include <memory>

using godot::Array;
using godot::Object;
using godot::String;
using godot::RefCounted;

namespace ImGui::Godot {

class ImGui : public Object
{
    GDCLASS(ImGui, Object);

protected:
    static void _bind_methods();

public:
    ImGui();
    ~ImGui();

    static bool Begin(String name, Array p_open);
    static void End();

    static void Text(String text);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

class ImGuiIOPtr : public RefCounted
{
    GDCLASS(ImGuiIOPtr, RefCounted);

protected:
    static void _bind_methods();

public:
    void _set_io(ImGuiIO* p_io);

    void _set_ConfigFlags(int32_t flags);
    int32_t _get_ConfigFlags();

private:
    ImGuiIO* io = nullptr;
};

} // namespace ImGui::Godot
