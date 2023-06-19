#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/variant/variant.hpp>
#pragma warning(pop)

#include <imgui.h>
#include <memory>

using namespace godot;

namespace ImGui::Godot {

template <class T>
struct GdsPtr
{
    // TypedArray<T>& arr;
    Array& arr;
    T val;

    GdsPtr(Array& x) : arr(x), val()
    {
        if (arr.size() > 0)
            val = arr[0];
    }

    ~GdsPtr()
    {
        if (arr.size() > 0)
            arr[0] = val;
    }

    operator T*()
    {
        if (arr.size() > 0)
            return &val;
        return nullptr;
    }
};

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
    enum WindowFlags
    {
        WindowFlags_None = ImGuiWindowFlags_None,
        WindowFlags_NoTitleBar = ImGuiWindowFlags_NoTitleBar,
    };

    ImGui();
    ~ImGui();

    static void SetNextWindowPos(godot::Vector2i pos);
    static bool Begin(String name, Array p_open, BitField<WindowFlags> flags);
    static void End();

    static void Text(String text);

    static Ref<ImGuiIOPtr> GetIO();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot

VARIANT_BITFIELD_CAST(ImGui::Godot::ImGui::WindowFlags);
VARIANT_BITFIELD_CAST(ImGui::Godot::ConfigFlags);
