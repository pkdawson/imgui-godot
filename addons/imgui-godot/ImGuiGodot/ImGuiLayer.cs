using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
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

    [Export] public FontFile Font = null;
    [Export] public int FontSize = 16;
    [Export] public FontFile ExtraFont1 = null;
    [Export] public int ExtraFont1Size = 16;
    [Export] public FontFile ExtraFont2 = null;
    [Export] public int ExtraFont2Size = 16;
    [Export] public bool MergeFonts = true;
    [Export] public bool AddDefaultFont = true;

    [Export(PropertyHint.Range, "0.25,4.0,or_greater")]
    public new float Scale = 1.0f;
    [Export] public bool ScaleToDpi = true;

    [Export] public string IniFilename = "user://imgui.ini";

    [Export(PropertyHint.Enum, "RenderingDevice,Canvas,Dummy")]
    public string Renderer = "RenderingDevice";

    /// <summary>
    /// Do NOT connect to this directly, please use <see cref="Connect"/> instead
    /// </summary>
    [Signal] public delegate void ImGuiLayoutEventHandler();

    private Window _window;
    private Rid _subViewportRid;
    private Vector2I _subViewportSize = Vector2I.Zero;
    private Rid _ci;
    private Transform2D _finalTransform = Transform2D.Identity;
    private UpdateFirst _updateFirst;
    private static readonly HashSet<GodotObject> _connectedObjects = new();
    private int sizeCheck = 0;
    private bool _headless = false;

    private sealed partial class UpdateFirst : Node
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
    }

    public override void _EnterTree()
    {
        //var ctx = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
        //ctx.Unloading += context =>
        //{
        //    GD.Print("unloading");
        //};

        Instance = this;
        _headless = DisplayServer.GetName() == "headless";
        _window = GetWindow();

        CheckContentScale();

        ProcessPriority = int.MaxValue;
        VisibilityChanged += OnChangeVisibility;

        ImGuiGD.ScaleToDpi = ScaleToDpi;
        ImGuiGD.Init(_window, Scale, _headless ? RendererType.Dummy : Enum.Parse<RendererType>(Renderer));
        ImGui.GetIO().SetIniFilename(IniFilename);
        if (Font is not null)
        {
            ImGuiGD.AddFont(Font, FontSize);
            if (ExtraFont1 is not null)
            {
                ImGuiGD.AddFont(ExtraFont1, ExtraFont1Size, MergeFonts);
            }
            if (ExtraFont2 is not null)
            {
                ImGuiGD.AddFont(ExtraFont2, ExtraFont2Size, MergeFonts);
            }
        }

        if (AddDefaultFont)
        {
            ImGuiGD.AddFontDefault();
        }
        ImGuiGD.RebuildFontAtlas();
        _subViewportRid = Internal.Util.AddLayerSubViewport(this);
        _ci = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_ci, GetCanvas());

        Internal.State.Instance.Renderer.InitViewport(_subViewportRid);

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
        ImGuiGD.Render(_subViewportRid);
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
