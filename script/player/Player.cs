using Godot;
using System;

public partial class Player : CharacterBody2D
{
    // ================ 移动参数 ================
    [ExportGroup("移动")]
    [Export] public float MaxSpeed = 300f;             // 最大水平速度
    [Export] public float Acceleration = 1600f;        // 加速力度
    [Export] public float Friction = 1200f;             // 摩擦力/减速
    [Export] public float AirMultiplier = 0.65f;        // 空中控制系数（<1 降低空中机动性）

    // ================ 跳跃参数 ================
    [ExportGroup("跳跃")]
    [Export] public float JumpForce = -480f;            // 跳跃初速度（负值=向上）
    [Export] public float Gravity = 1400f;              // 重力加速度
    [Export] public float FallMax = 900f;               // 最大下落速度
    [Export] public float JumpCutMultiplier = 0.4f;     // 松键时速度衰减（可变跳跃高度）
    [Export] public float CoyoteTime = 0.08f;           // 离开地面后的缓冲时间
    [Export] public float JumpBufferTime = 0.08f;       // 落地前的跳跃输入缓冲

    // ================ 攻击参数 ================
    [ExportGroup("攻击")]
    [Export] public float AttackDuration = 0.15f;       // 攻击判定持续时长
    [Export] public Vector2 AttackBoxSize = new Vector2(50, 40);  // 攻击矩形大小
    [Export] public float AttackOffset = 30f;           // 攻击矩形离玩家中心的偏移
    [Export] public int AttackDamage = 1;               // 伤害值

    // ================ 外观 ================
    [ExportGroup("外观")]
    [Export] private Color _color = new Color(0.2f, 0.5f, 0.9f);
    private Vector2 _size = new Vector2(28, 44);

    // ================ 状态 ================
    private float _coyoteTimer = 0f;
    private float _jumpBufferTimer = 0f;
    private int _facingDirection = 1;       // 1=右, -1=左
    private bool _isAttacking = false;
    private float _attackTimer = 0f;
    private Godot.Collections.Array<Rid> _alreadyHit = new();   // 本次攻击已命中的对象

    public override void _Ready()
    {
        SetupInputActions();
    }

    private void SetupInputActions()
    {
        // 移动 - 左 (A)
        if (!InputMap.HasAction("move_left"))
        {
            InputMap.AddAction("move_left");
            var ev = new InputEventKey { Keycode = Key.A };
            InputMap.ActionAddEvent("move_left", ev);
        }

        // 移动 - 右 (D)
        if (!InputMap.HasAction("move_right"))
        {
            InputMap.AddAction("move_right");
            var ev = new InputEventKey { Keycode = Key.D };
            InputMap.ActionAddEvent("move_right", ev);
        }

        // 跳跃 (K)
        if (!InputMap.HasAction("jump"))
        {
            InputMap.AddAction("jump");
            var ev = new InputEventKey { Keycode = Key.K };
            InputMap.ActionAddEvent("jump", ev);
        }

        // 攻击 (J)
        if (!InputMap.HasAction("attack"))
        {
            InputMap.AddAction("attack");
            var ev = new InputEventKey { Keycode = Key.J };
            InputMap.ActionAddEvent("attack", ev);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        bool onFloor = IsOnFloor();

        // ---- 输入 ----
        float direction = Input.GetAxis("move_left", "move_right");
        bool jumpJustPressed = Input.IsActionJustPressed("jump");
        bool jumpReleased = Input.IsActionJustReleased("jump");
        bool attackPressed = Input.IsActionJustPressed("attack");

        // ---- 面朝方向（攻击时锁定转向） ----
        if (direction != 0 && !_isAttacking)
            _facingDirection = direction > 0 ? 1 : -1;

        // ---- 攻击 ----
        if (attackPressed && !_isAttacking)
        {
            StartAttack();
        }

        if (_isAttacking)
        {
            _attackTimer -= dt;
            if (_attackTimer <= 0f)
            {
                EndAttack();
            }
        }

        // ---- 重力 ----
        if (!onFloor)
        {
            Velocity += Vector2.Down * Gravity * dt;
        }

        // ---- Coyote Time（离开地面后仍能跳） ----
        if (onFloor)
            _coyoteTimer = CoyoteTime;
        else
            _coyoteTimer -= dt;

        // ---- Jump Buffer（提前按跳跃会在落地后自动触发） ----
        if (jumpJustPressed)
            _jumpBufferTimer = JumpBufferTime;
        else
            _jumpBufferTimer -= dt;

        // ---- 跳跃执行 ----
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            Velocity = new Vector2(Velocity.X, JumpForce);
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }

        // ---- 可变跳跃高度（轻点跳跃 = 跳得矮） ----
        if (jumpReleased && Velocity.Y < 0f)
        {
            Velocity = new Vector2(Velocity.X, Velocity.Y * JumpCutMultiplier);
        }

        // ---- 水平移动（基于加速度，手感更顺滑） ----
        float multiplier = onFloor ? 1f : AirMultiplier;
        float targetVelX = direction * MaxSpeed;
        float accel = direction != 0f ? Acceleration : Friction;

        Velocity = new Vector2(
            Mathf.MoveToward(Velocity.X, targetVelX, accel * multiplier * dt),
            Velocity.Y
        );

        // ---- 限制最大下落速度 ----
        if (Velocity.Y > FallMax)
            Velocity = new Vector2(Velocity.X, FallMax);

        MoveAndSlide();

        // 触发重绘（让 _Draw 更新）
        QueueRedraw();
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _attackTimer = AttackDuration;
        _alreadyHit.Clear();
        PerformHitDetection();
    }

    private void EndAttack()
    {
        _isAttacking = false;
        _attackTimer = 0f;
        _alreadyHit.Clear();
    }

    private void PerformHitDetection()
    {
        // 用物理空间查询检测攻击矩形范围内有哪些物体
        var space = GetWorld2D().DirectSpaceState;
        var shape = new RectangleShape2D();
        shape.Size = AttackBoxSize;

        Vector2 attackCenter = GlobalPosition + new Vector2(_facingDirection * AttackOffset, 0);
        var query = new PhysicsShapeQueryParameters2D();
        query.Shape = shape;
        query.Transform = new Transform2D(0, attackCenter);
        query.CollisionMask = 2;  // 检测碰撞层 2（木桩所在层）

        var results = space.IntersectShape(query);

        foreach (var result in results)
        {
            Rid rid = (Rid)result["rid"];

            // 避免同一攻击多次命中同一目标
            if (_alreadyHit.Contains(rid))
                continue;

            _alreadyHit.Add(rid);

            Node collider = result["collider"].As<Node>();
            if (collider != null && collider.HasMethod("TakeDamage"))
            {
                collider.Call("TakeDamage", AttackDamage);
            }
        }
    }

    public override void _Draw()
    {
        // 画玩家方块
        DrawRect(new Rect2(-_size.X / 2, -_size.Y / 2, _size.X, _size.Y), _color);

        // 画攻击矩形（绿色半透明）
        if (_isAttacking)
        {
            Vector2 offset = new Vector2(_facingDirection * AttackOffset, 0);
            Color attackColor = new Color(0f, 1f, 0f, 0.5f);
            DrawRect(new Rect2(
                -AttackBoxSize.X / 2 + offset.X,
                -AttackBoxSize.Y / 2 + offset.Y,
                AttackBoxSize.X,
                AttackBoxSize.Y
            ), attackColor);
        }
    }
}
