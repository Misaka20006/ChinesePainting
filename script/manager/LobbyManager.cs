using Godot;

public partial class LobbyManager : Control
{
    [Export] public Label modeText;
    [Export] public Button addLocalPlayerButton;
    [Export] public Button removeLocalPlayerButton;
    [Export] public Button startButton;
    [Export] public Button networkHostButton;
    [Export] public Button networkClientButton;

    [Export] public Label networkTitleText;
    [Export] public LineEdit networkIPInputField;
    [Export] public Label networkIPDisplayText;
    [Export] public Button copyIPButton;
    [Export] public LineEdit networkPortInputField;
    [Export] public Button networkActionButton;
    [Export] public Button networkBackButton;
    [Export] public Label statusText;

    [Export] public Control mainPanel;
    [Export] public Control networkPanel;
    [Export] public Control keybindPanel;

    private bool isHosting = false;
    private string localIPAddress = "127.0.0.1";

    public override void _Ready()
    {
        UpdateUI();

        if (addLocalPlayerButton != null)
            addLocalPlayerButton.Pressed += OnAddLocalPlayer;

        if (removeLocalPlayerButton != null)
            removeLocalPlayerButton.Pressed += OnRemoveLocalPlayer;

        if (startButton != null)
            startButton.Pressed += OnStartButtonClicked;

        if (networkHostButton != null)
            networkHostButton.Pressed += OnNetworkHostButtonClicked;

        if (networkClientButton != null)
            networkClientButton.Pressed += () => OnShowNetworkPanel(false);

        if (networkActionButton != null)
            networkActionButton.Pressed += OnNetworkAction;

        if (networkBackButton != null)
            networkBackButton.Pressed += HideNetworkPanel;

        if (copyIPButton != null)
            copyIPButton.Pressed += CopyIPAddress;

        if (mainPanel != null)
            mainPanel.Visible = true;

        if (networkPanel != null)
            networkPanel.Visible = false;

        if (keybindPanel != null)
            keybindPanel.Visible = false;

        if (networkPortInputField != null)
            networkPortInputField.Text = "8888";

        localIPAddress = GetLocalIPAddress();
    }

    public override void _Process(double delta)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        GameMode currentMode = GameManager.Instance.GetCurrentGameMode();
        bool isServerRunning = GameManager.Instance.IsServerRunning();

        if (modeText != null)
        {
            switch (currentMode)
            {
                case GameMode.SinglePlayer:
                    modeText.Text = "当前模式: 单人游戏";
                    break;
                case GameMode.LocalCoop:
                    modeText.Text = "当前模式: 本地双人";
                    break;
                case GameMode.NetworkCoop:
                    modeText.Text = isServerRunning ? "当前模式: 网络联机 (主机)" : "当前模式: 网络联机";
                    break;
            }
        }

        if (addLocalPlayerButton != null)
        {
            addLocalPlayerButton.Disabled = !(currentMode == GameMode.SinglePlayer && !isServerRunning);
        }

        if (removeLocalPlayerButton != null)
        {
            removeLocalPlayerButton.Disabled = (currentMode != GameMode.LocalCoop);
        }

        if (networkHostButton != null)
        {
            Label buttonLabel = networkHostButton.GetNodeOrNull<Label>(".");

            if (isServerRunning)
            {
                if (buttonLabel != null)
                    buttonLabel.Text = "取消创建";
                networkHostButton.Modulate = new Color(1f, 0.5f, 0.5f, 1f);
            }
            else
            {
                if (buttonLabel != null)
                    buttonLabel.Text = "创建主机";
                networkHostButton.Modulate = new Color(1, 1, 1, 1);
            }
        }

