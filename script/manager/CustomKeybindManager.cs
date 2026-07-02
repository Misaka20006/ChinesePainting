using Godot;
using System;
using System.Collections.Generic;

public partial class CustomKeybindManager : Node
{
	public static CustomKeybindManager Instance { get; private set; }

	[Signal] public delegate void KeyReboundEventHandler(int playerID, string keyName);

	private Dictionary<int, ControlConfig> playerControls = new Dictionary<int, ControlConfig>();

	private int rebindingPlayerID = 0;
	private string rebindingKey = "";
	private bool isRebinding = false;

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
			LoadDefaultControls();
			LoadSavedControls();
		}
		else
		{
			QueueFree();
		}
	}

	private void LoadDefaultControls()
	{
		ControlConfig player1 = new ControlConfig();
		playerControls[1] = player1;

		ControlConfig player2 = new ControlConfig();
		player2.moveLeft = Key.Left;
		player2.moveRight = Key.Right;
		player2.jumpKey = Key.Up;
		player2.shootKey = Key.Kp0;
		player2.aimUp = Key.Up;
		player2.aimDown = Key.Down;
		playerControls[2] = player2;
	}

	public ControlConfig GetPlayerControls(int playerID)
	{
		if (playerControls.ContainsKey(playerID))
		{
			return playerControls[playerID];
		}
		return null;
	}

	public void StartRebinding(int playerID, string keyName)
	{
		rebindingPlayerID = playerID;
		rebindingKey = keyName;
		isRebinding = true;
		GD.Print($"玩家 {playerID} 正在重新绑定 {keyName}，请按下新按键...");
	}

	public override void _Input(InputEvent @event)
	{
		if (!isRebinding) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			Key key = keyEvent.Keycode;
			if (key != Key.None && key != Key.Escape)
			{
				ApplyRebinding(key);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void ApplyRebinding(Key newKey)
	{
		if (playerControls.ContainsKey(rebindingPlayerID))
		{
			ControlConfig config = playerControls[rebindingPlayerID];

			switch (rebindingKey)
			{
				case "moveLeft":
					config.moveLeft = newKey;
					break;
				case "moveRight":
					config.moveRight = newKey;
					break;
				case "jumpKey":
					config.jumpKey = newKey;
					break;
				case "shootKey":
					config.shootKey = newKey;
					break;
				case "aimUp":
					config.aimUp = newKey;
					break;
				case "aimDown":
					config.aimDown = newKey;
					break;
			}

			playerControls[rebindingPlayerID] = config;
			SaveControls();

			GD.Print($"玩家 {rebindingPlayerID} 的 {rebindingKey} 已绑定到 {newKey}");

			EmitSignal(SignalName.KeyRebound, rebindingPlayerID, rebindingKey);
		}

		isRebinding = false;
		rebindingPlayerID = 0;
		rebindingKey = "";
	}

	public void ResetToDefaults(int playerID)
	{
		if (playerID == 1)
		{
			playerControls[1] = new ControlConfig();
		}
		else if (playerID == 2)
		{
			ControlConfig config = new ControlConfig();
			config.moveLeft = Key.Left;
			config.moveRight = Key.Right;
			config.jumpKey = Key.Up;
			config.shootKey = Key.Kp0;
			config.aimUp = Key.Up;
			config.aimDown = Key.Down;
			playerControls[2] = config;
		}

		SaveControls();
		GD.Print($"玩家 {playerID} 的按键已重置为默认值");

		EmitSignal(SignalName.KeyRebound, playerID, "default");
	}

	private void SaveControls()
	{
		var settings = new ConfigFile();
		settings.SetValue("Player1", "moveLeft", (int)playerControls[1].moveLeft);
		settings.SetValue("Player1", "moveRight", (int)playerControls[1].moveRight);
		settings.SetValue("Player1", "jumpKey", (int)playerControls[1].jumpKey);
		settings.SetValue("Player1", "shootKey", (int)playerControls[1].shootKey);
		settings.SetValue("Player1", "aimUp", (int)playerControls[1].aimUp);
		settings.SetValue("Player1", "aimDown", (int)playerControls[1].aimDown);

		settings.SetValue("Player2", "moveLeft", (int)playerControls[2].moveLeft);
		settings.SetValue("Player2", "moveRight", (int)playerControls[2].moveRight);
		settings.SetValue("Player2", "jumpKey", (int)playerControls[2].jumpKey);
		settings.SetValue("Player2", "shootKey", (int)playerControls[2].shootKey);
		settings.SetValue("Player2", "aimUp", (int)playerControls[2].aimUp);
		settings.SetValue("Player2", "aimDown", (int)playerControls[2].aimDown);

		settings.Save("user://keybindings.cfg");
	}

	private void LoadSavedControls()
	{
		var settings = new ConfigFile();
		Error err = settings.Load("user://keybindings.cfg");
		if (err != Error.Ok)
			return;

		if (settings.HasSection("Player1"))
		{
			ControlConfig config = playerControls[1];
			config.moveLeft = (Key)(int)settings.GetValue("Player1", "moveLeft", (int)config.moveLeft);
			config.moveRight = (Key)(int)settings.GetValue("Player1", "moveRight", (int)config.moveRight);
			config.jumpKey = (Key)(int)settings.GetValue("Player1", "jumpKey", (int)config.jumpKey);
			config.shootKey = (Key)(int)settings.GetValue("Player1", "shootKey", (int)config.shootKey);
			config.aimUp = (Key)(int)settings.GetValue("Player1", "aimUp", (int)config.aimUp);
			config.aimDown = (Key)(int)settings.GetValue("Player1", "aimDown", (int)config.aimDown);
		}

		if (settings.HasSection("Player2"))
		{
			ControlConfig config = playerControls[2];
			config.moveLeft = (Key)(int)settings.GetValue("Player2", "moveLeft", (int)config.moveLeft);
			config.moveRight = (Key)(int)settings.GetValue("Player2", "moveRight", (int)config.moveRight);
			config.jumpKey = (Key)(int)settings.GetValue("Player2", "jumpKey", (int)config.jumpKey);
			config.shootKey = (Key)(int)settings.GetValue("Player2", "shootKey", (int)config.shootKey);
			config.aimUp = (Key)(int)settings.GetValue("Player2", "aimUp", (int)config.aimUp);
			config.aimDown = (Key)(int)settings.GetValue("Player2", "aimDown", (int)config.aimDown);
		}
	}
}

public class ControlConfig
{
	public Key moveLeft = Key.A;
	public Key moveRight = Key.D;
	public Key jumpKey = Key.W;
	public Key shootKey = Key.J;
	public Key aimUp = Key.W;
	public Key aimDown = Key.S;
}
