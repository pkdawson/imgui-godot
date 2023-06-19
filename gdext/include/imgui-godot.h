#pragma once

#ifdef _WIN32
#ifdef IGN_EXPORT
#define IGN_API __declspec(dllexport)
#else
#define IGN_API __declspec(dllimport)
#endif
#else
#define IGN_API
#endif

#include <imgui.h>

#if __has_include("godot_cpp/classes/engine.hpp")
#pragma warning(push, 0)
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/input_event.hpp>
#include <godot_cpp/classes/sub_viewport.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/variant/callable.hpp>
#pragma warning(pop)

using godot::Callable;
using godot::CharString;
using godot::Color;
using godot::Engine;
using godot::InputEvent;
using godot::JoyButton;
using godot::Key;
using godot::Object;
using godot::Ref;
using godot::RID;
using godot::SubViewport;
using godot::Texture2D;
using godot::Vector2;
using godot::Window;
#else
#include "core/config/engine.h"
#include "core/variant/callable.h"
#include "scene/main/viewport.h"
#include "scene/main/window.h"
#include "scene/resources/texture.h"
#endif

static_assert(sizeof(RID) == 8);
static_assert(sizeof(void*) == 8);

namespace ImGui::Godot {
#ifdef IGN_EXPORT
void Init(Window* mainWindow, RID canvasItem, Object* config = nullptr);
void Update(double delta);
bool ProcessInput(const Ref<InputEvent>& evt, Window* window);
void ProcessNotification(int what);
void Render();
void Shutdown();
void Connect(const Callable& callable);

bool SubViewport(SubViewport* svp);

namespace detail {
inline static float Scale = 1.0f;
} // namespace detail

#else
namespace detail {
inline static Object* ImGuiGD = nullptr;

inline bool GET_IMGUIGD()
{
    if (ImGuiGD)
        return true;
    ImGuiGD = Engine::get_singleton()->get_singleton("ImGuiGD");
    return ImGuiGD != nullptr;
}
} // namespace detail

inline void Init(Window* window, RID canvasItem)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Init", canvasItem);
}

inline void Update(double delta)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Update", delta);
}

inline void Render()
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Render");
}

inline void Shutdown()
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Shutdown");
}

inline void Connect(const Callable& callable)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("Connect", callable);
}

inline bool ProcessInput(const Ref<InputEvent>& evt, Window* window)
{
    ERR_FAIL_COND_V(!detail::GET_IMGUIGD(), false);
    return detail::ImGuiGD->call("ProcessInput", evt, window);
}

inline void ProcessNotification(int what)
{
    // quick filter
    if (what < 2000)
        return;

    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    detail::ImGuiGD->call("ProcessNotification", what);
}

inline bool SubViewport(SubViewport* svp)
{
    ERR_FAIL_COND_V(!detail::GET_IMGUIGD(), false);
    return detail::ImGuiGD->call("SubViewport", svp);
}

inline void SetJoyAxisDeadZone(float deadZone)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    // TODO: set in Input
}

inline void SetJoyButtonSwapAB(bool swap)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    // TODO: set in Input
}

inline void SetScale(float scale)
{
    ERR_FAIL_COND(!detail::GET_IMGUIGD());
    //if (scale != detail::Scale && scale >= 0.25f)
    //{
    //    // TODO: ...
    //}
}
#endif

inline ImTextureID BindTexture(Texture2D* tex)
{
    return reinterpret_cast<ImTextureID>(tex->get_rid().get_id());
}

inline void Image(Texture2D* tex, const Vector2& size, const Vector2& uv0 = {0, 0}, const Vector2& uv1 = {1, 1},
                  const Color& tint_col = {1, 1, 1, 1}, const Color& border_col = {0, 0, 0, 0})
{
    ImGui::Image(BindTexture(tex), size, uv0, uv1, tint_col, border_col);
}

inline bool ImageButton(const char* str_id, Texture2D* tex, const Vector2& size, const Vector2& uv0 = {0, 0},
                        const Vector2& uv1 = {1, 1}, const Color& bg_col = {0, 0, 0, 0},
                        const Color& tint_col = {1, 1, 1, 1})
{
    ImGui::ImageButton(str_id, BindTexture(tex), size, uv0, uv1, bg_col, tint_col);
}

inline bool ImageButton(CharString str_id, Texture2D* tex, const Vector2& size, const Vector2& uv0 = {0, 0},
                        const Vector2& uv1 = {1, 1}, const Color& bg_col = {0, 0, 0, 0},
                        const Color& tint_col = {1, 1, 1, 1})
{
    ImGui::ImageButton(str_id.get_data(), BindTexture(tex), size, uv0, uv1, bg_col, tint_col);
}

