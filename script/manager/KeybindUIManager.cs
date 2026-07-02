using Godot;

public partial class KeybindUIManager : Control
{
	[Export] public Label titleText;
	[Export] public Button player1Button;
	[Export] public Button player2Button;
	private int selectedPlayerID = 1;

	[Export] public Button moveLeftButton;
	[Export] public Button moveRightButton;
	[Export] public Button jumpButton;
	[Export] public Button shootButton;
	[Export] public Button aimUpButton;
	[Export] public Button aimDownButton;

	[Export] public Label moveLeftText;
	[Export] public Label moveRightText;
	[Export] public Label jumpText;
	[Export] public Label shootText;
	[Export] public Label aimUpText;
	[Export] public Label aimDownText;

	[Export] public Button resetButton;
	[Export] public Button backButton;
	[Export] public Button confirmButton;

	public override void _Ready()
	{
		if (CustomKeybindManager.Instance != null)
		{
			CustomKeybindManager.Instance.KeyRebound += HandleKeyRebound;
		}

		if (CustomKeybindManager.Instance == null)
		{
			var manager = new CustomKeybindManager();
			AddChild(manager);
			CustomKeybindManager.Instance.KeyRebound += HandleKeyRebound;
		}

		SetupButtons();
		UpdateKeyDisplay();
	}

	private void SetupButtons()
	{
		if (player1Button != null)
			player1Button.Pressed += () => SelectPlayer(1);

		if (player2Button != null)
			player2Button.Pressed += () => SelectPlayer(2);

		if (moveLeftButton != null)
			moveLeftButton.Pressed += () => StartRebinding("moveLeft");

		if (moveRightButton != null)
			moveRightButton.Pressed += () => StartRebinding("moveRight");

		if (jumpButton != null)
			jumpButton.Pressed += () => StartRebinding("jumpKey");

		if (shootButton != null)
			shootButton.Pressed += () => StartRebinding("shootKey");

		if (aimUpButton != null)
			aimUpButton.Pressed += () => StartRebinding("aimUp");

		if (aimDownButton != null)
			aimDownButton.Pressed += () => StartRebinding("aimDown");

		if (resetButton != null)
			resetButton.Pressed += ResetControls;

		if (backButton != null)
			backButton.Pressed += GoBack;

		if (confirmButton != null)
			confirmButton.Pressed += ConfirmAndStart;
	}

	private void HandleKeyRebound(int playerID, string keyName)
	{
		if (playerID == selectedPlayerID)
		{
			UpdateKeyDisplay();
		}
	}

	public void SetSinglePlayerMode()
	{
		selectedPlayerID = 1;

		if (titleText != null)
		{
			titleText.Text = "单人游戏 - 按键设置";
		}

		if (player1Button != null)
		{
			player1Button.Disabled = false;
			UpdatePlayerButtonVisual(player1Button, true);
		}

		if (player2Button != null)
		{
			player2Button.Visible = false;
		}

		UpdateKeyDisplay();
	}

	public void SetLocalCoopMode()
	{
		selectedPlayerID = 1;

		if (titleText != null)
		{
			titleText.Text = "本地双人 - 按键设置";
		}

		if (player1Button != null)
		{
			player1Button.Visible = true;
			player1Button.Disabled = false;
			UpdatePlayerButtonVisual(player1Button, true);
		}

		if (player2Button != null)
		{
			player2Button.Visible = true;
			player2Button.Disabled = false;
			UpdatePlayerButtonVisual(player2Button, false);
		}

		UpdateKeyDisplay();
	}

	private void SelectPlayer(int playerID)
	{
		selectedPlayerID = playerID;

		if (player1Button != null && player1Button.Visible)
		{
			UpdatePlayerButtonVisual(player1Button, playerID == 1);
		}

		if (player2Button != null && player2Button.Visible)
		{
			UpdatePlayerButtonVisual(player2Button, playerID == 2);
		}

		UpdateKeyDisplay();
	}

	private void UpdatePlayerButtonVisual(Button button, bool isSelected)
	{
		if (button == null) return;

		if (isSelected)
		{
			button.Modulate = new Color(0.5f, 0.5f, 0.5f, 1f);
		}
		else
		{
			button.Modulate = new Color(1, 1, 1, 1);
		}
	}

	private void UpdateKeyDisplay()
	{
		ControlConfig config = CustomKeybindManager.Instance.GetPlayerControls(selectedPlayerID);
		if (config == null) return;

		UpdateButtonLabel(moveLeftButton, config.moveLeft.ToString());
		UpdateButtonLabel(moveRightButton, config.moveRight.ToString());
		UpdateButtonLabel(jumpButton, config.jumpKey.ToString());
		UpdateButtonLabel(shootButton, config.shootKey.ToString());
		UpdateButtonLabel(aimUpButton, config.aimUp.ToString());
		UpdateButtonLabel(aimDownButton, config.aimDown.ToString());
	}

	private void UpdateButtonLabel(Button button, string keyName)
	{
		if (button != null)
		{
			Label label = button.GetNodeOrNull<Label>(".");
			if (label != null)
			{
				label.Text = keyName;
			}
		}
	}

	private void StartRebinding(string keyName)
	{
		CustomKeybindManager.Instance.StartRebinding(selectedPlayerID, keyName);

		Button targetButton = null;

		switch (keyName)
		{
			case "moveLeft":
				targetButton = moveLeftButton;
				break;
			case "moveRight":
				targetButton = moveRightButton;
				break;
			case "jumpKey":
				targetButton = jumpButton;
				break;
			case "shootKey":
				targetButton = shootButton;
				break;
			case "aimUp":
				targetButton = aimUpButton;
				break;
			case "aimDown":
				targetButton = aimDownButton;
				break;
		}

		if (targetButton != null)
		{
			Label buttonLabel = targetButton.GetNodeOrNull<Label>(".");
			if (buttonLabel != null)
			{
				buttonLabel.Text = "按下任意键...";
			}
		}
	}

	private void ResetControls()
	{
		CustomKeybindManager.Instance.ResetToDefaults(selectedPlayerID);
	}

	private void ConfirmAndStart()
	{
		LobbyManager lobbyManager = GetNodeOrNull<LobbyManager>("..");
		if (lobbyManager != null)
		{
			lobbyManager.ConfirmAndStartGame();
		}
	}

	private void GoBack()
	{
		LobbyManager lobbyManager = GetNodeOrNull<LobbyManager>("..");
		if (lobbyManager != null)
		{
			lobbyManager.CloseKeybindSettings();
		}
	}
}
