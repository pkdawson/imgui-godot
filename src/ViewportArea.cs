using Godot;

public partial class ViewportArea : Area3D
{
    private MeshInstance3D piece;
    private MeshInstance3D board;
    private Texture2D decalTexture;

    public override void _Ready()
    {
        piece = GetNode<MeshInstance3D>("%Piece");
        board = GetNode<MeshInstance3D>("%Board");
        decalTexture = GD.Load<Texture2D>("res://data/icon.svg");
    }

    public override void _InputEvent(Camera3D cam, InputEvent evt, Vector3 pos, Vector3 normal, long shapeIdx)
    {
        if (evt is InputEventMouseMotion)
        {
            piece.Position = pos;
        }
        else if (evt is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                var decal = new Decal();
                decal.TextureAlbedo = decalTexture;
                decal.Scale = new(10, 10, 10);
                decal.Position = pos;
                board.AddChild(decal);
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                var child = board.GetChildOrNull<Decal>(-1);
                if (child != null)
                {
                    board.RemoveChild(child);
                }
            }
        }
    }

    public override void _UnhandledKeyInput(InputEvent evt)
    {
        if (evt is InputEventKey k && k.Pressed)
        {
            if (k.Keycode == Key.R)
            {
                foreach (var child in board.GetChildren())
                {
                    board.RemoveChild(child);
                }
            }
        }
    }
}
