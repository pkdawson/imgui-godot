using Godot;
using System;
using System.Collections.Generic;
using ImGuiNET;

public class ImGuiNode : Node2D
{
    [Export]
    DynamicFont Font = null;

    [Signal]
    public delegate void IGLayout();

    public virtual void Init(ImGuiIOPtr io)
    {
        if (Font is null)
        {
            io.Fonts.AddFontDefault();
        }
        else
        {
            ImGuiGD.AddFont(Font);
        }
        ImGuiGD.RebuildFontAtlas();
    }

    public virtual void Layout()
    {
        EmitSignal(nameof(IGLayout));
    }

    public override void _EnterTree()
    {
        ImGuiGD.Init(GetViewport());
        Init(ImGui.GetIO());
    }

    public override void _Process(float delta)
    {
        if (Visible)
        {
            ImGuiGD.Update(delta, GetViewport());
            Layout();
            ImGuiGD.Render(GetCanvasItem());
        }
    }

    public override void _Input(InputEvent evt)
    {
        if (Visible && ImGuiGD.ProcessInput(evt))
        {
            GetTree().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        ImGuiGD.Shutdown();
    }
}
