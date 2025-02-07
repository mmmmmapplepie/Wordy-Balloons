using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class MyLobby : NetworkBehaviour {
	public static MyLobby Instance;

	#region Events
	public static event Action LobbyCreatedEvent, LobbyLeft, LobbyDeleted, LobbyJoined, LobbyJoinFail;

	#endregion

	void Awake() {
		Instance = this;
		SetupColoredLists();
	}

	void Start() {
		//creating lobby
		LobbyManager.CreatedLobby += LobbyCreated;
		LobbyNetcodeManager.ServerStartSuccess += ServerStartedSuccess;
		LobbyNetcodeManager.ServerStartFail += ServerStartedFail;

		//joining lobby
		LobbyManager.JoinedLobby += JoinedLobby;
		LobbyManager.PlayerJoinedLobby += JoinedLobbyTemp;
		LobbyNetcodeManager.ClientStartSuccess += ClientStartedSuccess;
		LobbyNetcodeManager.ClientConnected += ClientConnected;

		//leaving lobby
		LobbyManager.LeaveLobbyComplete += LeaveLobby;
		LobbyManager.KickedFromLobbyEvent += LeaveLobby;
		LobbyNetcodeManager.ServerStoppedEvent += ServerStopped;
		LobbyNetcodeManager.ClientDisconnected += ClientDisconnected;
		LobbyNetcodeManager.ClientStoppedEvent += ClientStopped;

		LobbyPlayer.TeamChangeEvent += ChangeTeam;
		TeamBox.TeamChangeEvent += ChangeTeam;
	}

	public override void OnDestroy() {
		OnNetworkDespawn();
		//creating lobby
		LobbyManager.CreatedLobby -= LobbyCreated;
		LobbyNetcodeManager.ServerStartSuccess -= ServerStartedSuccess;

		//joining lobby
		LobbyManager.JoinedLobby -= JoinedLobby;
		LobbyNetcodeManager.ClientStartSuccess -= ClientStartedSuccess;
		LobbyNetcodeManager.ClientConnected -= ClientConnected;

		//leaving lobby
		LobbyManager.LeaveLobbyComplete -= LeaveLobby;
		LobbyManager.KickedFromLobbyEvent -= LeaveLobby;
		LobbyNetcodeManager.ServerStoppedEvent -= ServerStopped;
		LobbyNetcodeManager.ClientDisconnected -= ClientDisconnected;
		LobbyNetcodeManager.ClientStoppedEvent -= ClientStopped;

		LobbyPlayer.TeamChangeEvent -= ChangeTeam;
		TeamBox.TeamChangeEvent -= ChangeTeam;
		base.OnDestroy();
	}

	#region LobbyVariables

	Dictionary<ulong, int> ClientID_KEY_ColorIndex_VAL = new Dictionary<ulong, int>();
	Dictionary<ulong, string> ClientID_KEY_LobbyID_VAL = new Dictionary<ulong, string>();
	HashSet<ulong> team1 = new HashSet<ulong>(), team2 = new HashSet<ulong>();
	List<LobbyPlayer> playersObjects = new List<LobbyPlayer>();
	int team1Max = 0; int team2Max = 0;

	#endregion

	#region  LobbyCreation
	void LobbyCreated() {
		Debug.LogWarning("Lobby Created");
		LobbyNetcodeManager.Instance.StartHost();
	}
	void ServerStartedFail() {
		Debug.LogWarning("Server start fail");
		LobbyManager.Instance.LeaveLobby();
	}
	void ServerStartedSuccess() {
		Debug.LogWarning("Server started");
		LobbyCreatedEvent?.Invoke();
		InitializeNewLobby();
		LobbyManager.Instance.MakeLobbyPublic();
	}

	void InitializeNewLobby() {
		ResetLobbyVariables();
		ulong clientID = NetworkManager.Singleton.LocalClientId;
		LobbyPlayer p = AddPlayerToLobby(clientID);
		p.ConfirmJoin(LobbyManager.playerName);
	}

	void ResetLobbyVariables() {
		ClientID_KEY_ColorIndex_VAL.Clear();
		ClientID_KEY_LobbyID_VAL.Clear();
		team1.Clear();
		team2.Clear();
		playersObjects.Clear();
		int slots = LobbyManager.Instance.hostLobby.MaxPlayers;
		team1Max = Mathf.CeilToInt(slots / 2f);
		team2Max = slots - team1Max;
		StopAllCoroutines();
		tempJoinedList.Clear();
	}
	#endregion




	#region  LobbyJoining
	void JoinedLobby() {
		Debug.LogWarning("Lobby joined");
		LobbyJoined?.Invoke();
		LobbyNetcodeManager.Instance.StartClient();
	}
	List<(string id, Coroutine timeout)> tempJoinedList = new List<(string id, Coroutine timeout)>();
	void JoinedLobbyTemp(string id) {
		if (!NetworkManager.Singleton.IsServer) return;
		StopTimeout(id);
		Coroutine newJoinTimeout = StartCoroutine(nameof(JoinConfirmationTimeout), id);
		tempJoinedList.Add((id, newJoinTimeout));
	}
	void StopTimeout(string id) {
		int inx = tempJoinedList.FindIndex(x => x.id == id);
		if (inx != -1) {
			StopCoroutine(tempJoinedList[inx].timeout);
			tempJoinedList.RemoveAt(inx);
		}
	}
	//need to add part where i cancel if the player already left the lobby or smthing.
	const float JoinConfirmationTimeoutTime = 10f;
	IEnumerator JoinConfirmationTimeout(string id) {
		yield return new WaitForSeconds(JoinConfirmationTimeoutTime);
		KickDueToTimeout(id);
	}
	void KickDueToTimeout(string id) {
		LobbyManager.Instance.KickFromLobby(id);
		int inx = tempJoinedList.FindIndex(x => x.id == id);
		if (inx != -1) {
			tempJoinedList.RemoveAt(inx);
		}
	}
	void ClientStartedSuccess() {
		Debug.LogWarning("Client started");
		//check if in lobby: if not then yeet self out
		if (LobbyManager.Instance.joinedLobby == null) { LeaveLobby(); return; }
		SendLobbyJoinConfirmationServerRPC(AuthenticationService.Instance.PlayerId, NetworkManager.Singleton.LocalClientId, LobbyManager.playerName);
	}

	[ServerRpc(RequireOwnership = false)]
	void SendLobbyJoinConfirmationServerRPC(string lobbyID, ulong clientID, string playerName) {
		StopTimeout(lobbyID);
		if (!ClientID_KEY_LobbyID_VAL.ContainsKey(clientID)) ClientID_KEY_LobbyID_VAL.Add(clientID, lobbyID);
		LobbyPlayer playerObj = FindPlayerFromClientID(clientID);
		if (playerObj != null) playerObj.ConfirmJoin(playerName);
	}

	void ClientConnected(ulong id) {
		if (!NetworkManager.Singleton.IsServer) return;
		if (id == NetworkManager.Singleton.LocalClientId) return;
		AddPlayerToLobby(id);
		Debug.LogWarning(id.ToString() + " : client started. Total connected: " + NetworkManager.Singleton.ConnectedClients.Count);
	}
	#endregion


	#region  lobbyLeaving
	void LeaveLobby() {
		LobbyNetcodeManager.Instance.ShutDownNetwork();
		if (LobbyManager.Instance.joinedLobby != null) LobbyManager.Instance.LeaveLobby();
	}
	void CheckPlayersInLobby(List<Player> remaninigPlayers) {
		//dont really matter as clientdisconnected will be called.
	}
	void ServerStopped() {
		LeaveLobby();
	}
	void ClientDisconnected(ulong id) {
		if (!NetworkManager.Singleton.IsServer) return;
		string lobbyID = null;
		if (ClientID_KEY_LobbyID_VAL.ContainsKey(id)) lobbyID = ClientID_KEY_LobbyID_VAL[id];
		ClientID_KEY_LobbyID_VAL.Remove(id);
		RemovePlayer(id);

		if (lobbyID != null) LobbyManager.Instance.KickFromLobby(lobbyID);
	}
	void ClientStopped(bool b) {
	}

	#endregion


	#region Team and Player object
	[Header("Team")]
	[SerializeField] Transform team1Holder, team2Holder, lobbyPlayerPrefab;

	Team AddNewPlayerToTeam(ulong clientID) {
		Team freeTeam = GetTeamToPlaceIn();
		if (freeTeam == Team.t1 && !team1.Contains(clientID)) {
			team1.Add(clientID);
		} else if (!team2.Contains(clientID)) {
			team2.Add(clientID);
		}
		return freeTeam;
	}

	Team GetTeamToPlaceIn() {
		return team1.Count < team1Max ? Team.t1 : Team.t2;
	}

	LobbyPlayer AddPlayerToLobby(ulong clientID) {
		Team team = AddNewPlayerToTeam(clientID);
		LobbyPlayer player = CreatePlayerObject(clientID, team);
		int colorIndex = GetFirstAvailableColor();
		ClientID_KEY_ColorIndex_VAL.Add(clientID, colorIndex);
		player.SetupPlayer(clientID, colorIndex);
		return player;
	}

	LobbyPlayer CreatePlayerObject(ulong clientID, Team team) {
		Transform targetHolder = team == Team.t1 ? team1Holder : team2Holder;
		Transform newP = Instantiate(lobbyPlayerPrefab, targetHolder);
		LobbyPlayer script = newP.GetComponent<LobbyPlayer>();
		// script.clientID.Value = clientID;
		newP.GetComponent<NetworkObject>().Spawn(true);
		newP.GetComponent<NetworkObject>().TrySetParent(targetHolder);
		playersObjects.Add(script);
		return script;
	}

	LobbyPlayer FindPlayerFromClientID(ulong clientID) {
		foreach (LobbyPlayer p in playersObjects) {
			if (p.clientID.Value == clientID) return p;
		}
		return null;
	}

	void RemovePlayer(ulong id) {
		LobbyPlayer p = FindPlayerFromClientID(id);
		RemovePlayerObject(p);
		team1.Remove(id);
		team2.Remove(id);
		ClientID_KEY_ColorIndex_VAL.Remove(id);
	}

	void RemovePlayerObject(LobbyPlayer obj) {
		playersObjects.Remove(obj);
		obj.GetComponent<NetworkObject>().Despawn(true);
	}

	void ChangeTeam(Transform targetT, LobbyPlayer dragPlayer, LobbyPlayer swapPlayer) {
		print(swapPlayer);
		if (!CanStopSceneLoading) return;
		if (swapPlayer == null) {
			if (targetT == team1Holder) {
				print("T1");
				if (team1.Contains(dragPlayer.clientID.Value) || team1Holder.childCount >= team1Max) return;
				team2.Remove(dragPlayer.clientID.Value);
				team1.Add(dragPlayer.clientID.Value);
				dragPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(team1Holder);
				dragPlayer.transform.SetAsLastSibling();
			}
			if (targetT == team2Holder) {
				print("T2");
				if (team2.Contains(dragPlayer.clientID.Value) || team2Holder.childCount >= team2Max) return;
				team1.Remove(dragPlayer.clientID.Value);
				team2.Add(dragPlayer.clientID.Value);
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

		if (dragObjHolder == team1Holder) { team1.Remove(dragPlayer.clientID.Value); team1.Add(swapPlayer.clientID.Value); }
		if (dragObjHolder == team2Holder) { team2.Remove(dragPlayer.clientID.Value); team2.Add(swapPlayer.clientID.Value); };
		if (swapObjHolder == team1Holder) { team1.Remove(swapPlayer.clientID.Value); team1.Add(dragPlayer.clientID.Value); };
		if (swapObjHolder == team2Holder) { team2.Remove(swapPlayer.clientID.Value); team2.Add(dragPlayer.clientID.Value); };

		dragPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(swapObjHolder);
		swapPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(dragObjHolder);
		dragPlayer.siblingNum.Value = swapObjOrder;
		swapPlayer.siblingNum.Value = dragObjOrder;
	}

	#endregion


	#region Color
	[Header("ColorSettings")]
	public List<Color> _allColorOptions;
	public static List<Color> allColorOptions;
	public static List<Sprite> allColorOptionSprites = new List<Sprite>();
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
	int GetFirstAvailableColor() {
		for (int i = 0; i < _allColorOptions.Count; i++) {
			if (!ClientID_KEY_ColorIndex_VAL.ContainsValue(i)) {
				return i;
			}
		}
		return -1;
	}
	public void RequestColorChange(int colorIndex) {
		RequestColorChangeServerRpc(colorIndex);
	}

	[ServerRpc(RequireOwnership = false)]
	void RequestColorChangeServerRpc(int targetIndex, ServerRpcParams option = default) {
		ulong senderID = option.Receive.SenderClientId;
		//set the color regardless. just set it to the original if new one is not available.
		int prevColorIndx = ClientID_KEY_ColorIndex_VAL[senderID];
		if (prevColorIndx == targetIndex) return;

		if (ClientID_KEY_ColorIndex_VAL.ContainsValue(targetIndex) || !CanStopSceneLoading) {
			targetIndex = prevColorIndx;
		}
		LobbyPlayer targetPlayerObj = playersObjects.Find(x => x.clientID.Value == senderID);
		if (targetPlayerObj == null) return;

		ClientID_KEY_ColorIndex_VAL[senderID] = targetIndex;

		targetPlayerObj.SetColor(targetIndex);
	}

	#endregion


	#region Entering game
	public static event Action StartSceneLoading;
	public static event Action<bool> LockOnLoading;
	//for start: turn on loading panel, disable leave, disable changing teams/colors
	//for stop: inverse of all the above.
	bool CheckIfPlayersFilled() {
		return team1Max + team2Max == team1.Count + team2.Count;
	}
	Coroutine loadNextScene = null;
	public void EnterGame() {
		if (loadNextScene != null) StopCoroutine(loadNextScene);
		loadNextScene = StartCoroutine(LoadNextScene());
	}
	public static bool CanStopSceneLoading = true;
	IEnumerator LoadNextScene() {
		SendTeamLists();
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

		if (teamDataRpcSent > 0) {
			LockOnSceneClientRpc(false);
			//show warning that connection was too slow or smthing (team data couldn't be passed around)
			yield break;
		}
		//set up team references accessible for clients as well.

		//have a check if all have laoded or soemthing idk.
		SceneEventProgressStatus sceneStatus = NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
		if (sceneStatus != SceneEventProgressStatus.Started) {
			LockOnSceneClientRpc(false);
		} else {
			//change data
			GameData.InSinglePlayerMode = false;
			GameData.allColorOptions = allColorOptions;
			GameData.ClientID_KEY_ColorIndex_VAL = ClientID_KEY_ColorIndex_VAL;
			// GameData.LobbyID_KEY_ClientID_VAL = LobbyID_KEY_ClientID_VAL;
			// GameData.team1 = team1; GameData.team2 = team2;
		}
	}
	const string splitter = "/";
	int teamDataRpcSent = 0;
	void SendTeamLists() {
		teamDataRpcSent = (team1.Count + team2.Count) * 2;
		string team1List = "", team2List = "";
		// foreach (string lobbyID in team1) {
		// 	team1List += LobbyID_KEY_ClientID_VAL[lobbyID] + splitter;
		// }
		// foreach (string lobbyID in team2) {
		// 	team2List += LobbyID_KEY_ClientID_VAL[lobbyID] + splitter;
		// }
		team1List.Remove(team1List.Length - 1);
		team2List.Remove(team2List.Length - 1);
		SendTeamListClientRpc(team1List, 1);
		SendTeamListClientRpc(team2List, 2);
	}
	[ClientRpc]
	void SendTeamListClientRpc(string teamIDs, int associatedTeam) {
		string[] idList = teamIDs.Split(splitter);
		List<ulong> targetList = associatedTeam == 1 ? GameData.team1IDList : GameData.team2IDList;
		targetList.Clear();
		foreach (string id in idList) {
			if (ulong.TryParse(id, out ulong idNum)) {
				targetList.Add(idNum);
			}
		}
		TeamUpdatedServerRpc(NetworkManager.Singleton.LocalClientId);
	}
	[ServerRpc(RequireOwnership = false)]
	void TeamUpdatedServerRpc(ulong id) {
		teamDataRpcSent--;
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


}
