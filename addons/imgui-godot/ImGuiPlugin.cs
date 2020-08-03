#if TOOLS
using Godot;
using System;

[Tool]
public class ImGuiPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        var script = GD.Load<Script>("res://addons/imgui-godot/ImGuiNode.cs");
        var texture = GD.Load<Texture>("res://addons/imgui-godot/imgui-icon.png");
        AddCustomType("ImGuiNode", "Node2D", script, texture);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("ImGuiNode");
    }
}
#endif
