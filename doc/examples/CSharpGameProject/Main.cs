using Godot;
using ImGuiNET;

namespace CSharpGameProject;

public partial class Main : Node
{
    public override void _Ready()
    {
        if (DisplayServer.WindowGetVsyncMode() == DisplayServer.VSyncMode.Disabled)
        {
            int refreshRate = (int)DisplayServer.ScreenGetRefreshRate();
            Engine.MaxFps = refreshRate > 0 ? refreshRate : 60;
        }

#if IMGUI
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
#endif
    }
}
