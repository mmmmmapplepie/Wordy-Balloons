using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IngameNetcodeAndSceneManager : NetworkBehaviour {
	public override void OnNetworkSpawn() {
		GameStateManager.GameRunning = false;

		if (GameData.InSinglePlayerMode) {
			GameStateManager.GameRunning = true;
			return;
		}

		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		if (NetworkManager.IsServer) {
			CheckAllPlayersPresent();
		}
	}
	void CheckAllPlayersPresent() {
		Dictionary<ulong, string> LCID = GameData.ClientID_KEY_LobbyID_VAL;
		for (int i = 0; i < LCID.Count;) {
			KeyValuePair<ulong, string> pair = LCID.ElementAt(i);
			if (NetworkManager.Singleton.ConnectedClientsIds.Contains(pair.Key)) { i++; continue; }
			GameData.team1.Remove(pair.Key);
			GameData.team2.Remove(pair.Key);
			LCID.Remove(pair.Key);
		}
		CheckTeamEmpty();
	}
	public override void OnNetworkDespawn() {

		if (NetworkManager.Singleton == null) return;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
	}
	public override void OnDestroy() {
		OnNetworkDespawn();
		base.OnDestroy();
	}
	[SerializeField] GameObject connectionStopped, gameEnded;
	void ConnectionStopped() {
		ShutDownNetwork();
		if (GameStateManager.GameRunning) return;
		connectionStopped.SetActive(true);
	}
	void OnClientDisconnectCallback(ulong clientID) {
		if (clientID == NetworkManager.ServerClientId) {
			ConnectionStopped();
		}
		if (!NetworkManager.Singleton.IsServer) return;
		GameData.team1.Remove(clientID);
		GameData.team2.Remove(clientID);
		GameData.ClientID_KEY_LobbyID_VAL.Remove(clientID);
		CheckTeamEmpty();
	}
	void CheckTeamEmpty() {
		if (NetworkManager.Singleton.IsServer) Debug.LogWarning(NetworkManager.Singleton.ConnectedClients.Count);
		int teamRemaining = 0;
		if (GameData.team1.Count == 0) teamRemaining = 2;
		if (GameData.team2.Count == 0) teamRemaining = 1;
		if (teamRemaining != 0) {
			TeamEmptyClientRpc();
		}
	}

	[ClientRpc]
	void TeamEmptyClientRpc() {
		ConnectionStopped();
	}

	void ShutDownNetwork() {
		if (!NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}
	[SerializeField] UnityEditor.SceneAsset mainMenuScene;
	public void GoToScene(UnityEditor.SceneAsset scene) {
		ShutDownNetwork();
		SceneManager.LoadScene(scene.name);
	}








}
