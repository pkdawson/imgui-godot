using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public partial class ImGuiGD
{
    public static float JoyAxisDeadZone { get; set; } = 0.15f;

    private static Dictionary<IntPtr, Texture2D> _loadedTextures = new Dictionary<IntPtr, Texture2D>();
    private static int _textureId = 0x100;
    private static IntPtr? _fontTextureId;
    private static List<RID> _children = new List<RID>();
    private static Vector2 _mouseWheel = Vector2.Zero;
    private static GCHandle _backendName = GCHandle.Alloc(Encoding.ASCII.GetBytes("imgui_impl_godot4"), GCHandleType.Pinned);

    public static IntPtr BindTexture(Texture2D tex)
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
    public static Texture2D GetTexture(IntPtr textureId)
    {
        return _loadedTextures[textureId];
    }

    public static unsafe ImFontPtr AddFont(FontFile fontData, float fontSize)
    {
        return AddFont(fontData, fontSize, ImGui.GetIO().Fonts.GetGlyphRangesDefault());
    }

    public static unsafe ImFontPtr AddFont(FontFile fontData, float fontSize, IntPtr glyphRanges)
    {
        // ImFontConfig has a constructor, so we don't zero it
        ImFontConfig* fc = ImGuiNative.ImFontConfig_ImFontConfig();
        string name = string.Format("{0}, {1}px", System.IO.Path.GetFileName(fontData.ResourcePath), fontSize);
        for (int i = 0; i < name.Length && i < 40; ++i)
        {
            fc->Name[i] = Convert.ToByte(name[i]);
        }

        ImFontPtr rv = AddFont(fontData, fontSize, glyphRanges, fc);
        ImGuiNative.ImFontConfig_destroy(fc);
        return rv;
    }

    public static unsafe ImFontPtr AddFontMerge(FontFile fontData, float fontSize, IntPtr glyphRanges)
    {
        ImFontConfig* fc = ImGuiNative.ImFontConfig_ImFontConfig();
        fc->MergeMode = 1;
        ImFontPtr rv = AddFont(fontData, fontSize, glyphRanges, fc);
        ImGui.GetIO().Fonts.Build();
        ImGuiNative.ImFontConfig_destroy(fc);
        return rv;
    }

    private static unsafe ImFontPtr AddFont(FontFile fontData, float fontSize, IntPtr glyphRanges, ImFontConfig* fc)
    {
        int len = fontData.Data.Length;
        // let ImGui manage this memory
        IntPtr p = Marshal.AllocHGlobal(len);
        Marshal.Copy(fontData.Data, 0, p, len);
        return ImGui.GetIO().Fonts.AddFontFromMemoryTTF(p, len, fontSize, fc, glyphRanges);
    }

    // only call this once, shortly after Init
    public static unsafe void RebuildFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        byte[] pixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);

        Image img = new Image();
        img.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);

        var imgtex = ImageTexture.CreateFromImage(img);

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
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;

        unsafe
        {
            io.NativePtr->BackendPlatformName = (byte*)_backendName.AddrOfPinnedObject();
            io.NativePtr->BackendRendererName = (byte*)_backendName.AddrOfPinnedObject();
        }

        var vpSize = vp.GetVisibleRect().Size;
        io.DisplaySize = new System.Numerics.Vector2(vpSize.x, vpSize.y);
    }

    public static void Update(double delta, Viewport vp)
    {
        var io = ImGui.GetIO();
        var vpSize = vp.GetVisibleRect().Size;
        io.DisplaySize = new System.Numerics.Vector2(vpSize.x, vpSize.y);
        io.DeltaTime = (float)delta;

        if (io.WantSetMousePos)
        {
            vp.WarpMouse(new Vector2(io.MousePos.X, io.MousePos.Y));
        }

        // scrolling works better if we allow no more than one event per frame
        if (_mouseWheel != Vector2.Zero)
        {
            io.AddMouseWheelEvent(_mouseWheel.x, _mouseWheel.y);
            _mouseWheel = Vector2.Zero;
        }

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
            RenderingServer.FreeRid(rid);
        }
        _children.Clear();
        ImGui.DestroyContext();
    }

    public static bool ProcessInput(InputEvent evt)
    {
        var io = ImGui.GetIO();
        bool consumed = false;

        if (evt is InputEventMouseMotion mm)
        {
            io.AddMousePosEvent(mm.Position.x, mm.Position.y);
            consumed = io.WantCaptureMouse;
        }
        else if (evt is InputEventMouseButton mb)
        {
            switch (mb.ButtonIndex)
            {
                case MouseButton.Left:
                    io.AddMouseButtonEvent((int)ImGuiMouseButton.Left, mb.Pressed);
                    break;
                case MouseButton.Right:
                    io.AddMouseButtonEvent((int)ImGuiMouseButton.Right, mb.Pressed);
                    break;
                case MouseButton.Middle:
                    io.AddMouseButtonEvent((int)ImGuiMouseButton.Middle, mb.Pressed);
                    break;
                case MouseButton.Xbutton1:
                    io.AddMouseButtonEvent((int)ImGuiMouseButton.Middle + 1, mb.Pressed);
                    break;
                case MouseButton.Xbutton2:
                    io.AddMouseButtonEvent((int)ImGuiMouseButton.Middle + 2, mb.Pressed);
                    break;
                case MouseButton.WheelUp:
                    _mouseWheel.y = mb.Factor;
                    break;
                case MouseButton.WheelDown:
                    _mouseWheel.y = -mb.Factor;
                    break;
                case MouseButton.WheelLeft:
                    _mouseWheel.x = -mb.Factor;
                    break;
                case MouseButton.WheelRight:
                    _mouseWheel.x = mb.Factor;
                    break;
            };
            consumed = io.WantCaptureMouse;
        }
        else if (evt is InputEventKey k)
        {
            UpdateKeyMods(io, k);
            Key kc = k.Keycode;
            if (ImGuiInput.KeyMap.TryGetValue(kc, out var igk))
            {
                io.AddKeyEvent(igk, k.Pressed);

                if (k.Pressed && k.Unicode != 0)
                {
                    io.AddInputCharacter((uint)k.Unicode);
                }
            }
            consumed = io.WantCaptureKeyboard || io.WantTextInput;
        }
        else if (evt is InputEventPanGesture pg)
        {
            _mouseWheel = new Vector2(-pg.Delta.x, -pg.Delta.y);
            consumed = io.WantCaptureMouse;
        }
        else if (evt is InputEventJoypadButton jb)
        {
            if (ImGuiInput.JoypadButtonMap.TryGetValue(jb.ButtonIndex, out var igk))
            {
                io.AddKeyEvent(igk, jb.Pressed);
            }
        }
        else if (evt is InputEventJoypadMotion jm)
        {
            bool pressed = true;
            float v = jm.AxisValue;
            if (Math.Abs(v) < JoyAxisDeadZone)
            {
                v = 0f;
                pressed = false;
            }
            switch (jm.Axis)
            {
                case JoyAxis.LeftX:
                    io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickRight, pressed, v);
                    break;
                case JoyAxis.LeftY:
                    io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickDown, pressed, v);
                    break;
                case JoyAxis.RightX:
                    io.AddKeyAnalogEvent(ImGuiKey.GamepadRStickRight, pressed, v);
                    break;
                case JoyAxis.RightY:
                    io.AddKeyAnalogEvent(ImGuiKey.GamepadRStickDown, pressed, v);
                    break;
                case JoyAxis.TriggerLeft:
                    io.AddKeyAnalogEvent(ImGuiKey.GamepadL2, pressed, v);
                    break;
                case JoyAxis.TriggerRight:
                    io.AddKeyAnalogEvent(ImGuiKey.GamepadR2, pressed, v);
                    break;
            };
        }

        return consumed;
    }

    private static void UpdateKeyMods(ImGuiIOPtr io, InputEventKey k)
    {
        io.AddKeyEvent(ImGuiKey.ModCtrl, k.CtrlPressed);
        io.AddKeyEvent(ImGuiKey.ModShift, k.ShiftPressed);
        io.AddKeyEvent(ImGuiKey.ModAlt, k.AltPressed);
        io.AddKeyEvent(ImGuiKey.ModSuper, k.MetaPressed);
    }

    private static void RenderDrawData(ImDrawDataPtr drawData, RID parent)
    {
        // allocate our CanvasItem pool as needed
        int neededNodes = 0;
        for (int i = 0; i < drawData.CmdListsCount; ++i)
        {
            var cmdBuf = drawData.CmdListsRange[i].CmdBuffer;
            neededNodes += cmdBuf.Size;
            for (int j = 0; j < cmdBuf.Size; ++j)
            {
                if (cmdBuf[j].ElemCount == 0)
                    --neededNodes;
            }
        }

        while (_children.Count < neededNodes)
        {
            RID newChild = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(newChild, parent);
            RenderingServer.CanvasItemSetDrawIndex(newChild, _children.Count);
            _children.Add(newChild);
        }

        // trim unused nodes
        while (_children.Count > neededNodes)
        {
            int idx = _children.Count - 1;
            RenderingServer.FreeRid(_children[idx]);
            _children.RemoveAt(idx);
        }

        // render
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);
        int nodeN = 0;

        for (int n = 0; n < drawData.CmdListsCount; ++n)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];

            int nVert = cmdList.VtxBuffer.Size;

            var vertices = new Vector2[nVert];
            var colors = new Color[nVert];
            var uvs = new Vector2[nVert];

            for (int i = 0; i < cmdList.VtxBuffer.Size; ++i)
            {
                var v = cmdList.VtxBuffer[i];
                vertices[i] = new Vector2(v.pos.X, v.pos.Y);
                // need to reverse the color bytes
                uint rgba = v.col;
                float r = (rgba & 0xFFu) / 255f;
                rgba >>= 8;
                float g = (rgba & 0xFFu) / 255f;
                rgba >>= 8;
                float b = (rgba & 0xFFu) / 255f;
                rgba >>= 8;
                float a = (rgba & 0xFFu) / 255f;
                colors[i] = new Color(r, g, b, a);
                uvs[i] = new Vector2(v.uv.X, v.uv.Y);
            }

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; ++cmdi)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                if (drawCmd.ElemCount == 0)
                {
                    continue;
                }

                var indices = new int[drawCmd.ElemCount];
                int idxOffset = (int)drawCmd.IdxOffset;
                for (int i = idxOffset, j = 0; i < idxOffset + drawCmd.ElemCount; ++i, ++j)
                {
                    indices[j] = cmdList.IdxBuffer[i];
                }

                RID child = _children[nodeN++];

                Texture2D tex = GetTexture(drawCmd.GetTexID());
                RenderingServer.CanvasItemClear(child);
                RenderingServer.CanvasItemSetClip(child, true);
                RenderingServer.CanvasItemSetCustomRect(child, true, new Rect2(
                    drawCmd.ClipRect.X,
                    drawCmd.ClipRect.Y,
                    drawCmd.ClipRect.Z - drawCmd.ClipRect.X,
                    drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                RenderingServer.CanvasItemAddTriangleArray(child, indices, vertices, colors, uvs, null, null, tex.GetRid(), -1);
            }
        }
    }
};
