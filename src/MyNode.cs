using Godot;
using ImGuiNET;

namespace DemoProject;

public partial class MyNode : Node
{
    private Window _window = null!;

    public override void _Ready()
    {
        _window = GetWindow();
        GetNode<Button>("../Button1").Pressed += OnButton1Pressed;

        if (DisplayServer.WindowGetVsyncMode() == DisplayServer.VSyncMode.Disabled)
        {
            int refreshRate = (int)DisplayServer.ScreenGetRefreshRate();
            Engine.MaxFps = refreshRate > 0 ? refreshRate : 60;
        }
    }

    public override void _Process(double delta)
    {
#if GODOT_PC
        ImGui.ShowDemoWindow();
#endif
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
