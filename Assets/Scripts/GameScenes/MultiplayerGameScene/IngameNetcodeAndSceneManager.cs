using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using WebSocketSharp;

public class IngameNetcodeAndSceneManager : NetworkBehaviour {
	void Awake() {
		GameStateManager.GameResultSetEvent += GameResultSet;
	}
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		InternetConnectivityCheck.ConnectedStateEvent += ConnectionChanged;
		NetworkManager.SceneManager.OnLoadEventCompleted += SceneLoaded;
		if (NetworkManager.IsServer) {
			UpdateConnectedPlayers();
		}
	}
	public override void OnNetworkDespawn() {
		base.OnNetworkDespawn();
		if (NetworkManager.Singleton == null) return;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
		InternetConnectivityCheck.ConnectedStateEvent -= ConnectionChanged;
		NetworkManager.SceneManager.OnLoadEventCompleted -= SceneLoaded;

	}
	public override void OnDestroy() {
		GameStateManager.GameResultSetEvent -= GameResultSet;
		OnNetworkDespawn();
		ShutDownNetwork();
		base.OnDestroy();
		Time.timeScale = 1f;
	}
	void GameResultSet(GameResult r) {
		ChangeReconnectionState(false);
		ShutDownNetwork();
		// Invoke(nameof(ShutDownNetwork), BaseManager.BaseDestroyAnimationTime);
	}

	void OnClientDisconnectCallback(ulong clientID) {
		print("client disconnected:" + clientID);
		if (NetworkManager.Singleton.IsServer) {
			GameData.team1.Remove(clientID);
			GameData.team2.Remove(clientID);
			GameData.ClientID_KEY_LobbyID_VAL.Remove(clientID);
			GameData.ClientID_KEY_LobbyID_NAME.Remove(clientID);
			if (reconnectingClients.Count > 0) CheckDisconnectedIsInReconnectionTimeout(clientID);
			UpdateConnectedPlayers();
		}
		if (clientID == NetworkManager.Singleton.LocalClientId) {
			disconnectIssueTxt.text = "Disconnected from session";
			DisconnectingEvent?.Invoke();
		}
	}
	void UpdateConnectedPlayers() {
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
	private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
		if (NetworkManager.Singleton.IsServer) {
			print("players timeout on loading " + clientsTimedOut.Count);
			foreach (ulong id in clientsTimedOut) {
				OnClientDisconnectCallback(id);
				NetworkManager.Singleton.DisconnectClient(id);
			}
		}
	}
	void CheckTeamEmpty() {
		Team? teamRemaining = null;
		if (GameData.team1.Count == 0) teamRemaining = teamRemaining = Team.t2;
		if (GameData.team2.Count == 0) teamRemaining = teamRemaining = Team.t1;
		if (teamRemaining != null) TeamEmptyEvent?.Invoke((Team)teamRemaining);
	}
	//bool is for as host or just client. host = true, just client = false
	public static event Action DisconnectingEvent;
	public static event Action<Team> TeamEmptyEvent;
	public void ShutDownNetwork() {
		print("shutting down network");
		if (NetworkManager.Singleton != null && !NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}

	int connectionFalseTicks = 0;
	void ConnectionChanged(bool connected) {
		print("connection state of: " + connected);
		if (connected) { connectionFalseTicks = 0; } else {
			connectionFalseTicks++;
			if (connectionFalseTicks == 2) {
				disconnectIssueTxt.text = "Internet unavailable";
				DisconnectingEvent?.Invoke();
			}
		}
	}






	#region connectionHandlingPinging
	float pingInterval = 3f;
	float pingWaitTime = 8f;
	void Update() {
		if (GameData.PlayMode != PlayModeEnum.Multiplayer) return;
		if (GameStateManager.CurrGameResult != GameResult.Undecided) return;

		if (NetworkManager.Singleton.IsHost) {
			PingClientsForConnection();
		}
		CheckServerUnresponsive();
	}

	uint pingIndex = 0;
	float lastPingTime = -100f;
	void PingClientsForConnection() {
		if (Time.unscaledTime - lastPingTime < pingInterval) return;
		CheckReplies();
		idsPendingWithPendingReplies = NetworkManager.Singleton.ConnectedClientsIds.ToList();
		// print(idsWaitingForRepliesFrom.Count);
		lastPingTime = Time.unscaledTime;
		pingIndex++;
		// print($"Pinging clinets {pingIndex}");
		PingClientsForConnectionClientRpc(pingIndex);
	}
	public GameObject reconnectingPanel;
	public TextMeshProUGUI disconnectIssueTxt, waitingForPlayersTxt;

	float latestServerPingReceivedTime = 0f;
	void CheckServerUnresponsive() {
		if (Time.unscaledTime - latestServerPingReceivedTime > pingInterval * 1.5f) {
			waitingForPlayersTxt.text = "Host Unresponsive";
			ChangeReconnectionState(true);
		}
		if (Time.unscaledTime - latestServerPingReceivedTime > pingWaitTime) {
			disconnectIssueTxt.text = "Host Timeout";
			ChangeReconnectionState(false);
			DisconnectingEvent?.Invoke();
		}
	}
	List<ulong> idsPendingWithPendingReplies = new List<ulong>();
	HashSet<ulong> reconnectingClients = new HashSet<ulong>();
	void CheckReplies() {
		foreach (ulong id in reconnectingClients) {
			if (!NetworkManager.Singleton.ConnectedClientsIds.Contains(id)) reconnectingClients.Remove(id);
		}
		foreach (ulong id in idsPendingWithPendingReplies) {
			if (!NetworkManager.Singleton.ConnectedClientsIds.Contains(id)) {
				continue;
			}
			reconnectingClients.Add(id);
		}
		foreach (ulong id in reconnectingClients) {
			print("reconnecting to: id-" + id);
		}
		if (reconnectingClients.Count > 0) {
			List<string> nameslist = new List<string>();
			foreach (ulong id in reconnectingClients) {
				string name = GameData.ClientID_KEY_LobbyID_NAME[id];
				if (!name.IsNullOrEmpty()) nameslist.Add(name);
			}
			string names = string.Join(NameSeparator, nameslist);
			PauseGameClientRpc(names);
		} else {
			ResumeGameClientRpc();
		}
	}
	[ClientRpc]
	void PingClientsForConnectionClientRpc(uint index) {
		// print($"ping receveid: {index}");
		latestServerPingReceivedTime = Time.unscaledTime;
		PingReceivedReplyServerRpc(NetworkManager.Singleton.LocalClientId, index);
	}
	[ServerRpc(RequireOwnership = false)]
	void PingReceivedReplyServerRpc(ulong id, uint index) {
		// print($"Ping received from {id} with index {index}");
		if (index != pingIndex) return;
		idsPendingWithPendingReplies.Remove(id);
		reconnectingClients.Remove(id);

		if (reconnectingClients.Count == 0) {
			ResumeGameClientRpc();
		}
	}
	const string NameSeparator = "\0";

	[ClientRpc]
	void PauseGameClientRpc(string names) {
		string displayString = names.Replace(NameSeparator, "\n");
		waitingForPlayersTxt.text = "PLAYER(S) TRYING TO RECONNECT:\n" + displayString;
		if (GameStateManager.CurrGameResult == GameResult.Undecided) {
			ChangeReconnectionState(true);
		}
	}
	[ClientRpc]
	void ResumeGameClientRpc() {
		if (GameStateManager.CurrGameResult == GameResult.Undecided) {
			ChangeReconnectionState(false);
		}
	}
	void CheckDisconnectedIsInReconnectionTimeout(ulong id) {
		reconnectingClients.Remove(id);
		if (reconnectingClients.Count == 0) ResumeGameClientRpc();
	}
	void ChangeReconnectionState(bool waitingForReconnection) {
		reconnectingPanel.SetActive(waitingForReconnection);
		Time.timeScale = waitingForReconnection ? 0f : 1f;
	}

	#endregion


}