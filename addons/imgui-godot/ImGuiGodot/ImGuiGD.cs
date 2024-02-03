#if GODOT_PC
using Godot;
using ImGuiGodot.Internal;
using System;

namespace ImGuiGodot;

public static class ImGuiGD
{
    private static readonly IBackend _backend;

    /// <summary>
    /// Deadzone for all axes
    /// </summary>
    public static float JoyAxisDeadZone
    {
        get => _backend.JoyAxisDeadZone;
        set => _backend.JoyAxisDeadZone = value;
    }

    /// <summary>
    /// Setting this property will reload fonts and modify the ImGuiStyle
    /// </summary>
    public static float Scale
    {
        get => _backend.Scale;
        set
        {
            //if (_inProcessFrame)
            //    throw new InvalidOperationException("scale cannot be changed during process frame");

            if (_backend.Scale != value && value >= 0.25f)
            {
                _backend.Scale = value;
                RebuildFontAtlas();
            }
        }
    }

    public static bool Visible { get; set; }

    static ImGuiGD()
    {
        _backend = ClassDB.ClassExists("ImGuiGD") ? new BackendNative() : new BackendNet();
    }

    public static IntPtr BindTexture(Texture2D tex)
    {
        return (IntPtr)tex.GetRid().Id;
    }

    public static void ResetFonts()
    {
        _backend.ResetFonts();
    }

    public static void AddFont(FontFile fontData, int fontSize, bool merge = false)
    {
        _backend.AddFont(fontData, fontSize, merge);
    }

    public static void AddFontDefault()
    {
        _backend.AddFontDefault();
    }

    public static void RebuildFontAtlas()
    {
        //if (_inProcessFrame)
        //    throw new InvalidOperationException("fonts cannot be changed during process frame");

        _backend.RebuildFontAtlas(Scale);
    }

    public static void Connect(Callable callable)
    {
        _backend.Connect(callable);
    }

    public static void Connect(Action action)
    {
        Connect(Callable.From(action));
    }

    public static bool ToolInit()
    {
        if (_backend is BackendNative nbe)
        {
            nbe.ToolInit();
            return true;
        }

        return false;
    }

    internal static bool SubViewportWidget(SubViewport svp)
    {
        return _backend.SubViewportWidget(svp);
    }
}
#endif
