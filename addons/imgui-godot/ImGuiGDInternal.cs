using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CursorShape = Godot.DisplayServer.CursorShape;

internal static class ImGuiGDInternal
{
    internal static SubViewport CurrentSubViewport { get; set; }
    internal static System.Numerics.Vector2 CurrentSubViewportPos { get; set; }

    private static Texture2D _fontTexture;
    private static List<RID> _children = new();
    private static Vector2 _mouseWheel = Vector2.Zero;
    private static ImGuiMouseCursor _currentCursor = ImGuiMouseCursor.None;
    private static GCHandle _backendName = GCHandle.Alloc(Encoding.ASCII.GetBytes("imgui_impl_godot4"), GCHandleType.Pinned);

    // necessary because we can't construct arbitrary RIDs without using reflection
    private static Dictionary<IntPtr, RID> _rids = new();

    public static IntPtr BindTexture(Texture2D tex)
    {
        RID rid = tex.GetRid();
        IntPtr texid = (IntPtr)rid.Id;
        _rids.TryAdd(texid, rid);
        return texid;
    }

    public static void UnbindTexture(IntPtr texid)
    {
        _rids.Remove(texid);
    }

    public static unsafe ImFontPtr AddFont(FontFile fontData, float fontSize, bool merge)
    {
        ImVector ranges = GetRanges(fontData);
        return AddFont(fontData, fontSize, ranges.Data, merge);
    }

    public static unsafe ImFontPtr AddFont(FontFile fontData, float fontSize, IntPtr glyphRanges, bool merge)
    {
        ImFontPtr rv = null;
        // ImFontConfig has a constructor, so we don't zero it
        ImFontConfig* fc = ImGuiNative.ImFontConfig_ImFontConfig();

        if (merge)
        {
            fc->MergeMode = 1;
        }

        string name = string.Format("{0}, {1}px", System.IO.Path.GetFileName(fontData.ResourcePath), fontSize);
        for (int i = 0; i < name.Length && i < 40; ++i)
        {
            fc->Name[i] = Convert.ToByte(name[i]);
        }
        rv = AddFont(fontData, fontSize, glyphRanges, fc);

        if (merge)
        {
            ImGui.GetIO().Fonts.Build();
        }
        ImGuiNative.ImFontConfig_destroy(fc);
        return rv;
    }

    private static unsafe ImVector GetRanges(Font font)
    {
        var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
        builder.AddText(font.GetSupportedChars());
        builder.BuildRanges(out ImVector vec);
        builder.Destroy();
        return vec;
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
        Marshal.Copy((IntPtr)pixelData, pixels, 0, pixels.Length);

        Image img = new();
        img.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);

