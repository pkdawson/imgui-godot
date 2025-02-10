using Godot;
using System;
using Xunit;
using ImGuiGodot;
using ImGuiNET;

namespace test;

public partial class Main : Node
{
    [Signal]
    public delegate void WithinProcessEventHandler();

    public override async void _Ready()
    {
        try
        {
            var io = ImGui.GetIO();
            io.SetIniFilename("");

            await ToSignal(this, SignalName.WithinProcess);

            Assert.Equal(ImGuiGD.Scale, 2);
            Assert.Equal(ImGui.GetFontSize(), 26.0f);

            // just for comparison with GDScript test
            Assert.Equal(ImGui.GetID("test_id"), 3584119329u);

            CallDeferred(nameof(ChangeScale));

            await ToSignal(this, SignalName.WithinProcess);

            Assert.Equal(ImGuiGD.Scale, 4);
            Assert.Equal(ImGui.GetFontSize(), 52.0f);

            // IniSavingRate
            GetTree().CreateTimer(5.1).Timeout += OnTimeout;
        }
        catch (Exception e)
        {
            GD.Print(e);
            GetTree().Quit(1);
        }
    }

    public async void OnTimeout()
    {
        try
        {
            await ToSignal(this, SignalName.WithinProcess);

            Assert.False(FileAccess.FileExists("user://imgui.ini"));

            GD.Print("All tests passed.");
            GetTree().Quit(0);
        }
        catch (Exception e)
        {
            GD.Print(e);
            GetTree().Quit(1);
        }
    }

    public override void _Process(double delta)
    {
        EmitSignal(SignalName.WithinProcess);
    }

    private static void ChangeScale()
    {
        ImGuiGD.Scale = 4;
    }
}
