using Godot;
using ImGuiGodot.Internal;
using ImGuiNET;
using System;
#if IMGUI_GODOT_DEV
using System.Runtime.InteropServices;
#endif

namespace ImGuiGodot;

public partial class ImGuiLayer : CanvasLayer
{
    public static ImGuiLayer Instance { get; private set; }

#if IMGUI_GODOT_DEV
    public ImGuiAPI API = new();
#endif

    [Export(PropertyHint.ResourceType, "ImGuiConfig")]
    public GodotObject Config = null;

    private Window _window;
    private Rid _subViewportRid;
    private Vector2I _subViewportSize = Vector2I.Zero;
    private Rid _ci;
    private Transform2D _finalTransform = Transform2D.Identity;
    private ImGuiHelper _helper;
    private bool _useNative = ProjectSettings.HasSetting("autoload/imgui_godot_native");
    public Node Signaler { get; private set; }

    private sealed partial class ImGuiHelper : Node
    {
        private uint _counter = 0;
        private ImGuiLayer _parent;
        private Window _window;

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

        public override void _Notification(int what)
        {
            Internal.Input.ProcessNotification(what);
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
        _window = GetWindow();

        // check for imgui-godot-native
        if (_useNative)
        {
            ImGuiGD.SyncImGuiPtrs();
            Instance = null;
            QueueFree();
            return;
        }

        CheckContentScale();

        ProcessPriority = int.MaxValue;
        VisibilityChanged += OnChangeVisibility;

        _subViewportRid = Util.AddLayerSubViewport(this);
        _ci = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_ci, GetCanvas());

        Resource cfg = Config as Resource ?? (Resource)((GDScript)GD.Load("res://addons/imgui-godot/scripts/ImGuiConfig.gd")).New();
        Layer = (int)cfg.Get("Layer");

        ImGuiGD.Init(_window, _subViewportRid, cfg);

        _helper = new ImGuiHelper
        {
            Name = "ImGuiHelper",
            ProcessPriority = int.MinValue,
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(_helper);

        Signaler = (Node)((GDScript)GD.Load("res://addons/imgui-godot/scripts/ImGuiSignaler.gd")).New();
        AddChild(Signaler);
    }

    public override void _Ready()
    {
        if (_useNative)
        {
            SetProcess(false);
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            SetProcessUnhandledKeyInput(false);
            return;
        }
        OnChangeVisibility();
    }

    public override void _ExitTree()
    {
        if (_useNative) return;
        ImGuiGD.Shutdown();
        RenderingServer.FreeRid(_ci);
        RenderingServer.FreeRid(_subViewportRid);
#if IMGUI_GODOT_DEV
        API.Free();
#endif
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
            State.Instance.Renderer.OnHide();
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

        Instance.Signaler.EmitSignal("imgui_layout");
        ImGuiGD.Render();
    }

    public override void _Input(InputEvent e)
    {
        if (ImGuiGD.ProcessInput(e, _window))
        {
            _window.SetInputAsHandled();
        }
    }

    [Obsolete("use ImGuiGD.Connect instead")]
    public static void Connect(Action d)
    {
        ImGuiGD.Connect(d);
    }

#if IMGUI_GODOT_DEV
#pragma warning disable CA1822 // Mark members as static
    // WIP, this will probably be changed or moved
    public long[] GetImGuiPtrs(string version, int ioSize, int vertSize, int idxSize)
    {
        if (version != ImGui.GetVersion() ||
            ioSize != Marshal.SizeOf<ImGuiIO>() ||
            vertSize != Marshal.SizeOf<ImDrawVert>() ||
            idxSize != sizeof(ushort))
        {
            throw new PlatformNotSupportedException($"ImGui version mismatch, use {ImGui.GetVersion()}-docking");
        }

        IntPtr mem_alloc = IntPtr.Zero;
        IntPtr mem_free = IntPtr.Zero;
        unsafe
        {
            void* user_data = null;
            ImGui.GetAllocatorFunctions(ref mem_alloc, ref mem_free, ref user_data);
        }

        return new[] {
            (long)ImGui.GetCurrentContext(),
            (long)mem_alloc,
            (long)mem_free
        };
    }
#pragma warning restore CA1822 // Mark members as static
#endif

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