inline ImGuiKey ToImGuiKey(Key key)
{
    switch (key)
    {
    case Key::KEY_ESCAPE:
        return ImGuiKey_Escape;
    case Key::KEY_TAB:
        return ImGuiKey_Tab;
    case Key::KEY_BACKSPACE:
        return ImGuiKey_Backspace;
    case Key::KEY_ENTER:
        return ImGuiKey_Enter;
    case Key::KEY_KP_ENTER:
        return ImGuiKey_KeyPadEnter;
    case Key::KEY_INSERT:
        return ImGuiKey_Insert;
    case Key::KEY_DELETE:
        return ImGuiKey_Delete;
    case Key::KEY_PAUSE:
        return ImGuiKey_Pause;
    case Key::KEY_PRINT:
        return ImGuiKey_PrintScreen;
    case Key::KEY_HOME:
        return ImGuiKey_Home;
    case Key::KEY_END:
        return ImGuiKey_End;
    case Key::KEY_LEFT:
        return ImGuiKey_LeftArrow;
    case Key::KEY_UP:
        return ImGuiKey_UpArrow;
    case Key::KEY_RIGHT:
        return ImGuiKey_RightArrow;
    case Key::KEY_DOWN:
        return ImGuiKey_DownArrow;
    case Key::KEY_PAGEUP:
        return ImGuiKey_PageUp;
    case Key::KEY_PAGEDOWN:
        return ImGuiKey_PageDown;
    case Key::KEY_SHIFT:
        return ImGuiKey_LeftShift;
    case Key::KEY_CTRL:
        return ImGuiKey_LeftCtrl;
    case Key::KEY_META:
        return ImGuiKey_LeftSuper;
    case Key::KEY_ALT:
        return ImGuiKey_LeftAlt;
    case Key::KEY_CAPSLOCK:
        return ImGuiKey_CapsLock;
    case Key::KEY_NUMLOCK:
        return ImGuiKey_NumLock;
    case Key::KEY_SCROLLLOCK:
        return ImGuiKey_ScrollLock;
    case Key::KEY_F1:
        return ImGuiKey_F1;
    case Key::KEY_F2:
        return ImGuiKey_F2;
    case Key::KEY_F3:
        return ImGuiKey_F3;
    case Key::KEY_F4:
        return ImGuiKey_F4;
    case Key::KEY_F5:
        return ImGuiKey_F5;
    case Key::KEY_F6:
        return ImGuiKey_F6;
    case Key::KEY_F7:
        return ImGuiKey_F7;
    case Key::KEY_F8:
        return ImGuiKey_F8;
    case Key::KEY_F9:
        return ImGuiKey_F9;
    case Key::KEY_F10:
        return ImGuiKey_F10;
    case Key::KEY_F11:
        return ImGuiKey_F11;
    case Key::KEY_F12:
        return ImGuiKey_F12;
    case Key::KEY_KP_MULTIPLY:
        return ImGuiKey_KeypadMultiply;
    case Key::KEY_KP_DIVIDE:
        return ImGuiKey_KeypadDivide;
    case Key::KEY_KP_SUBTRACT:
        return ImGuiKey_KeypadSubtract;
    case Key::KEY_KP_PERIOD:
        return ImGuiKey_KeypadDecimal;
    case Key::KEY_KP_ADD:
        return ImGuiKey_KeypadAdd;
    case Key::KEY_KP_0:
        return ImGuiKey_Keypad0;
    case Key::KEY_KP_1:
        return ImGuiKey_Keypad1;
    case Key::KEY_KP_2:
        return ImGuiKey_Keypad2;
    case Key::KEY_KP_3:
        return ImGuiKey_Keypad3;
    case Key::KEY_KP_4:
        return ImGuiKey_Keypad4;
    case Key::KEY_KP_5:
        return ImGuiKey_Keypad5;
    case Key::KEY_KP_6:
        return ImGuiKey_Keypad6;
    case Key::KEY_KP_7:
        return ImGuiKey_Keypad7;
    case Key::KEY_KP_8:
        return ImGuiKey_Keypad8;
    case Key::KEY_KP_9:
        return ImGuiKey_Keypad9;
    case Key::KEY_MENU:
        return ImGuiKey_Menu;
    case Key::KEY_SPACE:
        return ImGuiKey_Space;
    case Key::KEY_APOSTROPHE:
        return ImGuiKey_Apostrophe;
    case Key::KEY_COMMA:
        return ImGuiKey_Comma;
    case Key::KEY_MINUS:
        return ImGuiKey_Minus;
    case Key::KEY_PERIOD:
        return ImGuiKey_Period;
    case Key::KEY_SLASH:
        return ImGuiKey_Slash;
    case Key::KEY_0:
        return ImGuiKey_0;
    case Key::KEY_1:
        return ImGuiKey_1;
    case Key::KEY_2:
        return ImGuiKey_2;
    case Key::KEY_3:
        return ImGuiKey_3;
    case Key::KEY_4:
        return ImGuiKey_4;
    case Key::KEY_5:
        return ImGuiKey_5;
    case Key::KEY_6:
        return ImGuiKey_6;
    case Key::KEY_7:
        return ImGuiKey_7;
    case Key::KEY_8:
        return ImGuiKey_8;
    case Key::KEY_9:
        return ImGuiKey_9;
    case Key::KEY_SEMICOLON:
        return ImGuiKey_Semicolon;
    case Key::KEY_EQUAL:
        return ImGuiKey_Equal;
    case Key::KEY_A:
        return ImGuiKey_A;
    case Key::KEY_B:
        return ImGuiKey_B;
    case Key::KEY_C:
        return ImGuiKey_C;
    case Key::KEY_D:
        return ImGuiKey_D;
    case Key::KEY_E:
        return ImGuiKey_E;
    case Key::KEY_F:
        return ImGuiKey_F;
    case Key::KEY_G:
        return ImGuiKey_G;
    case Key::KEY_H:
        return ImGuiKey_H;
    case Key::KEY_I:
        return ImGuiKey_I;
    case Key::KEY_J:
        return ImGuiKey_J;
    case Key::KEY_K:
        return ImGuiKey_K;
    case Key::KEY_L:
        return ImGuiKey_L;
    case Key::KEY_M:
        return ImGuiKey_M;
    case Key::KEY_N:
        return ImGuiKey_N;
    case Key::KEY_O:
        return ImGuiKey_O;
    case Key::KEY_P:
        return ImGuiKey_P;
    case Key::KEY_Q:
        return ImGuiKey_Q;
    case Key::KEY_R:
        return ImGuiKey_R;
    case Key::KEY_S:
        return ImGuiKey_S;
    case Key::KEY_T:
        return ImGuiKey_T;
    case Key::KEY_U:
        return ImGuiKey_U;
    case Key::KEY_V:
        return ImGuiKey_V;
    case Key::KEY_W:
        return ImGuiKey_W;
    case Key::KEY_X:
        return ImGuiKey_X;
    case Key::KEY_Y:
        return ImGuiKey_Y;
    case Key::KEY_Z:
        return ImGuiKey_Z;
    case Key::KEY_BRACKETLEFT:
        return ImGuiKey_LeftBracket;
    case Key::KEY_BACKSLASH:
        return ImGuiKey_Backslash;
    case Key::KEY_BRACKETRIGHT:
        return ImGuiKey_RightBracket;
    case Key::KEY_QUOTELEFT:
        return ImGuiKey_GraveAccent;
    default:
        return ImGuiKey_None;
    };
}

