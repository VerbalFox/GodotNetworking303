using Godot;
using System;
using System.Text;

using System.Net.Sockets;

public class MenuManager : Control
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	
	private NetworkManager networkManager;
	private Button submitButton, connectButton;
	private TextEdit ipTextBox, portTextBox;
	private RichTextLabel debugTextLabel;
	private Label ipLabel, portLabel;
	private CheckBox checkBox;

	Random rnd = new Random();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		connectButton = GetNode<Button>("Connect");
		submitButton = GetNode<Button>("Submit");
		ipTextBox = GetNode<TextEdit>("IpTextEdit");
		portTextBox = GetNode<TextEdit>("PortTextEdit");
		debugTextLabel = GetNode<RichTextLabel>("DebugTextLabel");
		checkBox = GetNode<CheckBox>("CheckBox");

		ipLabel = GetNode<Label>("IpLabel");
		portLabel = GetNode<Label>("PortLabel");
		
		networkManager.isServer = false;
		
		connectButton.Connect("pressed", this, "NetworkConnect");
		submitButton.Connect("pressed", this, "NetworkSubmit");
		checkBox.Connect("pressed", this, "ToggleIsServer");

	}

	public void NetworkConnect() {
		if (networkManager.isServer) {
			networkManager.OpenSocket(portTextBox.Text.ToInt());
		} else {
            networkManager.OpenSocket(rnd.Next(4000, 4100));

            if (int.TryParse(portTextBox.Text, out int portNumber)) {
				networkManager.SendConnectionRequest(ipTextBox.Text, portTextBox.Text.ToInt());
			}
		}

		connectButton.Disabled = true;
	}

	public void NetworkSubmit() {
		if (networkManager.isServer) {
			networkManager.ServerSyncStartGame();
		}
	}
	
	public void ToggleIsServer() {
		networkManager.isServer = !networkManager.isServer;

		ipTextBox.Visible = !networkManager.isServer;
		ipLabel.Visible = !networkManager.isServer;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(float delta)
	{
		debugTextLabel.Text = networkManager.timeElapsed.ToString() + "\n" + networkManager.networkId + "\n";
		foreach (var connection in networkManager.connections)
		{
			debugTextLabel.Text += ($"IP: {connection.ip} | Port: {connection.port} | Network ID: {connection.networkId} \n");
		};
	}
}
