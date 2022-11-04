using Godot;
using ImGuiNET;
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

    [Export(PropertyHint.Range, "0.25,4.0,or_greater")]
    public new float Scale = 1.0f;
    [Export] public bool ScaleToDpi = true;

    [Export] public string IniFilename = "user://imgui.ini";

    /// <summary>
    /// Do NOT connect to this directly, please use <see cref="Connect"/> instead
    /// </summary>
    [Signal] public delegate void ImGuiLayoutEventHandler();

    private RID _canvas;
    private RID _canvasItem;
    private Window _window;
    private SubViewportContainer _subViewportContainer;
    private SubViewport _subViewport;
    private static readonly HashSet<Godot.Object> _connectedObjects = new();

    private partial class UpdateFirst : Node
    {
        public Viewport GuiViewport { get; set; }

        public override void _Process(double delta)
        {
            ImGuiGD.Update(delta, GuiViewport);
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
        _window = (Window)GetViewport();

        CheckContentScale();

        ProcessPriority = int.MaxValue;
        VisibilityChanged += OnChangeVisibility;

        ImGuiGD.ScaleToDpi = ScaleToDpi;
        ImGuiGD.Init(Scale);
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

        _subViewportContainer = new SubViewportContainer
        {
            Name = "ImGuiLayer_SubViewportContainer",
            AnchorsPreset = 15,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Stretch = true
        };

        _subViewport = new SubViewport
        {
            Name = "ImGuiLayer_SubViewport",
            TransparentBg = true,
            HandleInputLocally = false,
            GuiDisableInput = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };

        _subViewportContainer.AddChild(_subViewport);

        _canvas = RenderingServer.CanvasCreate();
        _canvasItem = RenderingServer.CanvasItemCreate();
        RenderingServer.ViewportAttachCanvas(_subViewport.GetViewportRid(), _canvas);
        RenderingServer.CanvasItemSetParent(_canvasItem, _canvas);
        AddChild(_subViewportContainer);

        AddChild(new UpdateFirst
        {
            Name = "ImGuiLayer_UpdateFirst",
            ProcessPriority = int.MinValue,
            GuiViewport = _subViewport
        });

        _subViewport.SizeChanged += OnWindowSizeChanged;
    }

    public override void _Ready()
    {
        OnChangeVisibility();
    }

    private void OnWindowSizeChanged()
    {
        _subViewport.Size = _window.Size;
    }

    public override void _ExitTree()
    {
        ImGuiGD.Shutdown();
        RenderingServer.FreeRid(_canvasItem);
        RenderingServer.FreeRid(_canvas);
    }

    private void OnChangeVisibility()
    {
        RenderingServer.CanvasItemSetVisible(_canvasItem, Visible);
        if (Visible)
        {
            ProcessMode = ProcessModeEnum.Always;
            SetProcessInput(true);
            // TODO: show all windows
        }
        else
        {
            ProcessMode = ProcessModeEnum.Disabled;
            SetProcessInput(false);
            Internal.ClearCanvasItems();
            // TODO: hide all windows
        }
    }

    public override void _Process(double delta)
    {
        EmitSignal(SignalName.ImGuiLayout);
        ImGuiGD.Render(_canvasItem);
    }

    public override void _Notification(long what)
    {
        Internal.ProcessNotification(what);
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

    private void CheckContentScale()
    {
        switch (_window.ContentScaleMode)
        {
            case Window.ContentScaleModeEnum.Disabled:
                break;
            case Window.ContentScaleModeEnum.CanvasItems:
                if (_window.ContentScaleAspect != Window.ContentScaleAspectEnum.Expand)
                {
                    PrintErrContentScale();
                }
                break;
            case Window.ContentScaleModeEnum.Viewport:
                PrintErrContentScale();
                break;
        }
    }

    private void PrintErrContentScale()
    {
        GD.PrintErr($"imgui-godot only supports content scale modes {Window.ContentScaleModeEnum.Disabled}" +
            $" or {Window.ContentScaleModeEnum.CanvasItems}/{Window.ContentScaleAspectEnum.Expand}");
        GD.PrintErr($"  current mode is {_window.ContentScaleMode}/{_window.ContentScaleAspect}");
    }
}
