using Godot;
using ImGuiNET;
using System;

public partial class ImGuiNode : Node2D
{
    [Export]
    public FontFile Font = null;

    [Export]
    public float FontSize = 16.0f;

    [Export]
    public FontFile ExtraFont = null;

    [Export]
    public float ExtraFontSize = 16.0f;

    [Export(PropertyHint.Enum, "Korean,Japanese,ChineseFull,ChineseSimplifiedCommon,Cyrillic,Thai,Vietnamese")]
    public string ExtraFontGlyphRange = "Japanese";

    [Export]
    public bool IncludeDefaultFont = true;

    [Signal]
    public delegate void imgui_layoutEventHandler();

    public virtual void Init(ImGuiIOPtr io)
    {
        if (Font is not null)
        {
            ImGuiGD.AddFont(Font, FontSize);
            if (ExtraFont is not null)
            {
                IntPtr gr = ExtraFontGlyphRange switch
                {
                    "Korean" => io.Fonts.GetGlyphRangesKorean(),
                    "Japanese" => io.Fonts.GetGlyphRangesJapanese(),
                    "ChineseFull" => io.Fonts.GetGlyphRangesChineseFull(),
                    "ChineseSimplifiedCommon" => io.Fonts.GetGlyphRangesChineseSimplifiedCommon(),
                    "Cyrillic" => io.Fonts.GetGlyphRangesCyrillic(),
                    "Thai" => io.Fonts.GetGlyphRangesThai(),
                    "Vietnamese" => io.Fonts.GetGlyphRangesVietnamese(),
                    _ => throw new Exception("invalid glyph range")
                };
                ImGuiGD.AddFontMerge(ExtraFont, ExtraFontSize, gr);
            }
        }

        if (IncludeDefaultFont)
        {
            io.Fonts.AddFontDefault();
        }
    }

    public override void _EnterTree()
    {
        ProcessPriority = int.MaxValue; // try to be last
        ImGuiGD.Init(GetViewport());
        Init(ImGui.GetIO());
        ImGuiGD.RebuildFontAtlas();
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            ImGuiGD.Update(delta, GetViewport());
            EmitSignal("imgui_layout");
            ImGuiGD.Render(GetCanvasItem());
        }
    }

    public override void _Input(InputEvent evt)
    {
        if (Visible && ImGuiGD.ProcessInput(evt))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        ImGuiGD.Shutdown();
    }
}
