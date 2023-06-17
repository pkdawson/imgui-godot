#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/ref_counted.hpp>
#pragma warning(pop)

#include <imgui.h>
#include <memory>

using godot::Array;
using godot::Object;
using godot::Ref;
using godot::RefCounted;
using godot::String;

namespace ImGui::Godot {

enum ConfigFlags
{
    ConfigFlags_ViewportsEnable = ImGuiConfigFlags_ViewportsEnable,
};

class ImGuiIOPtr : public RefCounted
{
    GDCLASS(ImGuiIOPtr, RefCounted);

protected:
    static void _bind_methods();

public:
    ImGuiIOPtr();

    void _set_ConfigFlags(int32_t flags);
    int32_t _get_ConfigFlags();

private:
    ImGuiIO* io = nullptr;
};

class ImGui : public Object
{
    GDCLASS(ImGui, Object);

protected:
    static void _bind_methods();

public:
    ImGui();
    ~ImGui();

    static void SetNextWindowPos(godot::Vector2i pos);
    static bool Begin(String name, Array p_open);
    static void End();

    static void Text(String text);

    static Ref<ImGuiIOPtr> GetIO();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot

VARIANT_BITFIELD_CAST(ImGui::Godot::ConfigFlags);