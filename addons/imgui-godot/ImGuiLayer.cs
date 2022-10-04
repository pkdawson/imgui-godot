using Godot;

public partial class ImGuiLayer : CanvasLayer
{
    public static ImGuiLayer Instance { get; private set; }

    [Export]
    public FontFile Font = null;

    [Export]
    public int FontSize = 16;

    [Export]
    public FontFile ExtraFont = null;

    [Export]
    public int ExtraFontSize = 16;

    [Export]
    public bool MergeFonts = true;

    [Export]
    public bool AddDefaultFont = true;

    private RID _canvasItem;

    private partial class RenderLast : Node
    {
        private RID _ci;

        public RenderLast(RID ci)
        {
            _ci = ci;
        }

        public override void _EnterTree()
        {
            ProcessPriority = int.MaxValue;
        }

        public override void _Process(double delta)
        {
            ImGuiGD.Render(_ci);
        }
    }


    public override void _EnterTree()
    {
        Instance = this;

        ProcessPriority = int.MinValue;
        _canvasItem = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasItem, GetCanvas());
        VisibilityChanged += OnChangeVisibility;

        ImGuiGD.Init();
        if (Font is not null)
        {
            ImGuiGD.AddFont(Font, FontSize);
            if (ExtraFont is not null)
            {
                ImGuiGD.AddFont(ExtraFont, ExtraFontSize, MergeFonts);
            }
        }

        if (AddDefaultFont)
        {
            ImGuiGD.AddFontDefault();
        }
        ImGuiGD.RebuildFontAtlas();

        OnChangeVisibility();

        AddChild(new RenderLast(_canvasItem));
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
        ImGuiGD.Update(delta, GetViewport());
    }

    public override void _Input(InputEvent e)
    {
        if (ImGuiGD.ProcessInput(e))
        {
            GetViewport().SetInputAsHandled();
        }
    }
}
