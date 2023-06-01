#pragma once

#pragma warning(push, 0)
#include <godot_cpp/variant/vector2.hpp>
#include <godot_cpp/variant/vector2i.hpp>
#include <godot_cpp/variant/vector4.hpp>
#pragma warning(pop)

#define IM_VEC2_CLASS_EXTRA                                                                             \
    constexpr ImVec2(const godot::Vector2& f) : x(f.x), y(f.y)                                          \
    {                                                                                                   \
    }                                                                                                   \
    operator godot::Vector2() const                                                                     \
    {                                                                                                   \
        return godot::Vector2(x, y);                                                                    \
    }                                                                                                   \
    constexpr ImVec2(const godot::Vector2i& f) : x(static_cast<float>(f.x)), y(static_cast<float>(f.y)) \
    {                                                                                                   \
    }                                                                                                   \
    operator godot::Vector2i() const                                                                    \
    {                                                                                                   \
        return godot::Vector2i(static_cast<int32_t>(x), static_cast<int32_t>(y));                       \
    }

#define IM_VEC4_CLASS_EXTRA                                                    \
    constexpr ImVec4(const godot::Vector4& f) : x(f.x), y(f.y), z(f.z), w(f.w) \
    {                                                                          \
    }                                                                          \
    operator godot::Vector4() const                                            \
    {                                                                          \
        return godot::Vector4(x, y, z, w);                                     \
    }
