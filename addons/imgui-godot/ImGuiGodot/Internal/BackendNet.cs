using Godot;
using ImGuiNET;
using System;
using Vector2 = System.Numerics.Vector2;

namespace ImGuiGodot.Internal;

internal sealed class BackendNet : IBackend
{
    public float JoyAxisDeadZone { get; set; } = 0.15f;
    public float Scale
    {
        get => State.Instance.Scale;
        set => State.Instance.Scale = value;
    }

    public void AddFont(FontFile fontData, int fontSize, bool merge)
    {
        State.Instance.Fonts.AddFont(fontData, fontSize, merge);
    }

    public void AddFontDefault()
    {
        State.Instance.Fonts.AddFont(null, 13, false);
    }

    public void Connect(Callable callable)
    {
        ImGuiLayer.Instance?.Signaler.Connect("imgui_layout", callable);
    }

    public void RebuildFontAtlas(float scale)
    {
        State.Instance.Fonts.RebuildFontAtlas(scale);
    }

    public void ResetFonts()
    {
        State.Instance.Fonts.ResetFonts();
    }

    public bool SubViewportWidget(SubViewport vp)
    {
        Vector2 vpSize = new(vp.Size.X, vp.Size.Y);
        var pos = ImGui.GetCursorScreenPos();
        var pos_max = new Vector2(pos.X + vpSize.X, pos.Y + vpSize.Y);
        ImGui.GetWindowDrawList().AddImage((IntPtr)vp.GetTexture().GetRid().Id, pos, pos_max);

        ImGui.PushID(vp.NativeInstance);
        ImGui.InvisibleButton("godot_subviewport", vpSize);
        ImGui.PopID();

        if (ImGui.IsItemHovered())
        {
            State.Instance.Input.CurrentSubViewport = vp;
            State.Instance.Input.CurrentSubViewportPos = pos;
            return true;
        }
        return false;
    }
}
