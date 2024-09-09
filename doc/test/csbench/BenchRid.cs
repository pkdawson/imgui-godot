using Godot;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace csbench;

[MemoryDiagnoser]
public class BenchRid
{
    private readonly Func<ulong, Rid> _constructRid;
    private readonly ulong _id = 12345;

    public BenchRid()
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
        _constructRid = dm.CreateDelegate<Func<ulong, Rid>>();
    }

    [Benchmark]
    public Rid ConstructRid_Emitted()
    {
        return _constructRid(_id);
    }

    [Benchmark]
    public Rid ConstructRid_Marshal()
    {
        nint ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Rid>());
        byte[] bytes = BitConverter.GetBytes(_id);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Rid rv = Marshal.PtrToStructure<Rid>(ptr);
        Marshal.FreeHGlobal(ptr);
        return rv;
    }

    [Benchmark]
    public Rid ConstructRid_MemoryMarshal_WriteRead()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        MemoryMarshal.TryWrite(bytes, in _id);
        return MemoryMarshal.Read<Rid>(bytes);
    }

    [Benchmark]
    public Rid ConstructRid_MemoryMarshal_SpanCast()
    {
        ReadOnlySpan<ulong> uspan = new(in _id);
        ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<ulong, byte>(uspan);
        return MemoryMarshal.Read<Rid>(bytes);
    }

    [Benchmark]
    public Rid ConstructRid_UnsafeAccessor()
    {
        return RidConstructor(_id);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern Rid RidConstructor(ulong id);

    [Benchmark(Baseline = true)]
    public unsafe Rid ConstructRid_Unsafe_DirectCopy()
    {
        Rid rv;
        fixed (ulong* p = &_id)
        {
            Buffer.MemoryCopy(p, &rv, sizeof(Rid), sizeof(ulong));
        }
        return rv;
    }
}
