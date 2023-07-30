using Godot;
using ImGuiNET;
using System;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace ImGuiGodot;

public static class ImGuiGD
{
    /// <summary>
    /// Deadzone for all axes
    /// </summary>
    public static float JoyAxisDeadZone
    {
        get => _deadZone;
        set
        {
            _gd.SetJoyAxisDeadZone(value);
            _deadZone = value;
        }
    }
    private static float _deadZone = 0.15f;

    /// <summary>
    /// Swap the functionality of the activate (face down) and cancel (face right) buttons
    /// </summary>
    public static bool JoyButtonSwapAB
    {
        get => _swapAB;
        set
        {
            _gd.SetJoyButtonSwapAB(value);
            _swapAB = value;
        }
    }
    private static bool _swapAB = false;

    /// <summary>
    /// Setting this property will reload fonts and modify the ImGuiStyle
    /// </summary>
    public static float Scale
    {
        get => _scale;
        set
        {
            if (_scale != value && value >= 0.25f)
            {
                _scale = value;
                RebuildFontAtlas();
            }
        }
    }
    private static float _scale = 1.0f;

    public static bool Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            _gd.SetVisible(_visible);
        }
    }
    private static bool _visible = true;

    private static readonly Internal.IPublicInterface _gd;

    static ImGuiGD()
    {
        bool useNative = ProjectSettings.HasSetting("autoload/imgui_godot_native");
        _gd = useNative ? new Internal.PublicInterfaceNative() : new Internal.PublicInterfaceNet();
    }

    public static IntPtr BindTexture(Texture2D tex)
    {
        return (IntPtr)tex.GetRid().Id;
    }

    public static void Init(Window mainWindow, Rid mainSubViewport, Resource configResource = null)
    {
        configResource ??= (Resource)((GDScript)GD.Load("res://addons/imgui-godot/scripts/ImGuiConfig.gd")).New();
        _gd.Init(mainWindow, mainSubViewport, configResource);
    }

    public static void ResetFonts()
    {
        _gd.ResetFonts();
    }

    public static void AddFont(FontFile fontData, int fontSize, bool merge = false)
    {
        _gd.AddFont(fontData, fontSize, merge);
    }

    public static void AddFontDefault()
    {
        _gd.AddFont(null, 13, false);
    }

    public static void RebuildFontAtlas()
    {
        _gd.RebuildFontAtlas(Scale);
    }

    public static void Update(double delta, Vector2 displaySize)
    {
        _gd.Update(delta, displaySize);
    }

    public static void Render()
    {
        _gd.Render();
    }

    public static void Shutdown()
    {
        _gd.Shutdown();
    }

    public static void Connect(Callable callable)
    {
        // if (UseNative)
        //     Engine.GetSingleton("ImGuiGD").Call("Connect", callable);
        _gd.Connect(callable);
    }

    public static void Connect(Action action)
    {
        Connect(Callable.From(action));
    }

    /// <returns>
    /// True if the InputEvent was consumed
    /// </returns>
    public static bool ProcessInput(InputEvent evt, Window window)
    {
        return Internal.State.Instance.Input.ProcessInput(evt, window);
    }

    public static void SyncImGuiPtrs()
    {
        _gd.SyncImGuiPtrs();
    }

    /// <summary>
    /// Call in _Ready() to use ImGui in editor
    /// Requires imgui-godot-native
    /// </summary>
    public static bool ToolInit()
    {
        SyncImGuiPtrs();
        return _gd.ToolInit();
    }

    public static bool SubViewportWidget(SubViewport vp)
    {
        return _gd.SubViewport(vp);
    }

    /// <summary>
    /// Extension method to translate between <see cref="Key"/> and <see cref="ImGuiKey"/>
    /// </summary>
    public static ImGuiKey ToImGuiKey(this Key key)
    {
        return Internal.Input.ConvertKey(key);
    }

    /// <summary>
    /// Extension method to translate between <see cref="JoyButton"/> and <see cref="ImGuiKey"/>
    /// </summary>
    public static ImGuiKey ToImGuiKey(this JoyButton button)
    {
        return Internal.Input.ConvertJoyButton(button);
    }

    /// <summary>
    /// Convert <see cref="Color"/> to ImGui color RGBA
    /// </summary>
    public static Vector4 ToVector4(this Color color)
    {
        return new Vector4(color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// Convert <see cref="Color"/> to ImGui color RGB
    /// </summary>
    public static Vector3 ToVector3(this Color color)
    {
        return new Vector3(color.R, color.G, color.B);
    }

    /// <summary>
    /// Convert RGB <see cref="Vector3"/> to <see cref="Color"/>
    /// </summary>
    public static Color ToColor(this Vector3 vec)
    {
        return new Color(vec.X, vec.Y, vec.Z);
    }

    /// <summary>
    /// Convert RGBA <see cref="Vector4"/> to <see cref="Color"/>
    /// </summary>
    public static Color ToColor(this Vector4 vec)
    {
        return new Color(vec.X, vec.Y, vec.Z, vec.W);
    }

    /// <summary>
    /// Set IniFilename, converting Godot path to native
    /// </summary>
    public static void SetIniFilename(this ImGuiIOPtr io, string fileName)
    {
        _gd.SetIniFilename(io, fileName);
    }
}
