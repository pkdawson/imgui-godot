#if IMGUI_GODOT_DEV
using Godot;
using ImGuiNET;
using System.Collections.Generic;

namespace ImGuiGodot;

internal static class InternalFontBuilder
{
    public static Window MainWindow { get; set; }

    public static Dictionary<RID, Image> BuildFont(Font font, int fontSize, Godot.Collections.Dictionary variationCoords = null)
    {
        if (font is FontVariation fontVariation)
        {
            variationCoords = fontVariation.VariationOpentype;
        }

        variationCoords ??= new();

        // prevent Godot from re-scaling the fonts
        var oldScaleMode = MainWindow.ContentScaleMode;
        MainWindow.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;

        TextServer ts = TextServerManager.GetPrimaryInterface(); // same as the TS macro in Godot C++
        GD.Print(font.GetSupportedVariationList());
        RID fontRid = font.FindVariation(variationCoords);

        if (fontRid.Id == 0)
        {
            GD.PrintErr("font variation not found");
            return null;
        }

        var fontSizeVec = new Vector2i(fontSize, 0);

        string supportedChars = ts.FontGetSupportedChars(fontRid);
        HashSet<RID> texRids = new();

        GD.Print("num supported chars = ", supportedChars.Length);

        foreach (char ch in supportedChars)
        {
            if ((ch >= 0xd800 && ch <= 0xdfff) || ch > 0x10ffff)
            {
                // invalid unicode?
                continue;
            }
            long glyphIndex = ts.FontGetGlyphIndex(fontRid, fontSize, ch, 0);
            ts.FontRenderGlyph(fontRid, fontSizeVec, glyphIndex);
            RID gtrid = ts.FontGetGlyphTextureRid(fontRid, fontSizeVec, glyphIndex);
            if (gtrid.Id != 0)
            {
                texRids.Add(gtrid);
            }
            else
            {
            }
        }

        Dictionary<RID, Image> glyphTextures = new();
        foreach (RID trid in texRids)
        {
            glyphTextures[trid] = RenderingServer.Texture2dGet(trid);
        }

        MainWindow.ContentScaleMode = oldScaleMode;

        int[] glyphList = ts.FontGetGlyphList(fontRid, fontSizeVec);
        GD.Print($"num glyphs = {glyphList.Length}");

        var io = ImGui.GetIO();
        return glyphTextures;
        //ImFontPtr fontPtr = io.Fonts.AddFontDefault();
        //List<int> rectIds = new();

        //int pad = 0; // TODO: padding

        //foreach (int glyph in glyphList)
        //{
        //    var glyphSize = ts.FontGetGlyphSize(fontRid, fontSizeVec, glyph);
        //    var glyphAdvance = ts.FontGetGlyphAdvance(fontRid, fontSizeVec.x, glyph);
        //    var glyphOffset = ts.FontGetGlyphOffset(fontRid, fontSizeVec, glyph);
        //    GD.Print($"size = {glyphSize}, advance = {glyphAdvance}, offset = {glyphOffset}");
        //    int rectId = io.Fonts.AddCustomRectFontGlyph(fontPtr, (ushort)glyph, (int)glyphSize.x + pad,
        //        (int)glyphSize.y + pad, glyphAdvance.x, new(glyphOffset.x, glyphOffset.y));
        //    rectIds.Add(rectId);
        //}

        //foreach (int rectId in rectIds)
        //{
        //    var rectPtr = io.Fonts.GetCustomRectByIndex(rectId);
        //}

        //return null;
    }
}
#endif
