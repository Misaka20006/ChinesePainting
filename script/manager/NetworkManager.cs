using Godot;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class NetworkPacket
{
	public string messageType;
	public int senderID;
	public string data;
}

public class PlayerJoinData
{
	public int playerID;
}

public class EmptyData
{
}

public partial class NetworkManager : Node
{
	public static NetworkManager Instance { get; private set; }

	[Export] public string serverIP = "127.0.0.1";
	[Export] public int serverPort = 8888;

	public bool isHost = false;
	public bool isConnected = false;
	public int playerID = 0;

	private Socket socket;
	private Thread networkThread;
	private bool isRunning = false;

	private Queue<byte[]> messageQueue = new Queue<byte[]>();
	private object lockObject = new object();

	private List<Socket> connectedClients = new List<Socket>();

	public delegate void NetworkMessageHandler(byte[] data);
	private Dictionary<string, NetworkMessageHandler> messageHandlers = new Dictionary<string, NetworkMessageHandler>();

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
			RegisterDefaultHandlers();
		}
		else
		{
			QueueFree();
		}
	}

	public override void _Process(double delta)
	{
		lock (lockObject)
		{
			while (messageQueue.Count > 0)
			{
				byte[] data = messageQueue.Dequeue();
				ProcessMessage(data);
			}
		}
	}

	private void RegisterDefaultHandlers()
	{
		RegisterMessageHandler("StartGame", HandleStartGame);
	}

	public void RegisterMessageHandler(string messageType, NetworkMessageHandler handler)
	{
		if (!messageHandlers.ContainsKey(messageType))
		{
			messageHandlers[messageType] = handler;
		}
	}

	private void HandleStartGame(byte[] data)
	{
		GD.Print("收到主机开始游戏指令，加载场景...");

		if (!isHost)
		{
			GetTree().ChangeSceneToFile("res://scene/Level1.tscn");
		}
	}

	public void StartHost()
	{
		isHost = true;
		playerID = 1;
		StartServer();
		ConnectToLocalhost();
	}

	public void StartClient(string ip)
	{
		isHost = false;
		playerID = 2;
		serverIP = ip;
		ConnectToServer();
	}

	public void BroadcastStartGame()
	{
		if (isHost)
		{
			GD.Print("主机广播开始游戏指令...");

			SendMessage("StartGame", new EmptyData());

			GetTree().ChangeSceneToFile("res://scene/Level1.tscn");
		}
	}

	private void StartServer()
	{
		try
		{
			Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
			listenSocket.Bind(endPoint);
			listenSocket.Listen(10);

			networkThread = new Thread(() => AcceptClients(listenSocket));
			networkThread.IsBackground = true;
			networkThread.Start();

			GD.Print($"服务器已启动，监听端口: {serverPort}");
		}
		catch (Exception e)
		{
			GD.PrintErr($"启动服务器失败: {e.Message}");
		}
	}

	private void AcceptClients(Socket listenSocket)
	{
		while (isRunning || listenSocket.IsBound)
		{
			try
			{
				if (listenSocket.Poll(100000, SelectMode.SelectRead))
				{
					Socket clientSocket = listenSocket.Accept();
					GD.Print($"客户端连接: {clientSocket.RemoteEndPoint}");

					lock (lockObject)
					{
						connectedClients.Add(clientSocket);
					}

					Thread clientThread = new Thread(() => HandleClient(clientSocket));
					clientThread.IsBackground = true;
					clientThread.Start();
				}
			}
			catch
			{
				break;
			}
		}
	}

	private void HandleClient(Socket clientSocket)
	{
		try
		{
			while (isRunning && clientSocket.Connected)
			{
				byte[] buffer = new byte[4096];
				int bytesRead = clientSocket.Receive(buffer);

				if (bytesRead > 0)
				{
					byte[] data = new byte[bytesRead];
					Array.Copy(buffer, data, bytesRead);

					lock (lockObject)
					{
						messageQueue.Enqueue(data);
					}
				}
				else
				{
					break;
				}
			}
		}
		catch (SocketException ex)
		{
			if (ex.ErrorCode != 10004)
			{
				GD.PrintErr($"客户端处理异常: {ex.Message}");
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"客户端处理异常: {e.Message}");
		}
		finally
		{
			lock (lockObject)
			{
				connectedClients.Remove(clientSocket);
			}
			clientSocket.Close();
		}
	}

	private void ConnectToLocalhost()
	{
		ConnectToServer();
	}

	private void ConnectToServer()
	{
		try
		{
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(serverIP, serverPort);

			isRunning = true;
			isConnected = true;

			Thread receiveThread = new Thread(ReceiveLoop);
			receiveThread.IsBackground = true;
			receiveThread.Start();

			GD.Print($"成功连接到服务器: {serverIP}:{serverPort}");

			SendPlayerJoin();
		}
		catch (Exception e)
		{
			GD.PrintErr($"连接服务器失败: {e.Message}");
		}
	}

	private void ReceiveLoop()
	{
		try
		{
			while (isRunning && socket != null && socket.Connected)
			{
				byte[] buffer = new byte[4096];
				int bytesRead = socket.Receive(buffer);

				if (bytesRead > 0)
				{
					byte[] data = new byte[bytesRead];
					Array.Copy(buffer, data, bytesRead);

					lock (lockObject)
					{
						messageQueue.Enqueue(data);
					}
				}
				else
				{
					break;
				}
			}
		}
		catch (SocketException ex)
		{
			if (ex.ErrorCode != 10004)
			{
				GD.PrintErr($"接收数据异常: {ex.Message}");
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"接收数据异常: {e.Message}");
		}
		finally
		{
			isConnected = false;
		}
	}

	private void ProcessMessage(byte[] data)
	{
		string json = Encoding.UTF8.GetString(data);

		var jsonObj = new Json();
		jsonObj.Parse(json);
		var dict = jsonObj.Data.AsGodotDictionary();
		if (dict != null && dict.ContainsKey("messageType"))
		{
			string messageType = dict["messageType"].AsString();
			if (messageHandlers.ContainsKey(messageType))
			{
				messageHandlers[messageType](data);
			}
		}
	}

	public void SendMessage(string messageType, object data)
	{
		if (!isConnected) return;

		var packet = new Godot.Collections.Dictionary
		{
			["messageType"] = messageType,
			["senderID"] = playerID,
			["data"] = data != null ? data.ToString() : ""
		};

		string json = Json.Stringify(packet);
		byte[] bytes = Encoding.UTF8.GetBytes(json);

		try
		{
			if (socket != null && socket.Connected)
			{
				socket.Send(bytes);
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"发送消息失败: {e.Message}");
		}
	}

	private void SendPlayerJoin()
	{
		var joinData = new Godot.Collections.Dictionary
		{
			["playerID"] = playerID
		};
		SendMessage("PlayerJoin", joinData);
	}

	public void Disconnect()
	{
		isRunning = false;
		isConnected = false;

		if (socket != null)
		{
			try
			{
				socket.Shutdown(SocketShutdown.Both);
			}
			catch
			{
			}
			finally
			{
				socket.Close();
				socket = null;
			}
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationPredelete)
		{
			Disconnect();
		}
	}
}
