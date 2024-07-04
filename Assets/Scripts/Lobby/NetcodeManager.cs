using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetcodeManager : NetworkBehaviour {
	[SerializeField] Transform team1Holder, team2Holder, lobbyPlayerPrefab;

	public List<Color> _allColorOptions;
	public static List<Color> allColorOptions;
	public static List<Sprite> allColorOptionSprites = new List<Sprite>();
	Dictionary<ulong, int> ClientID_KEY_ColorIndex_VAL = new Dictionary<ulong, int>();

	Dictionary<string, ulong> LobbyID_KEY_ClientID_VAL = new Dictionary<string, ulong>();
	HashSet<string> team1 = new HashSet<string>(), team2 = new HashSet<string>();
	List<LobbyPlayer> playersObjects = new List<LobbyPlayer>();
	int team1Max = 0; int team2Max = 0;
	public static NetcodeManager Instance;
	void Awake() {
		Instance = this;
		SetupColoredLists();

		CanStopSceneLoading = true;
	}
	void SetupColoredLists() {
		allColorOptions = _allColorOptions;
		foreach (Color c in allColorOptions) {
			allColorOptionSprites.Add(CreateColoredSprite(c));
		}
	}

	Sprite CreateColoredSprite(Color c) {
		Texture2D onePixel = new Texture2D(1, 1);
		onePixel.filterMode = FilterMode.Point;
		onePixel.SetPixel(0, 0, c);
		Sprite s = Sprite.Create(onePixel, new Rect(0f, 0f, onePixel.width, onePixel.height), Vector2.zero);
		onePixel.Apply();
		s.name = c.ToString();
		return s;
	}

	public override void OnNetworkSpawn() {
		NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedToNGO;

		//a client that is disconnecting also gets this callback (as if the still connected are disconnecting...)
		NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectedFromNGO;
		NetworkManager.Singleton.OnClientDisconnectCallback += StopLoadingLevel;
		NetworkManager.Singleton.OnClientStopped += ClientStopped;

	}



	public override void OnNetworkDespawn() {
		if (NetworkManager.Singleton != null) {
			NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnectedToNGO;
			NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnectedFromNGO;
			NetworkManager.Singleton.OnClientDisconnectCallback -= StopLoadingLevel;
			NetworkManager.Singleton.OnClientStopped -= ClientStopped;
		}
		if (MyLobby.Instance != null) MyLobby.Instance.KickedFromLobby();
	}

	void Start() {
		//events from lobby
		MyLobby.LobbyCreated += JoinAsHost;
		MyLobby.PlayerJoinedLobby += PlayerJoinedLobby;
		MyLobby.JoinLobbyNetcode += JoinAsClient;
		MyLobby.LeaveLobbyComplete += LeftLobby;
		MyLobby.PlayersLeft += PlayersLeft;
		MyLobby.PlayersLeft += StopLoadingLevel;


		TeamBox.TeamChangeEvent += ChangeTeam;
		LobbyUI.GoingToMainMenu += ShutDownNetwork;
		LobbyPlayer.TeamChangeEvent += ChangeTeam;
	}
	public override void OnDestroy() {
		OnNetworkDespawn();
		//events from lobby
		MyLobby.LobbyCreated -= JoinAsHost;
		MyLobby.PlayerJoinedLobby -= PlayerJoinedLobby;
		MyLobby.JoinLobbyNetcode -= JoinAsClient;
		MyLobby.LeaveLobbyComplete -= LeftLobby;
		MyLobby.PlayersLeft -= PlayersLeft;
		MyLobby.PlayersLeft -= StopLoadingLevel;


		TeamBox.TeamChangeEvent -= ChangeTeam;
		LobbyUI.GoingToMainMenu -= ShutDownNetwork;
		LobbyPlayer.TeamChangeEvent -= ChangeTeam;


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
		int colorIndex = GetFirstAvailableColor();
		// usedColorIndex.Add(colorIndex);
		ClientID_KEY_ColorIndex_VAL.Add(NetworkManager.Singleton.LocalClientId, colorIndex);

		//playerdata
		PlayerData data = new PlayerData(NetworkManager.LocalClientId, MyLobby.Instance.authenticationID, MyLobby.playerName, colorIndex);
		player.SetupPlayer(data);

		//make lobby public
		MyLobby.Instance.MakeLobbyPublic();
	}
	void ResetVariables() {
		ClientID_KEY_ColorIndex_VAL.Clear();
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
		script.netScript = this;
		newP.GetComponent<NetworkObject>().Spawn(true);
		newP.GetComponent<NetworkObject>().TrySetParent(targetHolder);
		playersObjects.Add(script);
		return script;
	}

	int GetFirstAvailableColor() {
		for (int i = 0; i < _allColorOptions.Count; i++) {
			if (!ClientID_KEY_ColorIndex_VAL.ContainsValue(i)) {
				return i;
			}
		}
		return -1;
	}
	#endregion




















	#region joining as clinet
	void JoinAsClient() {
		NetworkManager.Singleton.StartClient();
	}
	void PlayerJoinedLobby(string newJoinedLobbyID) {
		if (NetworkManager.Singleton.IsServer) {
			int team = AddNewPlayerToTeam(newJoinedLobbyID);
			CreatePlayerObject(newJoinedLobbyID, team);
		}
	}

	void ClientConnectedToNGO(ulong clientID) {
		//non server can't call this
		if (!NetworkManager.Singleton.IsServer) return;
		//host shoudln't call for itself
		if (NetworkManager.Singleton.LocalClientId == clientID) return;
		ClientRpcParams option = new ClientRpcParams {
			Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientID } }
		};
		AskForPlayerDataClientRPC(option);
	}

	[ClientRpc]
	void AskForPlayerDataClientRPC(ClientRpcParams option = default) {
		SendPlayerDataServerRPC(AuthenticationService.Instance.PlayerId, NetworkManager.Singleton.LocalClientId, MyLobby.playerName);
	}

	[ServerRpc(RequireOwnership = false)]
	void SendPlayerDataServerRPC(string lobbyID, ulong clientID, string playerName) {
		LobbyPlayer playerObj = FindPlayerFromLobbyID(lobbyID);
		if (playerObj == null || !LobbyID_KEY_ClientID_VAL.ContainsKey(lobbyID)) {
			LobbyID_KEY_ClientID_VAL.Remove(lobbyID);
			if (playerObj != null) RemovePlayerObject(playerObj);
			MyLobby.Instance.KickFromLobby(lobbyID);
			if (clientID != ulong.MaxValue) NetworkManager.DisconnectClient(clientID);
			return;
		}

		int colorIndex = GetFirstAvailableColor();
		if (ClientID_KEY_ColorIndex_VAL.ContainsKey(clientID)) {
			ClientID_KEY_ColorIndex_VAL[clientID] = colorIndex;
		} else {
			ClientID_KEY_ColorIndex_VAL.Add(clientID, colorIndex);
		}

		PlayerData data = new PlayerData();
		data.Name = playerName;
		data.LobbyID = lobbyID;
		data.ClientID = clientID;
		data.ColorIndex = colorIndex;

		LobbyID_KEY_ClientID_VAL[lobbyID] = clientID;
		playerObj.SetupPlayer(data);

		ClientRpcParams option = new ClientRpcParams {
			Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientID } }
		};
		JoinConfirmedClientRpc(option);
		CheckLobbyFull();
	}
	[ClientRpc]
	void JoinConfirmedClientRpc(ClientRpcParams option = default) {
		MyLobby.Instance.JoinLobbySuccessful(true);
	}
	public static event Action LobbyFull;
	void CheckLobbyFull() {
		if (ClientID_KEY_ColorIndex_VAL.Count == MyLobby.Instance.hostLobby.MaxPlayers) LobbyFull?.Invoke();
	}

	LobbyPlayer FindPlayerFromLobbyID(string lobbyID) {
		foreach (LobbyPlayer p in playersObjects) {
			if (p.lobbyID == lobbyID) return p;
		}
		return null;
	}
	#endregion



	void ClientDisconnectedFromNGO(ulong clientID) {
		if (MyLobby.Instance.hostLobby == null) return;
		if (NetworkManager.Singleton.IsServer) {
			foreach (KeyValuePair<string, ulong> pair in LobbyID_KEY_ClientID_VAL) {
				if (pair.Value == clientID) {
					MyLobby.Instance.KickFromLobby(pair.Key);
					return;
				}
			}
		}
	}
	void ClientStopped(bool wasHost) {
		print("stopped");
		if (!wasHost) {
			MyLobby.Instance.KickedFromLobby();
		}
	}

	void LeftLobby() {
		ShutDownNetwork();
	}

	void PlayersLeft(List<Player> currentPlayersInLobby) {
		if (!CanStopSceneLoading) return;
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

			if (ClientID_KEY_ColorIndex_VAL.ContainsKey(clientID)) {
				ClientID_KEY_ColorIndex_VAL.Remove(clientID);
			}

			LobbyPlayer playerObj = playersObjects.Find(x => x.lobbyID == id);
			if (playerObj != null) {
				RemovePlayerObject(playerObj);
			}

			if (clientID != ulong.MaxValue) NetworkManager.DisconnectClient(clientID);
		}
	}

	void RemovePlayerObject(LobbyPlayer obj) {
		playersObjects.Remove(obj);
		obj.GetComponent<NetworkObject>().Despawn(true);
	}





	#region changingTeams

	void ChangeTeam(Transform targetT, LobbyPlayer dragPlayer, LobbyPlayer swapPlayer) {
		if (!CanStopSceneLoading) return;
		if (swapPlayer == null) {
			//can't add more players to the teams
			if ((targetT == team1Holder && team1Holder.childCount >= team1Max) || (targetT == team2Holder && team2Holder.childCount >= team2Max)) return;

			if (targetT == team1Holder) {
				if (team1.Contains(dragPlayer.lobbyID)) return;
				team2.Remove(dragPlayer.lobbyID);
				team1.Add(dragPlayer.lobbyID);
				dragPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(team1Holder);
				dragPlayer.transform.SetAsLastSibling();
			}
			if (targetT == team2Holder) {
				if (team2.Contains(dragPlayer.lobbyID)) return;
				team1.Remove(dragPlayer.lobbyID);
				team2.Add(dragPlayer.lobbyID);
				dragPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(team2Holder);
				dragPlayer.transform.SetAsLastSibling();
			}
			return;
		}


		//swap scenario
		Transform dragObjHolder = dragPlayer.transform.parent;
		int dragObjOrder = dragPlayer.transform.GetSiblingIndex();
		Transform swapObjHolder = swapPlayer.transform.parent;
		int swapObjOrder = swapPlayer.transform.GetSiblingIndex();


		//remove and add to corresponding teams
		if (dragObjHolder == team1Holder) { team1.Remove(dragPlayer.lobbyID); team1.Add(swapPlayer.lobbyID); }
		if (dragObjHolder == team2Holder) { team2.Remove(dragPlayer.lobbyID); team2.Add(swapPlayer.lobbyID); };
		if (swapObjHolder == team1Holder) { team1.Remove(swapPlayer.lobbyID); team1.Add(dragPlayer.lobbyID); };
		if (swapObjHolder == team2Holder) { team2.Remove(swapPlayer.lobbyID); team2.Add(dragPlayer.lobbyID); };


		//then move swap to drag position
		dragPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(swapObjHolder);
		swapPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(dragObjHolder);
		//might need to networkvariable that in the lobby player
		dragPlayer.siblingNum.Value = swapObjOrder;
		swapPlayer.siblingNum.Value = dragObjOrder;
	}
	#endregion



	#region changingColor
	public void RequestColorChange(int colorIndex) {
		RequestColorChangeServerRpc(colorIndex);
	}

	[ServerRpc(RequireOwnership = false)]
	void RequestColorChangeServerRpc(int targetIndex, ServerRpcParams option = default) {
		ulong senderID = option.Receive.SenderClientId;
		//set the color regardless. just set it to the original if new one is not available.
		int previousIndex = ClientID_KEY_ColorIndex_VAL[senderID];
		if (previousIndex == targetIndex) return;

		if (ClientID_KEY_ColorIndex_VAL.ContainsValue(targetIndex) || !CanStopSceneLoading) {
			targetIndex = previousIndex;
		}
		LobbyPlayer targetPlayerObj = playersObjects.Find(x => x.clientID.Value == senderID);
		if (targetPlayerObj == null) return;

		ClientID_KEY_ColorIndex_VAL[senderID] = targetIndex;

		targetPlayerObj.SetColor(targetIndex);
	}


	#endregion









	#region startNextScene
	public static event Action StartSceneLoading;
	public static event Action<bool> LockOnLoading;
	//for start: turn on loading panel, disable leave, disable changing teams/colors
	//for stop: inverse of all the above.
	bool CheckIfPlayersFilled() {
		return team1Max + team2Max == LobbyID_KEY_ClientID_VAL.Count;
	}
	Coroutine loadNextScene = null;
	public void EnterGame() {
		if (loadNextScene != null) StopCoroutine(loadNextScene);
		loadNextScene = StartCoroutine(LoadNextScene());
	}
	public static bool CanStopSceneLoading = true;
	IEnumerator LoadNextScene() {
		StartSceneLoading?.Invoke();
		int countDown = 5;
		while (countDown > 0) {
			countDown--;
			yield return new WaitForSeconds(1f);
		}
		LockOnSceneClientRpc(true);
		countDown = 3;
		while (countDown > 0) {
			countDown--;
			yield return new WaitForSeconds(1f);
		}
		//have a check if all have laoded or soemthing idk.
		SceneEventProgressStatus sceneStatus = NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
		if (sceneStatus != SceneEventProgressStatus.Started) {
			LockOnSceneClientRpc(false);
		} else {
			//change data
			GameData.allColorOptions = allColorOptions;
			GameData.ClientID_KEY_ColorIndex_VAL = ClientID_KEY_ColorIndex_VAL;
			GameData.LobbyID_KEY_ClientID_VAL = LobbyID_KEY_ClientID_VAL;
			GameData.team1 = team1; GameData.team2 = team2;
		}
	}
	[ClientRpc]
	void LockOnSceneClientRpc(bool lockOn) {
		CanStopSceneLoading = !lockOn;
		LockOnLoading?.Invoke(lockOn);
	}
	void StopLoadingLevel() {
		if (loadNextScene != null) StopCoroutine(loadNextScene);
		loadNextScene = null;
		CanStopSceneLoading = true;
		if (NetworkManager.Singleton.IsServer) LockOnSceneClientRpc(false);
	}
	void StopLoadingLevel(ulong i) {
		StopLoadingLevel();
	}
	void StopLoadingLevel(List<Player> p) {
		StopLoadingLevel();
	}









	#endregion











	void ShutDownNetwork() {
		if (!NetworkManager.Singleton.ShutdownInProgress) {
			CanStopSceneLoading = true;
			NetworkManager.Singleton.Shutdown();
		}
	}

}
