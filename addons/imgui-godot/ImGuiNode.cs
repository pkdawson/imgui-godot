using Godot;
using System;
using System.Collections.Generic;
using ImGuiNET;

public class ImGuiNode : Node2D
{
    [Export]
    DynamicFont Font = null;

    [Signal]
    public delegate void IGLayout();

    private List<ImGuiClippedNode> _children;

    private class ImGuiClippedNode
    {
        public RID CanvasItem;
        public ArrayMesh Mesh;
        public Texture Texture;
        public IntPtr TextureId;
    }

    public ImGuiNode()
    {
        _children = new List<ImGuiClippedNode>();
    }

    public virtual void Init(ImGuiNET.ImGuiIOPtr io)
    {
        if (Font is null)
        {
            io.Fonts.AddFontDefault();
        }
        else
        {
            ImGuiGD.AddFont(Font);
        }
        ImGuiGD.RebuildFontAtlas();
    }

    public virtual void Layout()
    {
        EmitSignal(nameof(IGLayout));
    }

    public override void _EnterTree()
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();

        io.BackendFlags = 0;
        // io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
        // io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        // io.BackendPlatformName = "imgui_impl_godot";

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

        if (io.WantSetMousePos)
        {
            GetViewport().WarpMouse(new Godot.Vector2(io.MousePos.X, io.MousePos.Y));
        }

        ImGui.NewFrame();

        Layout();

        ImGui.Render();
        if (Visible)
            RenderDrawData(ImGui.GetDrawData());
    }

    public override void _Input(InputEvent evt)
    {
        if (Visible && ProcessInput(evt))
        {
            GetTree().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        foreach (var node in _children)
        {
            VisualServer.FreeRid(node.CanvasItem);
        }
        // crashes FIXME
        // ImGui.DestroyContext();
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

        while (_children.Count < neededNodes)
        {
            ImGuiClippedNode newChild = new ImGuiClippedNode();
            newChild.Mesh = new ArrayMesh();
            newChild.CanvasItem = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemSetParent(newChild.CanvasItem, GetCanvasItem());
            VisualServer.CanvasItemSetDrawIndex(newChild.CanvasItem, _children.Count);
            _children.Add(newChild);
        }

        // trim unused nodes to reduce draw calls
        while (_children.Count > neededNodes)
        {
            int idx = _children.Count - 1;
            VisualServer.FreeRid(_children[idx].CanvasItem);
            _children.RemoveAt(idx);
        }

        foreach (var node in _children)
        {
            while (node.Mesh.GetSurfaceCount() > 0)
            {
                node.Mesh.SurfaceRemove(0);
            }
        }

        // render
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);
        int nodeN = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];
            // int vtxOffset = 0;
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
                // int vtxCount = nVert - vtxOffset;

                var arrays = new Godot.Collections.Array();
                arrays.Resize((int)ArrayMesh.ArrayType.Max);

                T[] ArraySlice<T>(T[] src, int start, int count)
                {
                    T[] dst = new T[count];
                    Array.Copy(src, start, dst, 0, count);
                    return dst;
                }

                arrays[(int)ArrayMesh.ArrayType.Vertex] = vertices; // ArraySlice(vertices, vtxOffset, vtxCount);
                arrays[(int)ArrayMesh.ArrayType.Color] = colors; // ArraySlice(colors, vtxOffset, vtxCount);
                arrays[(int)ArrayMesh.ArrayType.TexUv] = uvs; // ArraySlice(uvs, vtxOffset, vtxCount);
                arrays[(int)ArrayMesh.ArrayType.Index] = ArraySlice(indices, idxOffset, (int)drawCmd.ElemCount);

                var node = _children[nodeN];

                node.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

                VisualServer.CanvasItemSetClip(node.CanvasItem, true);
                VisualServer.CanvasItemSetCustomRect(node.CanvasItem, true, new Godot.Rect2(
                    drawCmd.ClipRect.X,
                    drawCmd.ClipRect.Y,
                    drawCmd.ClipRect.Z - drawCmd.ClipRect.X,
                    drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                if (node.TextureId != drawCmd.TextureId)
                {
                    // need to redraw node if the texture changes
                    node.Texture = ImGuiGD.GetTexture(drawCmd.TextureId);
                    node.TextureId = drawCmd.TextureId;
                    VisualServer.CanvasItemClear(node.CanvasItem);

                    // need to take care of the normalMap RID like this because of a bug in the C# binding
                    VisualServer.CanvasItemAddMesh(node.CanvasItem, node.Mesh.GetRid(), null, null, node.Texture.GetRid(), new RID(null));
                }

                idxOffset += (int)drawCmd.ElemCount;
            }

            // vtxOffset += cmdList.VtxBuffer.Size;
        }
    }

    private int FixKey(Godot.KeyList kc)
    {
        // Godot reserves the first 24 bits for printable characters, but ImGui needs keycodes <512
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
            switch ((Godot.ButtonList)mb.ButtonIndex)
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
            io.MouseWheelH = -pg.Delta.x;
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