        var imgtex = ImageTexture.CreateFromImage(img);
        _fontTexture = imgtex;
        io.Fonts.SetTexID(BindTexture(_fontTexture));
        io.Fonts.ClearTexData();
    }

    public static void Init()
    {
        if (ImGui.GetCurrentContext() != IntPtr.Zero)
        {
            ImGui.DestroyContext();
        }

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();

        io.BackendFlags = 0;
        io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

        unsafe
        {
            io.NativePtr->BackendPlatformName = (byte*)_backendName.AddrOfPinnedObject();
            io.NativePtr->BackendRendererName = (byte*)_backendName.AddrOfPinnedObject();
        }
    }

    public static void Update(double delta, Viewport vp)
    {
        var io = ImGui.GetIO();
        var vpSize = vp.GetVisibleRect().Size;
        io.DisplaySize = new(vpSize.x, vpSize.y);
        io.DeltaTime = (float)delta;

        if (io.WantSetMousePos)
        {
            vp.WarpMouse(new(io.MousePos.X, io.MousePos.Y));
        }

        // scrolling works better if we allow no more than one event per frame
        if (_mouseWheel != Vector2.Zero)
        {
            io.AddMouseWheelEvent(_mouseWheel.x, _mouseWheel.y);
            _mouseWheel = Vector2.Zero;
        }

        if (io.WantCaptureMouse)
        {
            var newCursor = ImGui.GetMouseCursor();
            if (newCursor != _currentCursor)
            {
                DisplayServer.CursorSetShape(ConvertCursorShape(newCursor));
                _currentCursor = newCursor;
            }
        }
        else
        {
            _currentCursor = ImGuiMouseCursor.None;
        }

        CurrentSubViewport = null;
        ImGui.NewFrame();
    }

    public static void ClearCanvasItems()
    {
        foreach (RID rid in _children)
        {
            RenderingServer.FreeRid(rid);
        }
        _children.Clear();
    }

    public static bool ProcessInput(InputEvent evt)
    {
        if (CurrentSubViewport != null)
        {
            // TODO: correct mouse pos
            CurrentSubViewport.PushInput(evt, true);
            if (evt is not InputEventMouseMotion)
                return true;
        }

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
            UpdateKeyMods(io);
            ImGuiKey igk = ConvertKey(k.Keycode);
            if (igk != ImGuiKey.None)
            {
                io.AddKeyEvent(igk, k.Pressed);

                if (k.Pressed && k.Unicode != 0 && io.WantTextInput)
                {
                    io.AddInputCharacter((uint)k.Unicode);
                }
            }
            consumed = io.WantCaptureKeyboard || io.WantTextInput;
        }
        else if (evt is InputEventPanGesture pg)
        {
            _mouseWheel = new(-pg.Delta.x, -pg.Delta.y);
            consumed = io.WantCaptureMouse;
        }
        else if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.NavEnableGamepad))
        {
            if (evt is InputEventJoypadButton jb)
            {
                ImGuiKey igk = ConvertJoyButton(jb.ButtonIndex);
                if (igk != ImGuiKey.None)
                {
                    io.AddKeyEvent(igk, jb.Pressed);
                    consumed = true;
                }
            }
            else if (evt is InputEventJoypadMotion jm)
            {
                bool pressed = true;
                float v = jm.AxisValue;
                if (Math.Abs(v) < ImGuiGD.JoyAxisDeadZone)
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
                consumed = true;
            }
        }

        return consumed;
    }

    private static void UpdateKeyMods(ImGuiIOPtr io)
    {
        io.AddKeyEvent(ImGuiKey.ModCtrl, Input.IsKeyPressed(Key.Ctrl));
        io.AddKeyEvent(ImGuiKey.ModShift, Input.IsKeyPressed(Key.Shift));
        io.AddKeyEvent(ImGuiKey.ModAlt, Input.IsKeyPressed(Key.Alt));
        io.AddKeyEvent(ImGuiKey.ModSuper, Input.IsKeyPressed(Key.SuperL));
    }

    public static void RenderDrawData(ImDrawDataPtr drawData, RID parent)
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
                vertices[i] = new(v.pos.X, v.pos.Y);
                // need to reverse the color bytes
                uint rgba = v.col;
                float r = (rgba & 0xFFu) / 255f;
                rgba >>= 8;
                float g = (rgba & 0xFFu) / 255f;
                rgba >>= 8;
                float b = (rgba & 0xFFu) / 255f;
                rgba >>= 8;
                float a = (rgba & 0xFFu) / 255f;
                colors[i] = new(r, g, b, a);
                uvs[i] = new(v.uv.X, v.uv.Y);
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

                IntPtr texid = drawCmd.GetTexID();
                RID texrid = _rids[texid];
                RenderingServer.CanvasItemClear(child);
                RenderingServer.CanvasItemSetClip(child, true);
                RenderingServer.CanvasItemSetCustomRect(child, true, new Rect2(
                    drawCmd.ClipRect.X,
                    drawCmd.ClipRect.Y,
                    drawCmd.ClipRect.Z - drawCmd.ClipRect.X,
                    drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                RenderingServer.CanvasItemAddTriangleArray(child, indices, vertices, colors, uvs, null, null, texrid, -1);
            }
        }
    }

    private static CursorShape ConvertCursorShape(ImGuiMouseCursor cur) => cur switch
    {
        ImGuiMouseCursor.Arrow => CursorShape.Arrow,
        ImGuiMouseCursor.TextInput => CursorShape.Ibeam,
        ImGuiMouseCursor.ResizeAll => CursorShape.Move,
        ImGuiMouseCursor.ResizeNS => CursorShape.Vsize,
        ImGuiMouseCursor.ResizeEW => CursorShape.Hsize,
        ImGuiMouseCursor.ResizeNESW => CursorShape.Bdiagsize,
        ImGuiMouseCursor.ResizeNWSE => CursorShape.Fdiagsize,
        ImGuiMouseCursor.Hand => CursorShape.PointingHand,
        ImGuiMouseCursor.NotAllowed => CursorShape.Forbidden,
        _ => CursorShape.Arrow,
    };

    private static ImGuiKey ConvertJoyButton(JoyButton btn) => btn switch
    {
        JoyButton.Start => ImGuiKey.GamepadStart,
        JoyButton.Back => ImGuiKey.GamepadBack,
        JoyButton.Y => ImGuiKey.GamepadFaceUp,
        JoyButton.A => ImGuiGD.JoyButtonSwapAB ? ImGuiKey.GamepadFaceRight : ImGuiKey.GamepadFaceDown,
        JoyButton.X => ImGuiKey.GamepadFaceLeft,
        JoyButton.B => ImGuiGD.JoyButtonSwapAB ? ImGuiKey.GamepadFaceDown : ImGuiKey.GamepadFaceRight,
        JoyButton.DpadUp => ImGuiKey.GamepadDpadUp,
        JoyButton.DpadDown => ImGuiKey.GamepadDpadDown,
        JoyButton.DpadLeft => ImGuiKey.GamepadDpadLeft,
        JoyButton.DpadRight => ImGuiKey.GamepadDpadRight,
        JoyButton.LeftShoulder => ImGuiKey.GamepadL1,
        JoyButton.RightShoulder => ImGuiKey.GamepadR1,
        JoyButton.LeftStick => ImGuiKey.GamepadL3,
        JoyButton.RightStick => ImGuiKey.GamepadR3,
        _ => ImGuiKey.None
    };

    private static ImGuiKey ConvertKey(Key k) => k switch
    {
        Key.Tab => ImGuiKey.Tab,
        Key.Left => ImGuiKey.LeftArrow,
        Key.Right => ImGuiKey.RightArrow,
        Key.Up => ImGuiKey.UpArrow,
        Key.Down => ImGuiKey.DownArrow,
        Key.Pageup => ImGuiKey.PageUp,
        Key.Pagedown => ImGuiKey.PageDown,
        Key.Home => ImGuiKey.Home,
        Key.End => ImGuiKey.End,
        Key.Insert => ImGuiKey.Insert,
        Key.Delete => ImGuiKey.Delete,
        Key.Backspace => ImGuiKey.Backspace,
        Key.Space => ImGuiKey.Space,
        Key.Enter => ImGuiKey.Enter,
        Key.Escape => ImGuiKey.Escape,
        Key.Ctrl => ImGuiKey.LeftCtrl,
        Key.Shift => ImGuiKey.LeftShift,
        Key.Alt => ImGuiKey.LeftAlt,
        Key.SuperL => ImGuiKey.LeftSuper,
        Key.SuperR => ImGuiKey.RightSuper,
        Key.Menu => ImGuiKey.Menu,
        Key.Key0 => ImGuiKey._0,
        Key.Key1 => ImGuiKey._1,
        Key.Key2 => ImGuiKey._2,
        Key.Key3 => ImGuiKey._3,
        Key.Key4 => ImGuiKey._4,
        Key.Key5 => ImGuiKey._5,
        Key.Key6 => ImGuiKey._6,
        Key.Key7 => ImGuiKey._7,
        Key.Key8 => ImGuiKey._8,
        Key.Key9 => ImGuiKey._9,
        Key.Apostrophe => ImGuiKey.Apostrophe,
        Key.Comma => ImGuiKey.Comma,
        Key.Minus => ImGuiKey.Minus,
        Key.Period => ImGuiKey.Period,
        Key.Slash => ImGuiKey.Slash,
        Key.Semicolon => ImGuiKey.Semicolon,
        Key.Equal => ImGuiKey.Equal,
        Key.Bracketleft => ImGuiKey.LeftBracket,
        Key.Backslash => ImGuiKey.Backslash,
        Key.Bracketright => ImGuiKey.RightBracket,
        Key.Quoteleft => ImGuiKey.GraveAccent,
        Key.Capslock => ImGuiKey.CapsLock,
        Key.Scrolllock => ImGuiKey.ScrollLock,
        Key.Numlock => ImGuiKey.NumLock,
        Key.Print => ImGuiKey.PrintScreen,
        Key.Pause => ImGuiKey.Pause,
        Key.Kp0 => ImGuiKey.Keypad0,
        Key.Kp1 => ImGuiKey.Keypad1,
        Key.Kp2 => ImGuiKey.Keypad2,
        Key.Kp3 => ImGuiKey.Keypad3,
        Key.Kp4 => ImGuiKey.Keypad4,
        Key.Kp5 => ImGuiKey.Keypad5,
        Key.Kp6 => ImGuiKey.Keypad6,
        Key.Kp7 => ImGuiKey.Keypad7,
        Key.Kp8 => ImGuiKey.Keypad8,
        Key.Kp9 => ImGuiKey.Keypad9,
        Key.KpPeriod => ImGuiKey.KeypadDecimal,
        Key.KpDivide => ImGuiKey.KeypadDivide,
        Key.KpMultiply => ImGuiKey.KeypadMultiply,
        Key.KpSubtract => ImGuiKey.KeypadSubtract,
        Key.KpAdd => ImGuiKey.KeypadAdd,
        Key.KpEnter => ImGuiKey.KeypadEnter,
        Key.A => ImGuiKey.A,
        Key.B => ImGuiKey.B,
        Key.C => ImGuiKey.C,
        Key.D => ImGuiKey.D,
        Key.E => ImGuiKey.E,
        Key.F => ImGuiKey.F,
        Key.G => ImGuiKey.G,
        Key.H => ImGuiKey.H,
        Key.I => ImGuiKey.I,
        Key.J => ImGuiKey.J,
        Key.K => ImGuiKey.K,
        Key.L => ImGuiKey.L,
        Key.M => ImGuiKey.M,
        Key.N => ImGuiKey.N,
        Key.O => ImGuiKey.O,
        Key.P => ImGuiKey.P,
        Key.Q => ImGuiKey.Q,
        Key.R => ImGuiKey.R,
        Key.S => ImGuiKey.S,
        Key.T => ImGuiKey.T,
        Key.U => ImGuiKey.U,
        Key.V => ImGuiKey.V,
        Key.W => ImGuiKey.W,
        Key.X => ImGuiKey.X,
        Key.Y => ImGuiKey.Y,
        Key.Z => ImGuiKey.Z,
        Key.F1 => ImGuiKey.F1,
        Key.F2 => ImGuiKey.F2,
        Key.F3 => ImGuiKey.F3,
        Key.F4 => ImGuiKey.F4,
        Key.F5 => ImGuiKey.F5,
        Key.F6 => ImGuiKey.F6,
        Key.F7 => ImGuiKey.F7,
        Key.F8 => ImGuiKey.F8,
        Key.F9 => ImGuiKey.F9,
        Key.F10 => ImGuiKey.F10,
        Key.F11 => ImGuiKey.F11,
        Key.F12 => ImGuiKey.F12,
        _ => ImGuiKey.None
    };
};
