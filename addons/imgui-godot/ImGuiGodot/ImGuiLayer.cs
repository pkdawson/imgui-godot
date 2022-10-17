using Godot;
using System;
using System.Collections.Generic;

namespace ImGuiGodot;

public partial class ImGuiLayer : CanvasLayer
{
    public static ImGuiLayer Instance { get; private set; }

    [Export] public FontFile Font = null;
    [Export] public int FontSize = 16;
    [Export] public FontFile ExtraFont1 = null;
    [Export] public int ExtraFont1Size = 16;
    [Export] public FontFile ExtraFont2 = null;
    [Export] public int ExtraFont2Size = 16;
    [Export] public bool MergeFonts = true;
    [Export] public bool AddDefaultFont = true;

    /// <summary>
    /// Do NOT connect to this directly, please use <see cref="Connect"/> instead
    /// </summary>
    [Signal] public delegate void ImGuiLayoutEventHandler();

    private RID _canvasItem;
    private static SubViewport _subViewport;
    private static readonly HashSet<Godot.Object> _connectedObjects = new();

    private partial class UpdateFirst : Node
    {
        public override void _EnterTree()
        {
            Name = "ImGuiLayer_UpdateFirst";
            ProcessPriority = int.MinValue;
        }

        public override void _Process(double delta)
        {
            ImGuiGD.Update(delta, _subViewport);
        }
    }

    public override void _EnterTree()
    {
        Instance = this;

        CheckContentScale();

        ProcessPriority = int.MaxValue;
        _canvasItem = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasItem, GetCanvas());
        VisibilityChanged += OnChangeVisibility;

        ImGuiGD.Init();
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

        AddChild(new UpdateFirst());
    }

    public override void _Ready()
    {
        OnChangeVisibility();

        _subViewport = GetNode<SubViewport>("/root/ImGuiLayer/SubViewportContainer/SubViewport");
        _subViewport.GuiDisableInput = true;

        // TODO: cleanup
        RID canvas = RenderingServer.CanvasCreate();
        RenderingServer.ViewportAttachCanvas(_subViewport.GetViewportRid(), canvas);
        RenderingServer.CanvasItemSetParent(_canvasItem, canvas);

        Window window = (Window)GetViewport();
        _subViewport.SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowSizeChanged()
    {
        _subViewport.Size = ((Window)GetViewport()).Size;
    }

    public override void _ExitTree()
    {
        ImGuiGD.Shutdown();
        RenderingServer.FreeRid(_canvasItem);
    }

    private void OnChangeVisibility()
    {
        RenderingServer.CanvasItemSetVisible(_canvasItem, Visible);
        if (Visible)
        {
            ProcessMode = ProcessModeEnum.Always;
            SetProcessInput(true);
        }
        else
        {
            ProcessMode = ProcessModeEnum.Disabled;
            SetProcessInput(false);
            ImGuiGDInternal.ClearCanvasItems();
        }
    }

    public override void _Process(double delta)
    {
        EmitSignal(SignalName.ImGuiLayout);
        ImGuiGD.Render(_canvasItem);
    }

    public override void _Notification(long what)
    {
        ImGuiGDInternal.ProcessNotification(what);
    }

    public override void _Input(InputEvent e)
    {
        if (ImGuiGD.ProcessInput(e))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public static void Connect(ImGuiLayoutEventHandler d)
    {
        if (Instance != null)
        {
            Instance.ImGuiLayout += d;

            if (d.Target is Godot.Object obj)
            {
                if (_connectedObjects.Count == 0)
                {
                    Instance.GetTree().NodeRemoved += OnNodeRemoved;
                }
                _connectedObjects.Add(obj);
            }
        }
    }

    private static void OnNodeRemoved(Node node)
    {
        // signals declared in C# don't (yet?) work like normal Godot signals,
        // we need to clean up after removed Objects ourselves

        if (!_connectedObjects.Contains(node))
        {
            return;
        }
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

    private static void CheckContentScale()
    {
        Window window = (Window)Instance.GetViewport();
        switch (window.ContentScaleMode)
        {
            case Window.ContentScaleModeEnum.Disabled:
                break;
            case Window.ContentScaleModeEnum.CanvasItems:
                if (window.ContentScaleAspect != Window.ContentScaleAspectEnum.Expand)
                {
                    PrintErrContentScale(window);
                }
                break;
            case Window.ContentScaleModeEnum.Viewport:
                PrintErrContentScale(window);
                break;
        }
    }

    private static void PrintErrContentScale(Window window)
    {
        GD.PrintErr($"imgui-godot only supports content scale modes {Window.ContentScaleModeEnum.Disabled}" +
            $" or {Window.ContentScaleModeEnum.CanvasItems}/{Window.ContentScaleAspectEnum.Expand}");
        GD.PrintErr($"  current mode is {window.ContentScaleMode}/{window.ContentScaleAspect}");
    }
}
