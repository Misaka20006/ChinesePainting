using Godot;

public partial class WoodenDummy : StaticBody2D
{
    // ================ 外观参数 ================
    [ExportGroup("外观")]
    [Export] private Vector2 _size = new Vector2(32, 48);
    [Export] private Color _normalColor = new Color(0.2f, 0.4f, 0.8f);   // 蓝色
    [Export] private Color _hitColor = new Color(0.9f, 0.2f, 0.2f);      // 红色
    [Export] private float _hitDuration = 0.4f;                            // 变红持续时间

    // ================ 状态 ================
    private Color _currentColor;
    private float _hitTimer = 0f;

    public override void _Ready()
    {
        _currentColor = _normalColor;
        // 放到敌人组，方便攻击检测
        AddToGroup("enemies");
    }

    /// <summary>
    /// 被攻击时由 Player 调用
    /// </summary>
    public void TakeDamage(int damage)
    {
        _currentColor = _hitColor;
        _hitTimer = _hitDuration;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_hitTimer > 0f)
        {
            _hitTimer -= (float)delta;
            if (_hitTimer <= 0f)
            {
                _currentColor = _normalColor;
                _hitTimer = 0f;
                QueueRedraw();
            }
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-_size.X / 2, -_size.Y / 2, _size.X, _size.Y), _currentColor);
    }
}
