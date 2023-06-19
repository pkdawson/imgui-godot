#include "Input.h"
#include "ImGuiGD.h"

#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/input.hpp>
#include <godot_cpp/classes/input_event_key.hpp>
#include <godot_cpp/classes/input_event_mouse.hpp>
#include <godot_cpp/classes/input_event_mouse_button.hpp>
#include <godot_cpp/classes/input_event_mouse_motion.hpp>
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
    godot::SubViewport* currentSubViewport = nullptr;
    Vector2 currentSubViewportPos;
    Vector2 mouseWheel;
    ImGuiMouseCursor currentCursor = ImGuiMouseCursor_None;
};

namespace {
DisplayServer::CursorShape ConvertCursorShape(ImGuiMouseCursor cur)
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
    io.AddKeyEvent(ImGuiKey_ModCtrl, INP->is_key_pressed(Key::KEY_CTRL));
    io.AddKeyEvent(ImGuiKey_ModShift, INP->is_key_pressed(Key::KEY_SHIFT));
    io.AddKeyEvent(ImGuiKey_ModAlt, INP->is_key_pressed(Key::KEY_ALT));
    io.AddKeyEvent(ImGuiKey_ModSuper, INP->is_key_pressed(Key::KEY_META));
}

} // namespace

Input::Input(Window* mainWindow) : impl(std::make_unique<Impl>())
{
    impl->mainWindow = mainWindow;
}

Input::~Input()
{
}

void Input::Update()
{
    ImGuiIO& io = ImGui::GetIO();

    DisplayServer* DS = DisplayServer::get_singleton();
    Vector2i mousePos = DS->mouse_get_position();

    if (io.ConfigFlags & ImGuiConfigFlags_ViewportsEnable)
    {
        if (io.WantSetMousePos)
        {
            // TODO:
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
            Vector2i winPos = impl->mainWindow->get_position();
            io.AddMousePosEvent(mousePos.x - winPos.x, mousePos.y - winPos.y);
        }
    }

    if (impl->mouseWheel != Vector2())
    {
        io.AddMouseWheelEvent(impl->mouseWheel.x, impl->mouseWheel.y);
        impl->mouseWheel = Vector2();
    }

    if (io.WantCaptureMouse && !(io.ConfigFlags & ImGuiConfigFlags_NoMouseCursorChange))
    {
        ImGuiMouseCursor newCursor = ImGui::GetMouseCursor();
        if (newCursor != impl->currentCursor)
        {
            DS->cursor_set_shape(ConvertCursorShape(newCursor));
        }
    }
    else
    {
        impl->currentCursor = ImGuiMouseCursor_None;
    }

    impl->currentSubViewport = nullptr;
}

bool Input::ProcessInput(const Ref<InputEvent>& evt, Window* window)
{
    ImGuiIO& io = ImGui::GetIO();

    Vector2i windowPos;
    if (io.ConfigFlags & ImGuiConfigFlags_ViewportsEnable)
        windowPos = window->get_position();

    if (impl->currentSubViewport != nullptr)
    {
        Ref<InputEvent> vpevt = evt->duplicate();

        if (Ref<InputEventMouse> me = vpevt; me.is_valid())
        {
        }
    }

    bool consumed = false;

    if (Ref<InputEventMouseMotion> mm = evt; mm.is_valid())
    {
        consumed = true;
    }
    else if (Ref<InputEventMouseButton> mb = evt; mb.is_valid())
    {
        bool pressed = mb->is_pressed();
        switch (mb->get_button_index())
        {
        case MOUSE_BUTTON_LEFT:
            io.AddMouseButtonEvent(ImGuiMouseButton_Left, pressed);
#ifdef _WIN32
            if (io.ConfigFlags & ImGuiConfigFlags_ViewportsEnable && !pressed)
            {
                HWND hwnd = GetCapture();
                if (hwnd != nullptr)
                {
                    PostMessageW(hwnd, WM_LBUTTONUP, 0, 0);
                }
            }
#endif
            break;

        case MOUSE_BUTTON_WHEEL_UP:
            impl->mouseWheel.y = mb->get_factor();
            break;
        case MOUSE_BUTTON_WHEEL_DOWN:
            impl->mouseWheel.y = -mb->get_factor();
            break;
        }
        consumed = true;
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

} // namespace ImGui::Godot
