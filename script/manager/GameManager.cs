using Godot;

public enum GameMode
{
    SinglePlayer,
    LocalCoop,
    NetworkCoop
}

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    private GameMode currentMode = GameMode.SinglePlayer;
    private bool isNetworkHost = false;
    private bool isServerRunning = false;

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree();
        }
    }

    public void SetGameMode(GameMode mode)
    {
        currentMode = mode;
        GD.Print($"游戏模式设置为: {mode}");
    }

    public GameMode GetCurrentGameMode()
    {
        return currentMode;
    }

    public bool IsServerRunning()
    {
        return isServerRunning;
    }

    public void CancelNetworkHost()
    {
        isServerRunning = false;
        isNetworkHost = false;
        SetGameMode(GameMode.SinglePlayer);
        GD.Print("已取消主机，恢复到单人模式");
    }

    public void StartSinglePlayer()
    {
        SetGameMode(GameMode.SinglePlayer);
        GetTree().ChangeSceneToFile("res://scene/Level1.tscn");
    }

    public void StartLocalCoop()
    {
        SetGameMode(GameMode.LocalCoop);
        GetTree().ChangeSceneToFile("res://scene/Level1.tscn");
    }

    public void SetupNetworkHost()
    {
        isNetworkHost = true;
        SetGameMode(GameMode.NetworkCoop);

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.StartHost();
            isServerRunning = true;
            GD.Print("主机已创建，等待玩家连接...");
        }
    }

    public void StartNetworkGame(bool asHost)
    {
        isNetworkHost = asHost;
        SetGameMode(GameMode.NetworkCoop);

        if (asHost && !isServerRunning)
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.StartHost();
                isServerRunning = true;
            }
        }

        if (asHost)
        {
            NetworkManager.Instance.BroadcastStartGame();
        }
        else
        {
            GetTree().ChangeSceneToFile("res://scene/Level1.tscn");
        }
    }

    public void ConnectToNetwork(string serverIP)
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.StartClient(serverIP);
            SetGameMode(GameMode.NetworkCoop);
            GD.Print($"正在连接到服务器: {serverIP}");
        }
    }

    public void AddLocalPlayerInLobby()
    {
        if (currentMode == GameMode.SinglePlayer)
        {
            SetGameMode(GameMode.LocalCoop);
            GD.Print("已添加本地玩家，切换到本地双人模式");
        }
    }

    public void RemoveLocalPlayerInLobby()
    {
        if (currentMode == GameMode.LocalCoop)
        {
            SetGameMode(GameMode.SinglePlayer);
            GD.Print("已移除本地玩家，切换到单人模式");
        }
    }
}
