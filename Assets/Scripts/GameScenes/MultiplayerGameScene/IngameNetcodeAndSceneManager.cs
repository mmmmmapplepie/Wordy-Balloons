using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IngameNetcodeAndSceneManager : NetworkBehaviour {
	void Awake() {
		shutdownNetworkEvent += StopChecksForConnection;
		GameStateManager.GameResultSetEvent += GameResult;
	}
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		NetworkManager.Singleton.OnClientStopped += ClientStopped;
		NetworkManager.Singleton.OnServerStopped += ServerStopped;

		if (NetworkManager.IsServer) {
			CheckAllPlayersPresent();
		}
	}
	public override void OnNetworkDespawn() {
		base.OnNetworkDespawn();
		if (NetworkManager.Singleton == null) return;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
		NetworkManager.Singleton.OnClientStopped -= ClientStopped;
		NetworkManager.Singleton.OnServerStopped -= ServerStopped;

	}
	public override void OnDestroy() {
		shutdownNetworkEvent -= StopChecksForConnection;
		GameStateManager.GameResultSetEvent -= GameResult;
		OnNetworkDespawn();
		ShutDownNetwork();
		base.OnDestroy();
		Time.timeScale = 1f;
	}
	void GameResult(GameStateManager.GameResult r) {
		if (r == GameStateManager.GameResult.Draw) return;
		Invoke(nameof(ShutDownNetwork), BaseManager.BaseDestroyAnimationTime);
	}
	void ClientStopped(bool host) {
		if (NetworkManager.Singleton.IsConnectedClient) OnClientDisconnectCallback(NetworkManager.Singleton.LocalClientId);
	}
	void OnClientDisconnectCallback(ulong clientID) {
		if (clientID == NetworkManager.Singleton.LocalClientId) {
			GameStateManager.GameResult loss = GameStateManager.GameResult.Team1Win;
			if (BalloonManager.team == Team.t1) loss = GameStateManager.GameResult.Team2Win;
			GameResultChangeByConnection?.Invoke(loss);
			ChangeReconnectionState(false);
		}
		if (clientID == NetworkManager.ServerClientId) {
			StopConnection();
			return;
		}
		if (!NetworkManager.Singleton.IsServer) return;
		GameData.team1.Remove(clientID);
		GameData.team2.Remove(clientID);
		GameData.ClientID_KEY_LobbyID_VAL.Remove(clientID);
		if (reconnectingClients.Count > 0) CheckDisconnectedIsInReconnectionTimeout(clientID);
		CheckAllPlayersPresent();
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
		// if (NetworkManager.Singleton.IsServer) Debug.LogWarning(NetworkManager.Singleton.ConnectedClients.Count);
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
		ChangeReconnectionState(false);
	}
	void ServerStopped(bool b) {
		StopConnection();
	}

	[ClientRpc]
	void TeamEmptyClientRpc() {
		StopConnection();
	}

	public static event Action shutdownNetworkEvent;
	public void ShutDownNetwork() {
		shutdownNetworkEvent?.Invoke();
		if (NetworkManager.Singleton != null && !NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}









	#region connectionHandling
	float pingRate = 4f;
	float pingWaitTime;
	void Update() {
		if (GameData.PlayMode != PlayModeEnum.Multiplayer) return;
		if (GameStateManager.CurrGameResult != GameStateManager.GameResult.Undecided) return;

		if (NetworkManager.Singleton.IsHost) {
			PingClientsForConnection();
		}
		CheckLackOfServerPing();
	}

	public GameObject reconnectingPanel;
	public TextMeshProUGUI reconnectingIssueTxt;

	float latestServerPingReceivedTime = 0f;
	bool noServerPing = false;
	void CheckLackOfServerPing() {
		if (Time.time - latestServerPingReceivedTime > pingWaitTime * 1.5f) {
			reconnectingIssueTxt.text = "Server Unresponsive";
			noServerPing = true;
		}
		if ((!noServerPing && !waitingForPlayers) || GameStateManager.CurrGameResult == GameStateManager.GameResult.Undecided) {
			ChangeReconnectionState(false);
		} else {
			ChangeReconnectionState(true);
		}
	}
	List<ulong> idsWaitingForRepliesFrom = new List<ulong>();
	HashSet<ulong> reconnectingClients = new HashSet<ulong>();
	uint pingIndex = 0;
	float lastPingTime = -100f;
	void PingClientsForConnection() {
		if (Time.unscaledTime - lastPingTime < pingRate) return;
		CheckReplies();
		idsWaitingForRepliesFrom = NetworkManager.Singleton.ConnectedClientsIds.ToList();
		// print(idsWaitingForRepliesFrom.Count);
		lastPingTime = Time.unscaledTime;
		pingIndex++;
		// print($"Pinging clinets {pingIndex}");
		PingClientsForConnectionClientRpc(pingIndex);
	}
	void CheckReplies() {
		foreach (ulong id in reconnectingClients) {
			if (!NetworkManager.Singleton.ConnectedClientsIds.Contains(id)) reconnectingClients.Remove(id);
		}
		foreach (ulong id in idsWaitingForRepliesFrom) {
			if (!NetworkManager.Singleton.ConnectedClientsIds.Contains(id)) {
				continue;
			}
			reconnectingClients.Add(id);
		}
		foreach (ulong id in reconnectingClients) {
			print(id);
		}
		if (reconnectingClients.Count > 0) {
			PauseGameClientRpc(reconnectingClients.Count);
		} else {
			ResumeGameClientRpc();
		}
	}
	[ClientRpc]
	void PingClientsForConnectionClientRpc(uint index) {
		// print($"ping receveid: {index}");
		noServerPing = false;
		latestServerPingReceivedTime = Time.unscaledTime;
		PingReceivedReplyServerRpc(NetworkManager.Singleton.LocalClientId, index);
	}
	[ServerRpc(RequireOwnership = false)]
	void PingReceivedReplyServerRpc(ulong id, uint index) {
		// print($"Ping received from {id} with index {index}");
		if (index != pingIndex) return;
		idsWaitingForRepliesFrom.Remove(id);
		reconnectingClients.Remove(id);
	}

	bool waitingForPlayers = false;
	[ClientRpc]
	void PauseGameClientRpc(int playersReconnecting) {
		waitingForPlayers = true;
		reconnectingIssueTxt.text = $"{playersReconnecting}: Player(s) trying to reconnect";
	}
	[ClientRpc]
	void ResumeGameClientRpc() {
		waitingForPlayers = false;
	}
	void ChangeReconnectionState(bool waitingForReconnection) {
		reconnectingPanel.SetActive(waitingForReconnection);
		Time.timeScale = waitingForReconnection ? 0f : 1f;
	}

	void CheckDisconnectedIsInReconnectionTimeout(ulong id) {
		reconnectingClients.Remove(id);
		if (reconnectingClients.Count == 0) ResumeGameClientRpc();
	}

	void StopChecksForConnection() {
		CancelInvoke();
		reconnectingPanel.SetActive(false);
	}

	#endregion


}