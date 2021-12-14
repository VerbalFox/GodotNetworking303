using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

//Class for keeping track of connections with other instances
public class NetworkConnection {
	public NetworkConnection(string newIp, int newPort, int newNetworkId) {
		ip = newIp;
		port = newPort;
		networkId = newNetworkId;
		timeSinceLastPacketReceived = 0;
	}
	public string ip;
	public int port;
	public int networkId;
	public float timeSinceLastPacketReceived;
}

//Class for keeping and syncing information across all instances
public class NetworkPlayer {
	public int playerNum;
	public Player playerObject;
}

public class NetworkManager : Node
{
    private const float timeSyncPacketSendIncrement = 5f;

    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    Random rnd;
	UdpClient udpClient;
	public double timeElapsed = 0, timeSinceLastTimeSync = 0, timeSinceLastPositionUpdate = 0;
	public int networkId = 0;
	public int playerId = 0;
	public int playerCount = 0;
	public bool isServer = false;
	private List<short> acknowledgementTokens;
	public List<NetworkConnection> connections;
	//For protecting the variables used in tasks/multithreading.
	public static System.Threading.Mutex tokenListMutex = new System.Threading.Mutex();
	public static System.Threading.Mutex gameStartedMutex = new System.Threading.Mutex();
	SceneSwitcher sceneSwitcher;
	PackedScene playerScene;
	Player[] players;
	Task gameStartTask = null;
	bool gameStarted = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		rnd = new Random();
		networkId = rnd.Next(int.MaxValue);

		acknowledgementTokens = new List<short>();
		connections = new List<NetworkConnection>();

