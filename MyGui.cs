using Godot;
using System;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;

public class MyGui : ImGuiNode
{
    public override void Init(ImGuiIOPtr io)
    {
        // instead of calling base.Init(io) to setup the font, we'll do it ourselves

        // add font directly from the filesystem, not a resource
        io.Fonts.AddFontFromFileTTF("Hack-Regular.ttf", 16);
        io.Fonts.AddFontDefault(); // just for comparison
        ImGuiGD.RebuildFontAtlas();
    }

    public override void Layout()
    {
        ImGui.ShowDemoWindow();

        base.Layout(); // this emits the signal
    }
}
