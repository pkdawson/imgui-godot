using Godot;
using ImGuiGodot;
using ImGuiNET;

namespace DemoProject;

public partial class MySecondNode : Node
{
    private Texture2D iconTexture;
    private SubViewport vp;
    private int iconSize = 64;
    private float scale;
    private ImFontPtr proggy;
    private ColorRect background;
    private int numClicks = 0;

    private static bool fontLoaded = false;
    private static System.Numerics.Vector4 myTextColor = Colors.Aquamarine.ToVector4();

    private static readonly ImGuiWindowFlags cswinflags =
        ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.AlwaysAutoResize |
        ImGuiWindowFlags.NoSavedSettings |
        ImGuiWindowFlags.NoFocusOnAppearing |
        ImGuiWindowFlags.NoNav |
        ImGuiWindowFlags.NoMove;

    private static readonly string versionString =
        $"Godot {Engine.GetVersionInfo()["string"].AsString()} with .NET " +
        $"{System.Environment.Version}";

    public override void _EnterTree()
    {
        if (!fontLoaded)
        {
            // it's easier to configure fonts in the ImGuiLayer scene,
            // but here's how it can be done in code

            ImGuiGD.ResetFonts();

            // use Hack for the default glyphs, M+2 for Japanese
            ImGuiGD.AddFont(GD.Load<FontFile>("res://data/Hack-Regular.ttf"), 18);
            ImGuiGD.AddFont(GD.Load<FontFile>("res://data/MPLUS2-Regular.ttf"), 22,
                merge: true);

            ImGuiGD.AddFontDefault();
            ImGuiGD.RebuildFontAtlas();
            fontLoaded = true;
        }

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
        proggy = io.Fonts.Fonts[1];
        background = GetNode<ColorRect>("/root/Background");
    }

    public override void _Ready()
    {
        ImGuiGD.Connect(OnImGuiLayout);
        iconTexture = GD.Load<Texture2D>("res://data/icon.svg");
        vp = GetNode<SubViewport>("%SubViewport");
        scale = ImGuiGD.Scale;
        GetNode<Button>("%ShowHideButton").Pressed += OnShowHidePressed;
    }

    private void OnImGuiLayout()
    {
        ImGui.ShowDemoWindow();

        float fh = ImGui.GetFrameHeight();

        var mainVpPos = ImGui.GetMainViewport().WorkPos;

        ImGui.SetNextWindowPos(new(mainVpPos.X + 10, mainVpPos.Y + 10));
        ImGui.Begin("change scene window", cswinflags);

        if (ImGui.Button("change scene"))
            GetTree().ChangeSceneToFile("res://data/demo.tscn");

        ImGui.End();

        ImGui.SetNextWindowPos(new(fh, ImGui.GetIO().DisplaySize.Y - (11 * fh) - 5.0f),
            ImGuiCond.Once);

        ImGui.SetNextWindowSize(new(18 * fh, 11 * fh), ImGuiCond.Once);

        if (ImGui.Begin("SubViewport (press R to reset)"))
        {
            var size = ImGui.GetContentRegionAvail();
            if (size.X > 5 && size.Y > 5)
            {
                vp.CallDeferred(SubViewport.MethodName.SetSize,
                    new Vector2I((int)size.X - 5, (int)size.Y - 5));

                Widgets.SubViewport(vp);
            }
        }

        ImGui.End();

        ImGui.SetNextWindowPos(new(fh, 3 * fh), ImGuiCond.Once);
        ImGui.Begin("Scene 2", ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.PushFont(proggy);
        ImGui.TextColored(myTextColor, versionString);
        ImGui.PopFont();

        ImGui.Separator();
        ImGui.Text("Simple texture");
        Widgets.Image(iconTexture, new(iconSize, iconSize));
        ImGui.DragInt("size", ref iconSize, 1.0f, 32, 512);

        ImGui.Separator();
        ImGui.Text("ImageButton");

        if (Widgets.ImageButton("myimgbtn", iconTexture, new(128, 128)))
            ++numClicks;

        ImGui.SameLine();
        ImGui.Text($"{numClicks}");

        ImGui.Separator();
        ImGui.Text("Unicode");
        ImGui.Text("Hiragana: こんばんは");
        ImGui.Text("Katakana: ハロウィーン");
        ImGui.Text("   Kanji: 日本語");

        ImGui.Separator();
        ImGui.Text("GUI scale");

        for (int i = 0; i < 6; ++i)
        {
            float s = 0.75f + (i * 0.25f);
            if (ImGui.RadioButton($"{s:0.00}", scale == s))
            {
                scale = s;
                CallDeferred("OnScaleChanged");
            }

            if (i < 5) ImGui.SameLine();
        }

        ImGui.Separator();
        var col = background.Color.ToVector3();

        if (ImGui.ColorEdit3("background color", ref col))
            background.Color = col.ToColor();

        ImGui.End();
    }

    private void OnShowHidePressed()
    {
        ImGuiGD.Visible = !ImGuiGD.Visible;
        GetNode<Button>("%ShowHideButton").Text = ImGuiGD.Visible
            ? "hide" : "show";
    }

    private void OnScaleChanged()
    {
        ImGuiGD.Scale = scale;

        // old font pointers are invalid after changing scale
        proggy = ImGui.GetIO().Fonts.Fonts[1];
    }
}
