using Godot;
using ImGuiNET;
using System;
using Vec2 = System.Numerics.Vector2;

public partial class MySecondNode : Node
{
    private IntPtr iconTextureId;
    private Texture2D iconTexture;
    private int iconSize = 64;
    private static bool fontLoaded = false;

    public override void _EnterTree()
    {
        if (!fontLoaded)
        {
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

        // let ImGui draw the mouse cursor
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.MouseDrawCursor = true;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public override void _Ready()
    {
        ImGuiLayer.Instance?.Connect(_ImGuiLayout);
        iconTexture = GD.Load<Texture2D>("res://data/icon.svg");
        iconTextureId = ImGuiGD.BindTexture(iconTexture);
    }

    public override void _ExitTree()
    {
        // TODO: remove after beta 3
        ImGuiLayer.Instance.ImGuiLayout -= _ImGuiLayout;
        ImGuiGD.UnbindTexture(iconTextureId);

        // restore the hardware mouse cursor
        var io = ImGui.GetIO();
        io.MouseDrawCursor = false;
        //io.BackendFlags &= ~ImGuiBackendFlags.HasMouseCursors;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void _ImGuiLayout()
    {
        ImGui.Begin("Scene 2");
        ImGui.Text("hello Godot 4");
        ImGui.Image(iconTextureId, new Vec2(iconSize, iconSize));
        ImGui.DragInt("size", ref iconSize, 1.0f, 32, 512);

        ImGui.Dummy(new Vec2(0, 20.0f));

        if (ImGui.Button("change scene"))
        {
            GetTree().ChangeSceneToFile("res://data/demo.tscn");
        }

        ImGui.End();

        ImGui.Begin("Unicode test");
        ImGui.Text("Hiragana: こんばんは");
        ImGui.Text("Katakana: ハロウィーン");
        ImGui.Text("   Kanji: 日本語");
        ImGui.End();

        ImGui.ShowDemoWindow();
    }

    private void _on_show_hide()
    {
        if (ImGuiLayer.Instance.Visible)
        {
            ImGuiLayer.Instance.Visible = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            ImGuiLayer.Instance.Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Hidden;
        }
    }
}
