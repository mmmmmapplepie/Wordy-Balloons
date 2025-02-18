using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IngameNetcodeAndSceneManager : NetworkBehaviour {
	public override void OnNetworkSpawn() {
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		NetworkManager.Singleton.OnServerStopped += ServerStopped;
		if (NetworkManager.IsServer) {
			CheckAllPlayersPresent();
		}
	}
	public override void OnNetworkDespawn() {
		if (NetworkManager.Singleton == null) return;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
	}
	public override void OnDestroy() {
		OnNetworkDespawn();
		base.OnDestroy();
	}
	void OnClientDisconnectCallback(ulong clientID) {
		if (clientID == NetworkManager.ServerClientId) {
			StopConnection();
			return;
		}
		if (!NetworkManager.Singleton.IsServer) return;
		GameData.team1.Remove(clientID);
		GameData.team2.Remove(clientID);
		GameData.ClientID_KEY_LobbyID_VAL.Remove(clientID);
		CheckTeamEmpty();
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
	void CheckTeamEmpty() {
		if (NetworkManager.Singleton.IsServer) Debug.LogWarning(NetworkManager.Singleton.ConnectedClients.Count);
		int teamRemaining = 0;
		if (GameData.team1.Count == 0) teamRemaining = 2;
		if (GameData.team2.Count == 0) teamRemaining = 1;
		if (teamRemaining != 0) {
			TeamEmptyClientRpc();
		}
	}
	public static event Action<GameStateManager.GameResult> GameResultChange;
	void StopConnection() {
		ShutDownNetwork();
		print("ConnectionStopped");
		GameResultChange?.Invoke(GameStateManager.GameResult.Draw);
	}
	void ServerStopped(bool b) {
		print("ServerStop");
		GameResultChange?.Invoke(GameStateManager.GameResult.Draw);
	}

	[ClientRpc]
	void TeamEmptyClientRpc() {
		print("TeamEmptyClientRPC");
		StopConnection();
	}

	void ShutDownNetwork() {
		if (!NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}
	public void GoToScene(string scene) {
		ShutDownNetwork();
		SceneManager.LoadScene(scene);
	}








}
