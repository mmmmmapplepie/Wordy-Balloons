using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;

public class NetcodeManager : NetworkBehaviour {
	[SerializeField] Transform team1Holder, team2Holder, lobbyPlayerPrefab;
	[SerializeField] MyLobby lobbyScript;


	public List<Color> colorOptions;
	NetworkList<Color> colorsBeingUsed;
	Dictionary<ulong, Color> ClientID_KEY_Color_VAL = new Dictionary<ulong, Color>();
	Dictionary<string, ulong> LobbyID_KEY_ClientID_VAL = new Dictionary<string, ulong>();
	HashSet<string> team1 = new HashSet<string>(), team2 = new HashSet<string>();
	List<LobbyPlayer> playersObjects = new List<LobbyPlayer>();
	int team1Max = 0; int team2Max = 0;

	void Awake() {
		colorsBeingUsed = new NetworkList<Color>();
	}

	public override void OnNetworkSpawn() {
		NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedToNGO;
		NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectedFromNGO;
	}

	void Start() {
		//events from lobby
		MyLobby.Instance.LobbyCreated += JoinAsHost;
		MyLobby.Instance.LobbyJoined += PlayerJoinedLobby;
		MyLobby.Instance.JoinLobbyNetcode += JoinAsClient;
		MyLobby.Instance.LeaveLobbyBegin += ShutDownNetwork;
		MyLobby.Instance.PlayersLeft += PlayersLeft;
	}
	public override void OnDestroy() {
		//events from lobby
		if (MyLobby.Instance != null) {
			MyLobby.Instance.LobbyCreated -= JoinAsHost;
			MyLobby.Instance.LobbyJoined -= PlayerJoinedLobby;
			MyLobby.Instance.JoinLobbyNetcode -= JoinAsClient;
			MyLobby.Instance.LeaveLobbyBegin -= ShutDownNetwork;
			MyLobby.Instance.PlayersLeft -= PlayersLeft;
		}
		base.OnDestroy();
	}


	#region connecting as host
	Coroutine startingHost;
	void JoinAsHost() {
		if (startingHost != null) StopCoroutine(startingHost);
		NetworkManager.Singleton.StartHost();
		HostActive();
	}
	void HostActive() {
		ResetVariables();

		//team
		int team = AddNewPlayerToTeam(MyLobby.Instance.authenticationID);
		LobbyID_KEY_ClientID_VAL[MyLobby.Instance.authenticationID] = NetworkManager.Singleton.LocalClientId;

		//actual object
		LobbyPlayer player = CreatePlayerObject(MyLobby.Instance.authenticationID, team);

		//color
		Color c = GetFirstAvailableColor();
		colorsBeingUsed.Add(c);
		ClientID_KEY_Color_VAL.Add(NetworkManager.Singleton.LocalClientId, c);

		//playerdata
		PlayerData data = new PlayerData(NetworkManager.LocalClientId, MyLobby.Instance.authenticationID, MyLobby.Instance.playerName, c);
		player.SetupPlayer(data);

		//make lobby public
		MyLobby.Instance.MakeLobbyPublic();
	}
	void ResetVariables() {
		colorsBeingUsed.Clear();
		ClientID_KEY_Color_VAL.Clear();
		team1.Clear();
		team2.Clear();
		LobbyID_KEY_ClientID_VAL.Clear();
		playersObjects.Clear();

		int slots = MyLobby.Instance.hostLobby.MaxPlayers;
		team1Max = Mathf.CeilToInt(slots / 2f);
		team2Max = slots - team1Max;
	}



	//place in team one first if available and then into team 2.
	int GetTeamToPlaceIn() {
		return team1.Count < team1Max ? 1 : 2;
	}

	int AddNewPlayerToTeam(string lobbyID) {
		int teamInt = GetTeamToPlaceIn();
		if (teamInt == 1 && !team1.Contains(lobbyID)) {
			team1.Add(lobbyID);
		} else if (!team2.Contains(lobbyID)) {
			team2.Add(lobbyID);
		}

		if (LobbyID_KEY_ClientID_VAL.ContainsKey(lobbyID)) {
			LobbyID_KEY_ClientID_VAL[lobbyID] = ulong.MaxValue;
		} else {
			LobbyID_KEY_ClientID_VAL.Add(lobbyID, ulong.MaxValue);
		}
		return teamInt;
	}

	LobbyPlayer CreatePlayerObject(string lobbyID, int team) {
		Transform targetHolder = team == 1 ? team1Holder : team2Holder;
		Transform newP = Instantiate(lobbyPlayerPrefab, targetHolder);
		LobbyPlayer script = newP.GetComponent<LobbyPlayer>();
		script.lobbyID = lobbyID;
		newP.GetComponent<NetworkObject>().Spawn(true);
		newP.GetComponent<NetworkObject>().TrySetParent(targetHolder);
		playersObjects.Add(script);
		return script;
	}

	Color GetFirstAvailableColor() {
		foreach (Color c in colorOptions) {
			if (!colorsBeingUsed.Contains(c)) {
				return c;
			}
		}
		return default;
	}
	#endregion




















