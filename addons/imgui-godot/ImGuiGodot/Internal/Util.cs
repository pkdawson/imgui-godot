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
        ConstructorInfo cinfo = typeof(Rid).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(ulong) }) ??
            throw new PlatformNotSupportedException("failed to get Rid constructor");
        DynamicMethod dm = new("ConstructRid", typeof(Rid), new[] { typeof(ulong) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, cinfo);
        il.Emit(OpCodes.Ret);
        ConstructRid = dm.CreateDelegate<Func<ulong, Rid>>();
    }

    public static Rid AddLayerSubViewport(Node parent)
    {
        Rid svp = RenderingServer.ViewportCreate();
        RenderingServer.ViewportSetTransparentBackground(svp, true);
        RenderingServer.ViewportSetUpdateMode(svp, RenderingServer.ViewportUpdateMode.Always);
        RenderingServer.ViewportSetClearMode(svp, RenderingServer.ViewportClearMode.Always);
        RenderingServer.ViewportSetActive(svp, true);
        RenderingServer.ViewportSetParentViewport(svp, parent.GetWindow().GetViewportRid());
        return svp;
    }
}
