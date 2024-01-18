using Godot;
using ImGuiGodot;
using ImGuiNET;

namespace DemoProject;

public partial class MySecondNode : Node
{
#if GODOT_PC
    private Texture2D _iconTexture = null!;
    private AtlasTexture _atlasTexture = null!;
    private SubViewport _vp = null!;
    private int _iconSize = 64;
    private float _scale;
    private ImFontPtr _proggy;
    private ColorRect _background = null!;
    private int _numClicks = 0;

    private static bool _fontLoaded = false;
    private static readonly System.Numerics.Vector4 MyTextColor = Colors.Aquamarine.ToVector4();

    private static readonly ImGuiWindowFlags CsWinFlags =
        ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.AlwaysAutoResize |
        ImGuiWindowFlags.NoSavedSettings |
        ImGuiWindowFlags.NoFocusOnAppearing |
        ImGuiWindowFlags.NoNav |
        ImGuiWindowFlags.NoMove;

    private static readonly string VersionString =
        $"Godot {Engine.GetVersionInfo()["string"].AsString()} with .NET " +
        $"{System.Environment.Version}";

    public override void _EnterTree()
    {
        if (!_fontLoaded)
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
            _fontLoaded = true;
        }

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
        _proggy = io.Fonts.Fonts[1];
        _background = GetNode<ColorRect>("/root/Background");
    }

    public override void _Ready()
    {
        ImGuiGD.Connect(OnImGuiLayout);
        _iconTexture = GD.Load<Texture2D>("res://data/icon.svg");
        _atlasTexture = GD.Load<AtlasTexture>("res://data/robot_eye.tres");
        _vp = GetNode<SubViewport>("%SubViewport");
        _scale = ImGuiGD.Scale;
        GetNode<Button>("%ShowHideButton").Pressed += OnShowHidePressed;
    }

    private void OnImGuiLayout()
    {
        ImGui.ShowDemoWindow();

        float fh = ImGui.GetFrameHeight();

        var mainVpPos = ImGui.GetMainViewport().WorkPos;

        ImGui.SetNextWindowPos(new(mainVpPos.X + 10, mainVpPos.Y + 10));
        ImGui.Begin("change scene window", CsWinFlags);

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
                _vp.CallDeferred(SubViewport.MethodName.SetSize,
                    new Vector2I((int)size.X - 5, (int)size.Y - 5));

                Widgets.SubViewport(_vp);
            }
        }

        ImGui.End();

        ImGui.SetNextWindowPos(new(fh, 3 * fh), ImGuiCond.Once);
        ImGui.Begin("Scene 2", ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.PushFont(_proggy);
        ImGui.TextColored(MyTextColor, VersionString);
        ImGui.PopFont();

        ImGui.Separator();
        ImGui.Text("Textures");
        Widgets.Image(_iconTexture, new(_iconSize, _iconSize));
        ImGui.SameLine();
        Widgets.Image(_atlasTexture, new(_iconSize, _iconSize));
        ImGui.DragInt("size", ref _iconSize, 1.0f, 32, 512);

        ImGui.Separator();
        ImGui.Text("ImageButton");

        if (Widgets.ImageButton("myimgbtn", _iconTexture, new(128, 128)))
            ++_numClicks;

        ImGui.SameLine();
        ImGui.Text($"{_numClicks}");

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
            if (ImGui.RadioButton($"{s:0.00}", _scale == s))
            {
                _scale = s;
                CallDeferred(nameof(OnScaleChanged));
            }

            if (i < 5) ImGui.SameLine();
        }

        ImGui.Separator();
        var col = _background.Color.ToVector3();

        if (ImGui.ColorEdit3("background color", ref col))
            _background.Color = col.ToColor();

        ImGui.End();
    }

    private void OnShowHidePressed()
    {
        ImGuiLayer.Instance.Visible = !ImGuiLayer.Instance.Visible;
        GetNode<Button>("%ShowHideButton").Text = ImGuiLayer.Instance.Visible
            ? "hide" : "show";
    }

    private void OnScaleChanged()
    {
        ImGuiGD.Scale = _scale;

        // old font pointers are invalid after changing scale
        _proggy = ImGui.GetIO().Fonts.Fonts[1];
    }
#endif
}
