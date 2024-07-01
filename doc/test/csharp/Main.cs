using Godot;
using System;
using Xunit;
using ImGuiGodot;
using ImGuiNET;

namespace test;

public partial class Main : Node
{
    private int _frame = 0;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        try
        {
            if (_frame == 0)
                FrameZero();
            else
                FrameOne();
        }
        catch (Exception e)
        {
            GD.Print(e);
            GetTree().Quit(1);
        }
        _frame++;
    }

    private void FrameZero()
    {
        Assert.Equal(ImGuiGD.Scale, 2);
        Assert.Equal(ImGui.GetFontSize(), 26.0f);

        CallDeferred(nameof(ChangeScale));
    }

    private void FrameOne()
    {
        Assert.Equal(ImGuiGD.Scale, 4);
        Assert.Equal(ImGui.GetFontSize(), 52.0f);

        GD.Print("All tests passed.");
        GetTree().Quit(0);
    }

    private static void ChangeScale()
    {
        ImGuiGD.Scale = 4;
    }
}
