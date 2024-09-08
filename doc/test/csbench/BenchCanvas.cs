using ImGuiNET;
using System.Runtime.InteropServices;

namespace csbench;

[MemoryDiagnoser]
public class BenchCanvas
{
    private readonly ImDrawVertPtr v;
    private readonly Godot.Vector2[] points = new Godot.Vector2[1];
    private readonly Godot.Color[] colors = new Godot.Color[1];
    private readonly Godot.Vector2[] uvs = new Godot.Vector2[1];

    public unsafe BenchCanvas()
    {
        nint ptr = Marshal.AllocHGlobal(sizeof(ImDrawVert));
        v = new(ptr)
        {
            col = 0x12345678,
            uv = new(0.1f, 0.2f),
            pos = new(0.3f, 0.4f)
        };
    }

    [Benchmark(Baseline = true)]
    public void ConvertVertex1()
    {
        points[0] = new(v.pos.X, v.pos.Y);
        uint rgba = v.col;
        float r = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        float g = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        float b = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        float a = (rgba & 0xFFu) / 255f;
        colors[0] = new(r, g, b, a);
        uvs[0] = new(v.uv.X, v.uv.Y);
    }

    // same as #1
    [Benchmark]
    public void ConvertVertex2()
    {
        ref var out_pos = ref points[0];
        ref var out_color = ref colors[0];
        ref var out_uv = ref uvs[0];

        out_pos.X = v.pos.X;
        out_pos.Y = v.pos.Y;
        uint rgba = v.col;
        out_color.R = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        out_color.G = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        out_color.B = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        out_color.A = (rgba & 0xFFu) / 255f;
        out_uv.X = v.uv.X;
        out_uv.Y = v.uv.Y;
    }

    // this is a bit faster
    [Benchmark]
    public unsafe void ConvertVertex3()
    {
        ref var out_pos = ref points[0];
        ref var out_color = ref colors[0];
        ref var out_uv = ref uvs[0];

        ImDrawVert* p = v;

        out_pos.X = p->pos.X;
        out_pos.Y = p->pos.Y;
        uint rgba = p->col;
        out_color.R = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        out_color.G = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        out_color.B = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        out_color.A = (rgba & 0xFFu) / 255f;
        out_uv.X = p->uv.X;
        out_uv.Y = p->uv.Y;
    }

    // same as #3
    [Benchmark]
    public unsafe void ConvertVertex4()
    {
        ImDrawVert* p = v;

        points[0] = new(p->pos.X, p->pos.Y);
        uint rgba = p->col;
        float r = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        float g = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        float b = (rgba & 0xFFu) / 255f;
        rgba >>= 8;
        float a = (rgba & 0xFFu) / 255f;
        colors[0] = new(r, g, b, a);
        uvs[0] = new(p->uv.X, p->uv.Y);
    }
}
