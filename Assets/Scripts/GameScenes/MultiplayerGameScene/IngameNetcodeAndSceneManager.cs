using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IngameNetcodeAndSceneManager : NetworkBehaviour {
	public override void OnNetworkSpawn() {
		GameStateManager.GameRunning = false;

		if (GameData.SinglePlayerMode) {
			GameStateManager.GameRunning = true;
			return;
		}

		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		if (NetworkManager.IsServer) {
			CheckAllPlayersPresent();
		}
	}
	void CheckAllPlayersPresent() {
		Dictionary<string, ulong> LCID = GameData.LobbyID_KEY_ClientID_VAL;
		for (int i = 0; i < LCID.Count;) {
			KeyValuePair<string, ulong> pair = LCID.ElementAt(i);
			if (NetworkManager.Singleton.ConnectedClientsIds.Contains(pair.Value)) { i++; continue; }
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
		foreach (KeyValuePair<string, ulong> pair in GameData.LobbyID_KEY_ClientID_VAL) {
			if (pair.Value == clientID) {
				GameData.team1.Remove(pair.Key);
				GameData.team2.Remove(pair.Key);
				GameData.LobbyID_KEY_ClientID_VAL.Remove(pair.Key);
				break;
			}
		}
		CheckTeamEmpty();
	}
	void CheckTeamEmpty() {
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
