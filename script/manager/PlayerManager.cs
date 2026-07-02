using Godot;
using System.Collections.Generic;

public partial class PlayerManager : Node
{
    public static PlayerManager Instance { get; private set; }

    [Export] public PackedScene playerPrefab;

    [Export] public Godot.Collections.Array<Node2D> spawnPoints = new Godot.Collections.Array<Node2D>();

    [Export] public Godot.Collections.Array<Node2D> heartUIs = new Godot.Collections.Array<Node2D>();

    private List<Node2D> activePlayers = new List<Node2D>();
    private int nextPlayerID = 1;

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree();
            return;
        }

        Callable.From(InitializePlayers).CallDeferred();
    }

    private void InitializePlayers()
    {
        ClearExistingPlayers();

        GameMode gameMode = GameManager.Instance.GetCurrentGameMode();

        switch (gameMode)
        {
            case GameMode.SinglePlayer:
                SpawnSinglePlayer();
                break;
            case GameMode.LocalCoop:
                SpawnLocalCoopPlayers();
                break;
            case GameMode.NetworkCoop:
                SpawnNetworkPlayers();
                break;
        }
    }

    private void ClearExistingPlayers()
    {
        foreach (Node2D player in activePlayers)
        {
            if (player != null)
                player.QueueFree();
        }
        activePlayers.Clear();
        nextPlayerID = 1;
    }

    private void SpawnSinglePlayer()
    {
        GD.Print("生成单人玩家");

        if (spawnPoints.Count > 0 && playerPrefab != null)
        {
            Node2D player = playerPrefab.Instantiate<Node2D>();
            GetTree().CurrentScene.AddChild(player);
            player.GlobalPosition = spawnPoints[0].GlobalPosition;

            SetupPlayer(player, 1, PlayerController.PlayerType.LocalKeyboard);
            activePlayers.Add(player);

            if (heartUIs.Count > 0)
            {
                PlayerHealth playerHealth = player.GetNodeOrNull<PlayerHealth>(".");
                if (playerHealth != null)
                    playerHealth.heartUIManager = heartUIs[0];
            }
        }
    }

    private void SpawnLocalCoopPlayers()
    {
        GD.Print("生成本地双人玩家");

        int playerCount = Mathf.Min(2, spawnPoints.Count);

        for (int i = 0; i < playerCount; i++)
        {
            if (playerPrefab != null)
            {
                Node2D player = playerPrefab.Instantiate<Node2D>();
                GetTree().CurrentScene.AddChild(player);
                player.GlobalPosition = spawnPoints[i].GlobalPosition;

                SetupPlayer(player, i + 1, PlayerController.PlayerType.LocalKeyboard);
                activePlayers.Add(player);

                if (i < heartUIs.Count)
                {
                    PlayerHealth playerHealth = player.GetNodeOrNull<PlayerHealth>(".");
                    if (playerHealth != null)
                        playerHealth.heartUIManager = heartUIs[i];
                }
            }
        }
    }

    private void SpawnNetworkPlayers()
    {
        GD.Print("生成网络联机玩家");

        if (NetworkManager.Instance != null && NetworkManager.Instance.isConnected)
        {
            int localPlayerID = NetworkManager.Instance.playerID;
            int spawnIndex = localPlayerID - 1;

            if (spawnIndex < spawnPoints.Count && playerPrefab != null)
            {
                Node2D player = playerPrefab.Instantiate<Node2D>();
                GetTree().CurrentScene.AddChild(player);
                player.GlobalPosition = spawnPoints[spawnIndex].GlobalPosition;

                PlayerController controller = player.GetNodeOrNull<PlayerController>(".");
                if (controller != null)
                {
                    controller.SetAsNetworkPlayer(localPlayerID);
                }

                if (spawnIndex < heartUIs.Count)
                {
                    PlayerHealth playerHealth = player.GetNodeOrNull<PlayerHealth>(".");
                    if (playerHealth != null)
                        playerHealth.heartUIManager = heartUIs[spawnIndex];
                }

                activePlayers.Add(player);
            }
        }
    }

    private void SetupPlayer(Node2D player, int playerID, PlayerController.PlayerType type)
    {
        PlayerController controller = player.GetNodeOrNull<PlayerController>(".");
        if (controller != null)
        {
            if (type == PlayerController.PlayerType.LocalKeyboard)
            {
                controller.SetAsLocalPlayer(playerID);
            }
        }
    }

    public void AddLocalPlayer()
    {
        if (activePlayers.Count >= 2)
        {
            GD.Print("已达到最大本地玩家数量");
            return;
        }

        int spawnIndex = activePlayers.Count;
        if (spawnIndex < spawnPoints.Count && playerPrefab != null)
        {
            Node2D player = playerPrefab.Instantiate<Node2D>();
            GetTree().CurrentScene.AddChild(player);
            player.GlobalPosition = spawnPoints[spawnIndex].GlobalPosition;

            SetupPlayer(player, nextPlayerID, PlayerController.PlayerType.LocalKeyboard);
            activePlayers.Add(player);

            if (spawnIndex < heartUIs.Count)
            {
                PlayerHealth playerHealth = player.GetNodeOrNull<PlayerHealth>(".");
                if (playerHealth != null)
                    playerHealth.heartUIManager = heartUIs[spawnIndex];
            }

            nextPlayerID++;
            GD.Print($"添加本地玩家 {nextPlayerID - 1}");
        }
    }

    public void RemoveLocalPlayer(int playerID)
    {
        Node2D playerToRemove = null;

        foreach (Node2D player in activePlayers)
        {
            PlayerController controller = player.GetNodeOrNull<PlayerController>(".");
            if (controller != null && controller.playerID == playerID)
            {
                playerToRemove = player;
                break;
            }
        }

        if (playerToRemove != null)
        {
            activePlayers.Remove(playerToRemove);
            playerToRemove.QueueFree();
            GD.Print($"移除本地玩家 {playerID}");
        }
    }

    public int GetActivePlayerCount()
    {
        return activePlayers.Count;
    }

    public List<Node2D> GetActivePlayers()
    {
        return activePlayers;
    }
}
