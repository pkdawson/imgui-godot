using Godot;
#if GODOT_PC
using ImGuiNET;
using System;


namespace ImGuiGodot;

public partial class ImGuiLayer : CanvasLayer, ISerializationListener
{
    public static ImGuiLayer Instance { get; internal set; } = null!;

    [Export(PropertyHint.ResourceType, "ImGuiConfig")]
    public GodotObject Config = null!;

    private Window _window = null!;
    private Rid _subViewportRid;
    private Vector2I _subViewportSize = Vector2I.Zero;
    private Rid _ci;
    private Transform2D _finalTransform = Transform2D.Identity;
    private WeakReference< UpdateFirst > _updateFirst = null!;
    private bool _headless = false;
    private Callable _onChangeVisibilityCallback;

    public Node Signaler { get; private set; } = null!;

    private sealed partial class UpdateFirst : Node
    {
        private uint _counter = 0;
        private ImGuiLayer _parent = null!;
        private Window _window = null!;
        private Callable _onChangeVisibilityCallback;

        public override void _Ready()
        {
            _parent = (ImGuiLayer)GetParent();
            _window = GetWindow();
            _onChangeVisibilityCallback = new Callable( this, UpdateFirst.MethodName.OnChangeVisibility );
            _parent.Connect( ImGuiLayer.SignalName.VisibilityChanged, _onChangeVisibilityCallback );
            OnChangeVisibility();
        }

        public override void _ExitTree() {
            if ( _parent is not null ) {
                _parent.Disconnect( ImGuiLayer.SignalName.VisibilityChanged, _onChangeVisibilityCallback );
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            // call NewFrame occasionally if GUI isn't visible, to prevent leaks
            if (unchecked(_counter++) % 60 == 0)
                ImGui.NewFrame();
        }

        public override void _Process(double delta)
        {
            ImGuiGD.Update(delta, _window.Size);
        }

        private void OnChangeVisibility()
        {
            _counter = 0;
            bool vis = _parent.Visible;
            SetProcess(vis);
            SetPhysicsProcess(!vis);
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
        _headless = DisplayServer.GetName() == "headless";
        _window = GetWindow();

        CheckContentScale();

        ProcessPriority = int.MaxValue;
        if ( !Engine.IsEditorHint() ) {
            _onChangeVisibilityCallback = new Callable( this, ImGuiLayer.MethodName.OnChangeVisibility );
            Connect( ImGuiLayer.SignalName.VisibilityChanged, _onChangeVisibilityCallback );
        }

        _subViewportRid = Internal.Util.AddLayerSubViewport(this);
        _ci = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_ci, GetCanvas());

        Resource cfg = Config as Resource ?? (Resource)((GDScript)GD.Load("res://addons/imgui-godot/scripts/ImGuiConfig.gd")).New();
        Layer = (int)cfg.Get("Layer");

        ImGuiGD.ScaleToDpi = (bool)cfg.Get("ScaleToDpi");
        ImGuiGD.Init(_window, _subViewportRid, (float)cfg.Get("Scale"),
            _headless ? RendererType.Dummy : Enum.Parse<RendererType>((string)cfg.Get("Renderer")));

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        ImGui.GetIO().SetIniFilename((string)cfg.Get("IniFilename"));

        var fonts = (Godot.Collections.Array)cfg.Get("Fonts");
        bool merge = (bool)cfg.Get("MergeFonts");

        for (int i = 0; i < fonts.Count; ++i)
        {
            var fontres = (Resource)fonts[i];
            var font = (FontFile)fontres.Get("FontData");
            int fontSize = (int)fontres.Get("FontSize");
            if (i == 0)
                ImGuiGD.AddFont(font, fontSize);
            else
                ImGuiGD.AddFont(font, fontSize, merge);
        }
        if ((bool)cfg.Get("AddDefaultFont"))
        {
            ImGuiGD.AddFontDefault();
        }
        ImGuiGD.RebuildFontAtlas();

        var updateFirst = new UpdateFirst {
            Name = "ImGuiLayer_UpdateFirst", ProcessPriority = int.MinValue, ProcessMode = ProcessModeEnum.Always
        };
        _updateFirst = new WeakReference< UpdateFirst >( updateFirst );
        AddChild(updateFirst);

        Signaler = (Node)((GDScript)GD.Load("res://addons/imgui-godot/scripts/ImGuiSignaler.gd")).New();
        Signaler.Name = "Signaler";
        AddChild(Signaler);
    }

