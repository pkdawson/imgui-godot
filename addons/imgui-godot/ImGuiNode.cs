using Godot;
using System;
using ImGuiNET;
using System.Runtime.InteropServices;
using System.Collections.Generic;

[Tool]
public class ImGuiNode : Control
{
    [Export]
    DynamicFont Font = null;

    private Dictionary<IntPtr, Texture> _loadedTextures;
    private int _textureId;
    private IntPtr? _fontTextureId;
    private Godot.Collections.Array<ArrayMesh> _meshes;
    private Godot.Collections.Array<ImGuiClippedNode> _children;
    private Godot.Collections.Array<byte[]> _fontStorage; // ugly...

    private class ImGuiClippedNode : Control
    {
        public ArrayMesh Mesh { get; set; }
        public Texture Texture { get; set; }

        public override void _Draw()
        {
            DrawMesh(Mesh, Texture);
        }
    }

    public ImGuiNode()
    {
        _textureId = 100;
        _loadedTextures = new Dictionary<IntPtr, Texture>();
        _meshes = new Godot.Collections.Array<ArrayMesh>();
        _children = new Godot.Collections.Array<ImGuiClippedNode>();
        _fontStorage = new Godot.Collections.Array<byte[]>();
    }

    public virtual void Init(ImGuiNET.ImGuiIOPtr io)
    {
        if (Font is null)
        {
            io.Fonts.AddFontDefault();
        }
        else
        {
            AddFont(Font);
        }
        RebuildFontAtlas();
    }

    public virtual void Layout()
    {
        // override me
    }

    public override void _Ready()
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();

        io.KeyMap[(int)ImGuiKey.Tab] = FixKey(Godot.KeyList.Tab);
        io.KeyMap[(int)ImGuiKey.LeftArrow] = FixKey(Godot.KeyList.Left);
        io.KeyMap[(int)ImGuiKey.RightArrow] = FixKey(Godot.KeyList.Right);
        io.KeyMap[(int)ImGuiKey.UpArrow] = FixKey(Godot.KeyList.Up);
        io.KeyMap[(int)ImGuiKey.DownArrow] = FixKey(Godot.KeyList.Down);
        io.KeyMap[(int)ImGuiKey.PageUp] = FixKey(Godot.KeyList.Pageup);
        io.KeyMap[(int)ImGuiKey.PageDown] = FixKey(Godot.KeyList.Pagedown);
        io.KeyMap[(int)ImGuiKey.Home] = FixKey(Godot.KeyList.Home);
        io.KeyMap[(int)ImGuiKey.End] = FixKey(Godot.KeyList.End);
        io.KeyMap[(int)ImGuiKey.Insert] = FixKey(Godot.KeyList.Insert);
        io.KeyMap[(int)ImGuiKey.Delete] = FixKey(Godot.KeyList.Delete);
        io.KeyMap[(int)ImGuiKey.Backspace] = FixKey(Godot.KeyList.Backspace);
        io.KeyMap[(int)ImGuiKey.Space] = FixKey(Godot.KeyList.Space);
        io.KeyMap[(int)ImGuiKey.Enter] = FixKey(Godot.KeyList.Enter);
        io.KeyMap[(int)ImGuiKey.Escape] = FixKey(Godot.KeyList.Escape);
        io.KeyMap[(int)ImGuiKey.KeyPadEnter] = FixKey(Godot.KeyList.KpEnter);
        io.KeyMap[(int)ImGuiKey.A] = (int)Godot.KeyList.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Godot.KeyList.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Godot.KeyList.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Godot.KeyList.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Godot.KeyList.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Godot.KeyList.Z;

        io.DisplaySize = new System.Numerics.Vector2(GetViewport().Size.x, GetViewport().Size.y);

        GetViewport().Connect("size_changed", this, nameof(_onViewportResize));

