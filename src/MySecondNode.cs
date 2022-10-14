using Godot;
using ImGuiGodot;
using ImGuiNET;

public partial class MySecondNode : Node
{
    private Texture2D iconTexture;
    private SubViewport vp;
    private int iconSize = 64;
    private static bool fontLoaded = false;
    private static readonly ImGuiWindowFlags cswinflags = ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings |
        ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoMove;

    public override void _EnterTree()
    {
        if (!fontLoaded)
        {
            // it's easier to configure fonts in the ImGuiLayer scene,
            // but here's how it can be done in code

            ImGuiGD.Init();

            // use Hack for the default glyphs, M+2 for Japanese
            ImGuiGD.AddFont(GD.Load<FontFile>("res://data/Hack-Regular.ttf"), 18.0f);
            ImGuiGD.AddFont(GD.Load<FontFile>("res://data/MPLUS2-Regular.ttf"), 22.0f, merge: true);

            ImGuiGD.AddFontDefault();
            ImGuiGD.RebuildFontAtlas();
            fontLoaded = true;
        }

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
    }

    public override void _Ready()
    {
        ImGuiLayer.Connect(_ImGuiLayout);
        iconTexture = GD.Load<Texture2D>("res://data/icon.svg");
        vp = GetNode<SubViewport>("%SubViewport");
    }

    public override void _ExitTree()
    {
        // call UnbindTexture when you're done to free a few bytes of memory
        ImGuiGD.UnbindTexture(iconTexture);
        ImGuiGD.UnbindTexture(vp.GetTexture());
    }

    private void _ImGuiLayout()
    {
        ImGui.ShowDemoWindow();

        ImGui.SetNextWindowPos(new(10, 10));
        ImGui.Begin("change scene window", cswinflags);
        if (ImGui.Button("change scene"))
        {
            GetTree().ChangeSceneToFile("res://data/demo.tscn");
            // return so we don't try to draw a viewport texture after it's deleted
            return;
        }
        ImGui.End();

        ImGui.SetNextWindowPos(new(20, 400), ImGuiCond.Once);
        ImGui.Begin("Scene 2");
        ImGui.Text("hello Godot 4");

        ImGui.Separator();
        ImGui.Text("Simple texture");
        Widgets.Image(iconTexture, new(iconSize, iconSize));
        ImGui.DragInt("size", ref iconSize, 1.0f, 32, 512);

        ImGui.Separator();
        ImGui.Text("Unicode");
        ImGui.Text("Hiragana: こんばんは");
        ImGui.Text("Katakana: ハロウィーン");
        ImGui.Text("   Kanji: 日本語");
        ImGui.End();

        ImGui.SetNextWindowPos(new(20, 60), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new(400, 300), ImGuiCond.Once);
        if (ImGui.Begin("SubViewport (press R to reset)"))
        {
            var size = ImGui.GetContentRegionAvail();
            if (size.X > 5 && size.Y > 5)
            {
                vp.Size = new((int)size.X - 5, (int)size.Y - 5);
                Widgets.SubViewport(vp);
            }
        }
        ImGui.End();
    }

    private void _on_show_hide()
    {
        ImGuiLayer.Instance.Visible = !ImGuiLayer.Instance.Visible;
        GetNode<Button>("%ShowHideButton").Text = ImGuiLayer.Instance.Visible ? "hide" : "show";
    }
}
