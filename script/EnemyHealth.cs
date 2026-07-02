using Godot;

public partial class EnemyHealth : HealthComponent
{
	[Export] public AnimationPlayer animator;
	[Export] public Node2D enemyBehaviour;

	[Export] public PackedScene[] dropItems;
	[Export(PropertyHint.Range, "0,1")] public float dropChance = 0.3f;

	[Export] public float knockbackForce = 5f;

	public bool canHurt = true;
	private RigidBody2D parentRigidBody;

	public override void _Ready()
	{
		base._Ready();
		parentRigidBody = GetParent<RigidBody2D>();
	}

	public override void TakeDamage(int damage)
	{
		if (IsDead)
			return;

		base.TakeDamage(damage);

		if (enemyBehaviour != null && enemyBehaviour.HasMethod("OnHurt"))
		{
			enemyBehaviour.Call("OnHurt", damage);
		}
	}

	public void TakeDamageWithKnockback(int damage, Vector2 knockbackDirection)
	{
		TakeDamage(damage);

		RigidBody2D rb2d = parentRigidBody;
		if (rb2d != null && !IsDead)
		{
			rb2d.LinearVelocity = knockbackDirection.Normalized() * knockbackForce;
		}
	}

	protected override void Die()
	{
		base.Die();

		DropItems();

		if (enemyBehaviour != null && enemyBehaviour.HasMethod("OnDeath"))
		{
			enemyBehaviour.Call("OnDeath");
		}

		QueueFree();
	}

	protected override void PlayHitEffects()
	{
		base.PlayHitEffects();

		if (animator != null)
		{
			animator.Play("Hit");
		}
	}

	private void DropItems()
	{
		if (dropItems == null || dropItems.Length == 0)
			return;

		if (GD.Randf() <= dropChance)
		{
			int index = GD.RandRange(0, dropItems.Length - 1);
			if (dropItems[index] != null)
			{
				Node2D item = dropItems[index].Instantiate<Node2D>();
				GetParent().AddChild(item);
				item.GlobalPosition = GlobalPosition;
			}
		}
	}

	public void SetEnemyBehaviour(Node2D behaviour)
	{
		enemyBehaviour = behaviour;
	}
}
