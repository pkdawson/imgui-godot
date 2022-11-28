using Godot;
using ImGuiGodot;
using ImGuiNET;
using System;

public partial class Button1 : Button
{
    private IntPtr _freeTypeBuilder = IntPtr.Zero;

    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    private void OnPressed()
    {
        ImGuiGD.ResetFonts();
        ImGuiGD.AddFont(GD.Load<FontFile>("res://Hack-Regular.ttf"), 18);
        ImGuiGD.RebuildFontAtlas();

        // toggle builder with each click
        unsafe
        {
            ImFontAtlas* atlas = ImGui.GetIO().NativePtr->Fonts;
            if (_freeTypeBuilder == IntPtr.Zero)
                _freeTypeBuilder = (IntPtr)atlas->FontBuilderIO;

            if (atlas->FontBuilderIO == null)
                atlas->FontBuilderIO = (IntPtr*)_freeTypeBuilder;
            else
                atlas->FontBuilderIO = null;
        }
    }
}
