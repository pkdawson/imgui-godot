using Godot;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ImGuiGodot.Internal;

internal static class Util
{
    public static readonly Func<ulong, RID> ConstructRID;

    static Util()
    {
        ConstructorInfo cinfo = typeof(RID).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(ulong) });
        if (cinfo is null)
        {
            throw new PlatformNotSupportedException("failed to get RID constructor");
        }

        DynamicMethod dm = new("ConstructRID", typeof(RID), new[] { typeof(ulong) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, cinfo);
        il.Emit(OpCodes.Ret);
        ConstructRID = dm.CreateDelegate<Func<ulong, RID>>();
    }
}
