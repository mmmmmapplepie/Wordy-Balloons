using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;

public class NetcodeManager : MonoBehaviour {
	public static event Action ServerStartSuccess, ServerStartFail, ServerStoppedEvent, ShuttingDownNetwork, TransportFailureEvent;
	public static event Action ClientStartSuccess, ClientStartFail;
	public static event Action<ulong> ClientConnected, ClientDisconnected;
	public static event Action<bool> ClientStoppedEvent;

	public static NetcodeManager Instance = null;
	void Start() {
		if (Instance == null) {
			Instance = this;
			DontDestroyOnLoad(gameObject);
		} else {
			Destroy(gameObject);
			return;
		}

		NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedToNGO;
		NetworkManager.Singleton.OnServerStarted += ServerStarted;
		NetworkManager.Singleton.OnServerStopped += ServerStopped;
		NetworkManager.Singleton.OnTransportFailure += TransportFailure;

		NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectedFromNGO;
		NetworkManager.Singleton.OnClientStopped += ClientStopped;
	}

	public void OnDestroy() {
		if (Instance != this) return;
		if (NetworkManager.Singleton != null) {
			NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnectedToNGO;
			NetworkManager.Singleton.OnServerStarted -= ServerStarted;
			NetworkManager.Singleton.OnServerStopped -= ServerStopped;
			NetworkManager.Singleton.OnTransportFailure -= TransportFailure;
			NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnectedFromNGO;
			NetworkManager.Singleton.OnClientStopped -= ClientStopped;
		}
		Instance = null;
	}
	public void StartHost() {
		if (!NetworkManager.Singleton.StartHost()) ServerStartFail?.Invoke();
	}
	void ServerStarted() {
		ServerStartSuccess?.Invoke();
	}
	public void StartClient() {
		print("starting client");

		if (!NetworkManager.Singleton.StartClient()) {
			print("client start fail"); ClientStartFail?.Invoke();
		}
		print("finish starting client");
	}

	public void ShutDownNetwork() {
		StartCoroutine(ShutdownRoutine());
	}
	public bool shuttingDown { get; private set; } = false;
	readonly object shutdownLock = new object();
	IEnumerator ShutdownRoutine() {
		print("Shutdown NGO");
		lock (shutdownLock) {
			if (shuttingDown) yield break;
			shuttingDown = true;
		}
		ShuttingDownNetwork?.Invoke();
		if (NetworkManager.Singleton == null) { shuttingDown = false; yield break; }
		NetworkManager.Singleton.Shutdown();
		print(NetworkManager.Singleton.ShutdownInProgress);
		while (true) {
			if (NetworkManager.Singleton == null) break;
			if (!NetworkManager.Singleton.ShutdownInProgress && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) break;
			yield return null;
		}
		lock (shutdownLock) {
			shuttingDown = false;
		}
	}


	void ClientConnectedToNGO(ulong clientID) {
		print("m local id is : " + NetworkManager.Singleton.LocalClientId);
		print($"clinet started {clientID}");
		ClientConnected?.Invoke(clientID);
		if (clientID == NetworkManager.Singleton.LocalClientId) ClientStartSuccess?.Invoke();
	}
	void ClientDisconnectedFromNGO(ulong clientID) {
		print("disconnected");
		ClientDisconnected?.Invoke(clientID);
	}
	void ServerStopped(bool stopped) {
		ServerStoppedEvent?.Invoke();
	}
	void ClientStopped(bool wasHost) {
		print("clientStopped");
		ClientStoppedEvent?.Invoke(wasHost);
		// DespawnAllNetworkObjectsWithoutDestroying();
	}
	void DespawnAllNetworkObjectsWithoutDestroying() {
		foreach (NetworkObject netObj in FindObjectsOfType<NetworkObject>()) {
			if (netObj == null)
				continue;

			print(netObj.gameObject.name);

			// if (netObj.IsSpawned && !netObj.IsOwner && netObj.IsSceneObject != false)
			// 	continue; // Clients can't despawn remote non-scene objects

			if (netObj.IsSpawned) {
				// Despawn but don't destroy the GameObject
				netObj.Despawn(false);
				Debug.Log($"Despawned (kept): {netObj.name}");
			}
		}
	}
	void TransportFailure() {
		TransportFailureEvent?.Invoke();
	}

	public bool? InConnectedSession() {
		if (NetworkManager.Singleton == null) return null;
		if (NetworkManager.Singleton.ShutdownInProgress) return true;
		if (NetworkManager.Singleton.IsConnectedClient) return true;
		if (NetworkManager.Singleton.IsServer) return true;
		if (NetworkManager.Singleton.IsClient) return true;
		if (shuttingDown) return true;
		return false;
	}







}
