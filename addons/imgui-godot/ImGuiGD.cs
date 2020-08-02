using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;
using ImGuiNET;

public class ImGuiGD
{
    private static Dictionary<IntPtr, Texture> _loadedTextures = new Dictionary<IntPtr, Texture>();
    private static int _textureId = 100;
    private static IntPtr? _fontTextureId;
    private static Dictionary<string, byte[]> _fontStorage = new Dictionary<string, byte[]>();

    public static IntPtr BindTexture(Texture tex)
    {
        // decided not to add duplicate prevention, could cause problems
        var id = new IntPtr(_textureId++);
        _loadedTextures.Add(id, tex);
        return id;
    }

    public static void UnbindTexture(IntPtr textureId)
    {
        _loadedTextures.Remove(textureId);
    }

    // used by renderer
    public static Texture GetTexture(IntPtr textureId)
    {
        return _loadedTextures[textureId];
    }

    public static ImFontPtr AddFont(DynamicFont font)
    {
        return AddFont(font.FontData, font.Size);
    }

    public static unsafe ImFontPtr AddFont(DynamicFontData fontData, int fontSize)
    {
        ImFontPtr rv = null;

        if (!_fontStorage.ContainsKey(fontData.FontPath))
        {
            // store buf so it doesn't get GC'd
            Godot.File fi = new File();
            var err = fi.Open(fontData.FontPath, File.ModeFlags.Read);
            _fontStorage[fontData.FontPath] = fi.GetBuffer((int)fi.GetLen());
            fi.Close();
        }

        // can't add a name, ImFontConfig seems unusable

        byte[] buf = _fontStorage[fontData.FontPath];
        fixed (byte* p = buf)
        {
            IntPtr ptr = (IntPtr)p;
            rv = ImGui.GetIO().Fonts.AddFontFromMemoryTTF(ptr, buf.Length, (float)fontSize);
        }

        return rv;
    }

    public static unsafe void RebuildFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        byte[] pixels = new byte[width * height * bytesPerPixel];
        unsafe { Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length); }

        Image img = new Image();
        img.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);

        var imgtex = new ImageTexture();
        imgtex.CreateFromImage(img, 0);

        if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);
        _fontTextureId = BindTexture(imgtex);

        io.Fonts.SetTexID(_fontTextureId.Value);
        io.Fonts.ClearTexData();
    }
};
