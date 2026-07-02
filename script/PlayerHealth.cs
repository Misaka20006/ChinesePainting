using Godot;
using System;

public partial class PlayerHealth : HealthComponent
{
	[Export] public AnimationPlayer animator;
	[Export] public Node2D heartUIManager;

	[Export] public ColorRect damageOverlay;
	[Export] public float flashDuration = 0.2f;
	[Export] public float fadeSpeed = 2f;

	[Export] public Node2D playerRespawn;

	public override void _Ready()
	{
		base._Ready();

		if (damageOverlay != null)
		{
			damageOverlay.Color = new Color(1, 0, 0, 0);
		}
	}

	public override void TakeDamage(int damage)
	{
		if (isInvincible || IsDead)
			return;

		base.TakeDamage(damage);
	}

	protected override void Die()
	{
		if (playerRespawn != null && playerRespawn.HasMethod("Respawn"))
		{
			playerRespawn.Call("Respawn");
		}
		else
		{
			base.Die();
		}
	}

	protected override void PlayHitEffects()
	{
		base.PlayHitEffects();

		CameraShake();
		_ = FlashRedScreen();

		if (animator != null)
		{
			animator.Play("Hurt");
		}
	}

	protected override void UpdateHealthUI()
	{
		if (heartUIManager != null && heartUIManager.HasMethod("UpdateHearts"))
		{
			heartUIManager.Call("UpdateHearts", currentHealth);
		}
	}

	public void CameraShake()
	{
		// Camera shake can be implemented via Camera2D offset animation
		Camera2D camera = GetViewport().GetCamera2D();
		if (camera != null && camera.HasMethod("Shake"))
		{
			camera.Call("Shake");
		}
	}

	private async System.Threading.Tasks.Task FlashRedScreen()
	{
		if (damageOverlay == null)
			return;

		damageOverlay.Color = new Color(1, 0, 0, 0.4f);
		await ToSignal(GetTree().CreateTimer(flashDuration), Timer.SignalName.Timeout);

		double elapsed = 0;
		while (elapsed < 1.0)
		{
			Color c = damageOverlay.Color;
			c.A -= (float)(GetProcessDeltaTime() * fadeSpeed);
			damageOverlay.Color = c;
			elapsed += GetProcessDeltaTime();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	protected override async System.Threading.Tasks.Task InvincibilityRoutine()
	{
		isInvincible = true;

		Sprite2D spriteRenderer = GetNodeOrNull<Sprite2D>(".");
		if (spriteRenderer != null)
		{
			float timer = 0f;
			float blinkSpeed = 0.1f;

			while (timer < invincibleDuration)
			{
				timer += (float)GetProcessDeltaTime();
				spriteRenderer.Visible = !spriteRenderer.Visible;
				await ToSignal(GetTree().CreateTimer(blinkSpeed), Timer.SignalName.Timeout);
			}

			spriteRenderer.Visible = true;
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(invincibleDuration), Timer.SignalName.Timeout);
		}

		isInvincible = false;
	}
}
