using Godot;
using System;
#if GODOT_PC
using System.Reflection;
using System.Reflection.Emit;
#else
using System.Diagnostics;
using System.Runtime.InteropServices;
#endif

namespace ImGuiGodot.Internal;

internal static class Util
{
#if GODOT_PC
    // this is ~15x faster, so keep it for non-AOT platforms

    public static readonly Func<ulong, Rid> ConstructRid;

    static Util()
    {
        ConstructorInfo cinfo = typeof(Rid).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(ulong)]) ??
            throw new PlatformNotSupportedException("failed to get Rid constructor");
        DynamicMethod dm = new("ConstructRid", typeof(Rid), [typeof(ulong)]);
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, cinfo);
        il.Emit(OpCodes.Ret);
        ConstructRid = dm.CreateDelegate<Func<ulong, Rid>>();
    }
#else
    public static Rid ConstructRid(ulong id)
    {
        Debug.Assert(Marshal.SizeOf<Rid>() == sizeof(ulong));
        nint ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Rid>());
        byte[] bytes = BitConverter.GetBytes(id);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Rid rv = Marshal.PtrToStructure<Rid>(ptr);
        Marshal.FreeHGlobal(ptr);
        return rv;
    }
#endif
}
