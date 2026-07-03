using Godot;

public partial class Ground : StaticBody2D
{
    [ExportGroup("外观")]
    [Export] private Color _color = new Color(0.35f, 0.55f, 0.25f); // 草绿色
    [Export] private Vector2 _size = new Vector2(800, 48);

    public override void _Draw()
    {
        // 画一个矩形地面
        DrawRect(new Rect2(-_size.X / 2, -_size.Y / 2, _size.X, _size.Y), _color);
    }
}