        if (networkClientButton != null)
        {
            networkClientButton.Disabled = isServerRunning;
            networkClientButton.Modulate = isServerRunning ? new Color(0.5f, 0.5f, 0.5f, 1f) : new Color(1, 1, 1, 1);
        }
    }

    private void OnNetworkHostButtonClicked()
    {
        bool isServerRunning = GameManager.Instance.IsServerRunning();

        if (isServerRunning)
        {
            CancelHost();
        }
        else
        {
            OnShowNetworkPanel(true);
        }
    }

    private void OnAddLocalPlayer()
    {
        GameManager.Instance.AddLocalPlayerInLobby();

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddLocalPlayer();
        }
    }

    private void OnRemoveLocalPlayer()
    {
        GameManager.Instance.RemoveLocalPlayerInLobby();

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemoveLocalPlayer(2);
        }
    }

    private void OnStartButtonClicked()
    {
        OpenKeybindSettings();
    }

    private void OpenKeybindSettings()
    {
        if (mainPanel != null)
            mainPanel.Visible = false;

        if (networkPanel != null)
            networkPanel.Visible = false;

        if (keybindPanel != null)
        {
            keybindPanel.Visible = true;

            KeybindUIManager keybindManager = keybindPanel.GetNodeOrNull<KeybindUIManager>(".");
            if (keybindManager != null)
            {
                GameMode currentMode = GameManager.Instance.GetCurrentGameMode();

                if (currentMode == GameMode.SinglePlayer || currentMode == GameMode.NetworkCoop)
                {
                    keybindManager.SetSinglePlayerMode();
                }
                else if (currentMode == GameMode.LocalCoop)
                {
                    keybindManager.SetLocalCoopMode();
                }
            }
        }
    }

    public void CloseKeybindSettings()
    {
        if (keybindPanel != null)
            keybindPanel.Visible = false;

        if (mainPanel != null)
            mainPanel.Visible = true;
    }

    public void ConfirmAndStartGame()
    {
        if (keybindPanel != null)
            keybindPanel.Visible = false;

        GameMode currentMode = GameManager.Instance.GetCurrentGameMode();

        switch (currentMode)
        {
            case GameMode.SinglePlayer:
                GameManager.Instance.StartSinglePlayer();
                break;
            case GameMode.LocalCoop:
                GameManager.Instance.StartLocalCoop();
                break;
            case GameMode.NetworkCoop:
                bool isHost = GameManager.Instance.IsServerRunning();
                GameManager.Instance.StartNetworkGame(isHost);
                break;
        }
    }

    private void OnShowNetworkPanel(bool hosting)
    {
        bool isServerRunning = GameManager.Instance.IsServerRunning();

        if (isServerRunning && !hosting)
        {
            GD.Print("已创建主机，无法加入其他游戏");
            return;
        }

        isHosting = hosting;

        if (networkTitleText != null)
        {
            networkTitleText.Text = hosting ? "创建主机" : "加入游戏";
        }

        if (networkActionButton != null)
        {
            Label buttonLabel = networkActionButton.GetNodeOrNull<Label>(".");
            if (buttonLabel != null)
            {
                buttonLabel.Text = hosting ? "创建" : "连接";
            }
        }

        if (networkIPInputField != null)
        {
            networkIPInputField.Visible = !hosting;

            if (!hosting && string.IsNullOrEmpty(networkIPInputField.Text))
            {
                networkIPInputField.Text = "127.0.0.1";
            }
        }

        if (networkIPDisplayText != null)
        {
            networkIPDisplayText.Visible = hosting;
            networkIPDisplayText.Text = localIPAddress;
        }

        if (copyIPButton != null)
        {
            copyIPButton.Visible = hosting;
        }

        if (statusText != null)
        {
            statusText.Text = "";
        }

        if (mainPanel != null)
            mainPanel.Visible = false;

        if (networkPanel != null)
            networkPanel.Visible = true;
    }

    private void OnNetworkAction()
    {
        string portText = networkPortInputField != null ? networkPortInputField.Text : "8888";
        int port = 8888;

        if (!int.TryParse(portText, out port))
        {
            port = 8888;
            if (networkPortInputField != null)
                networkPortInputField.Text = "8888";
        }

        if (isHosting)
        {
            OnHostNetworkGame(port);
        }
        else
        {
            OnJoinNetworkGame(port);
        }
    }

    private void OnHostNetworkGame(int port)
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.serverPort = port;
        }

        if (statusText != null)
        {
            statusText.Text = $"正在创建主机...\n端口: {port}\nIP: {localIPAddress}";
        }

        GameManager.Instance.SetupNetworkHost();

        _ = ReturnToMainPanelAfterHost();
    }

    private async System.Threading.Tasks.Task ReturnToMainPanelAfterHost()
    {
        await ToSignal(GetTree().CreateTimer(1.5f), Timer.SignalName.Timeout);

        if (statusText != null)
        {
            statusText.Text = $"主机已创建！\nIP: {localIPAddress}\n端口: {(NetworkManager.Instance?.serverPort ?? 8888)}\n\n等待其他玩家加入...";
        }

        await ToSignal(GetTree().CreateTimer(2f), Timer.SignalName.Timeout);

        HideNetworkPanel();
    }

    public void CancelHost()
    {
        if (GameManager.Instance.IsServerRunning())
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.Disconnect();
            }

            GameManager.Instance.CancelNetworkHost();

            GD.Print("已取消主机");
        }
    }

    private void OnJoinNetworkGame(int port)
    {
        string ip = networkIPInputField != null ? networkIPInputField.Text : "127.0.0.1";

        if (string.IsNullOrEmpty(ip))
        {
            ip = "127.0.0.1";
            if (networkIPInputField != null)
                networkIPInputField.Text = ip;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.serverPort = port;
        }

        if (statusText != null)
        {
            statusText.Text = $"正在连接到 {ip}:{port}...";
        }

        GameManager.Instance.ConnectToNetwork(ip);
    }

    private void CopyIPAddress()
    {
        DisplayServer.ClipboardSet(localIPAddress);

        if (statusText != null)
        {
            statusText.Text = $"IP已复制到剪贴板: {localIPAddress}";
        }

        GD.Print($"IP地址已复制: {localIPAddress}");
    }

    private string GetLocalIPAddress()
    {
        string ipAddress = "127.0.0.1";

        try
        {
            using (var socket = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                if (endPoint != null)
                {
                    ipAddress = endPoint.Address.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            GD.Print($"获取本地IP失败: {e.Message}");

            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        ipAddress = ip.ToString();
                        break;
                    }
                }
            }
            catch
            {
                ipAddress = "127.0.0.1";
            }
        }

        return ipAddress;
    }

    public void HideNetworkPanel()
    {
        if (networkPanel != null)
            networkPanel.Visible = false;

        if (mainPanel != null)
            mainPanel.Visible = true;
    }

    public void BackToMainMenu()
    {
        GetTree().ChangeSceneToFile("res://scene/MainScene.tscn");
    }
}
