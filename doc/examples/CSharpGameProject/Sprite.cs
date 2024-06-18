using Godot;
using ImGuiNET;

namespace CSharpGameProject;

public partial class Sprite : Sprite2D
{
    public override void _Process(double delta)
    {
        Position = new((float)(Position.X + (delta * 10.0)), Position.Y);

#if IMGUI
        ImGui.Begin($"SpriteTool: {Name}", ImGuiWindowFlags.AlwaysAutoResize);
        int[] pos = [(int)Position.X, (int)Position.Y];
        if (ImGui.DragInt2("position", ref pos[0]))
        {
            Position = new(pos[0], pos[1]);
        }
        ImGui.End();
#endif
    }
}
