using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyNetcodeManager : NetworkBehaviour {
	public static event Action ServerStartSuccess, ServerStartFail, ServerStoppedEvent, ShuttingDownNetwork;
	public static event Action ClientStartSuccess, ClientStartFail;
	public static event Action<ulong> ClientConnected, ClientDisconnected;
	public static event Action<bool> ClientStoppedEvent;

	public static LobbyNetcodeManager Instance = null;
	void Awake() {
		Instance = this;
	}

	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedToNGO;
		NetworkManager.Singleton.OnServerStarted += ServerStarted;
		NetworkManager.Singleton.OnServerStopped += ServerStopped;

		//a client that is disconnecting also gets this callback (as if the still connected are disconnecting... as opposed to client stopped)
		NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectedFromNGO;
		NetworkManager.Singleton.OnClientStopped += ClientStopped;

	}
	public override void OnDestroy() {
		OnNetworkDespawn();
		base.OnDestroy();
		Instance = null;
	}
	public override void OnNetworkDespawn() {
		if (NetworkManager.Singleton != null) {
			NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnectedToNGO;
			NetworkManager.Singleton.OnServerStarted -= ServerStarted;
			NetworkManager.Singleton.OnServerStopped -= ServerStopped;
			NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnectedFromNGO;
			NetworkManager.Singleton.OnClientStopped -= ClientStopped;
		}
		base.OnDestroy();
	}

	public void StartHost() {
		if (!NetworkManager.Singleton.StartHost()) ServerStartFail?.Invoke();
	}
	void ServerStarted() {
		ServerStartSuccess?.Invoke();
	}
	public void StartClient() {
		if (!NetworkManager.Singleton.StartClient()) ClientStartFail?.Invoke();
	}

	public void ShutDownNetwork() {
		print("Shutdown NGO");
		ShuttingDownNetwork?.Invoke();
		if (NetworkManager.Singleton == null) return;
		if (!NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}


	void ClientConnectedToNGO(ulong clientID) {
		ClientConnected?.Invoke(clientID);
		if (clientID == NetworkManager.Singleton.LocalClientId) ClientStartSuccess?.Invoke();
	}
	void ClientDisconnectedFromNGO(ulong clientID) {
		ClientDisconnected?.Invoke(clientID);
	}
	void ServerStopped(bool stopped) {
		ServerStoppedEvent?.Invoke();
	}
	void ClientStopped(bool wasHost) {
		ClientStoppedEvent?.Invoke(wasHost);
	}









}
