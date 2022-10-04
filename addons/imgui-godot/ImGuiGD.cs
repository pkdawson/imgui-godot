using Godot;
using ImGuiNET;
using System;

public static class ImGuiGD
{
    public static float JoyAxisDeadZone { get; set; } = 0.15f;
    public static bool JoyButtonSwapAB { get; set; } = false;

    public static IntPtr BindTexture(Texture2D tex)
    {
        return ImGuiGDInternal.BindTexture(tex);
    }

    public static void UnbindTexture(IntPtr textureId)
    {
        ImGuiGDInternal.UnbindTexture(textureId);
    }

    public static Texture2D GetTexture(IntPtr textureId)
    {
        return ImGuiGDInternal.GetTexture(textureId);
    }

    public static void Init()
    {
        ImGuiGDInternal.Init();
    }

    public static ImFontPtr AddFont(FontFile fontData, float fontSize, bool merge = false)
    {
        return ImGuiGDInternal.AddFont(fontData, fontSize, merge);
    }

    public static ImFontPtr AddFont(FontFile fontData, float fontSize, IntPtr glyphRanges, bool merge = false)
    {
        return ImGuiGDInternal.AddFont(fontData, fontSize, glyphRanges, merge);
    }

    public static ImFontPtr AddFontDefault()
    {
        // right now this is a simple wrapper, but that will change
        return ImGui.GetIO().Fonts.AddFontDefault();
    }

    // only call this once, shortly after Init
    public static void RebuildFontAtlas()
    {
        ImGuiGDInternal.RebuildFontAtlas();
    }

    public static void Update(double delta, Viewport vp)
    {
        ImGuiGDInternal.Update(delta, vp);
    }

    public static void Render(RID parent)
    {
        ImGui.Render();
        ImGuiGDInternal.RenderDrawData(ImGui.GetDrawData(), parent);
    }

    public static void Shutdown()
    {
        ImGuiGDInternal.ClearCanvasItems();
        if (ImGui.GetCurrentContext() != IntPtr.Zero)
            ImGui.DestroyContext();
    }

    /// <returns>
    /// True if the InputEvent was consumed
    /// </returns>
    public static bool ProcessInput(InputEvent evt)
    {
        return ImGuiGDInternal.ProcessInput(evt);
    }
}
