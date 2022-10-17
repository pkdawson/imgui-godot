#if IMGUI_GODOT_DEV
using Godot;
using ImGuiNET;
using System.Collections.Generic;

namespace ImGuiGodot;

internal class InternalFontBuilder
{
    public static ImFontPtr BuildFont(FontVariation fontVariation)
    {
        return BuildFont(fontVariation, fontVariation.VariationOpentype);
    }

    public static ImFontPtr BuildFont(Font font, Godot.Collections.Dictionary variationCoords = null)
    {
        variationCoords ??= new();

        TextServer ts = TextServerManager.GetPrimaryInterface(); // same as the TS macro in Godot C++
        RID fontRid = font.FindVariation(variationCoords);
        var fontSize = new Vector2i(16, 0);

        string supportedChars = ts.FontGetSupportedChars(fontRid);
        HashSet<RID> texRids = new();

        foreach (char ch in supportedChars)
        {
            ts.FontRenderGlyph(fontRid, fontSize, ch);
            RID gtrid = ts.FontGetGlyphTextureRid(fontRid, fontSize, ch);
            if (gtrid.Id != 0)
                texRids.Add(gtrid);
        }

        Dictionary<RID, Image> glyphTextures = new();
        foreach (RID trid in texRids)
        {
            glyphTextures[trid] = RenderingServer.Texture2dGet(trid);
        }

        int[] glyphList = ts.FontGetGlyphList(fontRid, fontSize);

        // TODO: use custom rects with added padding, then adjust the glyphs after?
        return null;
    }
}
#endif
