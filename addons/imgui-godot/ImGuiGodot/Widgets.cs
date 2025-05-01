using Godot;
using Hexa.NET.ImGui;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace ImGuiGodot;

public static partial class ImGuiGD
{
    /// <summary>
    /// Display an interactable SubViewport
    /// </summary>
    /// <remarks>
    /// Be sure to change the SubViewport's <see cref="SubViewport.RenderTargetUpdateMode"/> to <see cref="SubViewport.UpdateMode.Always"/>
    /// </remarks>
    /// <returns>
    /// True if active (mouse hovered)
    /// </returns>
    public static bool SubViewport(SubViewport svp)
    {
        return _backend.SubViewportWidget(svp);
    }

    public static void Image(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1)
    {
        ImGui.Image(tex.GetRid().Id, size, uv0, uv1);
    }

    public static void Image(Texture2D tex, Vector2 size, Vector2 uv0)
        => Image(tex, size, uv0, Vector2.One);

    public static void Image(Texture2D tex, Vector2 size)
        => Image(tex, size, Vector2.Zero, Vector2.One);

    public static void Image(AtlasTexture tex, Vector2 size)
    {
        (Vector2 uv0, Vector2 uv1) = GetAtlasUVs(tex);
        ImGui.Image(tex.GetRid().Id, size, uv0, uv1);
    }

    public static bool ImageButton(string str_id, Texture2D tex, Vector2 size)
        => ImageButton(str_id, tex, size, Vector2.Zero, Vector2.One, Vector4.Zero, Vector4.One);

    public static bool ImageButton(string str_id, Texture2D tex, Vector2 size, Vector2 uv0)
        => ImageButton(str_id, tex, size, uv0, Vector2.One, Vector4.Zero, Vector4.One);

    public static bool ImageButton(
        string str_id,
        Texture2D tex,
        Vector2 size,
        Vector2 uv0,
        Vector2 uv1)
        => ImageButton(str_id, tex, size, uv0, uv1, Vector4.Zero, Vector4.One);

    public static bool ImageButton(
        string str_id,
        Texture2D tex,
        Vector2 size,
        Vector2 uv0,
        Vector2 uv1,
        Vector4 bg_col)
        => ImageButton(str_id, tex, size, uv0, uv1, bg_col, Vector4.One);

    public static bool ImageButton(
        string str_id,
        Texture2D tex,
        Vector2 size,
        Vector2 uv0,
        Vector2 uv1,
        Vector4 bg_col,
        Vector4 tint_col)
    {
        return ImGui.ImageButton(str_id, tex.GetRid().Id, size, uv0, uv1, bg_col, tint_col);
    }

    public static bool ImageButton(string str_id, AtlasTexture tex, Vector2 size)
        => ImageButton(str_id, tex, size, Vector4.Zero, Vector4.One);

    public static bool ImageButton(string str_id, AtlasTexture tex, Vector2 size, Vector4 bg_col)
        => ImageButton(str_id, tex, size, bg_col, Vector4.One);

    public static bool ImageButton(
        string str_id,
        AtlasTexture tex,
        Vector2 size,
        Vector4 bg_col,
        Vector4 tint_col)
    {
        var (uv0, uv1) = GetAtlasUVs(tex);
        return ImGui.ImageButton(str_id, tex.GetRid().Id, size, uv0, uv1, bg_col, tint_col);
    }

    public static void ImageWithBg(
        Texture2D tex,
        Vector2 imageSize,
        Vector2 uv0,
        Vector2 uv1,
        Vector4 bgCol,
        Vector4 tintCol)
    {
        ImGui.ImageWithBg(tex.GetRid().Id, imageSize, uv0, uv1, bgCol, tintCol);
    }

    public static void ImageWithBg(
        Texture2D tex,
        Vector2 imageSize,
        Vector2 uv0,
        Vector2 uv1,
        Vector4 bgCol)
        => ImageWithBg(tex, imageSize, uv0, uv1, bgCol, Vector4.One);

    public static void ImageWithBg(
        Texture2D tex,
        Vector2 imageSize,
        Vector2 uv0,
        Vector2 uv1)
        => ImageWithBg(tex, imageSize, uv0, uv1, Vector4.Zero, Vector4.One);

