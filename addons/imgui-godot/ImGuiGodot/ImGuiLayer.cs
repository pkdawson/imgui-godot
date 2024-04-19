using Godot;
#if GODOT_PC

namespace ImGuiGodot;

public partial class ImGuiLayer : CanvasLayer
{
    public static ImGuiLayer Instance { get; private set; } = null!;

    private Window _window = null!;
    private Rid _subViewportRid;
    private Vector2I _subViewportSize = Vector2I.Zero;
    private Rid _canvasItem;
    private Transform2D _finalTransform = Transform2D.Identity;
    private ImGuiLayerHelper _helper = null!;
    private bool _visible = true;
    public Node Signaler { get; private set; } = null!;

    private sealed partial class ImGuiLayerHelper : Node
    {
        private Window _window = null!;

        public override void _Ready()
        {
            Name = "ImGuiLayerHelper";
            ProcessPriority = int.MinValue;
            ProcessMode = ProcessModeEnum.Always;
            _window = GetWindow();
        }

        public override void _Process(double delta)
        {
            Internal.State.Instance.Update(delta, _window.Size);
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
        _window = GetWindow();

        CheckContentScale();

        _subViewportRid = AddLayerSubViewport(this);
        _canvasItem = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasItem, GetCanvas());

        Node cfgScene = ResourceLoader.Load<PackedScene>("res://addons/imgui-godot/Config.tscn")
            .Instantiate();
        Resource cfg = (Resource)cfgScene.Get("Config") ?? (Resource)((GDScript)GD.Load(
            "res://addons/imgui-godot/scripts/ImGuiConfig.gd")).New();
        cfgScene.Free();

        Layer = (int)cfg.Get("Layer");

        Internal.State.Init(_window, _subViewportRid, cfg);

        _helper = new ImGuiLayerHelper();
        AddChild(_helper);

        Signaler = GetParent();
    }

    public override void _Ready()
    {
        ProcessPriority = int.MaxValue;
        VisibilityChanged += OnChangeVisibility;
        OnChangeVisibility();
    }

    public override void _ExitTree()
    {
        Internal.State.Instance.Dispose();
        RenderingServer.FreeRid(_canvasItem);
        RenderingServer.FreeRid(_subViewportRid);
    }

    private void OnChangeVisibility()
    {
        _visible = Visible;
        if (_visible)
        {
            SetProcessInput(true);
        }
        else
        {
            SetProcessInput(false);
            Internal.State.Instance.Renderer.OnHide();
            _subViewportSize = Vector2I.Zero;
            RenderingServer.CanvasItemClear(_canvasItem);
        }
    }

    public override void _Process(double delta)
    {
        if (_visible)
        {
            var winSize = _window.Size;
            var ft = _window.GetFinalTransform();
            if (_subViewportSize != winSize || _finalTransform != ft)
            {
                // this is more or less how SubViewportContainer works
                _subViewportSize = winSize;
                _finalTransform = ft;
                RenderingServer.ViewportSetSize(
                    _subViewportRid,
                    _subViewportSize.X,
                    _subViewportSize.Y);
                Rid vptex = RenderingServer.ViewportGetTexture(_subViewportRid);
                RenderingServer.CanvasItemClear(_canvasItem);
                RenderingServer.CanvasItemSetTransform(_canvasItem, ft.AffineInverse());
                RenderingServer.CanvasItemAddTextureRect(
                    _canvasItem,
                    new(0, 0, _subViewportSize.X, _subViewportSize.Y),
                    vptex);
            }

            Signaler.EmitSignal("imgui_layout");
        }
        Internal.State.Instance.Render();
    }

    public override void _Notification(int what)
    {
        Internal.Input.ProcessNotification(what);
    }

    public override void _Input(InputEvent @event)
    {
        if (Internal.State.Instance.ProcessInput(@event, _window))
        {
            _window.SetInputAsHandled();
        }
    }

    private static Rid AddLayerSubViewport(Node parent)
    {
        Rid svp = RenderingServer.ViewportCreate();
        RenderingServer.ViewportSetTransparentBackground(svp, true);
        RenderingServer.ViewportSetUpdateMode(svp, RenderingServer.ViewportUpdateMode.Always);
        RenderingServer.ViewportSetClearMode(svp, RenderingServer.ViewportClearMode.Always);
        RenderingServer.ViewportSetActive(svp, true);
        RenderingServer.ViewportSetParentViewport(svp, parent.GetWindow().GetViewportRid());
        return svp;
    }

    private void CheckContentScale()
    {
        if (_window.ContentScaleMode == Window.ContentScaleModeEnum.Viewport)
        {
            GD.PrintErr("imgui-godot: scale mode `viewport` is unsupported");
        }
    }
}
#else
namespace ImGuiNET
{
}

namespace ImGuiGodot
{
    public partial class ImGuiLayer : CanvasLayer
    {
        [Export(PropertyHint.ResourceType, "ImGuiConfig")]
        public GodotObject Config = null!;
    }
}
#endif
