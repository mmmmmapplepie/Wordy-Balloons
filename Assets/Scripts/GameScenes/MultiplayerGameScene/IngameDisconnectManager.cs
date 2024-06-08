using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IngameDisconnectManager : NetworkBehaviour {
	public override void OnNetworkSpawn() {
		GameData.GameFinished = false;
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
		CheckWin();
	}
	public override void OnNetworkDespawn() {
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
	}
	public override void OnDestroy() {
		ShutDownNetwork();
		base.OnDestroy();
	}
	[SerializeField] GameObject connectionStopped, gameEnded;
	void ConnectionStopped() {
		if (GameData.GameFinished) return;
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
		CheckWin();
	}
	void CheckWin() {
		int teamWon = 0;
		if (GameData.team1.Count == 0) teamWon = 2;
		if (GameData.team2.Count == 0) teamWon = 1;
		if (teamWon != 0) {
			TeamWonClientRpc(teamWon);
		}
	}

	[ClientRpc]
	void TeamWonClientRpc(int winningTeam) {
		print("win");
		GameData.GameFinished = true;
		GameEnded(winningTeam);
	}

	void GameEnded(int winningTeam) {
		gameEnded.SetActive(true);
	}

	void ShutDownNetwork() {
		if (NetworkManager.Singleton == null) return;
		if (!NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}
	[SerializeField] UnityEditor.SceneAsset mainMenuScene;
	public void GoToScene(UnityEditor.SceneAsset scene) {
		if (scene == mainMenuScene) ShutDownNetwork();
		SceneManager.LoadScene(scene.name);
	}

}