    public static void ImageWithBg(
        Texture2D tex,
        Vector2 imageSize,
        Vector2 uv0)
        => ImageWithBg(tex, imageSize, uv0, Vector2.One, Vector4.Zero, Vector4.One);

    public static void ImageWithBg(
        Texture2D tex,
        Vector2 imageSize)
        => ImageWithBg(tex, imageSize, Vector2.Zero, Vector2.One, Vector4.Zero, Vector4.One);

    public static void ImageWithBg(
        AtlasTexture tex,
        Vector2 imageSize,
        Vector4 bgCol,
        Vector4 tintCol)
    {
        (Vector2 uv0, Vector2 uv1) = GetAtlasUVs(tex);
        ImGui.ImageWithBg(tex.GetRid().Id, imageSize, uv0, uv1, bgCol, tintCol);
    }

    public static void ImageWithBg(
        AtlasTexture tex,
        Vector2 imageSize,
        Vector4 bgCol)
        => ImageWithBg(tex, imageSize, bgCol, Vector4.One);

    public static void ImageWithBg(
        AtlasTexture tex,
        Vector2 imageSize)
        => ImageWithBg(tex, imageSize, Vector4.Zero, Vector4.One);

    private static (Vector2 uv0, Vector2 uv1) GetAtlasUVs(AtlasTexture tex)
    {
        Godot.Vector2 atlasSize = tex.Atlas.GetSize();
        Godot.Vector2 guv0 = tex.Region.Position / atlasSize;
        Godot.Vector2 guv1 = tex.Region.End / atlasSize;
#pragma warning disable IDE0004 // Remove Unnecessary Cast
        return (new((float)guv0.X, (float)guv0.Y), new((float)guv1.X, (float)guv1.Y));
#pragma warning restore IDE0004 // Remove Unnecessary Cast
    }
}

/// <summary>
/// for backward compatibility
/// </summary>
/// <remarks>
/// will eventually add [Obsolete("Use ImGuiGD instead")]
/// </remarks>
public static class Widgets
{
    public static bool SubViewport(SubViewport svp) => ImGuiGD.SubViewport(svp);

    public static void Image(Texture2D tex, Vector2 size) => ImGuiGD.Image(tex, size);

    public static void Image(Texture2D tex, Vector2 size, Vector2 uv0)
        => ImGuiGD.Image(tex, size, uv0);

    public static void Image(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1)
        => ImGuiGD.Image(tex, size, uv0, uv1);

    public static void Image(AtlasTexture tex, Vector2 size) => ImGuiGD.Image(tex, size);

    public static bool ImageButton(string str_id, Texture2D tex, Vector2 size)
        => ImGuiGD.ImageButton(str_id, tex, size);

    public static bool ImageButton(string str_id, Texture2D tex, Vector2 size, Vector2 uv0)
        => ImGuiGD.ImageButton(str_id, tex, size, uv0);

    public static bool ImageButton(
        string str_id,
        Texture2D tex,
        Vector2 size,
        Vector2 uv0,
        Vector2 uv1) => ImGuiGD.ImageButton(str_id, tex, size, uv0, uv1);

    public static bool ImageButton(
        string str_id,
        Texture2D tex,
        Vector2 size,
        Vector2 uv0,
        Vector2 uv1,
        Vector4 bg_col) => ImGuiGD.ImageButton(str_id, tex, size, uv0, uv1, bg_col);

    public static bool ImageButton(
        string str_id,
        Texture2D tex,
        Vector2 size,
        Vector2 uv0,
        Vector2 uv1,
        Vector4 bg_col,
        Vector4 tint_col) => ImGuiGD.ImageButton(str_id, tex, size, uv0, uv1, bg_col, tint_col);

    public static bool ImageButton(string str_id, AtlasTexture tex, Vector2 size)
        => ImGuiGD.ImageButton(str_id, tex, size);

    public static bool ImageButton(string str_id, AtlasTexture tex, Vector2 size, Vector4 bg_col)
        => ImGuiGD.ImageButton(str_id, tex, size, bg_col);

    public static bool ImageButton(
        string str_id,
        AtlasTexture tex,
        Vector2 size,
        Vector4 bg_col,
        Vector4 tint_col) => ImGuiGD.ImageButton(str_id, tex, size, bg_col, tint_col);
}

#if NET10_0_OR_GREATER
// TODO: implicit extension GodotWidgets for ImGui
#endif
