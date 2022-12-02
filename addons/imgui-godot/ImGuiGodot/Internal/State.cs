using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImGuiGodot.Internal;

internal interface IRenderer
{
    public string Name { get; }
    public void Init(ImGuiIOPtr io);
    public void InitViewport(Viewport vp);
    public void CloseViewport(Viewport vp);
    public void RenderDrawData(Viewport vp, ImDrawDataPtr drawData);
    public void OnHide();
    public void Shutdown();
}

internal static class State
{
    private static Texture2D _fontTexture;
    private static readonly IntPtr _backendName = Marshal.StringToCoTaskMemAnsi("imgui_impl_godot4_net");
    private static IntPtr _rendererName = IntPtr.Zero;
    private static IntPtr _iniFilenameBuffer = IntPtr.Zero;
    internal static IRenderer Renderer { get; private set; }

    private class FontParams
    {
        public FontFile Font { get; init; }
        public int FontSize { get; init; }
        public bool Merge { get; init; }
    }
    private static readonly List<FontParams> _fontConfiguration = new();

    public static void AddFont(FontFile fontData, int fontSize, bool merge)
    {
        _fontConfiguration.Add(new FontParams { Font = fontData, FontSize = fontSize, Merge = merge });
    }

    private static unsafe void AddFontToAtlas(FontFile fontData, int fontSize, bool merge)
    {
        ImFontConfig* fc = ImGuiNative.ImFontConfig_ImFontConfig();
        if (merge)
        {
            fc->MergeMode = 1;
        }

        if (fontData == null)
        {
            // default font
            var fcptr = new ImFontConfigPtr(fc)
            {
                SizePixels = fontSize,
                OversampleH = 1,
                OversampleV = 1,
                PixelSnapH = true
            };
            ImGui.GetIO().Fonts.AddFontDefault(fc);
        }
        else
        {
            ImVector ranges = GetRanges(fontData);
            string name = $"{System.IO.Path.GetFileName(fontData.ResourcePath)}, {fontSize}px";
            for (int i = 0; i < name.Length && i < 40; ++i)
            {
                fc->Name[i] = Convert.ToByte(name[i]);
            }

            int len = fontData.Data.Length;
            // let ImGui manage this memory
            IntPtr p = ImGui.MemAlloc((uint)len);
            Marshal.Copy(fontData.Data, 0, p, len);
            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(p, len, fontSize, fc, ranges.Data);
        }

        if (merge)
        {
            ImGui.GetIO().Fonts.Build();
        }
        ImGuiNative.ImFontConfig_destroy(fc);
    }

    private static unsafe ImVector GetRanges(Font font)
    {
        var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
        builder.AddText(font.GetSupportedChars());
        builder.BuildRanges(out ImVector vec);
        builder.Destroy();
        return vec;
    }

    private static unsafe void ResetStyle()
    {
        ImGuiStylePtr defaultStyle = new(ImGuiNative.ImGuiStyle_ImGuiStyle());
        ImGuiStylePtr style = ImGui.GetStyle();

        style.WindowPadding = defaultStyle.WindowPadding;
        style.WindowRounding = defaultStyle.WindowRounding;
        style.WindowMinSize = defaultStyle.WindowMinSize;
        style.ChildRounding = defaultStyle.ChildRounding;
        style.PopupRounding = defaultStyle.PopupRounding;
        style.FramePadding = defaultStyle.FramePadding;
        style.FrameRounding = defaultStyle.FrameRounding;
        style.ItemSpacing = defaultStyle.ItemSpacing;
        style.ItemInnerSpacing = defaultStyle.ItemInnerSpacing;
        style.CellPadding = defaultStyle.CellPadding;
        style.TouchExtraPadding = defaultStyle.TouchExtraPadding;
        style.IndentSpacing = defaultStyle.IndentSpacing;
        style.ColumnsMinSpacing = defaultStyle.ColumnsMinSpacing;
        style.ScrollbarSize = defaultStyle.ScrollbarSize;
        style.ScrollbarRounding = defaultStyle.ScrollbarRounding;
        style.GrabMinSize = defaultStyle.GrabMinSize;
        style.GrabRounding = defaultStyle.GrabRounding;
        style.LogSliderDeadzone = defaultStyle.LogSliderDeadzone;
        style.TabRounding = defaultStyle.TabRounding;
        style.TabMinWidthForCloseButton = defaultStyle.TabMinWidthForCloseButton;
        style.DisplayWindowPadding = defaultStyle.DisplayWindowPadding;
        style.DisplaySafeAreaPadding = defaultStyle.DisplaySafeAreaPadding;
        style.MouseCursorScale = defaultStyle.MouseCursorScale;

        defaultStyle.Destroy();
    }

