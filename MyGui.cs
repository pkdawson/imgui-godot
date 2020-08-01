using Godot;
using System;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;

public class MyGui : ImGuiNode
{
    private IntPtr iconTextureId;

    public override void Init(ImGuiIOPtr io)
    {
        base.Init(io);
        iconTextureId = BindTexture(GD.Load<Texture>("res://icon.png"));
    }

    public override void Layout()
    {
        ImGui.Begin("test");
        ImGui.Text("hello Godot");
        ImGui.Image(iconTextureId, new Vector2(64, 64));
        ImGui.End();

        ImGui.ShowDemoWindow();
    }
}
