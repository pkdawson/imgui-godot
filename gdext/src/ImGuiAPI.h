#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/variant/variant.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#pragma warning(pop)

#include <cimgui.h>
#include <memory>

#include "imgui_enums.gen.h"
#include "imgui_funcs.gen.h"

using namespace godot;

namespace ImGui::Godot {

template <class T>
struct GdsPtr
{
    // TypedArray<T>& arr;
    Array& arr;
    T val;

    GdsPtr(Array& x) : arr(x), val(arr[0])
    {
    }

    ~GdsPtr()
    {
        arr[0] = val;
    }

    operator T*()
    {
        return &val;
    }
};

#define GDS_PTR(T, a) a.size() == 0 ? nullptr : GdsPtr<T>(a)

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

    static bool Begin(String name, Array p_open, BitField<WindowFlags> flags);

    DECLARE_IMGUI_FUNCS()

    static Ref<ImGuiIOPtr> GetIO();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot

