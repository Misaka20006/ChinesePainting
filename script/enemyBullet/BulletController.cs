using Godot;

public partial class BulletController : Area2D
{
	[Export] public int damage = 1;
	[Export] public float lifeTime = 5f;

	[Export] public string bulletTypeName = "Default";

	[Export] public PackedScene hitEffect;
	[Export] public AudioStream hitSound;

	private Vector2 direction;
	private float speed;
	private bool isPlayerBullet;
	private int ownerID;
	private float spawnTime;
	private RigidBody2D rb2d;

	public bool IsPlayerBullet => isPlayerBullet;
	public string BulletTypeName => bulletTypeName;

	public override void _Ready()
	{
		rb2d = GetNodeOrNull<RigidBody2D>(".");
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
	}

	public void Initialize(Vector2 dir, float spd, bool isPlayer, int id)
	{
		direction = dir.Normalized();
		speed = spd;
		isPlayerBullet = isPlayer;
		ownerID = id;
		spawnTime = Time.GetTicksMsec() / 1000f;

		if (rb2d != null)
		{
			rb2d.LinearVelocity = direction * speed;
		}

		float angle = Mathf.Atan2(direction.Y, direction.X) * 180f / Mathf.Pi;
		Rotation = Mathf.DegToRad(angle);
	}

	public override void _Process(double delta)
	{
		if (Time.GetTicksMsec() / 1000f - spawnTime > lifeTime)
		{
			Deactivate();
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		HandleCollision(area);
	}

	private void OnBodyEntered(Node2D body)
	{
		HandleCollision(body);
	}

	private void HandleCollision(Node2D other)
	{
		if (isPlayerBullet)
		{
			EnemyHealth enemyHealth = other as EnemyHealth ?? other.GetNodeOrNull<EnemyHealth>(".");
			if (enemyHealth != null)
			{
				enemyHealth.TakeDamage(damage);

				OnHit();
				return;
			}
		}
		else
		{
			PlayerHealth playerHealth = other as PlayerHealth ?? other.GetNodeOrNull<PlayerHealth>(".");
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(damage);
				OnHit();
				return;
			}
		}

		// Ground collision: anything that's not a health component counts as ground
		if (other is not EnemyHealth && other is not PlayerHealth)
		{
			OnHit();
		}
	}

	private void OnHit()
	{
		if (hitEffect != null)
		{
			Node2D effect = hitEffect.Instantiate<Node2D>();
			GetParent().AddChild(effect);
			effect.GlobalPosition = GlobalPosition;
		}

		if (hitSound != null)
		{
			AudioStreamPlayer2D player = new AudioStreamPlayer2D();
			player.Stream = hitSound;
			GetParent().AddChild(player);
			player.Play();
			player.Finished += player.QueueFree;
		}

		Deactivate();
	}

	public void Deactivate()
	{
		if (BulletManager.Instance != null)
		{
			BulletManager.Instance.ReturnBulletToPool(this, isPlayerBullet);
		}
		else
		{
			QueueFree();
		}
	}

	public void SetDamage(int dmg)
	{
		damage = dmg;
	}

	public void SetBulletTypeName(string typeName)
	{
		bulletTypeName = typeName;
	}
}