        Init(io);
    }

    public override void _Process(float delta)
    {
        var io = ImGui.GetIO();
        io.DeltaTime = delta;

        io.KeyCtrl = Godot.Input.IsKeyPressed((int)Godot.KeyList.Control);
        io.KeyAlt = Godot.Input.IsKeyPressed((int)Godot.KeyList.Alt);
        io.KeyShift = Godot.Input.IsKeyPressed((int)Godot.KeyList.Shift);
        io.KeySuper = Godot.Input.IsKeyPressed((int)Godot.KeyList.SuperL) || Godot.Input.IsKeyPressed((int)Godot.KeyList.SuperR);

        ImGui.NewFrame();

        Layout();

        ImGui.Render();
        unsafe { RenderDrawData(ImGui.GetDrawData()); }
    }

    public override void _Input(InputEvent evt)
    {
        if (ProcessInput(evt))
        {
            GetTree().SetInputAsHandled();
        }
    }

    public IntPtr BindTexture(Texture tex)
    {
        var id = new IntPtr(_textureId++);
        _loadedTextures.Add(id, tex);
        return id;
    }
    public void UnbindTexture(IntPtr textureId)
    {
        _loadedTextures.Remove(textureId);
    }

    public ImFontPtr AddFont(DynamicFont font)
    {
        return AddFont(font.FontData, font.Size);
    }

    public unsafe ImFontPtr AddFont(DynamicFontData fontData, int fontSize)
    {
        ImFontPtr rv = null;
        Godot.File fi = new File();
        var err = fi.Open(fontData.FontPath, File.ModeFlags.Read);
        byte[] buf = fi.GetBuffer((int)fi.GetLen());

        var io = ImGui.GetIO();
        fixed (byte* p = buf)
        {
            IntPtr ptr = (IntPtr)p;
            rv = io.Fonts.AddFontFromMemoryTTF(ptr, buf.Length, (float)fontSize);
        }

        // store buf so it doesn't get GC'd
        _fontStorage.Add(buf);

        return rv;
    }

    public unsafe void RebuildFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        byte[] pixels = new byte[width * height * bytesPerPixel];
        unsafe { Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length); }

        Image img = new Image();
        img.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);

        var imgtex = new ImageTexture();
        imgtex.CreateFromImage(img, 0);

        if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);
        _fontTextureId = BindTexture(imgtex);

        io.Fonts.SetTexID(_fontTextureId.Value);
        io.Fonts.ClearTexData();
    }


    private void _onViewportResize()
    {
        ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(GetViewport().Size.x, GetViewport().Size.y);
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        // allocate and clear out our mesh pool as needed
        int neededNodes = 0;
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            neededNodes += drawData.CmdListsRange[i].CmdBuffer.Size;
        }

        while (_meshes.Count < neededNodes)
        {
            _meshes.Add(new ArrayMesh());

            ImGuiClippedNode newChild = new ImGuiClippedNode();
            AddChild(newChild);
            _children.Add(newChild);
        }
        // TODO: trim unused nodes?
        foreach (ArrayMesh arrayMesh in _meshes)
        {
            while (arrayMesh.GetSurfaceCount() > 0)
            {
                arrayMesh.SurfaceRemove(0);
            }
        }

        // render
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);
        int nodeN = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];
            int vtxOffset = 0;
            int idxOffset = 0;

            int nVert = cmdList.VtxBuffer.Size;

            Godot.Vector2[] vertices = new Godot.Vector2[nVert];
            Godot.Color[] colors = new Godot.Color[nVert];
            Godot.Vector2[] uvs = new Godot.Vector2[nVert];
            int[] indices = new int[cmdList.IdxBuffer.Size];

            for (int i = 0; i < cmdList.VtxBuffer.Size; i++)
            {
                var v = cmdList.VtxBuffer[i];
                vertices[i] = new Godot.Vector2(v.pos.X, v.pos.Y);
                // need to reverse the color bytes
                byte[] col = BitConverter.GetBytes(v.col);
                colors[i] = Godot.Color.Color8(col[0], col[1], col[2], col[3]);
                uvs[i] = new Godot.Vector2(v.uv.X, v.uv.Y);
            }
            for (int i = 0; i < cmdList.IdxBuffer.Size; i++)
            {
                indices[i] = cmdList.IdxBuffer[i];
            }

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++, nodeN++)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];
                int vtxCount = nVert - vtxOffset;

                var arrays = new Godot.Collections.Array();
                arrays.Resize((int)ArrayMesh.ArrayType.Max);

                T[] ArraySlice<T>(T[] src, int start, int count)
                {
                    T[] dst = new T[count];
                    Array.Copy(src, start, dst, 0, count);
                    return dst;
                }

                arrays[(int)ArrayMesh.ArrayType.Vertex] = ArraySlice(vertices, vtxOffset, vtxCount);
                arrays[(int)ArrayMesh.ArrayType.Color] = ArraySlice(colors, vtxOffset, vtxCount);
                arrays[(int)ArrayMesh.ArrayType.TexUv] = ArraySlice(uvs, vtxOffset, vtxCount);
                arrays[(int)ArrayMesh.ArrayType.Index] = ArraySlice(indices, idxOffset, (int)drawCmd.ElemCount);

                ArrayMesh arrayMesh = _meshes[nodeN];
                arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

                VisualServer.CanvasItemSetClip(_children[nodeN].GetCanvasItem(), true);
                VisualServer.CanvasItemSetCustomRect(_children[nodeN].GetCanvasItem(), true, new Godot.Rect2(
                    drawCmd.ClipRect.X,
                    drawCmd.ClipRect.Y,
                    drawCmd.ClipRect.Z - drawCmd.ClipRect.X,
                    drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                if (_children[nodeN].Texture != _loadedTextures[drawCmd.TextureId])
                {
                    // need to redraw node if the texture changes
                    _children[nodeN].Texture = _loadedTextures[drawCmd.TextureId];
                    _children[nodeN].Update();
                }
                _children[nodeN].Mesh = arrayMesh;

                idxOffset += (int)drawCmd.ElemCount;
            }

            vtxOffset += cmdList.VtxBuffer.Size;
        }
    }

    private int FixKey(Godot.KeyList kc)
    {
        if ((int)kc < 256)
            return (int)kc;
        else
            return 255 + (int)((uint)kc & 0xFF);
    }

    protected bool ProcessInput(InputEvent evt)
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
            if (mb.ButtonIndex <= (int)Godot.ButtonList.Middle)
            {
                io.MouseDown[mb.ButtonIndex - 1] = mb.Pressed;
            }
            else if (mb.ButtonIndex == (int)Godot.ButtonList.WheelUp)
            {
                io.MouseWheel = mb.Factor * 1.0f;
            }
            else if (mb.ButtonIndex == (int)Godot.ButtonList.WheelDown)
            {
                io.MouseWheel = mb.Factor * -1.0f;
            }

            consumed = io.WantCaptureMouse;
        }
        else if (evt is InputEventKey k)
        {
            Godot.KeyList kc = (Godot.KeyList)k.Scancode;
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
            io.MouseWheel = -pg.Delta.y;
            consumed = io.WantCaptureMouse;
        }
        else if (evt is InputEventJoypadMotion jm)
        {
            // TODO
        }
        else if (evt is InputEventJoypadButton jb)
        {
            // TODO
        }

        return consumed;
    }
}
