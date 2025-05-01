using Godot;
using ImGuiGodot;
using Hexa.NET.ImGui;

namespace DemoProject;

public partial class ThirdNode : Node
{
    public override void _Ready()
    {
        SubViewport svp = GetNode<SubViewport>("../GUIPanel3D/SubViewport");
        ImGuiGD.SetMainViewport(svp);

        MeshInstance3D cube = GetNode<MeshInstance3D>("../Background/Cube");
        var mat = (StandardMaterial3D)cube.GetSurfaceOverrideMaterial(0);
        mat.AlbedoTexture = svp.GetTexture();
        mat.AlbedoColor = Colors.White;
    }

    public override void _Process(double delta)
    {
        ImGui.SetNextWindowPos(new(10, 10), ImGuiCond.Once);
        ImGui.Begin("hello 3D");
        if (ImGui.Button("back"))
            GetTree().ChangeSceneToFile("res://data/demo.tscn");
        ImGui.End();

        ImGui.ShowDemoWindow();
    }
}
