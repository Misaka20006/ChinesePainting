using Godot;

public partial class PlayerController : CharacterBody2D
{
	public int playerID = 1;
	public bool isLocalPlayer = true;
	public PlayerType playerType = PlayerType.None;

	public enum PlayerType
	{
		None,
		LocalKeyboard,
		NetworkClient
	}

	[Export] public AnimationPlayer animator;
	[Export] public PlayerHealth PlayerHealthComponent;

	[Export] public float moveSpeed = 8f;
	[Export] public float jumpForce = 10f;
	[Export] public float jumpHoldForce = 5f;
	[Export] public float jumpHoldDuration = 0.4f;

	[Export] public Node2D bulletSpawn;
	[Export] public float bulletSpeed = 10f;
	[Export] public float shootCD = 0.3f;
	[Export] public AudioStream shootSound;
	private float shootCDTimer = 0f;

	[Export] public Vector2 upShootOffset = new Vector2(0, 0.2f);
	[Export] public Vector2 idleOffset = new Vector2(0, 0.2f);
	[Export] public Vector2 diagonalUpShootOffset = new Vector2(0.15f, 0.15f);
	[Export] public Vector2 downShootOffset = new Vector2(0.15f, -0.15f);

	public Key moveLeft = Key.A;
	public Key moveRight = Key.D;
	public Key jumpKey = Key.W;
	public Key shootKey = Key.J;
	public Key aimUp = Key.W;
	public Key aimDown = Key.S;

	private float jumpTimeCounter = 0f;
	private bool canJump = true;

	private float gravity;

	public override void _Ready()
	{
		gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
		InitializeControls();

		animator = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		PlayerHealthComponent = GetNodeOrNull<PlayerHealth>(".");
	}

	public override void _Process(double delta)
	{
		if (!isLocalPlayer && playerType != PlayerType.LocalKeyboard)
			return;

		HandleInput(delta);
		UpdateShootCooldown(delta);
	}

	private void InitializeControls()
	{
		if (playerType == PlayerType.LocalKeyboard)
		{
			LoadCustomControls();
		}
	}

	private void HandleInput(double delta)
	{
		Walk();
		Jump(delta);
		HandleShooting();
	}

	private void Walk()
	{
		float horizontal = GetHorizontalInput();
		if (horizontal != 0)
		{
			if (animator != null) animator.Play("walk");
			Velocity = new Vector2(horizontal * moveSpeed, Velocity.Y);
			Scale = new Vector2(horizontal < 0 ? -1 : 1, 1);
		}
		else
		{
			if (animator != null) animator.Play("idle");
		}
	}

	private void Jump(double delta)
	{
		if (GetJumpPressed() && IsOnFloor())
		{
			if (animator != null) animator.Play("jump");
			canJump = false;
			jumpTimeCounter = jumpHoldDuration;
			Velocity = new Vector2(Velocity.X, -jumpForce);
		}

		if (GetJumpHeld() && !canJump)
		{
			if (jumpTimeCounter > 0)
			{
				Velocity = new Vector2(Velocity.X, -jumpHoldForce);
				jumpTimeCounter -= (float)delta;
			}
			else
			{
				canJump = true;
			}
		}

		if (GetJumpReleased())
		{
			canJump = true;
		}

		if (Velocity.Y > 0)
		{
			if (animator != null)
			{
				animator.Play("fall");
			}
		}
	}

	private void HandleShooting()
	{
		if (IsShooting() && shootCDTimer <= 0)
		{
			Shoot();
			shootCDTimer = shootCD;
		}
	}

	private void UpdateShootCooldown(double delta)
	{
		shootCDTimer -= (float)delta;
	}

	private void Shoot()
	{
		Vector2 shootDir = GetShootDirection();
		Vector2 shootPosition = GetShootPosition(shootDir);

		if (BulletManager.Instance != null)
		{
			BulletManager.Instance.SpawnPlayerBullet(shootPosition, 0, shootDir, bulletSpeed, playerID);
		}

		if (shootSound != null)
		{
			AudioStreamPlayer2D player = new AudioStreamPlayer2D();
			player.Stream = shootSound;
			GetParent().AddChild(player);
			player.Play();
			player.Finished += player.QueueFree;
		}
	}

