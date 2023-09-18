using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;

namespace ImGuiGodot;

public partial class ImGuiLayer : CanvasLayer
{
    public static ImGuiLayer Instance { get; private set; } = null!;

    [Export(PropertyHint.ResourceType, "ImGuiConfig")]
    public GodotObject Config = null!;

    /// <summary>
    /// Do NOT connect to this directly, please use <see cref="Connect"/> instead
    /// </summary>
    [Signal] public delegate void ImGuiLayoutEventHandler();

    private Window _window = null!;
    private Rid _subViewportRid;
    private Vector2I _subViewportSize = Vector2I.Zero;
    private Rid _ci;
    private Transform2D _finalTransform = Transform2D.Identity;
    private UpdateFirst _updateFirst = null!;
    private static readonly HashSet<GodotObject> _connectedObjects = new();
    private bool _headless = false;

    private sealed partial class UpdateFirst : Node
    {
        private uint _counter = 0;
        private ImGuiLayer _parent = null!;
        private Window _window = null!;

        public override void _Ready()
        {
            _parent = (ImGuiLayer)GetParent();
            _window = GetWindow();
            _parent.VisibilityChanged += OnChangeVisibility;
            OnChangeVisibility();
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
        VisibilityChanged += OnChangeVisibility;

        _subViewportRid = Internal.Util.AddLayerSubViewport(this);
        _ci = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_ci, GetCanvas());

        Resource cfg = Config as Resource ?? (Resource)((GDScript)GD.Load("res://addons/imgui-godot/scripts/ImGuiConfig.gd")).New();
        Layer = (int)cfg.Get("Layer");

        ImGuiGD.ScaleToDpi = (bool)cfg.Get("ScaleToDpi");
        ImGuiGD.Init(_window, _subViewportRid, (float)cfg.Get("Scale"),
            _headless ? RendererType.Dummy : Enum.Parse<RendererType>((string)cfg.Get("Renderer")));

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

        _updateFirst = new UpdateFirst
        {
            Name = "ImGuiLayer_UpdateFirst",
            ProcessPriority = int.MinValue,
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(_updateFirst);
    }

    public override void _Ready()
    {
        OnChangeVisibility();
    }

    public override void _ExitTree()
    {
        ImGuiGD.Shutdown();
        RenderingServer.FreeRid(_ci);
        RenderingServer.FreeRid(_subViewportRid);
    }

    private void OnChangeVisibility()
    {
        if (Visible)
        {
            ProcessMode = ProcessModeEnum.Always;
            // TODO: fix position with multiple monitors
            //foreach (Node child in GetChildren())
            //{
            //    if (child is Window w)
            //        w.Show();
            //}
        }
        else
        {
            ProcessMode = ProcessModeEnum.Disabled;
            Internal.State.Instance.Renderer.OnHide();
            _subViewportSize = Vector2I.Zero;
            RenderingServer.CanvasItemClear(_ci);
            //foreach (Node child in GetChildren())
            //{
            //    if (child is Window w)
            //        w.Hide();
            //}
        }
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

        EmitSignal(SignalName.ImGuiLayout);
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

    public static void Connect(ImGuiLayoutEventHandler d)
    {
        if (Instance is null)
            return;

        Instance.ImGuiLayout += d;

        if (d.Target is GodotObject obj)
        {
            if (_connectedObjects.Count == 0)
            {
                Instance.GetTree().NodeRemoved += OnNodeRemoved;
            }
            _connectedObjects.Add(obj);
        }
    }

    private static void OnNodeRemoved(Node node)
    {
        // signals declared in C# don't (yet?) work like normal Godot signals,
        // we need to clean up after removed Objects ourselves

        if (!_connectedObjects.Contains(node))
            return;

        _connectedObjects.Remove(node);

        // backing_ImGuiLayout is an implementation detail that could change
        foreach (Delegate d in Instance.backing_ImGuiLayout.GetInvocationList())
        {
            // remove ALL delegates with the removed Node as a target
            if (d.Target == node)
            {
                Instance.ImGuiLayout -= (ImGuiLayoutEventHandler)d;
            }
        }

        if (_connectedObjects.Count == 0)
        {
            Instance.GetTree().NodeRemoved -= OnNodeRemoved;
        }
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
}
