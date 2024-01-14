#if GODOT_PC
using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImGuiGodot.Internal;

internal sealed class Fonts
{
    private Texture2D? _fontTexture;

    private sealed class FontParams
    {
        public FontFile? Font { get; init; }
        public int FontSize { get; init; }
        public bool Merge { get; init; }
    }
    private readonly List<FontParams> _fontConfiguration = new();

    public Fonts()
    {
        _fontConfiguration.Clear();
    }

    public void ResetFonts()
    {
        var io = ImGui.GetIO();
        io.Fonts.Clear();
        unsafe { io.NativePtr->FontDefault = null; }
        _fontConfiguration.Clear();
    }

    public void AddFont(FontFile? fontData, int fontSize, bool merge)
    {
        _fontConfiguration.Add(new FontParams { Font = fontData, FontSize = fontSize, Merge = merge });
    }

    private static unsafe void AddFontToAtlas(FontFile? fontData, int fontSize, bool merge)
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
        style.SeparatorTextPadding = defaultStyle.SeparatorTextPadding;
        style.DockingSeparatorSize = defaultStyle.DockingSeparatorSize;
        style.DisplayWindowPadding = defaultStyle.DisplayWindowPadding;
        style.DisplaySafeAreaPadding = defaultStyle.DisplaySafeAreaPadding;
        style.MouseCursorScale = defaultStyle.MouseCursorScale;

        defaultStyle.Destroy();
    }

    public unsafe void RebuildFontAtlas(float scale)
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
}
#endif
