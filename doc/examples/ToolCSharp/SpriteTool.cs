using Godot;
using ImGuiNET;
using ImGuiGodot;

[Tool]
public partial class SpriteTool : Sprite2D
{
    private bool _showImGui = false;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            _showImGui = ImGuiGD.ToolInit();
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
            io.ConfigViewportsNoAutoMerge = true;
        }
    }

    public override void _Process(double delta)
    {
        if (_showImGui)
        {
            ImGui.Begin($"SpriteTool: {Name}");
            float scale = Scale.X;
            if (ImGui.DragFloat("scale", ref scale, 0.1f))
            {
                Scale = new(scale, scale);
            }
            ImGui.End();
        }
    }
}
