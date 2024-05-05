#if GODOT_PC
#nullable enable
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
            State.Instance.InProcessFrame = true;
            State.Instance.Update(delta, State.Instance.ViewportSize.ToImVec2());
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
        SetMainViewport(_window);
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
        State.Instance.Layer.UpdateViewport();
        Signaler.EmitSignal("imgui_layout");
        State.Instance.Render();
        State.Instance.InProcessFrame = false;
    }

    public override void _Notification(int what)
    {
        Internal.Input.ProcessNotification(what);
    }

    public void OnLayerExiting()
    {
        // an ImGuiLayer is being destroyed without calling SetMainViewport
        if (State.Instance.Layer.GetViewport() != _window)
        {
            // revert to main window
            SetMainViewport(_window);
        }
    }

    public void SetMainViewport(Viewport vp)
    {
        ImGuiLayer? oldLayer = State.Instance.Layer;
        if (oldLayer != null)
        {
            oldLayer.TreeExiting -= OnLayerExiting;
            oldLayer.QueueFree();
        }

        var newLayer = new ImGuiLayer();
        newLayer.TreeExiting += OnLayerExiting;

        if (vp is Window window)
        {
            State.Instance.Input = new Internal.Input();
            if (window == _window)
                AddChild(newLayer);
            else
                window.AddChild(newLayer);
            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        }
        else if (vp is SubViewport svp)
        {
            State.Instance.Input = new InputLocal();
            svp.AddChild(newLayer);
            ImGui.GetIO().BackendFlags &= ~ImGuiBackendFlags.PlatformHasViewports;
        }
        else
        {
            throw new System.ArgumentException("secret third kind of viewport??", nameof(vp));
        }
        State.Instance.Layer = newLayer;
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
