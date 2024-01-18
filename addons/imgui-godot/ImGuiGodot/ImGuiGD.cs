#if GODOT_PC
using Godot;
using System;

namespace ImGuiGodot;

public static class ImGuiGD
{
    /// <summary>
    /// Deadzone for all axes
    /// </summary>
    public static float JoyAxisDeadZone { get; set; } = 0.15f;

    /// <summary>
    /// Setting this property will reload fonts and modify the ImGuiStyle
    /// </summary>
    public static float Scale
    {
        get => Internal.State.Instance.Scale;
        set
        {
            //if (_inProcessFrame)
            //    throw new InvalidOperationException("scale cannot be changed during process frame");

            if (Internal.State.Instance.Scale != value && value >= 0.25f)
            {
                Internal.State.Instance.Scale = value;
                RebuildFontAtlas();
            }
        }
    }

    public static IntPtr BindTexture(Texture2D tex)
    {
        return (IntPtr)tex.GetRid().Id;
    }

    public static void ResetFonts()
    {
        Internal.State.Instance.Fonts.ResetFonts();
    }

    public static void AddFont(FontFile fontData, int fontSize, bool merge = false)
    {
        Internal.State.Instance.Fonts.AddFont(fontData, fontSize, merge);
    }

    public static void AddFontDefault()
    {
        Internal.State.Instance.Fonts.AddFont(null, 13, false);
    }

    public static void RebuildFontAtlas()
    {
        //if (_inProcessFrame)
        //    throw new InvalidOperationException("fonts cannot be changed during process frame");

        bool scaleToDpi = (bool)ProjectSettings.GetSetting("display/window/dpi/allow_hidpi");
        int dpiFactor = Math.Max(1, DisplayServer.ScreenGetDpi() / 96);

        Internal.State.Instance.Fonts.RebuildFontAtlas(scaleToDpi ? Scale * dpiFactor : Scale);
    }

    public static void Connect(Callable callable)
    {
        ImGuiLayer.Instance?.Signaler.Connect("imgui_layout", callable);
    }

    public static void Connect(Action action)
    {
        Connect(Callable.From(action));
    }
}
#endif
