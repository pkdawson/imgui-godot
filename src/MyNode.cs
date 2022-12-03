using Godot;
using ImGuiNET;

namespace DemoProject;

public partial class MyNode : Node
{
    private Window _window;

    public override void _Ready()
    {
        _window = (Window)GetViewport();
        GetNode<Button>("../Button1").Pressed += OnButton1Pressed;
    }

    public override void _Process(double delta)
    {
        ImGui.ShowDemoWindow();
    }

    private void OnButton1Pressed()
    {
        GetTree().ChangeSceneToFile("res://data/demo2.tscn");
    }

    private void OnContentScaleCIE()
    {
        _window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        _window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
    }

    private void OnContentScaleDisabled()
    {
        _window.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
    }
}