inline ImGuiKey ToImGuiKey(JoyButton btn)
{
    switch (btn)
    {
    case JoyButton::JOY_BUTTON_A:
        return ImGuiKey_GamepadFaceDown;
    case JoyButton::JOY_BUTTON_B:
        return ImGuiKey_GamepadFaceRight;
    case JoyButton::JOY_BUTTON_X:
        return ImGuiKey_GamepadFaceLeft;
    case JoyButton::JOY_BUTTON_Y:
        return ImGuiKey_GamepadFaceUp;
    case JoyButton::JOY_BUTTON_BACK:
        return ImGuiKey_GamepadBack;
    case JoyButton::JOY_BUTTON_START:
        return ImGuiKey_GamepadStart;
    case JoyButton::JOY_BUTTON_LEFT_STICK:
        return ImGuiKey_GamepadL3;
    case JoyButton::JOY_BUTTON_RIGHT_STICK:
        return ImGuiKey_GamepadR3;
    case JoyButton::JOY_BUTTON_LEFT_SHOULDER:
        return ImGuiKey_GamepadL1;
    case JoyButton::JOY_BUTTON_RIGHT_SHOULDER:
        return ImGuiKey_GamepadR1;
    case JoyButton::JOY_BUTTON_DPAD_UP:
        return ImGuiKey_GamepadDpadUp;
    case JoyButton::JOY_BUTTON_DPAD_DOWN:
        return ImGuiKey_GamepadDpadDown;
    case JoyButton::JOY_BUTTON_DPAD_LEFT:
        return ImGuiKey_GamepadDpadLeft;
    case JoyButton::JOY_BUTTON_DPAD_RIGHT:
        return ImGuiKey_GamepadDpadRight;
    default:
        return ImGuiKey_None;
    };
}

} // namespace ImGui::Godot