	private float GetHorizontalInput()
	{
		if (playerType == PlayerType.LocalKeyboard)
		{
			float left = Input.IsKeyPressed(moveLeft) ? -1 : 0;
			float right = Input.IsKeyPressed(moveRight) ? 1 : 0;
			return left + right;
		}
		return 0;
	}

	public float GetVerticalInput()
	{
		if (playerType == PlayerType.LocalKeyboard)
		{
			float up = Input.IsKeyPressed(aimUp) ? 1 : 0;
			float down = Input.IsKeyPressed(aimDown) ? -1 : 0;
			return up + down;
		}
		return 0;
	}

	private bool GetJumpPressed()
	{
		if (playerType == PlayerType.LocalKeyboard)
			return Input.IsKeyPressed(jumpKey) && !canJump;
		return false;
	}

	private bool GetJumpHeld()
	{
		if (playerType == PlayerType.LocalKeyboard)
			return Input.IsKeyPressed(jumpKey);
		return false;
	}

	private bool GetJumpReleased()
	{
		if (playerType == PlayerType.LocalKeyboard)
			return !Input.IsKeyPressed(jumpKey);
		return false;
	}

	public bool IsShooting()
	{
		if (playerType == PlayerType.LocalKeyboard)
			return Input.IsKeyPressed(shootKey);
		return false;
	}

	private Vector2 GetShootDirection()
	{
		float dir = Scale.X >= 0 ? 1 : -1;
		float vertical = GetVerticalInput();

		if (IsOnFloor())
		{
			if (vertical > 0.1f)
			{
				return Vector2.Up;
			}
			else
			{
				return new Vector2(dir, 0);
			}
		}
		else
		{
			if (vertical > 0.1f)
			{
				return Vector2.Up;
			}
			else if (vertical < -0.1f)
			{
				return Vector2.Down;
			}
			else
			{
				return new Vector2(dir, 0);
			}
		}
	}

	private Vector2 GetShootPosition(Vector2 direction)
	{
		float dir = Scale.X >= 0 ? 1 : -1;
		float verticalInput = GetVerticalInput();
		bool facingLeft = Scale.X < 0;

		Vector2 spawnPos = bulletSpawn != null ? bulletSpawn.GlobalPosition : GlobalPosition;

		if (IsOnFloor())
		{
			if (verticalInput < 0f)
			{
				Vector2 offset = downShootOffset;
				if (facingLeft) offset.X *= -1;
				spawnPos += offset;
				return spawnPos;
			}

			if (direction.Y > 0.8f && Mathf.Abs(direction.X) < 0.2f)
			{
				spawnPos += upShootOffset;
			}
			else if (direction.Y > 0.2f && Mathf.Abs(direction.X) > 0.2f)
			{
				Vector2 offset = diagonalUpShootOffset;
				if (facingLeft) offset.X *= -1;
				spawnPos += offset;
			}
			else
			{
				Vector2 offset = idleOffset;
				if (facingLeft) offset.X *= -1;
				spawnPos += offset;
			}
		}
		else
		{
			spawnPos += direction;
		}

		return spawnPos;
	}

	private void LoadCustomControls()
	{
		if (CustomKeybindManager.Instance != null)
		{
			ControlConfig config = CustomKeybindManager.Instance.GetPlayerControls(playerID);
			if (config != null)
			{
				moveLeft = config.moveLeft;
				moveRight = config.moveRight;
				jumpKey = config.jumpKey;
				shootKey = config.shootKey;
				aimUp = config.aimUp;
				aimDown = config.aimDown;
			}
		}
	}

	public void SetAsNetworkPlayer(int id)
	{
		playerID = id;
		playerType = PlayerType.NetworkClient;
		isLocalPlayer = false;
	}

	public void SetAsLocalPlayer(int id)
	{
		playerID = id;
		playerType = PlayerType.LocalKeyboard;
		isLocalPlayer = true;
		LoadCustomControls();
	}

	public void _OnAreaEntered(Area2D area)
	{
		if (!isLocalPlayer && playerType != PlayerType.LocalKeyboard)
			return;

		if (PlayerHealthComponent != null)
		{
			PlayerHealthComponent.TakeDamage(1);
		}
	}
}
