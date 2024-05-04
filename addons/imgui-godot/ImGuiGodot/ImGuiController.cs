#if GODOT_PC
using Godot;
using ImGuiGodot.Internal;
using ImGuiNET;

namespace ImGuiGodot;

public partial class ImGuiController : Node
{
    private Window _window = null!;
    public static ImGuiController Instance { get; private set; } = null!;
    private ImGuiControllerHelper _helper = null!;
    public Node Signaler { get; private set; } = null!;

    private sealed partial class ImGuiControllerHelper : Node
    {
        public override void _Ready()
        {
            Name = "ImGuiControllerHelper";
            ProcessPriority = int.MinValue;
            ProcessMode = ProcessModeEnum.Always;
        }

        public override void _Process(double delta)
        {
            Vector2I vpSize = ImGuiLayer.Instance!.ViewportSize;
            State.Instance.Update(delta, vpSize.ToImVec2());
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
        _window = GetWindow();

        CheckContentScale();

        Node cfgScene = ResourceLoader.Load<PackedScene>("res://addons/imgui-godot/Config.tscn")
            .Instantiate();
        Resource cfg = (Resource)cfgScene.Get("Config") ?? (Resource)((GDScript)GD.Load(
            "res://addons/imgui-godot/scripts/ImGuiConfig.gd")).New();
        cfgScene.Free();

        State.Init(cfg);

        _helper = new ImGuiControllerHelper();
        AddChild(_helper);

        Signaler = GetParent();
        AddChild(new ImGuiLayer());
    }

    public override void _Ready()
    {
        ProcessPriority = int.MaxValue;
    }

    public override void _ExitTree()
    {
        State.Instance.Dispose();
    }

    public override void _Process(double delta)
    {
        ImGuiLayer.Instance!.UpdateViewport();
        Signaler.EmitSignal("imgui_layout");
        State.Instance.Render();
    }

    public override void _Notification(int what)
    {
        Internal.Input.ProcessNotification(what);
    }

    public void SetMainViewport(Viewport vp)
    {
        ImGuiLayer? oldLayer = ImGuiLayer.Instance;
        ImGuiLayer.Instance = null;
        oldLayer?.Free();

        if (vp is Window window)
        {
            State.Instance.Input = new Internal.Input();
            if (window == _window)
                AddChild(new ImGuiLayer());
            else
                window.AddChild(new ImGuiLayer());
            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        }
        else if (vp is SubViewport svp)
        {
            State.Instance.Input = new InputLocal();
            svp.AddChild(new ImGuiLayer());
            ImGui.GetIO().BackendFlags &= ~ImGuiBackendFlags.PlatformHasViewports;
        }
        else
        {
            throw new System.ArgumentException("secret third kind of viewport??", nameof(vp));
        }
    }

    private void CheckContentScale()
    {
        if (_window.ContentScaleMode == Window.ContentScaleModeEnum.Viewport)
        {
            GD.PrintErr("imgui-godot: scale mode `viewport` is unsupported");
        }
    }
}
#endif
