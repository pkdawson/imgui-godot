using System;
using Godot;
using ImGuiNET;

namespace ImGuiGodot.Internal;

public class PublicInterfaceNet : IPublicInterface
{
    public void AddFont(FontFile fontData, int fontSize, bool merge)
    {
        State.Instance.Fonts.AddFont(fontData, fontSize, merge);
    }

    public void Connect(Callable callable)
    {
        ImGuiLayer.Instance.Signaler.Connect("imgui_layout", callable);
    }

    public void Init(Window mainWindow, Rid mainSubViewport, Resource cfg)
    {
        bool headless = DisplayServer.GetName() == "headless";
        float scale = (float)cfg.Get("Scale");
        RendererType renderer = headless ? RendererType.Dummy : Enum.Parse<RendererType>((string)cfg.Get("Renderer"));

        if (IntPtr.Size != sizeof(ulong))
        {
            throw new PlatformNotSupportedException("imgui-godot requires 64-bit pointers");
        }

        ImGuiGD.Scale = scale;

        // fall back to Canvas in OpenGL compatibility mode
        if (renderer == RendererType.RenderingDevice && RenderingServer.GetRenderingDevice() == null)
        {
            renderer = RendererType.Canvas;
        }

        int threadModel = (int)ProjectSettings.GetSetting("rendering/driver/threads/thread_model");

        State.Instance = new(mainWindow, mainSubViewport, renderer switch
        {
            RendererType.Dummy => new DummyRenderer(),
            RendererType.Canvas => new CanvasRenderer(),
            RendererType.RenderingDevice => threadModel == 2 ? new RdRendererThreadSafe() : new RdRenderer(),
            _ => throw new ArgumentException($"Invalid renderer: {renderer}", nameof(cfg))
        });
        State.Instance.Renderer.InitViewport(mainSubViewport);

        ImGui.GetIO().SetIniFilename((string)cfg.Get("IniFilename"));

        var fonts = (Godot.Collections.Array)cfg.Get("Fonts");

        for (int i = 0; i < fonts.Count; ++i)
        {
            var fontres = (Resource)fonts[i];
            var font = (FontFile)fontres.Get("FontData");
            int fontSize = (int)fontres.Get("FontSize");
            bool merge = (bool)fontres.Get("Merge");
            AddFont(font, fontSize, i > 0 && merge);
        }
        if ((bool)cfg.Get("AddDefaultFont"))
        {
            ImGuiGD.AddFontDefault();
        }
        ImGuiGD.RebuildFontAtlas();
    }

    public bool ProcessInput(InputEvent evt, Window window)
    {
        throw new NotImplementedException();
    }

    public void RebuildFontAtlas(float scale)
    {
        GD.Print($"rebuild font atlas @ {scale}");
        State.Instance.Fonts.RebuildFontAtlas(scale);
    }

    public void Render()
    {
        ImGui.Render();

        var io = ImGui.GetIO();
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            ImGui.UpdatePlatformWindows();
        }
        State.Instance.Renderer.RenderDrawData();
    }

    public void ResetFonts()
    {
        State.Instance.Fonts.ResetFonts();
    }

    public void SetIniFilename(ImGuiIOPtr io, string fileName)
    {
        State.Instance.SetIniFilename(io, fileName);
    }

    public void SetJoyAxisDeadZone(float zone)
    {
        Input.JoyAxisDeadZone = zone;
    }

    public void SetJoyButtonSwapAB(bool swap)
    {
        Input.JoyButtonSwapAB = swap;
    }

    public void SetScale(float scale)
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
    {
        State.Instance.Renderer.Shutdown();
        if (ImGui.GetCurrentContext() != IntPtr.Zero)
            ImGui.DestroyContext();
    }

    public void Update(double delta, Vector2 displaySize)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new(displaySize.X, displaySize.Y);
        io.DeltaTime = (float)delta;

        State.Instance.Input.Update(io);

        ImGui.NewFrame();
    }
}
