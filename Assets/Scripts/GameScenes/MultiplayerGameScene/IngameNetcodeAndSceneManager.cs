using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IngameNetcodeAndSceneManager : NetworkBehaviour {
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		NetworkManager.Singleton.OnServerStopped += ServerStopped;
		if (NetworkManager.IsServer) {
			CheckAllPlayersPresent();
		}
	}
	public override void OnNetworkDespawn() {
		base.OnNetworkDespawn();
		if (NetworkManager.Singleton == null) return;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
	}
	public override void OnDestroy() {
		OnNetworkDespawn();
		ShutDownNetwork();
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
	public static event Action<GameStateManager.GameResult> GameResultChangeByConnection;
	void StopConnection() {
		ShutDownNetwork();
		GameResultChangeByConnection?.Invoke(GameStateManager.GameResult.Draw);
	}
	void ServerStopped(bool b) {
		GameResultChangeByConnection?.Invoke(GameStateManager.GameResult.Draw);
	}

	[ClientRpc]
	void TeamEmptyClientRpc() {
		StopConnection();
	}

	public static void ShutDownNetwork() {
		if (NetworkManager.Singleton != null && !NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}







}
