using Godot;
using ImGuiGodot.Internal;
#if GODOT_PC

namespace ImGuiGodot;

public partial class ImGuiLayer : CanvasLayer
{
    public static ImGuiLayer? Instance { get; set; }

    private Rid _subViewportRid;
    private Vector2I _subViewportSize = Vector2I.Zero;
    private Rid _canvasItem;
    private Transform2D _finalTransform = Transform2D.Identity;
    private bool _visible = true;
    private Viewport _parentViewport = null!;
    public Vector2I ViewportSize { get; private set; }

    public override void _EnterTree()
    {
        if (Instance != null)
            throw new System.InvalidOperationException();
        Instance = this;

        Name = "ImGuiLayer";
        Layer = State.Instance.Layer;

        _parentViewport = GetViewport();
        _subViewportRid = AddLayerSubViewport(this);
        _canvasItem = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasItem, GetCanvas());

        State.Instance.Renderer.InitViewport(_subViewportRid);
        State.Instance.Viewports.SetMainViewport(_parentViewport, _subViewportRid);
    }

    public override void _Ready()
    {
        VisibilityChanged += OnChangeVisibility;
        OnChangeVisibility();
    }

    public override void _ExitTree()
    {
        RenderingServer.FreeRid(_canvasItem);
        RenderingServer.FreeRid(_subViewportRid);

        if (Instance == this)
        {
            Instance = null;
            Window mainWindow = ImGuiController.Instance.GetWindow();
            if (_parentViewport != mainWindow)
                ImGuiController.Instance.SetMainViewport(mainWindow);
        }
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
            State.Instance.Renderer.OnHide();
            _subViewportSize = Vector2I.Zero;
            RenderingServer.CanvasItemClear(_canvasItem);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (State.Instance.ProcessInput(@event))
        {
            _parentViewport.SetInputAsHandled();
        }
    }

    public void UpdateViewport()
    {
        if (_visible)
        {
#pragma warning disable IDE0045 // Convert to conditional expression
            if (_parentViewport is Window w)
                ViewportSize = w.Size;
            else if (_parentViewport is SubViewport svp)
                ViewportSize = svp.Size;
            else
                throw new System.InvalidOperationException();
#pragma warning restore IDE0045 // Convert to conditional expression

            var ft = _parentViewport.GetFinalTransform();
            if (_subViewportSize != ViewportSize || _finalTransform != ft)
            {
                // this is more or less how SubViewportContainer works
                _subViewportSize = ViewportSize;
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
}
#else
namespace ImGuiNET
{
}

namespace ImGuiGodot
{
}
#endif
