using Godot;

namespace DemoProject;

public partial class ViewportArea : Area3D
{
    private MeshInstance3D _piece = null!;
    private MeshInstance3D _board = null!;
    private Texture2D _decalTexture = null!;

    public override void _Ready()
    {
        _piece = GetNode<MeshInstance3D>("%Piece");
        _board = GetNode<MeshInstance3D>("%Board");
        _decalTexture = GD.Load<Texture2D>("res://data/icon.svg");
    }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 position,
        Vector3 normal, int shapeIdx)
    {
        if (@event is InputEventMouseMotion mm)
        {
            _piece.Position = position;
            mm.Dispose(); // prevent "leaks"
        }
        else if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                _board.AddChild(new Decal
                {
                    TextureAlbedo = _decalTexture,
                    Scale = new(10, 10, 10),
                    Position = position,
                    CullMask = 1,
                });
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                var child = _board.GetChildOrNull<Decal>(-1);
                if (child != null)
                {
                    _board.RemoveChild(child);
                }
            }
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed)
        {
            if (k.Keycode == Key.R)
            {
                foreach (var child in _board.GetChildren())
                {
                    _board.RemoveChild(child);
                    child.QueueFree();
                }
            }
        }
    }
}