    public static unsafe void RebuildFontAtlas(float scale)
    {
        var io = ImGui.GetIO();
        int fontIndex = -1;
        if (io.NativePtr->FontDefault != null)
        {
            for (int i = 0; i < io.Fonts.Fonts.Size; ++i)
            {
                if (io.Fonts.Fonts[i].NativePtr == io.FontDefault.NativePtr)
                {
                    fontIndex = i;
                    break;
                }
            }
            io.NativePtr->FontDefault = null;
        }
        io.Fonts.Clear();

        foreach (var fontParams in _fontConfiguration)
        {
            AddFontToAtlas(fontParams.Font, (int)(fontParams.FontSize * scale), fontParams.Merge);
        }

        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        byte[] pixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy((IntPtr)pixelData, pixels, 0, pixels.Length);

        var img = Image.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);

        var imgtex = ImageTexture.CreateFromImage(img);
        _fontTexture = imgtex;
        io.Fonts.SetTexID((IntPtr)_fontTexture.GetRid().Id);
        io.Fonts.ClearTexData();

        if (fontIndex != -1 && fontIndex < io.Fonts.Fonts.Size)
        {
            io.NativePtr->FontDefault = io.Fonts.Fonts[fontIndex].NativePtr;
        }

        ResetStyle();
        ImGui.GetStyle().ScaleAllSizes(scale);
    }

    public static void Init(IRenderer renderer)
    {
        Renderer = renderer;
        _fontConfiguration.Clear();

        if (ImGui.GetCurrentContext() != IntPtr.Zero)
        {
            ImGui.DestroyContext();
        }

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();

        io.BackendFlags = 0;
        io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

        if (_rendererName == IntPtr.Zero)
        {
            _rendererName = Marshal.StringToCoTaskMemAnsi(Renderer.Name);
        }

        unsafe
        {
            io.NativePtr->BackendPlatformName = (byte*)_backendName;
            io.NativePtr->BackendRendererName = (byte*)_rendererName;
        }

        Renderer.Init(io);
        InternalViewports.Init();
    }

    public static void ResetFonts()
    {
        var io = ImGui.GetIO();
        io.Fonts.Clear();
        unsafe { io.NativePtr->FontDefault = null; }
        _fontConfiguration.Clear();
    }

    public static unsafe void SetIniFilename(ImGuiIOPtr io, string fileName)
    {
        io.NativePtr->IniFilename = null;

        if (_iniFilenameBuffer != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(_iniFilenameBuffer);
            _iniFilenameBuffer = IntPtr.Zero;
        }

        if (fileName?.Length > 0)
        {
            fileName = ProjectSettings.GlobalizePath(fileName);
            _iniFilenameBuffer = Marshal.StringToCoTaskMemUTF8(fileName);
            io.NativePtr->IniFilename = (byte*)_iniFilenameBuffer;
        }
    }

    public static void Update(double delta, Viewport vp)
    {
        var io = ImGui.GetIO();
        var vpSize = vp.GetVisibleRect().Size;
        io.DisplaySize = new(vpSize.x, vpSize.y);
        io.DeltaTime = (float)delta;

        Input.Update(io);

        ImGui.NewFrame();
    }

    public static void ProcessNotification(long what)
    {
        switch (what)
        {
            case MainLoop.NotificationApplicationFocusIn:
                ImGui.GetIO().AddFocusEvent(true);
                break;
            case MainLoop.NotificationApplicationFocusOut:
                ImGui.GetIO().AddFocusEvent(false);
                break;
        };
    }

    public static void AddLayerSubViewport(Node parent, out SubViewportContainer subViewportContainer, out SubViewport subViewport)
    {
        subViewportContainer = new SubViewportContainer
        {
            Name = "ImGuiLayer_SubViewportContainer",
            AnchorsPreset = 15,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Stretch = true
        };

        subViewport = new SubViewport
        {
            Name = "ImGuiLayer_SubViewport",
            TransparentBg = true,
            HandleInputLocally = false,
            GuiDisableInput = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };

        subViewportContainer.AddChild(subViewport);
        parent.AddChild(subViewportContainer);
    }

    public static void Render(Viewport vp)
    {
        ImGui.Render();
        Renderer.RenderDrawData(vp, ImGui.GetDrawData());

        var io = ImGui.GetIO();
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            ImGui.UpdatePlatformWindows();
            InternalViewports.RenderViewports();
        }
    }
}
