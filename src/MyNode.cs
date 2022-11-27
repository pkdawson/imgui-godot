using Godot;
using ImGuiNET;

public partial class MyNode : Node
{
    private Window _window;

    public override void _Ready()
    {
        _window = (Window)GetViewport();
    }

    public override void _Process(double delta)
    {
        ImGui.ShowDemoWindow();
    }

    private void _on_button1_pressed()
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
