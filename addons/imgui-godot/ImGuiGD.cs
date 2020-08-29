using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class ImGuiGD
{
    private static Dictionary<IntPtr, Texture> _loadedTextures = new Dictionary<IntPtr, Texture>();
    private static int _textureId = 100;
    private static IntPtr? _fontTextureId;
    private static List<RID> _children = new List<RID>();
    private static List<ArrayMesh> _meshes = new List<ArrayMesh>();

    public static IntPtr BindTexture(Texture tex)
    {
        // decided not to add duplicate prevention, could cause problems
        var id = new IntPtr(_textureId++);
        _loadedTextures.Add(id, tex);
        return id;
    }

    public static void UnbindTexture(IntPtr textureId)
    {
        _loadedTextures.Remove(textureId);
    }

    // used by renderer
    public static Texture GetTexture(IntPtr textureId)
    {
        return _loadedTextures[textureId];
    }

    public static ImFontPtr AddFont(DynamicFont font)
    {
        return AddFont(font.FontData, font.Size);
    }

    public static unsafe ImFontPtr AddFont(DynamicFontData fontData, int fontSize)
    {
        ImFontPtr rv = null;

        Godot.File fi = new File();
        var err = fi.Open(fontData.FontPath, File.ModeFlags.Read);
        byte[] buf = fi.GetBuffer((int)fi.GetLen());
        fi.Close();

        // can't add a name, ImFontConfig seems unusable

        // let ImGui manage this memory
        IntPtr p = Marshal.AllocHGlobal(buf.Length);
        Marshal.Copy(buf, 0, p, buf.Length);
        rv = ImGui.GetIO().Fonts.AddFontFromMemoryTTF(p, buf.Length, (float)fontSize);

        return rv;
    }

    public static unsafe void RebuildFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        byte[] pixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);

        Image img = new Image();
        img.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);

        var imgtex = new ImageTexture();
        imgtex.CreateFromImage(img, 0);

        if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);
        _fontTextureId = BindTexture(imgtex);

        io.Fonts.SetTexID(_fontTextureId.Value);
        io.Fonts.ClearTexData();
    }

    public static void Init(Viewport vp)
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();

        io.BackendFlags = 0;
        io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
        // io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        // io.BackendPlatformName = "imgui_impl_godot";

        io.KeyMap[(int)ImGuiKey.Tab] = FixKey(KeyList.Tab);
        io.KeyMap[(int)ImGuiKey.LeftArrow] = FixKey(KeyList.Left);
        io.KeyMap[(int)ImGuiKey.RightArrow] = FixKey(KeyList.Right);
        io.KeyMap[(int)ImGuiKey.UpArrow] = FixKey(KeyList.Up);
        io.KeyMap[(int)ImGuiKey.DownArrow] = FixKey(KeyList.Down);
        io.KeyMap[(int)ImGuiKey.PageUp] = FixKey(KeyList.Pageup);
        io.KeyMap[(int)ImGuiKey.PageDown] = FixKey(KeyList.Pagedown);
        io.KeyMap[(int)ImGuiKey.Home] = FixKey(KeyList.Home);
        io.KeyMap[(int)ImGuiKey.End] = FixKey(KeyList.End);
        io.KeyMap[(int)ImGuiKey.Insert] = FixKey(KeyList.Insert);
        io.KeyMap[(int)ImGuiKey.Delete] = FixKey(KeyList.Delete);
        io.KeyMap[(int)ImGuiKey.Backspace] = FixKey(KeyList.Backspace);
        io.KeyMap[(int)ImGuiKey.Space] = FixKey(KeyList.Space);
        io.KeyMap[(int)ImGuiKey.Enter] = FixKey(KeyList.Enter);
        io.KeyMap[(int)ImGuiKey.Escape] = FixKey(KeyList.Escape);
        io.KeyMap[(int)ImGuiKey.KeyPadEnter] = FixKey(KeyList.KpEnter);
        io.KeyMap[(int)ImGuiKey.A] = (int)KeyList.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)KeyList.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)KeyList.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)KeyList.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)KeyList.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)KeyList.Z;

        io.DisplaySize = new System.Numerics.Vector2(vp.Size.x, vp.Size.y);
    }

    private static void UpdateJoypads()
    {
        var io = ImGui.GetIO();
        if (!io.ConfigFlags.HasFlag(ImGuiConfigFlags.NavEnableGamepad))
            return;

        void MapAnalog(ImGuiNavInput igni, string act)
        {
            if (Input.IsActionPressed(act))
            {
                io.NavInputs[(int)igni] = Input.GetActionStrength(act);
            }
        }

        void MapButton(ImGuiNavInput igni, string act)
        {
            if (Input.IsActionPressed(act))
            {
                io.NavInputs[(int)igni] = 1;
            }
        }

        MapButton(ImGuiNavInput.DpadUp, "ImGui_DpadUp");
        MapButton(ImGuiNavInput.DpadDown, "ImGui_DpadDown");
        MapButton(ImGuiNavInput.DpadLeft, "ImGui_DpadLeft");
        MapButton(ImGuiNavInput.DpadRight, "ImGui_DpadRight");

        MapAnalog(ImGuiNavInput.LStickUp, "ImGui_ScrollUp");
        MapAnalog(ImGuiNavInput.LStickDown, "ImGui_ScrollDown");
        MapAnalog(ImGuiNavInput.LStickLeft, "ImGui_ScrollLeft");
        MapAnalog(ImGuiNavInput.LStickRight, "ImGui_ScrollRight");

        MapButton(ImGuiNavInput.Activate, "ImGui_Activate");
        MapButton(ImGuiNavInput.Cancel, "ImGui_Cancel");
        MapButton(ImGuiNavInput.Input, "ImGui_Input");
        MapButton(ImGuiNavInput.Menu, "ImGui_Menu");
    }

    public static void Update(float delta, Viewport vp)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(vp.Size.x, vp.Size.y);
        io.DeltaTime = delta;

        io.KeyCtrl = Input.IsKeyPressed((int)KeyList.Control);
        io.KeyAlt = Input.IsKeyPressed((int)KeyList.Alt);
        io.KeyShift = Input.IsKeyPressed((int)KeyList.Shift);
        io.KeySuper = Input.IsKeyPressed((int)KeyList.SuperL) || Input.IsKeyPressed((int)KeyList.SuperR);

        if (io.WantSetMousePos)
        {
            vp.WarpMouse(new Godot.Vector2(io.MousePos.X, io.MousePos.Y));
        }

        UpdateJoypads();

        ImGui.NewFrame();
    }

    public static void Render(RID parent)
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData(), parent);
    }

    public static void Shutdown()
    {
        foreach (RID rid in _children)
        {
            VisualServer.FreeRid(rid);
        }
        ImGui.DestroyContext();
    }

    public static bool ProcessInput(InputEvent evt)
    {
        var io = ImGui.GetIO();
        bool consumed = false;

        if (evt is InputEventMouseMotion mm)
        {
            io.MousePos = new System.Numerics.Vector2(mm.Position.x, mm.Position.y);
            consumed = io.WantCaptureMouse;
        }
        else if (evt is InputEventMouseButton mb)
        {
            switch ((ButtonList)mb.ButtonIndex)
            {
                case ButtonList.Left:
                    io.MouseDown[(int)ImGuiMouseButton.Left] = mb.Pressed;
                    break;
                case ButtonList.Right:
                    io.MouseDown[(int)ImGuiMouseButton.Right] = mb.Pressed;
                    break;
                case ButtonList.Middle:
                    io.MouseDown[(int)ImGuiMouseButton.Middle] = mb.Pressed;
                    break;
                case ButtonList.WheelUp:
                    io.MouseWheel = mb.Factor * 1.0f;
                    break;
                case ButtonList.WheelDown:
                    io.MouseWheel = mb.Factor * -1.0f;
                    break;
                case ButtonList.WheelLeft:
                    io.MouseWheelH = mb.Factor * -1.0f;
                    break;
                case ButtonList.WheelRight:
                    io.MouseWheelH = mb.Factor * 1.0f;
                    break;
                case ButtonList.Xbutton1:
                    io.MouseDown[(int)ImGuiMouseButton.Middle + 1] = mb.Pressed;
                    break;
                case ButtonList.Xbutton2:
                    io.MouseDown[(int)ImGuiMouseButton.Middle + 2] = mb.Pressed;
                    break;
                default:
                    // more buttons not supported
                    break;
            };

            consumed = io.WantCaptureMouse;
        }
        else if (evt is InputEventKey k)
        {
            KeyList kc = (KeyList)k.Scancode;
            int code = FixKey(kc);

            io.KeysDown[code] = k.Pressed;
            if (k.Pressed)
            {
                io.AddInputCharacter(k.Unicode);
            }
            consumed = io.WantCaptureKeyboard || io.WantTextInput;
        }
        else if (evt is InputEventPanGesture pg)
        {
            io.MouseWheelH = -pg.Delta.x;
            io.MouseWheel = -pg.Delta.y;
            consumed = io.WantCaptureMouse;
        }

        return consumed;
    }

    private static int FixKey(KeyList kc)
    {
        // Godot reserves the first 24 bits for printable characters, but ImGui needs keycodes <512
        if ((int)kc < 256)
            return (int)kc;
        else
            return 256 + (int)((uint)kc & 0xFF);
    }

    private static void RenderDrawData(ImDrawDataPtr drawData, RID parent)
    {
        // allocate and clear out our CanvasItem pool as needed
        int neededNodes = 0;
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            neededNodes += drawData.CmdListsRange[i].CmdBuffer.Size;
        }

        while (_children.Count < neededNodes)
        {
            RID newChild = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemSetParent(newChild, parent);
            VisualServer.CanvasItemSetDrawIndex(newChild, _children.Count);
            _children.Add(newChild);
            _meshes.Add(new ArrayMesh());
        }

        // trim unused nodes to reduce draw calls
        while (_children.Count > neededNodes)
        {
            int idx = _children.Count - 1;
            VisualServer.FreeRid(_children[idx]);
            _children.RemoveAt(idx);
            _meshes.RemoveAt(idx);
        }

        // render
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);
        int nodeN = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];
            int idxOffset = 0;

            int nVert = cmdList.VtxBuffer.Size;

            Godot.Vector2[] vertices = new Godot.Vector2[nVert];
            Godot.Color[] colors = new Godot.Color[nVert];
            Godot.Vector2[] uvs = new Godot.Vector2[nVert];

            for (int i = 0; i < cmdList.VtxBuffer.Size; i++)
            {
                var v = cmdList.VtxBuffer[i];
                vertices[i] = new Godot.Vector2(v.pos.X, v.pos.Y);
                // need to reverse the color bytes
                byte[] col = BitConverter.GetBytes(v.col);
                colors[i] = Godot.Color.Color8(col[0], col[1], col[2], col[3]);
                uvs[i] = new Godot.Vector2(v.uv.X, v.uv.Y);
            }

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++, nodeN++)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                int[] indices = new int[drawCmd.ElemCount];
                for (int i = idxOffset, j = 0; i < idxOffset + drawCmd.ElemCount; i++, j++)
                {
                    indices[j] = cmdList.IdxBuffer[i];
                }

                var arrays = new Godot.Collections.Array();
                arrays.Resize((int)ArrayMesh.ArrayType.Max);
                arrays[(int)ArrayMesh.ArrayType.Vertex] = vertices;
                arrays[(int)ArrayMesh.ArrayType.Color] = colors;
                arrays[(int)ArrayMesh.ArrayType.TexUv] = uvs;
                arrays[(int)ArrayMesh.ArrayType.Index] = indices;

                var mesh = _meshes[nodeN];
                while (mesh.GetSurfaceCount() > 0)
                {
                    mesh.SurfaceRemove(0);
                }
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

                RID child = _children[nodeN];

                Texture tex = GetTexture(drawCmd.TextureId);
                VisualServer.CanvasItemClear(child);
                VisualServer.CanvasItemSetClip(child, true);
                VisualServer.CanvasItemSetCustomRect(child, true, new Godot.Rect2(
                    drawCmd.ClipRect.X,
                    drawCmd.ClipRect.Y,
                    drawCmd.ClipRect.Z - drawCmd.ClipRect.X,
                    drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );
                VisualServer.CanvasItemAddMesh(child, mesh.GetRid(), null, null, tex.GetRid(), new RID(null));

                // why doesn't this quite work?
                // VisualServer.CanvasItemAddTriangleArray(child, indices, vertices, colors, uvs, null, null, tex.GetRid(), -1, new RID(null));

                idxOffset += (int)drawCmd.ElemCount;
            }
        }
    }
};
