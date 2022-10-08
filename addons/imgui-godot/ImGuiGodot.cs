using Godot;
using ImGuiNET;
using System;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

// widgets

public static class ImGuiGodot
{
    /// <summary>
    /// Be sure to change the SubViewport's <c>Update Mode</c> to <b>Always</b>
    /// </summary>
    public static void SubViewport(SubViewport vp)
    {
        Vector2 vpSize = new(vp.Size.x, vp.Size.y);
        var pos = ImGui.GetCursorScreenPos();
        var pos_max = new Vector2(pos.X + vpSize.X, pos.Y + vpSize.Y);
        ImGui.GetWindowDrawList().AddImage(ImGuiGD.BindTexture(vp.GetTexture()), pos, pos_max);

        ImGui.PushID(vp.NativeInstance);
        ImGui.InvisibleButton("godot_subviewport", vpSize);
        ImGui.PopID();

        if (ImGui.IsItemHovered())
        {
            ImGuiGDInternal.CurrentSubViewport = vp;
            ImGuiGDInternal.CurrentSubViewportPos = pos;
        }
    }

    public static void Image(Texture2D tex, Vector2 size)
    {
        Image(tex, size, Vector2.Zero, Vector2.One, Vector4.One, Vector4.Zero);
    }

    public static void Image(Texture2D tex, Vector2 size, Vector2 uv0)
    {
        Image(tex, size, uv0, Vector2.One, Vector4.One, Vector4.Zero);
    }

    public static void Image(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1)
    {
        Image(tex, size, uv0, uv1, Vector4.One, Vector4.Zero);
    }

    public static void Image(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1, Vector4 tint_col)
    {
        Image(tex, size, uv0, uv1, tint_col, Vector4.Zero);
    }

    public static void Image(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1, Vector4 tint_col, Vector4 border_col)
    {
        IntPtr texid = ImGuiGDInternal.BindTexture(tex);
        ImGuiNative.igImage(texid, size, uv0, uv1, tint_col, border_col);
    }

    public static bool ImageButton(Texture2D tex, Vector2 size)
    {
        return ImageButton(tex, size, Vector2.Zero, Vector2.One, -1, Vector4.Zero, Vector4.One);
    }

    public static bool ImageButton(Texture2D tex, Vector2 size, Vector2 uv0)
    {
        return ImageButton(tex, size, uv0, Vector2.One, -1, Vector4.Zero, Vector4.One);
    }

    public static bool ImageButton(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1)
    {
        return ImageButton(tex, size, uv0, uv1, -1, Vector4.Zero, Vector4.One);
    }

    public static bool ImageButton(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1, int frame_padding)
    {
        return ImageButton(tex, size, uv0, uv1, frame_padding, Vector4.Zero, Vector4.One);
    }

    public static bool ImageButton(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1, int frame_padding, Vector4 bg_col)
    {
        return ImageButton(tex, size, uv0, uv1, frame_padding, bg_col, Vector4.One);
    }

    public static bool ImageButton(Texture2D tex, Vector2 size, Vector2 uv0, Vector2 uv1, int frame_padding, Vector4 bg_col, Vector4 tint_col)
    {
        IntPtr texid = ImGuiGDInternal.BindTexture(tex);
        return ImGuiNative.igImageButton(texid, size, uv0, uv1, frame_padding, bg_col, tint_col) != 0;
    }
}
