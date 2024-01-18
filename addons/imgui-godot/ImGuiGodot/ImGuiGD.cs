#if GODOT_PC
using Godot;
using ImGuiNET;
using System;

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
            if (_inProcessFrame)
                throw new InvalidOperationException("scale cannot be changed during process frame");

            if (_scale != value && value >= 0.25f)
            {
                _scale = value;
                RebuildFontAtlas();
            }
        }
    }
    private static float _scale = 1.0f;

    private static bool _inProcessFrame = false;

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

        Internal.IRenderer internalRenderer;
        try
        {
            internalRenderer = renderer switch
            {
                RendererType.Dummy => new Internal.DummyRenderer(),
                RendererType.Canvas => new Internal.CanvasRenderer(),
                RendererType.RenderingDevice => threadModel == 2 ? new Internal.RdRendererThreadSafe() : new Internal.RdRenderer(),
                _ => throw new ArgumentException("Invalid renderer", nameof(renderer))
            };
        }
        catch (Exception e)
        {
            if (renderer == RendererType.RenderingDevice)
            {
                GD.PushWarning($"imgui-godot: falling back to Canvas renderer ({e.Message})");
                internalRenderer = new Internal.CanvasRenderer();
            }
            else
            {
                GD.PushError("imgui-godot: failed to init renderer");
                internalRenderer = new Internal.DummyRenderer();
            }
        }

        Internal.State.Instance = new(mainWindow, mainSubViewport, internalRenderer);
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
        if (_inProcessFrame)
            throw new InvalidOperationException("fonts cannot be changed during process frame");

        Internal.State.Instance.Fonts.RebuildFontAtlas(ScaleToDpi ? Scale * DpiFactor : Scale);
    }

    public static void Update(double delta, Vector2 displaySize)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new(displaySize.X, displaySize.Y);
        io.DeltaTime = (float)delta;

        Internal.State.Instance.Input.Update(io);

        _inProcessFrame = true;
        ImGui.NewFrame();
    }

    public static void Render()
    {
        ImGui.Render();

        ImGui.UpdatePlatformWindows();
        Internal.State.Instance.Renderer.RenderDrawData();
        _inProcessFrame = false;
    }

    public static void Shutdown()
    {
        Internal.State.Instance.Renderer.Shutdown();
        if (ImGui.GetCurrentContext() != IntPtr.Zero)
            ImGui.DestroyContext();
    }

    [Obsolete("just set ImGuiConfigFlags.ViewportsEnable instead")]
    public static void ExperimentalEnableViewports()
    {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        if (OS.GetName() != "Windows")
        {
            GD.PushWarning("ImGui Viewports have issues on macOS and Linux https://github.com/ocornut/imgui/wiki/Multi-Viewports#issues");
        }
    }

    /// <returns>
    /// True if the InputEvent was consumed
    /// </returns>
    public static bool ProcessInput(InputEvent evt, Window window)
    {
        return Internal.State.Instance.Input.ProcessInput(evt, window);
    }
}

public enum RendererType
{
    Dummy,
    Canvas,
    RenderingDevice
}
#endif
