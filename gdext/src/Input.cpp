#include "Input.h"
#include "imgui-godot.h"

#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/input.hpp>
#include <godot_cpp/classes/input_event_joypad_button.hpp>
#include <godot_cpp/classes/input_event_joypad_motion.hpp>
#include <godot_cpp/classes/input_event_key.hpp>
#include <godot_cpp/classes/input_event_mouse.hpp>
#include <godot_cpp/classes/input_event_mouse_button.hpp>
#include <godot_cpp/classes/input_event_mouse_motion.hpp>
#include <godot_cpp/classes/input_event_pan_gesture.hpp>
#include <godot_cpp/classes/main_loop.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <godot_cpp/variant/variant.hpp>
#include <imgui.h>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#endif

using namespace godot;

namespace ImGui::Godot {

struct Input::Impl
{
    Window* mainWindow = nullptr;
    godot::SubViewport* previousSubViewport = nullptr;
    godot::SubViewport* currentSubViewport = nullptr;
    Vector2 currentSubViewportPos;
    Vector2 mouseWheel;
    ImGuiMouseCursor currentCursor = ImGuiMouseCursor_None;
    bool hasMouse;
    float deadZone = 0.15f;

    void UpdateMouse();
};

namespace {
DisplayServer::CursorShape ToCursorShape(ImGuiMouseCursor cur)
{
    switch (cur)
    {
    case ImGuiMouseCursor_Arrow:
        return DisplayServer::CURSOR_ARROW;
    case ImGuiMouseCursor_TextInput:
        return DisplayServer::CURSOR_IBEAM;
    case ImGuiMouseCursor_ResizeAll:
        return DisplayServer::CURSOR_MOVE;
    case ImGuiMouseCursor_ResizeNS:
        return DisplayServer::CURSOR_VSIZE;
    case ImGuiMouseCursor_ResizeEW:
        return DisplayServer::CURSOR_HSIZE;
    case ImGuiMouseCursor_ResizeNESW:
        return DisplayServer::CURSOR_BDIAGSIZE;
    case ImGuiMouseCursor_ResizeNWSE:
        return DisplayServer::CURSOR_FDIAGSIZE;
    case ImGuiMouseCursor_Hand:
        return DisplayServer::CURSOR_POINTING_HAND;
    case ImGuiMouseCursor_NotAllowed:
        return DisplayServer::CURSOR_FORBIDDEN;
    default:
        return DisplayServer::CURSOR_ARROW;
    };
}

void UpdateKeyMods(ImGuiIO& io)
{
    static godot::Input* INP = godot::Input::get_singleton();

    io.AddKeyEvent(ImGuiMod_Ctrl, INP->is_key_pressed(Key::KEY_CTRL));
    io.AddKeyEvent(ImGuiMod_Shift, INP->is_key_pressed(Key::KEY_SHIFT));
    io.AddKeyEvent(ImGuiMod_Alt, INP->is_key_pressed(Key::KEY_ALT));
    io.AddKeyEvent(ImGuiMod_Super, INP->is_key_pressed(Key::KEY_META));
}

} // namespace

Input::Input(Window* mainWindow) : impl(std::make_unique<Impl>())
{
    impl->mainWindow = mainWindow;
    impl->hasMouse = DisplayServer::get_singleton()->has_feature(DisplayServer::FEATURE_MOUSE);
}

Input::~Input()
{
}

void Input::Impl::UpdateMouse()
{
    ImGuiIO& io = ImGui::GetIO();

    DisplayServer* DS = DisplayServer::get_singleton();
    Vector2i mousePos = DS->mouse_get_position();

    if (io.ConfigFlags & ImGuiConfigFlags_ViewportsEnable)
    {
        if (io.WantSetMousePos)
        {
            // WarpMouse is relative to the current focused window
            PackedInt32Array windows = DS->get_window_list();
            for (int w : windows)
            {
                if (DS->window_is_focused(w))
                {
                    Vector2i winPos = DS->window_get_position(w);
                    DS->warp_mouse({(int)io.MousePos.x - winPos.x, (int)io.MousePos.y - winPos.y});
                    break;
                }
            }
        }
        else
        {
            io.AddMousePosEvent(mousePos.x, mousePos.y);
        }
    }
    else
    {
        if (io.WantSetMousePos)
        {
            godot::Input::get_singleton()->warp_mouse({io.MousePos.x, io.MousePos.y});
        }
        else
        {
            Vector2i winPos = mainWindow->get_position();
            io.AddMousePosEvent(mousePos.x - winPos.x, mousePos.y - winPos.y);
        }
    }

    if (mouseWheel != Vector2())
    {
        io.AddMouseWheelEvent(mouseWheel.x, mouseWheel.y);
        mouseWheel = Vector2();
    }

    if (io.WantCaptureMouse && !(io.ConfigFlags & ImGuiConfigFlags_NoMouseCursorChange))
    {
        ImGuiMouseCursor newCursor = ImGui::GetMouseCursor();
        if (newCursor != currentCursor)
        {
            DS->cursor_set_shape(ToCursorShape(newCursor));
        }
    }
    else
    {
        currentCursor = ImGuiMouseCursor_None;
    }
}

void Input::Update()
{
    if (impl->hasMouse)
        impl->UpdateMouse();

    impl->previousSubViewport = impl->currentSubViewport;
    impl->currentSubViewport = nullptr;
}

bool Input::ProcessInput(const Ref<InputEvent>& evt, Window* window)
{
    ImGuiIO& io = ImGui::GetIO();

    Vector2i windowPos;
    if (io.ConfigFlags & ImGuiConfigFlags_ViewportsEnable)
        windowPos = window->get_position();

    if (impl->currentSubViewport)
    {
        if (impl->currentSubViewport != impl->previousSubViewport)
            impl->currentSubViewport->notification(Node::NOTIFICATION_VP_MOUSE_ENTER);

        Ref<InputEvent> vpevt = evt->duplicate();
        if (Ref<InputEventMouse> me = vpevt; me.is_valid())
        {
            Vector2 gpos = me->get_global_position();
            me->set_position(Vector2(windowPos.x + gpos.x - impl->currentSubViewportPos.x,
                                     windowPos.y + gpos.y - impl->currentSubViewportPos.y)
                                 .clamp(Vector2(0, 0), impl->currentSubViewport->get_size()));
        }
        impl->currentSubViewport->push_input(vpevt, true);
    }
    else if (impl->previousSubViewport)
    {
        impl->previousSubViewport->notification(Node::NOTIFICATION_VP_MOUSE_EXIT);
    }

    bool consumed = false;

    if (Ref<InputEventMouseMotion> mm = evt; mm.is_valid())
    {
        consumed = io.WantCaptureMouse;
    }
    else if (Ref<InputEventMouseButton> mb = evt; mb.is_valid())
    {
        bool pressed = mb->is_pressed();
        switch (mb->get_button_index())
        {
        case MOUSE_BUTTON_LEFT:
            io.AddMouseButtonEvent(ImGuiMouseButton_Left, pressed);
            break;
        case MOUSE_BUTTON_RIGHT:
            io.AddMouseButtonEvent(ImGuiMouseButton_Right, pressed);
            break;
        case MOUSE_BUTTON_MIDDLE:
            io.AddMouseButtonEvent(ImGuiMouseButton_Middle, pressed);
            break;
        case MOUSE_BUTTON_XBUTTON1:
            io.AddMouseButtonEvent(ImGuiMouseButton_Middle + 1, pressed);
            break;
        case MOUSE_BUTTON_XBUTTON2:
            io.AddMouseButtonEvent(ImGuiMouseButton_Middle + 2, pressed);
            break;
        case MOUSE_BUTTON_WHEEL_UP:
            impl->mouseWheel.y = mb->get_factor();
            break;
        case MOUSE_BUTTON_WHEEL_DOWN:
            impl->mouseWheel.y = -mb->get_factor();
            break;
        case MOUSE_BUTTON_WHEEL_LEFT:
            impl->mouseWheel.x = -mb->get_factor();
            break;
        case MOUSE_BUTTON_WHEEL_RIGHT:
            impl->mouseWheel.x = mb->get_factor();
            break;
        case MOUSE_BUTTON_NONE:
            break;
        }
        consumed = io.WantCaptureMouse;
    }
    else if (Ref<InputEventKey> k = evt; k.is_valid())
    {
        UpdateKeyMods(io);
        ImGuiKey igk = ToImGuiKey(k->get_keycode());
        if (igk != ImGuiKey_None)
        {
            bool pressed = k->is_pressed();
            io.AddKeyEvent(igk, k->is_pressed());
            if (pressed && k->get_unicode() != 0 && io.WantTextInput)
            {
                io.AddInputCharacter(k->get_unicode());
            }
        }
        consumed = io.WantCaptureKeyboard || io.WantTextInput;
    }
    else if (Ref<InputEventPanGesture> pg = evt; pg.is_valid())
    {
        impl->mouseWheel = Vector2(-pg->get_delta().x, -pg->get_delta().y);
        consumed = io.WantCaptureMouse;
    }
    else if (io.ConfigFlags & ImGuiConfigFlags_NavEnableGamepad)
    {
        if (Ref<InputEventJoypadButton> jb = evt; jb.is_valid())
        {
            ImGuiKey igk = ToImGuiKey(jb->get_button_index());
            if (igk != ImGuiKey_None)
            {
                io.AddKeyEvent(igk, jb->is_pressed());
                consumed = true;
            }
        }
        else if (Ref<InputEventJoypadMotion> jm = evt; jm.is_valid())
        {
            bool pressed = true;
            float v = jm->get_axis_value();
            if (std::abs(v) < impl->deadZone)
            {
                v = 0;
                pressed = false;
            }

            switch (jm->get_axis())
            {
            case JOY_AXIS_LEFT_X:
                io.AddKeyAnalogEvent(ImGuiKey_GamepadLStickRight, pressed, v);
                break;
            case JOY_AXIS_LEFT_Y:
                io.AddKeyAnalogEvent(ImGuiKey_GamepadLStickDown, pressed, v);
                break;
            case JOY_AXIS_RIGHT_X:
                io.AddKeyAnalogEvent(ImGuiKey_GamepadRStickRight, pressed, v);
                break;
            case JOY_AXIS_RIGHT_Y:
                io.AddKeyAnalogEvent(ImGuiKey_GamepadRStickDown, pressed, v);
                break;
            case JOY_AXIS_TRIGGER_LEFT:
                io.AddKeyAnalogEvent(ImGuiKey_GamepadL2, pressed, v);
                break;
            case JOY_AXIS_TRIGGER_RIGHT:
                io.AddKeyAnalogEvent(ImGuiKey_GamepadR2, pressed, v);
                break;
            case JOY_AXIS_INVALID:
            case JOY_AXIS_SDL_MAX:
            case JOY_AXIS_MAX:
                break;
            };

            consumed = true;
        }
    }

    return consumed;
}

void Input::ProcessNotification(int what)
{
    switch (what)
    {
    case Node::NOTIFICATION_APPLICATION_FOCUS_IN:
        ImGui::GetIO().AddFocusEvent(true);
        break;
    case Node::NOTIFICATION_APPLICATION_FOCUS_OUT:
        ImGui::GetIO().AddFocusEvent(false);
        break;
    };
}

void Input::SetActiveSubViewport(godot::SubViewport* svp, Vector2 pos)
{
    impl->currentSubViewport = svp;
    impl->currentSubViewportPos = pos;
}

void Input::SetJoyAxisDeadZone(float val)
{
    impl->deadZone = val;
}

float Input::GetJoyAxisDeadZone()
{
    return impl->deadZone;
}

} // namespace ImGui::Godot
