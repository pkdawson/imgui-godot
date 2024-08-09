#pragma once
#include "GdsCache.h"
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <godot_cpp/variant/variant.hpp>
#include <memory>
#include <string_view>

#include <cimgui.h>

using namespace godot;

namespace ImGui::Godot {

template <class T>
struct GdsPtr
{
    // TypedArray<T>& arr;
    Array& arr;
    T val;

    GdsPtr(Array& x) : arr(x)
    {
        // safety for tool script reload, and programming errors
        if (arr.size() == 0) [[unlikely]]
            arr.append(T());
        val = arr[0];
    }

    ~GdsPtr() { arr[0] = val; }

    operator T*() { return &val; }
};

template <>
struct GdsPtr<String>
{
    Array& arr;
    std::vector<char>& buf;
    int64_t bufhash;

    GdsPtr(Array& x, size_t s, const StringName& label) : arr(x), buf(gdscache->GetTextBuf(label, s, x))
    {
        bufhash = std::hash<std::string_view>{}({buf.begin(), buf.end()});
    }

    ~GdsPtr()
    {
        if (bufhash != std::hash<std::string_view>{}({buf.begin(), buf.end()}))
        {
            arr[0] = String(buf.data());
        }
    }

    operator char*() { return buf.data(); }
};

template <class T>
struct GdsArray
{
    Array& arr;
    std::vector<T> buf;

    GdsArray(Array& a) : arr(a), buf(a.size())
    {
        for (int i = 0; i < arr.size(); ++i)
        {
            buf[i] = arr[i];
        }
    }

    ~GdsArray()
    {
        for (int i = 0; i < arr.size(); ++i)
        {
            arr[i] = buf[i];
        }
    }

    operator T*() { return buf.data(); }
};

// TODO: take PackedFloat32Array parameters instead
template <>
struct GdsArray<const float>
{
    std::vector<float> buf;

    GdsArray(Array& arr) : buf(arr.size())
    {
        for (int i = 0; i < arr.size(); ++i)
        {
            buf[i] = arr[i];
        }
    }

    operator const float*() { return buf.data(); }
};

template <>
struct GdsArray<const char* const>
{
    std::vector<CharString> buf;
    std::vector<const char*> ptrs;

    GdsArray(Array& arr) : buf(arr.size()), ptrs(arr.size())
    {
        for (int i = 0; i < arr.size(); ++i)
        {
            buf[i] = String(arr[i]).utf8();
            ptrs[i] = buf[i].get_data();
        }
    }

    operator const char* const*() { return ptrs.data(); }
};

struct GdsZeroArray
{
    const std::vector<char>& buf;

    GdsZeroArray(const Array& a) : buf(gdscache->GetZeroArray(a)) {}

    operator const char*() { return buf.data(); }
};

#define VARIANT_CSTR(v) v.get_type() == Variant::STRING ? static_cast<String>(v).utf8().get_data() : nullptr

#define GDS_PTR(T, a) a.size() == 0 ? nullptr : (T*)GdsPtr<T>(a)
// #define GDS_PTR_STR(a, len, label) a.size() == 0 ? nullptr : (char*)GdsPtr<String>(a, len, label)

} // namespace ImGui::Godot

#include "imgui_bindings.gen.h"
