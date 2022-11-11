using Godot;
using ImGuiNET;
using System;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace ImGuiGodot;

public static class ImGuiGD
{
    /// <summary>
    /// Deadzone for all axes
    /// </summary>
    public static float JoyAxisDeadZone { get; set; } = 0.15f;

    /// <summary>
    /// Swap the functionality of the activate (face down) and cancel (face right) buttons
    /// </summary>
    public static bool JoyButtonSwapAB { get; set; } = false;

    /// <summary>
    /// Try to calculate how many pixels squared per point. Should be 1 or 2 on non-mobile displays
    /// </summary>
    public static int DpiFactor
    {
        get => Math.Max(1, DisplayServer.ScreenGetDpi() / 96);
    }

    /// <summary>
    /// Adjust the scale based on <see cref="DpiFactor"/>
    /// </summary>
    public static bool ScaleToDpi { get; set; } = true;

    /// <summary>
    /// Setting this will reinitialize ImGui with rescaled fonts
    /// </summary>
    public static float Scale
    {
        get => _scale;
        set
        {
            if (_scale != value && value >= 0.25f)
            {
                _scale = value;
                RebuildFontAtlas();
            }
        }
    }
    private static float _scale = 1.0f;

    public static IntPtr BindTexture(Texture2D tex)
    {
        return (IntPtr)tex.GetRid().Id;
    }

    [Obsolete("UnbindTexture is no longer necessary")]
    public static void UnbindTexture(IntPtr texid)
    {
    }

    [Obsolete("UnbindTexture is no longer necessary")]
    public static void UnbindTexture(Texture2D tex)
    {
    }

    public static void Init(float? scale = null, Renderer renderer = Renderer.RenderingDevice)
    {
        if (IntPtr.Size != sizeof(ulong))
        {
            throw new PlatformNotSupportedException("imgui-godot requires 64-bit pointers");
        }

        if (scale != null)
        {
            _scale = scale.Value;
        }

        Internal.Init(renderer switch
        {
            Renderer.Canvas => new InternalCanvasRenderer(),
            Renderer.RenderingDevice => new InternalRdRenderer(),
            _ => throw new ArgumentException("Invalid renderer", nameof(renderer))
        });
    }

    public static void ResetFonts()
    {
        Internal.ResetFonts();
    }

    public static void AddFont(FontFile fontData, int fontSize, bool merge = false)
    {
        Internal.AddFont(fontData, fontSize, merge);
    }

    public static void AddFontDefault()
    {
        Internal.AddFont(null, 13, false);
    }

    public static void RebuildFontAtlas()
    {
        Internal.RebuildFontAtlas(ScaleToDpi ? Scale * DpiFactor : Scale);
    }

    public static void Update(double delta, Viewport vp)
    {
        Internal.Update(delta, vp);
    }

    public static void Render(Viewport vp)
    {
        Internal.Render(vp);
    }

    public static void Shutdown()
    {
        Internal.Renderer.Shutdown();
        if (ImGui.GetCurrentContext() != IntPtr.Zero)
            ImGui.DestroyContext();
    }

    /// <returns>
    /// True if the InputEvent was consumed
    /// </returns>
    public static bool ProcessInput(InputEvent evt)
    {
        return Internal.ProcessInput(evt);
    }

    /// <summary>
    /// Extension method to translate between <see cref="Key"/> and <see cref="ImGuiKey"/>
    /// </summary>
    public static ImGuiKey ToImGuiKey(this Key key)
    {
        return Internal.ConvertKey(key);
    }

    /// <summary>
    /// Extension method to translate between <see cref="JoyButton"/> and <see cref="ImGuiKey"/>
    /// </summary>
    public static ImGuiKey ToImGuiKey(this JoyButton button)
    {
        return Internal.ConvertJoyButton(button);
    }

    /// <summary>
    /// Convert <see cref="Color"/> to ImGui color RGBA
    /// </summary>
    public static Vector4 ToVector4(this Color color)
    {
        return new Vector4(color.r, color.g, color.b, color.a);
    }

    /// <summary>
    /// Convert <see cref="Color"/> to ImGui color RGB
    /// </summary>
    public static Vector3 ToVector3(this Color color)
    {
        return new Vector3(color.r, color.g, color.b);
    }

    /// <summary>
    /// Convert RGB <see cref="Vector3"/> to <see cref="Color"/>
    /// </summary>
    public static Color ToColor(this Vector3 vec)
    {
        return new Color(vec.X, vec.Y, vec.Z);
    }

    /// <summary>
    /// Convert RGBA <see cref="Vector4"/> to <see cref="Color"/>
    /// </summary>
    public static Color ToColor(this Vector4 vec)
    {
        return new Color(vec.X, vec.Y, vec.Z, vec.W);
    }

    /// <summary>
    /// Set IniFilename, converting Godot path to native
    /// </summary>
    public static void SetIniFilename(this ImGuiIOPtr io, string fileName)
    {
        Internal.SetIniFilename(io, fileName);
    }
}

public enum Renderer
{
    Canvas,
    RenderingDevice
}
