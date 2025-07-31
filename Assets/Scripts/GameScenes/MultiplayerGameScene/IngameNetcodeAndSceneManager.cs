using System;
using System.Collections;
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
		GameStateManager.GameStartEvent += GameStart;
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
		if (NetworkManager.SceneManager != null) NetworkManager.SceneManager.OnLoadEventCompleted -= SceneLoaded;

	}
	public override void OnDestroy() {
		GameStateManager.GameResultSetEvent -= GameResultSet;
		GameStateManager.GameStartEvent -= GameStart;
		OnNetworkDespawn();
		ShutDownNetwork();
		base.OnDestroy();
		Time.timeScale = 1f;
	}
	void GameResultSet(GameState r) {
		ChangeReconnectionState(false);
		StopAllCoroutines();
		if (r != GameState.Disconnect) Invoke(nameof(ShutDownNetwork), BaseManager.BaseDestroyAnimationTime);
		else ShutDownNetwork();
	}

	void OnClientDisconnectCallback(ulong clientID) {
		// print("client disconnected:" + clientID);
		if (clientID == NetworkManager.Singleton.LocalClientId) {
			print("self disconnect");
			disconnectIssueTxt.text = "Disconnected from session";
			DisconnectingEvent?.Invoke();
			return;
		}
		if (clientID == NetworkManager.ServerClientId) {
			print("host disconnect");
			disconnectIssueTxt.text = "Host disconnected";
			DisconnectingEvent?.Invoke();
			return;
		}
		if (NetworkManager.Singleton.IsServer) {
			GameData.team1.Remove(clientID);
			GameData.team2.Remove(clientID);
			GameData.ClientID_KEY_LobbyID_VAL.Remove(clientID);
			GameData.ClientID_KEY_LobbyID_NAME.Remove(clientID);
			UpdatePinglistForGivenDisconnect(clientID);
			UpdateConnectedPlayers();
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
		// print("connection state of: " + connected);
		if (connected) { connectionFalseTicks = 0; } else {
			connectionFalseTicks++;
			if (connectionFalseTicks == 2) {
				disconnectIssueTxt.text = "Internet unavailable";
				DisconnectingEvent?.Invoke();
			}
		}
	}






	#region connectionHandlingPinging
	const float pingInterval = 1f, lackOfPingAnswerTimeout = 4f, disconnectTimeout = 8f, pingCheckInterval = 0.5f;
	const string NameSeparator = "\0";
	float latestServerPingReceivedTime = -100f;
	Dictionary<ulong, float> latestPingAnswerTimes;
	bool gameStart = false;
	public GameObject reconnectingPanel;
	public TextMeshProUGUI disconnectIssueTxt, waitingForPlayersTxt;
	bool PingCheckRequired() {
		if (GameData.PlayMode != PlayModeEnum.Multiplayer) return false;
		if (GameStateManager.CurrGameState != GameState.InPlay) return false;
		if (!gameStart) return false;
		return true;
	}
	float checkTime = -100f;
	void Update() {
		if (!PingCheckRequired()) return;
		if (Time.unscaledTime - checkTime < pingCheckInterval) return;
		checkTime = Time.unscaledTime;
		if (NetworkManager.Singleton.IsServer) {
			CheckClientsUpdated();
		} else {
			CheckServerUpdated();
		}
	}
	void GameStart() {
		if (!NetworkManager.Singleton.IsServer) return;
		if (!gameStart) StartCoroutine(ClientPingRoutine());
		gameStart = true;
	}
	IEnumerator ClientPingRoutine() {
		while (true) {
			if (latestPingAnswerTimes == null) {
				latestPingAnswerTimes = new Dictionary<ulong, float>();
				foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
					AddClientToPingRecipient(id, Time.unscaledTime);
				}
			}
			PingClientsForConnectionClientRpc();
			yield return new WaitForSecondsRealtime(pingInterval);
		}
	}
	void AddClientToPingRecipient(ulong id, float time) {
		if (latestPingAnswerTimes.ContainsKey(id)) return;
		latestPingAnswerTimes.Add(id, time);
	}
	[ClientRpc]
	void PingClientsForConnectionClientRpc() {
		latestServerPingReceivedTime = Time.unscaledTime;
		PingReceivedReplyServerRpc(NetworkManager.Singleton.LocalClientId);
	}
	[ServerRpc(RequireOwnership = false)]
	void PingReceivedReplyServerRpc(ulong id) {
		if (!PingCheckRequired()) return;
		if (latestPingAnswerTimes.ContainsKey(id)) {
			latestPingAnswerTimes[id] = Time.unscaledTime;
		}
		List<(string, ulong)> latePlayers = GetPlayersLateOnPing(lackOfPingAnswerTimeout);
		if (latePlayers.Count == 0) ResumeGameClientRpc();
	}
	List<(string, ulong)> GetPlayersLateOnPing(float timeout) {
		List<(string, ulong)> latePlayersNameAndId = new List<(string, ulong)>();
		foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
			if (latestPingAnswerTimes.ContainsKey(id) && (Time.unscaledTime - latestPingAnswerTimes[id]) > timeout) {
				string name = GameData.ClientID_KEY_LobbyID_NAME.ContainsKey(id) ? GameData.ClientID_KEY_LobbyID_NAME[id] : id.ToString();
				latePlayersNameAndId.Add((name, id));
			} else {
				AddClientToPingRecipient(id, Time.unscaledTime);
			}
		}
		return latePlayersNameAndId;
	}
	[ClientRpc]
	void ResumeGameClientRpc() {
		if (GameStateManager.CurrGameState == GameState.InPlay) {
			ChangeReconnectionState(false);
		}
	}
	void ChangeReconnectionState(bool waitingForReconnection) {
		if (GameStateManager.CurrGameState != GameState.InPlay && waitingForReconnection) return;
		reconnectingPanel.SetActive(waitingForReconnection);
		Time.timeScale = waitingForReconnection ? 0f : 1f;
	}
	void UpdatePinglistForGivenDisconnect(ulong id) {
		if (!PingCheckRequired()) return;
		if (latestPingAnswerTimes.ContainsKey(id)) latestPingAnswerTimes.Remove(id);
		List<(string, ulong)> latePlayers = GetPlayersLateOnPing(lackOfPingAnswerTimeout);
		if (latePlayers.Count == 0) ResumeGameClientRpc();
	}

	void CheckServerUpdated() {
		if (latestServerPingReceivedTime < 0) latestServerPingReceivedTime = Time.unscaledTime;
		if (Time.unscaledTime - latestServerPingReceivedTime > disconnectTimeout) {
			disconnectIssueTxt.text = "Host Connection Lost";
			ChangeReconnectionState(false);
			DisconnectingEvent?.Invoke();
			return;
		}
		if (Time.unscaledTime - latestServerPingReceivedTime > lackOfPingAnswerTimeout) {
			waitingForPlayersTxt.text = "Waiting for Host";
			ChangeReconnectionState(true);
		}
	}
	void CheckClientsUpdated() {
		List<ulong> nonActivePlayers = latestPingAnswerTimes.Keys.Except(NetworkManager.Singleton.ConnectedClientsIds).ToList();
		foreach (ulong id in nonActivePlayers) {
			print("removing playaha as theyre non active");
			latestPingAnswerTimes.Remove(id);
		}
		foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
			if (!latestPingAnswerTimes.ContainsKey(id)) {
				AddClientToPingRecipient(id, Time.unscaledTime);
			}
		}

		List<(string, ulong)> timeoutPlayersToDisconnect = GetPlayersLateOnPing(disconnectTimeout);
		foreach ((string, ulong) player in timeoutPlayersToDisconnect) {
			OnClientDisconnectCallback(player.Item2);
			NetworkManager.Singleton.DisconnectClient(player.Item2);
		}

		List<(string, ulong)> latePlayers = GetPlayersLateOnPing(lackOfPingAnswerTimeout);
		if (latePlayers.Count == 0) ResumeGameClientRpc();
		else {
			List<string> nameslist = new List<string>();
			foreach ((string, ulong) player in latePlayers) {
				nameslist.Add(player.Item1);
			}
			string names = string.Join(NameSeparator, nameslist);
			PauseGameClientRpc(names);
		}
	}

	[ClientRpc]
	void PauseGameClientRpc(string names) {
		string displayString = names.Replace(NameSeparator, "\n");
		waitingForPlayersTxt.text = "PLAYER(S) TRYING TO RECONNECT:\n" + displayString;
		if (GameStateManager.CurrGameState == GameState.InPlay) {
			ChangeReconnectionState(true);
		}
	}

	#endregion


}