	#region joining as clinet
	void JoinAsClient() {
		// NetworkManager.Singleton.StartClient();
	}
	void PlayerJoinedLobby(string newJoinedLobbyID) {
		if (NetworkManager.Singleton.IsServer) {
			int team = AddNewPlayerToTeam(newJoinedLobbyID);
			CreatePlayerObject(newJoinedLobbyID, team);
		}
	}

	void ClientConnectedToNGO(ulong clientID) {
		//non server can't all this
		if (!NetworkManager.Singleton.IsServer) return;
		//server shoudln't call for itself
		if (NetworkManager.Singleton.LocalClientId == clientID) return;
		ClientRpcParams option = new ClientRpcParams {
			Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientID } }
		};
		AskForPlayerDataClientRPC(option);
	}

	[ClientRpc]
	void AskForPlayerDataClientRPC(ClientRpcParams option = default) {
		SendPlayerDataServerRPC(AuthenticationService.Instance.PlayerId, NetworkManager.Singleton.LocalClientId, MyLobby.Instance.playerName);
		MyLobby.Instance.JoinLobbySuccessful(true);
	}

	[ServerRpc(RequireOwnership = false)]
	void SendPlayerDataServerRPC(string lobbyID, ulong clientID, string playerName) {
		Color c = GetFirstAvailableColor();
		colorsBeingUsed.Add(c);
		if (ClientID_KEY_Color_VAL.ContainsKey(clientID)) {
			ClientID_KEY_Color_VAL[clientID] = c;
		} else {
			ClientID_KEY_Color_VAL.Add(clientID, c);
		}

		PlayerData data = new PlayerData();
		data.Name = playerName;
		data.LobbyID = lobbyID;
		data.ClientID = clientID;
		data.Color = c;

		if (LobbyID_KEY_ClientID_VAL.ContainsKey(lobbyID)) {
			LobbyID_KEY_ClientID_VAL[lobbyID] = clientID;
		} else {
			LobbyID_KEY_ClientID_VAL.Add(lobbyID, clientID);
		}

		LobbyPlayer playerObj = FindPlayerFromLobbyID(lobbyID);
		if (playerObj != null) {
			playerObj.SetupPlayer(data);
		} else {
			MyLobby.Instance.KickFromLobby(lobbyID);
		}
	}

	LobbyPlayer FindPlayerFromLobbyID(string lobbyID) {
		foreach (LobbyPlayer p in playersObjects) {
			if (p.lobbyID == lobbyID) return p;
		}
		return null;
	}
	#endregion



	void ClientDisconnectedFromNGO(ulong clientID) {
		if (!NetworkManager.Singleton.IsServer) return;
		foreach (KeyValuePair<string, ulong> pair in LobbyID_KEY_ClientID_VAL) {
			if (pair.Value == clientID) {
				MyLobby.Instance.KickFromLobby(pair.Key);
				return;
			}
		}
	}




	void PlayersLeft(List<Player> currentPlayersInLobby) {
		List<string> PlayersToRemove = new List<string>();
		foreach (KeyValuePair<string, ulong> pair in LobbyID_KEY_ClientID_VAL) {
			PlayersToRemove.Add(pair.Key);
		}
		foreach (Player p in currentPlayersInLobby) {
			if (PlayersToRemove.Contains(p.Id)) PlayersToRemove.Remove(p.Id);
		}
		foreach (string id in PlayersToRemove) {
			ulong clientID = LobbyID_KEY_ClientID_VAL[id];
			LobbyID_KEY_ClientID_VAL.Remove(id);
			team1.Remove(id);
			team2.Remove(id);
			LobbyPlayer playerObj = playersObjects.Find(x => x.lobbyID == id);
			if (playerObj != null) {
				playerObj.GetComponent<NetworkObject>().Despawn(true);
			}
			NetworkManager.DisconnectClient(clientID);
		}
	}





	#region changingTeams

	// void Start() {
	// 	TeamBox.teamChange += ChangeTeam;
	// }
	// void OnDisable() {
	// 	TeamBox.teamChange -= ChangeTeam;
	// }

	// void ChangeTeam(int newTeam, LobbyPlayer player) {
	// 	if (newTeam == 1) {
	// 		if (team1.Contains(player.lobbyID)) return;
	// 		team2.Remove(player.lobbyID);
	// 		team1.Add(player.lobbyID);
	// 		player.gameObject.GetComponent<NetworkObject>().TrySetParent(team1ListHolder);
	// 		player.transform.SetAsLastSibling();
	// 	}
	// 	if (newTeam == 2) {
	// 		if (team2.Contains(player.lobbyID)) return;
	// 		team1.Remove(player.lobbyID);
	// 		team2.Add(player.lobbyID);
	// 		player.gameObject.GetComponent<NetworkObject>().TrySetParent(team2ListHolder);
	// 		player.transform.SetAsLastSibling();
	// 	}
	// }
	#endregion



	#region changingColor



	#endregion





















	void ShutDownNetwork() {
		if (!NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}

}
