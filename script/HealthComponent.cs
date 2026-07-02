using Godot;
using System;

public partial class HealthComponent : Node2D
{
	[Export] protected int maxHealth = 3;
	protected int currentHealth;
	[Export] protected float healthRate = 1;

	[Export] protected float invincibleDuration = 1f;
	protected bool isInvincible = false;

	[Export] protected PackedScene deathEffect;
	[Export] protected AudioStream deathSound;

	[Export] protected PackedScene hitEffect;
	[Export] protected AudioStream hitSound;

	[Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
	[Signal] public delegate void DeathEventHandler();
	[Signal] public delegate void HitEventHandler();

	public int MaxHealth => maxHealth;
	public int CurrentHealth => currentHealth;
	public bool IsDead => currentHealth <= 0;

	public override void _Ready()
	{
		currentHealth = maxHealth;
		UpdateHealthUI();
	}

	public virtual void TakeDamage(int damage)
	{
		if (isInvincible || IsDead)
			return;

		currentHealth = Mathf.Max(0, currentHealth - damage);
		healthRate = currentHealth / (float)maxHealth;

		PlayHitEffects();
		UpdateHealthUI();

		EmitSignal(SignalName.HealthChanged, currentHealth, maxHealth);
		EmitSignal(SignalName.Hit);

		if (currentHealth <= 0)
		{
			Die();
		}
		else
		{
			_ = InvincibilityRoutine();
		}
	}

	public virtual void Heal(int amount)
	{
		if (IsDead)
			return;

		currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
		healthRate = currentHealth / (float)maxHealth;
		UpdateHealthUI();
		EmitSignal(SignalName.HealthChanged, currentHealth, maxHealth);
	}

	public virtual void FullHeal()
	{
		currentHealth = maxHealth;
		healthRate = 1;
		UpdateHealthUI();
		EmitSignal(SignalName.HealthChanged, currentHealth, maxHealth);
	}

	protected virtual void Die()
	{
		PlayDeathEffects();
		EmitSignal(SignalName.Death);
	}

	protected virtual async System.Threading.Tasks.Task InvincibilityRoutine()
	{
		isInvincible = true;
		await ToSignal(GetTree().CreateTimer(invincibleDuration), Timer.SignalName.Timeout);
		isInvincible = false;
	}

	protected virtual void PlayHitEffects()
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
	}

	protected virtual void PlayDeathEffects()
	{
		if (deathEffect != null)
		{
			Node2D effect = deathEffect.Instantiate<Node2D>();
			GetParent().AddChild(effect);
			effect.GlobalPosition = GlobalPosition;
		}

		if (deathSound != null)
		{
			AudioStreamPlayer2D player = new AudioStreamPlayer2D();
			player.Stream = deathSound;
			GetParent().AddChild(player);
			player.Play();
			player.Finished += player.QueueFree;
		}
	}

	protected virtual void UpdateHealthUI()
	{
	}

	public void SetMaxHealth(int health)
	{
		maxHealth = health;
		currentHealth = Mathf.Min(currentHealth, maxHealth);
		UpdateHealthUI();
	}

	public void AddMaxHealth(int amount)
	{
		maxHealth += amount;
		currentHealth += amount;
		UpdateHealthUI();
	}

	public float GetHealthRate()
	{
		return healthRate;
	}
}
