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
    private readonly nint _buf;

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

        _buf = Marshal.AllocHGlobal(Marshal.SizeOf<Rid>());
    }

    [Benchmark(Baseline = true)]
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
    public Rid ConstructRid_Marshal_PreAlloc()
    {
        // not thread-safe
        byte[] bytes = BitConverter.GetBytes(_id);
        Marshal.Copy(bytes, 0, _buf, bytes.Length);
        Rid rv = Marshal.PtrToStructure<Rid>(_buf);
        return rv;
    }

    [Benchmark]
    public unsafe Rid ConstructRid_Unsafe()
    {
        Rid rv;
        byte[] bytes = BitConverter.GetBytes(_id);
        fixed (byte* pbytes = bytes)
        {
            Buffer.MemoryCopy(pbytes, &rv, sizeof(Rid), bytes.Length);
        }
        return rv;
    }

    [Benchmark]
    public unsafe Rid ConstructRid_Unsafe_Direct()
    {
        Rid rv;
        fixed (ulong* p = &_id)
        {
            Buffer.MemoryCopy(p, &rv, sizeof(Rid), sizeof(ulong));
        }
        return rv;
    }
}
