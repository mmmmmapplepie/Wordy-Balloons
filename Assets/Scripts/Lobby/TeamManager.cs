using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour {

	// #region BasicMiscellaneous
	// [SerializeField] GameObject lobbyListItemPrefab, lobbyPlayerPrefab, loadingPanel;
	// [SerializeField] Transform lobbyListHolder, team1ListHolder, team2ListHolder;
	// bool loading = false;

	// void TurnOnLoadingPanel(bool load) {
	// 	loading = load;
	// 	loadingPanel.SetActive(load);
	// }
	// #endregion







	// Dictionary<ulong, string> ClientID_Key_LobbyID_Val = new Dictionary<ulong, string>();
	// HashSet<string> team1 = new HashSet<string>(), team2 = new HashSet<string>();

	// void Start() {
	// 	TeamBox.teamChange += ChangeTeam;
	// }
	// void OnDisable() {
	// 	TeamBox.teamChange -= ChangeTeam;
	// }





	// void AskForPlayerData(ulong clientID) {
	// 	ClientRpcParams option = new ClientRpcParams {
	// 		Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientID } }
	// 	};
	// 	AskForPlayerDataClientRPC(option);
	// }

	// [ClientRpc]
	// void AskForPlayerDataClientRPC(ClientRpcParams option = default) {
	// 	SendPlayerDataServerRPC(AuthenticationService.Instance.PlayerId, NetworkManager.Singleton.LocalClientId, playerName);
	// }

	// [ServerRpc(RequireOwnership = false)]
	// void SendPlayerDataServerRPC(string lobbyID, ulong clientID, string playerName) {
	// 	if (ClientID_Key_LobbyID_Val.ContainsKey(clientID)) {
	// 		ClientID_Key_LobbyID_Val[clientID] = lobbyID;
	// 	} else {
	// 		ClientID_Key_LobbyID_Val.Add(clientID, lobbyID);
	// 	}
	// 	PlayerData playerData = new PlayerData(clientID, lobbyID, playerName);
	// 	AddPlayerToLobby(playerData);
	// 	PlayerAddedToLobbyClientRpc();
	// }
	// List<LobbyPlayer> playersInLobbyObjectList = new List<LobbyPlayer>();
	// void AddPlayerToLobby(PlayerData playerData) {
	// 	//assign team
	// 	string LobbyID = playerData.LobbyID;
	// 	int teamMax = Mathf.CeilToInt(lobbyMaxPlayerNumber / 2);
	// 	int team;
	// 	if (team1.Contains(LobbyID) || team2.Contains(LobbyID)) return;
	// 	if (team1.Count < teamMax) {
	// 		team1.Add(LobbyID);
	// 		team = 1;
	// 	} else {
	// 		team2.Add(LobbyID);
	// 		team = 2;
	// 	}
	// 	Transform targetHolder = team == 1 ? team1ListHolder : team2ListHolder;
	// 	GameObject newPlayer = Instantiate(lobbyPlayerPrefab, targetHolder);
	// 	LobbyPlayer playerObj = newPlayer.GetComponent<LobbyPlayer>();
	// 	playerObj.SetupPlayer(playerData, this);
	// 	playersInLobbyObjectList.Add(playerObj);
	// 	newPlayer.GetComponent<NetworkObject>().Spawn();
	// 	newPlayer.GetComponent<NetworkObject>().TrySetParent(targetHolder);
	// }

	// [ClientRpc]
	// void PlayerAddedToLobbyClientRpc() {
	// 	MakeLobbyPublic();
	// }

	// void ClientDisconnect(ulong clientId) {
	// 	if (!ClientID_Key_LobbyID_Val.ContainsKey(clientId)) {
	// 		print("No Key");
	// 		return;
	// 	}
	// 	string lobbyID = ClientID_Key_LobbyID_Val[clientId];
	// 	team1.Remove(lobbyID);
	// 	team2.Remove(lobbyID);
	// 	for (int i = 0; i < playersInLobbyObjectList.Count - 1; i++) {
	// 		if (playersInLobbyObjectList[i].lobbyID == lobbyID) {
	// 			Destroy(playersInLobbyObjectList[i].gameObject);
	// 			break;
	// 		}
	// 	}
	// 	ClientID_Key_LobbyID_Val.Remove(clientId);
	// 	Player lobbyPlayer = joinedLobby.Players.Find(x => x.Id == lobbyID);
	// 	if (lobbyPlayer != null) {
	// 		LeaveLobby(lobbyPlayer.Id);
	// 	}
	// 	//removefrom lobby -- maybe just do this separately?
	// }

	// async void MakeLobbyPublic() {
	// 	OpenLobbyPanel(hostLobby != null);
	// 	if (hostLobby == null) {
	// 		TurnOnLoadingPanel(false);
	// 		return;
	// 	}
	// 	await UpdateLobbyToPublic();
	// 	TurnOnLoadingPanel(false);
	// }
	// public async Task UpdateLobbyToPublic() {
	// 	try {
	// 		hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
	// 			IsPrivate = false
	// 		});
	// 		joinedLobby = hostLobby;
	// 	} catch (LobbyServiceException e) {
	// 		print(e);
	// 	}
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









	// NetworkManager.Singleton.OnClientConnectedCallback += AskForPlayerData;
	// 		NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnect;
	// 		NetworkManager.Singleton.StartHost();



	// 	NetworkManager.Singleton.StartClient();
	// 	NetworkManager.Singleton.Shutdown();

	// if (NetworkManager.Singleton.IsServer) {
	// 				NetworkManager.Singleton.DisconnectClient(ClientID_Key_LobbyID_Val[id]);
	// 				ClientID_Key_LobbyID_Val.Remove(id);
	// 			}



	// if (hostLobby != null && id == authenticationID) {
	// 	DeleteLobby(true);
	// 	return;
	// }

	void ShutDownNetwork() {
		if (!NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}
}
