using Godot;

namespace ImGuiGodot
{
    public partial class ImGuiLayer : CanvasLayer
    {
        public static ImGuiLayer Instance { get; private set; }

        [Export]
        public FontFile Font = null;

        [Export]
        public int FontSize = 16;

        [Export]
        public FontFile ExtraFont1 = null;

        [Export]
        public int ExtraFont1Size = 16;

        [Export]
        public FontFile ExtraFont2 = null;

        [Export]
        public int ExtraFont2Size = 16;

        [Export]
        public bool MergeFonts = true;

        [Export]
        public bool AddDefaultFont = true;

        [Signal]
        public delegate void ImGuiLayoutEventHandler();

        private RID _canvasItem;

        private partial class UpdateFirst : Node
        {
            public override void _EnterTree()
            {
                ProcessPriority = int.MinValue;
            }

            public override void _Process(double delta)
            {
                ImGuiGD.Update(delta, GetViewport());
            }
        }

        public override void _EnterTree()
        {
            Instance = this;

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

        public void Connect(ImGuiLayoutEventHandler d)
        {
            // temporary workaround for a bug in the early Godot 4 betas
            Connect(SignalName.ImGuiLayout, new Callable(d.Target as Object, d.Method.Name));

            // TODO: use this after Godot 4.0-beta3 is released
            // ImGuiLayout += d;
        }
    }
}
