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
#if IMGUI_GODOT_DEV
        ImGui.SetNextWindowPos(new(100, 100));
        ImGui.SetNextWindowSize(new(200, 200));
        ImGui.Begin("test");
        ImGui.Text("some text");
        ImGui.End();
#endif

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