    public override void _Ready()
    {
        OnChangeVisibility();
    }

    public override void _ExitTree()
    {
        if ( !Engine.IsEditorHint() ) {
            Disconnect( ImGuiLayer.SignalName.VisibilityChanged, _onChangeVisibilityCallback );
        }

        ImGuiGD.Shutdown();
        RenderingServer.FreeRid(_ci);
        RenderingServer.FreeRid(_subViewportRid);

        if ( Instance == this ) {
            Instance = null;
        }
    }

    private void OnChangeVisibility()
    {
        if (Visible)
        {
            ProcessMode = ProcessModeEnum.Always;
        }
        else
        {
            ProcessMode = ProcessModeEnum.Disabled;
            Internal.State.Instance.Renderer.OnHide();
            _subViewportSize = Vector2I.Zero;
            RenderingServer.CanvasItemClear(_ci);
            CallDeferred(nameof(FinishHide));
        }
    }

    private static void FinishHide()
    {
        ImGui.NewFrame();
        ImGuiGD.Render();
    }

    public override void _Process(double delta)
    {
        var winSize = _window.Size;
        var ft = _window.GetFinalTransform();
        if (_subViewportSize != winSize || _finalTransform != ft)
        {
            // this is more or less how SubViewportContainer works
            _subViewportSize = winSize;
            _finalTransform = ft;
            RenderingServer.ViewportSetSize(_subViewportRid, _subViewportSize.X, _subViewportSize.Y);
            Rid vptex = RenderingServer.ViewportGetTexture(_subViewportRid);
            RenderingServer.CanvasItemClear(_ci);
            RenderingServer.CanvasItemSetTransform(_ci, ft.AffineInverse());
            RenderingServer.CanvasItemAddTextureRect(_ci, new(0, 0, _subViewportSize.X, _subViewportSize.Y), vptex);
        }

        Signaler.EmitSignal("imgui_layout");
        ImGuiGD.Render();
    }

    public override void _Notification(int what)
    {
        Internal.Input.ProcessNotification(what);
    }

    public override void _Input(InputEvent e)
    {
        if (ImGuiGD.ProcessInput(e, _window))
        {
            _window.SetInputAsHandled();
        }
    }

    public static void Connect(Callable callable)
    {
        Instance?.Signaler.Connect("imgui_layout", callable);
    }

    public static void Disconnect( Callable callable ) {
        Instance?.Signaler?.Disconnect( "imgui_layout", callable );
    }

    private void CheckContentScale()
    {
        switch (_window.ContentScaleMode)
        {
            case Window.ContentScaleModeEnum.Disabled:
            case Window.ContentScaleModeEnum.CanvasItems:
                break;
            case Window.ContentScaleModeEnum.Viewport:
                PrintErrContentScale();
                break;
        }
    }

    private void PrintErrContentScale()
    {
        GD.PrintErr($"imgui-godot only supports content scale modes {Window.ContentScaleModeEnum.Disabled}" +
            $" or {Window.ContentScaleModeEnum.CanvasItems}");
        GD.PrintErr($"  current mode is {_window.ContentScaleMode}/{_window.ContentScaleAspect}");
    }

    public void OnBeforeSerialize() {
        // Logger.Debug( Environment.StackTrace );
        // VisibilityChanged -= OnChangeVisibility;

        // if ( _updateFirst != null && _updateFirst.TryGetTarget( out var updateFirst ) ) {
        //     RemoveChild( updateFirst );
        //     updateFirst.Free();
        //     _updateFirst = null;
        // }
        //
        // if ( Signaler != null ) {
        //     RemoveChild( Signaler );
        //     Signaler.Free();
        //     Signaler = null;
        // }
    }

    public void OnAfterDeserialize() {

        Instance = this;

        var updateFirst = new UpdateFirst {
            Name = "ImGuiLayer_UpdateFirst", ProcessPriority = int.MinValue, ProcessMode = ProcessModeEnum.Always
        };
        _updateFirst = new WeakReference< UpdateFirst >( updateFirst );
        AddChild( updateFirst );

        Signaler = ( Node )( ( GDScript )GD.Load( "res://addons/imgui-godot/scripts/ImGuiSignaler.gd" ) ).New();
        Signaler.Name = "Signaler";
        AddChild( Signaler );
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