		playerScene = GD.Load<PackedScene>("res://Scenes/Player.tscn");
		sceneSwitcher = GetNode<SceneSwitcher>("/root/SceneSwitcher");
	}

	public void OpenSocket(int port) {
		udpClient = new UdpClient(port);
		
		//Begin receiving packets, task loops til Udpclient stops existing.
		Task.Run(Receive);
	}

	public void Connect(string ip, int port, int networkId) {
		connections.Add(new NetworkConnection(ip, port, networkId));
	}

	public void ServerSyncStartGame() {
		ServerStartGamePacket serverStartGamePacket = new ServerStartGamePacket();
		serverStartGamePacket.acknowledgementToken = (short)rnd.Next(short.MaxValue);
		serverStartGamePacket.startTime = timeElapsed + 5;
		serverStartGamePacket.networkId = networkId;

		SendToAllConnections(serverStartGamePacket.Serialise());
		SendAssignPlayerId();
		BeginStartGameTask(serverStartGamePacket.startTime);
	}

	public void StartGame(double startTime) {
		while (timeElapsed < startTime) {
			//Do nothing, wait
		}

		sceneSwitcher.LoadGame();
		
		//Create player objects.
		players = new Player[playerCount];

		for (int i = 0; i < playerCount; i++) {
			players[i] = playerScene.Instance() as Player;
			players[i]._Ready();
			AddChild(players[i]);

			players[i].Translate(new Vector3(i * 5, 0, 0));
			
			players[i].playerId = i + 1;
			
			if (players[i].playerId == playerId) {
				players[i].CameraVisible(true);
				players[i].canMove = true;
			} else {
				players[i].CameraVisible(false);
				players[i].canMove = false;
			}
		}
		
		gameStartedMutex.WaitOne();
		gameStarted = true;
		gameStartedMutex.ReleaseMutex();

		gameStartTask.Dispose();
		gameStartTask = null;
	}

	private void BeginStartGameTask(double startTime) {
		gameStartTask = Task.Run(() => {StartGame(startTime);});
	}

	private void SendToAllConnections(byte[] stream)
	{
		foreach (var connection in connections) {
			udpClient.Send(stream, stream.Length, connection.ip, connection.port);

			short ackToken = Packet.GetAcknowledgementTokenFromStream(stream);
			PacketType packetType = Packet.GetPacketTypeFromStream(stream);

			if (ackToken != 0 && packetType != PacketType.AcknowledgementResponse)
			{
				Task.Run(() => { AcknowledgementPacketSender(ackToken, stream, connection.ip, connection.port); });
			}
		}
	}

	private void SendToSingleConnection(byte[] stream, int index)
	{
		udpClient.Send(stream, stream.Length, connections[index].ip, connections[index].port);

		short ackToken = Packet.GetAcknowledgementTokenFromStream(stream);
		PacketType packetType = Packet.GetPacketTypeFromStream(stream);

		if (ackToken != 0 && packetType != PacketType.AcknowledgementResponse)
		{
			Task.Run(() => { AcknowledgementPacketSender(ackToken, stream, connections[index].ip, connections[index].port); });
		}
	}

	public void AcknowledgementPacketSender(short token, byte[] stream, string ip, int port) {
		bool acknowlodgementArrived = false;
		double timeSinceStreamLastSent = timeElapsed;

		while (!acknowlodgementArrived) {
			foreach (var tokens in acknowledgementTokens.ToList()) {
				if (tokens == token) {
					acknowledgementTokens.Remove(tokens);
					acknowlodgementArrived = true;
				}
			}

			if (timeElapsed > timeSinceStreamLastSent + .2f) {
				GD.Print("Acknolodgement not arrived, resending packet: " + Packet.GetPacketTypeFromStream(stream));
				udpClient.Send(stream, stream.Length, ip, port);
				timeSinceStreamLastSent = timeElapsed;
			}
		}
	}

	public void SendTimeSync()
	{
		float timestamp = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) * 0.001f;

		TimeSyncServerPacket sendPacket = new TimeSyncServerPacket();

		sendPacket.serverTime = timeElapsed;
		sendPacket.serverTimestamp = timestamp;
		sendPacket.acknowledgementToken = (short)rnd.Next(short.MaxValue);
		sendPacket.networkId = networkId;

		SendToAllConnections(sendPacket.Serialise());
	}

	public void SendCollisionServer(Vector3 position, Vector3 normal, Vector3 playerPosition, int newPlayerId) {
		CollisionServerPacket collisionServerPacket = new CollisionServerPacket();
		collisionServerPacket.networkId = networkId;
		collisionServerPacket.position = position;
		collisionServerPacket.normal = normal;
		collisionServerPacket.playerPosition = playerPosition;
		collisionServerPacket.playerId = newPlayerId;

		SendToAllConnections(collisionServerPacket.Serialise());
	}
	
	public void SendCollisionClient(Vector3 position, Vector3 normal, Vector3 playerPosition, int newPlayerId) {
		CollisionClientPacket collisionClientPacket = new CollisionClientPacket();
		collisionClientPacket.networkId = networkId;
		collisionClientPacket.position = position;
		collisionClientPacket.normal = normal;
		collisionClientPacket.playerPosition = playerPosition;

		GD.Print("Sending collision client");

		SendToSingleConnection(collisionClientPacket.Serialise(), newPlayerId - 2);
	}
	public void SendAssignPlayerId() {
		if (isServer) {
			playerId = 1;
			playerCount = 1 + connections.Count;
			for (int i = 0; i < connections.Count; i++) {
				ServerAssignPlayerIdPacket serverAssignPlayerIdPacket = new ServerAssignPlayerIdPacket();
				serverAssignPlayerIdPacket.networkId = networkId;
				serverAssignPlayerIdPacket.acknowledgementToken = (short)rnd.Next(short.MaxValue);
				serverAssignPlayerIdPacket.playerId = 2 + i;
				serverAssignPlayerIdPacket.playerTotal = playerCount;

				SendToSingleConnection(serverAssignPlayerIdPacket.Serialise(), i);
			}
		}
	}

	public void SendAcknowledgementResponse(short token) {
		AcknowledgementResponsePacket acknowledgementResponsePacket = new AcknowledgementResponsePacket();
		acknowledgementResponsePacket.acknowledgementToken = token;
		acknowledgementResponsePacket.networkId = networkId;
		
		SendToAllConnections(acknowledgementResponsePacket.Serialise());
	}

	public void SendConnectionRequest(string ip, int port) {
		ConnectionRequestPacket connectionRequestPacket = new ConnectionRequestPacket();
		connectionRequestPacket.networkId = networkId;
		connectionRequestPacket.acknowledgementToken = (short)rnd.Next(short.MaxValue);

		var connectionBytes = connectionRequestPacket.Serialise();
		udpClient.Send(connectionBytes, connectionBytes.Length, ip, port);
	}
	public void SendPositionUpdateClient()
    {
        ServerPositionUpdateClientPacket serverPositionUpdateClientPacket = new ServerPositionUpdateClientPacket();
		serverPositionUpdateClientPacket.position = players[playerId - 1].GlobalTransform.origin;
		serverPositionUpdateClientPacket.velocity = players[playerId - 1].velocity;
		serverPositionUpdateClientPacket.playerId = playerId;
		serverPositionUpdateClientPacket.networkId = networkId;
		serverPositionUpdateClientPacket.acknowledgementToken = 0;

		SendToAllConnections(serverPositionUpdateClientPacket.Serialise());
    }
	public void SendPositionUpdateServer()
    {
        ServerPositionUpdateServerPacket serverPositionUpdateServerPacket = new ServerPositionUpdateServerPacket();
		serverPositionUpdateServerPacket.playerCount = playerCount;
		serverPositionUpdateServerPacket.position = new Vector3[playerCount];
		serverPositionUpdateServerPacket.velocity = new Vector3[playerCount];

		serverPositionUpdateServerPacket.networkId = networkId;
		serverPositionUpdateServerPacket.acknowledgementToken = 0;

		for (int i = 0; i < playerCount; i++) {
			serverPositionUpdateServerPacket.position[i] = players[i].GlobalTransform.origin;
			serverPositionUpdateServerPacket.velocity[i] = players[i].velocity;
		}

		SendToAllConnections(serverPositionUpdateServerPacket.Serialise());
    }

	public async void Receive() {
		while (udpClient != null)
		{
			UdpReceiveResult receivedResults;
			try {
				receivedResults = await udpClient.ReceiveAsync();
			} catch (ObjectDisposedException e) {
				GD.Print(e.Message);
				continue;
			} catch (SocketException e) {
				GD.Print(e.Message);
				continue;
			} catch (NullReferenceException e) {
				GD.Print(e.Message);
				continue;
			}

			var packetType = Packet.GetPacketTypeFromStream(receivedResults.Buffer);
			var networkId = Packet.GetNetworkIdFromStream(receivedResults.Buffer);

			//GD.Print($"{(isServer ? "Server" : "Client")} received packet of type {packetType}");

			bool packetFromConnection = false;
			//Check connection health
			foreach (var connection in connections) {
				if (connection.networkId == networkId) {
					connection.timeSinceLastPacketReceived = 0;
					packetFromConnection = true;
				}
			}

			if (!packetFromConnection && packetType != PacketType.ConnectionRequest) {
				continue;
			}

			switch (packetType) {
				case PacketType.AcknowledgementResponse:
					AcknowledgementResponsePacket acknowledgementResponsePacket = new AcknowledgementResponsePacket();
					acknowledgementResponsePacket.Deserialise(receivedResults.Buffer);
					bool tokenAlreadyReceived = false;
					int packetToken = acknowledgementResponsePacket.acknowledgementToken;

					foreach (var token in acknowledgementTokens) {
						if (token == packetToken) {
							tokenAlreadyReceived = true;
						}
					}

					if (!tokenAlreadyReceived)
						acknowledgementTokens.Add(acknowledgementResponsePacket.acknowledgementToken);
					break;
				case PacketType.ConnectionRequest:
					//Maintain idempotency
					bool connectionExists = false;
					foreach(var connection in connections) {
						if (connection.networkId == networkId) {
							connectionExists = true;
						}
					}

					if (!connectionExists) {
						var ep = receivedResults.RemoteEndPoint;
						connections.Add(new NetworkConnection(ep.Address.ToString(), ep.Port, networkId));
						SendTimeSync();
						if (isServer) {
							SendConnectionRequest(ep.Address.ToString(), ep.Port);
						}
					}
					break;
				case PacketType.TimeSyncServer:
					TimeSyncServerPacket receivedPacket = new TimeSyncServerPacket();
					receivedPacket.Deserialise(receivedResults.Buffer);
			
					float currentTimestamp = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) * 0.001f;
					timeElapsed = receivedPacket.serverTime + (currentTimestamp - receivedPacket.serverTimestamp);
					break;
				case PacketType.StartGame:
					if (gameStartTask == null) {
						ServerStartGamePacket serverStartGamePacket = new ServerStartGamePacket();
						serverStartGamePacket.Deserialise(receivedResults.Buffer);

						BeginStartGameTask(serverStartGamePacket.startTime);
					}
					break;
				case PacketType.ServerAssignPlayerId:
					ServerAssignPlayerIdPacket serverAssignPlayerIdPacket = new ServerAssignPlayerIdPacket();
					serverAssignPlayerIdPacket.Deserialise(receivedResults.Buffer);

					playerCount = serverAssignPlayerIdPacket.playerTotal;

					if (!isServer) {
						playerId = serverAssignPlayerIdPacket.playerId;
					}

					break;
				case PacketType.PositionUpdateClient:
					ServerPositionUpdateClientPacket serverPositionUpdateClientPacket = new ServerPositionUpdateClientPacket();
					serverPositionUpdateClientPacket.Deserialise(receivedResults.Buffer);

					Player clientPlayer = players[serverPositionUpdateClientPacket.playerId - 1];
					
					try {
						if (clientPlayer.timeLastPositionReceived < timeElapsed) {
							clientPlayer.timeLastPositionReceived = timeElapsed;
							clientPlayer.timeSinceLastPositionUpdate = 0;
							clientPlayer.mostRecentReceivedPosition = serverPositionUpdateClientPacket.position;
							clientPlayer.mostRecentReceivedVelocity = serverPositionUpdateClientPacket.velocity;
						}
					} catch (NullReferenceException e) {

					}
					break;
				case PacketType.PositionUpdateServer:
					ServerPositionUpdateServerPacket serverPositionUpdateServerPacket = new ServerPositionUpdateServerPacket();
					serverPositionUpdateServerPacket.Deserialise(receivedResults.Buffer);
					
					try {
						for (int i = 0; i < serverPositionUpdateServerPacket.playerCount; i++) {
							if (players[i].timeLastPositionReceived < timeElapsed) {
								players[i].timeLastPositionReceived = timeElapsed;
								players[i].timeSinceLastPositionUpdate = 0;
								players[i].mostRecentReceivedPosition = serverPositionUpdateServerPacket.position[i];
								players[i].mostRecentReceivedVelocity = serverPositionUpdateServerPacket.velocity[i];
							}
						}
					} catch (NullReferenceException e) {

					}
					break;
				case PacketType.CollisionServer:
					CollisionServerPacket collisionServerPacket = new CollisionServerPacket();
					collisionServerPacket.Deserialise(receivedResults.Buffer);
					
					if (playerId == 1 && isServer)
						players[playerId - 1].Launch(collisionServerPacket.position, collisionServerPacket.normal, collisionServerPacket.playerPosition);
					else if (isServer)
						SendCollisionClient(collisionServerPacket.position, collisionServerPacket.normal, collisionServerPacket.playerPosition, collisionServerPacket.playerId);
					break;
				case PacketType.CollisionClient:
					CollisionClientPacket collisionClientPacket = new CollisionClientPacket();
					collisionClientPacket.Deserialise(receivedResults.Buffer);
					
        			GD.Print("Launch packet received");
					players[playerId - 1].Launch(collisionClientPacket.position, collisionClientPacket.normal, collisionClientPacket.playerPosition);
					//SendCollisionClient(collisionServerPacket.position, collisionServerPacket.normal, collisionServerPacket.playerPosition, collisionServerPacket.playerId - 1);
					break;
			}

			short ackToken = Packet.GetAcknowledgementTokenFromStream(receivedResults.Buffer);
			if (ackToken != 0 && packetType != PacketType.AcknowledgementResponse) {
				SendAcknowledgementResponse(ackToken);
			}
		}
	}

	public override void _Process(float dt) {
		timeElapsed += dt;
		timeSinceLastTimeSync += dt;
		timeSinceLastPositionUpdate += dt;

		foreach (var connection in connections.ToList()) {
			connection.timeSinceLastPacketReceived += dt;
			if (connection.timeSinceLastPacketReceived > 10f) {
				connections.Remove(connection);

				if (gameStarted) {
					sceneSwitcher.LoadMenu();
				}
			}
		}

		if (isServer && timeSinceLastTimeSync > 5f) {
			SendTimeSync();
			timeSinceLastTimeSync = 0;
		}

		
		gameStartedMutex.WaitOne();
		if (gameStarted && timeSinceLastPositionUpdate > .05f) {
			if (isServer) {
				SendPositionUpdateServer();
			} else {
				SendPositionUpdateClient();
			}

			timeSinceLastPositionUpdate = 0f;
		}
		gameStartedMutex.ReleaseMutex();

	}

    
}
