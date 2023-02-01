using Godot;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ImGuiGodot.Internal;

internal static class Util
{
    public static readonly Func<ulong, Rid> ConstructRid;

    static Util()
    {
        ConstructorInfo cinfo = typeof(Rid).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(ulong) });
        if (cinfo is null)
        {
            throw new PlatformNotSupportedException("failed to get Rid constructor");
        }

        DynamicMethod dm = new("ConstructRid", typeof(Rid), new[] { typeof(ulong) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, cinfo);
        il.Emit(OpCodes.Ret);
        ConstructRid = dm.CreateDelegate<Func<ulong, Rid>>();
    }

    public static void AddLayerSubViewport(Node parent, out SubViewportContainer subViewportContainer, out SubViewport subViewport)
    {
        subViewportContainer = new SubViewportContainer
        {
            Name = "ImGuiLayer_SubViewportContainer",
            LayoutMode = 1, // LAYOUT_MODE_ANCHORS
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Stretch = true
        };

        subViewport = new SubViewport
        {
            Name = "ImGuiLayer_SubViewport",
            TransparentBg = true,
            HandleInputLocally = false,
            GuiDisableInput = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };

        subViewportContainer.AddChild(subViewport);
        parent.AddChild(subViewportContainer);
    }
}
