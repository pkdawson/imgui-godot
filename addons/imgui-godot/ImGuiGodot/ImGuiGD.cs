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
    public static int DpiFactor => Math.Max(1, DisplayServer.ScreenGetDpi() / 96);

    /// <summary>
    /// Adjust the scale based on <see cref="DpiFactor"/>
    /// </summary>
    public static bool ScaleToDpi { get; set; } = true;

    /// <summary>
    /// Setting this property will reload fonts and modify the ImGuiStyle
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

    public static void Init(Window mainWindow, Rid mainSubViewport, float? scale = null, RendererType renderer = RendererType.RenderingDevice)
    {
        if (IntPtr.Size != sizeof(ulong))
        {
            throw new PlatformNotSupportedException("imgui-godot requires 64-bit pointers");
        }

        if (scale != null)
        {
            _scale = scale.Value;
        }

        // fall back to Canvas in OpenGL compatibility mode
        if (renderer == RendererType.RenderingDevice && RenderingServer.GetRenderingDevice() == null)
        {
            renderer = RendererType.Canvas;
        }

        // there's no way to get the actual current thread model, eg if --render-thread is used
        int threadModel = (int)ProjectSettings.GetSetting("rendering/driver/threads/thread_model");

        Internal.State.Instance = new(mainWindow, mainSubViewport, renderer switch
        {
            RendererType.Dummy => new Internal.DummyRenderer(),
            RendererType.Canvas => new Internal.CanvasRenderer(),
            RendererType.RenderingDevice => threadModel == 2 ? new Internal.RdRendererThreadSafe() : new Internal.RdRenderer(),
            _ => throw new ArgumentException("Invalid renderer", nameof(renderer))
        });
        Internal.State.Instance.Renderer.InitViewport(mainSubViewport);
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
        Internal.State.Instance.Fonts.RebuildFontAtlas(ScaleToDpi ? Scale * DpiFactor : Scale);
    }

    public static void Update(double delta, Vector2 displaySize)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new(displaySize.X, displaySize.Y);
        io.DeltaTime = (float)delta;

        Internal.State.Instance.Input.Update(io);

        ImGui.NewFrame();
    }

    public static void Render()
    {
        ImGui.Render();

        ImGui.UpdatePlatformWindows();
        Internal.State.Instance.Renderer.RenderDrawData();
    }

    public static void Shutdown()
    {
        Internal.State.Instance.Renderer.Shutdown();
        if (ImGui.GetCurrentContext() != IntPtr.Zero)
            ImGui.DestroyContext();
    }

    /// <summary>
    /// EXPERIMENTAL! Please report bugs, with steps to reproduce.
    /// </summary>
    public static void ExperimentalEnableViewports()
    {
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        if (OS.GetName() != "Windows")
        {
            GD.PushWarning("ImGui Viewports have issues on macOS and Linux https://github.com/ocornut/imgui/wiki/Multi-Viewports#issues");
        }

        var mainvp = ImGuiLayer.Instance.GetViewport();
        if (mainvp.GuiEmbedSubwindows)
        {
            GD.PushWarning("ImGui Viewports: 'display/window/subwindows/embed_subwindows' needs to be disabled");
            mainvp.GuiEmbedSubwindows = false;
        }
    }

    /// <returns>
    /// True if the InputEvent was consumed
    /// </returns>
    public static bool ProcessInput(InputEvent evt, Window window)
    {
        return Internal.State.Instance.Input.ProcessInput(evt, window);
    }

    /// <summary>
    /// Extension method to translate between <see cref="Key"/> and <see cref="ImGuiKey"/>
    /// </summary>
    public static ImGuiKey ToImGuiKey(this Key key)
    {
        return Internal.Input.ConvertKey(key);
    }

    /// <summary>
    /// Extension method to translate between <see cref="JoyButton"/> and <see cref="ImGuiKey"/>
    /// </summary>
    public static ImGuiKey ToImGuiKey(this JoyButton button)
    {
        return Internal.Input.ConvertJoyButton(button);
    }

    /// <summary>
    /// Convert <see cref="Color"/> to ImGui color RGBA
    /// </summary>
    public static Vector4 ToVector4(this Color color)
    {
        return new Vector4(color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// Convert <see cref="Color"/> to ImGui color RGB
    /// </summary>
    public static Vector3 ToVector3(this Color color)
    {
        return new Vector3(color.R, color.G, color.B);
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
        Internal.State.Instance.SetIniFilename(io, fileName);
    }
}

public enum RendererType
{
    Dummy,
    Canvas,
    RenderingDevice
}
