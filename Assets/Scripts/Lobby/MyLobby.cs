using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class MyLobby : NetworkBehaviour {
	public static MyLobby Instance;

	#region Events
	public static event Action LobbyCreatedEvent, LobbyLeft, LobbyDeleted, LobbyJoined, LobbyJoinFail;

	#endregion

	void Awake() {
		Instance = this;
		SetupColoredLists();
	}
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		LoadingSceneBool.Value = false;
		LoadingCountdown.Value = sceneLoadTimer;
	}

	void Start() {
		//creating lobby
		LobbyManager.AuthenticationSuccess += AuthenticationDone;
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
		LobbyManager.AuthenticationSuccess -= AuthenticationDone;
		LobbyManager.CreatedLobby -= LobbyCreated;
		LobbyNetcodeManager.ServerStartSuccess -= ServerStartedSuccess;
		LobbyNetcodeManager.ServerStartFail -= ServerStartedFail;

		//joining lobby
		LobbyManager.JoinedLobby -= JoinedLobby;
		LobbyManager.PlayerJoinedLobby -= JoinedLobbyTemp;
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
	// int team1Max = 0; int team2Max = 0;
	int teamMax = 3; int teamMin = 1;

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
		SetTeamMaxAndMin(slots);
		// team1Max = Mathf.CeilToInt(slots / 2f);
		// team2Max = slots - team1Max;
		StopAllCoroutines();
		tempJoinedList.Clear();
	}
	void SetTeamMaxAndMin(int maxSlots) {
		teamMax = maxSlots > 3 ? 3 : maxSlots - 1;
		teamMin = 1;
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

		if (LoadingSceneBool.Value == true) {
			//kick player
			LobbyManager.Instance.KickFromLobby(id);
			return;
		}

		StopTimeout(id);
		Coroutine newJoinTimeout = StartCoroutine(nameof(JoinConfirmationTimeout), id);
		tempJoinedList.Add((id, newJoinTimeout));
	}
	bool StopTimeout(string id) {
		int inx = tempJoinedList.FindIndex(x => x.id == id);
		if (inx != -1) {
			StopCoroutine(tempJoinedList[inx].timeout);
			tempJoinedList.RemoveAt(inx);
			return true;
		}
		return false;
	}
	const float JoinConfirmationTimeoutTime = 10f;
	IEnumerator JoinConfirmationTimeout(string id) {
		yield return new WaitForSeconds(JoinConfirmationTimeoutTime);
		print("Kicking " + id + " from timeout.");
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
		if (LobbyManager.Instance.joinedLobby == null) { LeaveLobby(); return; }
		SendLobbyJoinConfirmationServerRPC(AuthenticationService.Instance.PlayerId, NetworkManager.Singleton.LocalClientId, LobbyManager.playerName);
	}

	[ServerRpc(RequireOwnership = false)]
	void SendLobbyJoinConfirmationServerRPC(string lobbyID, ulong clientID, string playerName) {
		if (!StopTimeout(lobbyID)) return;//for disabling ppl joining once scene loading is started.
		if (!ClientID_KEY_LobbyID_VAL.ContainsKey(clientID)) ClientID_KEY_LobbyID_VAL.Add(clientID, lobbyID);
		LobbyPlayer playerObj = FindPlayerFromClientID(clientID);
		if (playerObj != null) playerObj.ConfirmJoin(playerName);
		CheckIfPlayersFilled();
	}

	void ClientConnected(ulong id) {
		if (!NetworkManager.Singleton.IsServer) return;
		if (id == NetworkManager.Singleton.LocalClientId) return;
		AddPlayerToLobby(id);
		// Debug.LogWarning(id.ToString() + " : client started. Total connected: " + NetworkManager.Singleton.ConnectedClients.Count);
	}
	#endregion


	#region  LobbyLeaving
	void LeaveLobby() {
		if (LobbyNetcodeManager.Instance == null) return;
		LobbyNetcodeManager.Instance.ShutDownNetwork();
		if (LobbyManager.Instance == null) return;
		if (LobbyManager.Instance.joinedLobby != null) LobbyManager.Instance.LeaveLobby();
	}
	void ServerStopped() {
		LeaveLobby();
	}
	void ClientDisconnected(ulong id) {
		if (!NetworkManager.Singleton.IsServer) return;
		StopSceneLoading();
		string lobbyID = null;
		if (ClientID_KEY_LobbyID_VAL.ContainsKey(id)) lobbyID = ClientID_KEY_LobbyID_VAL[id];
		ClientID_KEY_LobbyID_VAL.Remove(id);
		RemovePlayer(id);
		CheckIfPlayersFilled();

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
		return team1.Count < teamMax ? Team.t1 : Team.t2;
		// return team1.Count < team1Max ? Team.t1 : Team.t2;
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
		if (p != null) RemovePlayerObject(p);
		team1.Remove(id);
		team2.Remove(id);
		ClientID_KEY_ColorIndex_VAL.Remove(id);
	}

	void RemovePlayerObject(LobbyPlayer obj) {
		playersObjects.Remove(obj);
		obj.GetComponent<NetworkObject>().Despawn(true);
	}

	void ChangeTeam(Transform targetT, LobbyPlayer dragPlayer, LobbyPlayer swapPlayer) {
		if (LoadingSceneBool.Value) return;
		if (swapPlayer == null) {
			if (targetT == team1Holder) {
				if (team1.Contains(dragPlayer.clientID.Value) || team1Holder.childCount >= teamMax) return;
				// if (team1.Contains(dragPlayer.clientID.Value) || team1Holder.childCount >= team1Max) return;
				team2.Remove(dragPlayer.clientID.Value);
				team1.Add(dragPlayer.clientID.Value);
				dragPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(team1Holder);
				dragPlayer.transform.SetAsLastSibling();
			}
			if (targetT == team2Holder) {
				if (team2.Contains(dragPlayer.clientID.Value) || team2Holder.childCount >= teamMax) return;
				// if (team2.Contains(dragPlayer.clientID.Value) || team2Holder.childCount >= team2Max) return;
				team1.Remove(dragPlayer.clientID.Value);
				team2.Add(dragPlayer.clientID.Value);
				dragPlayer.gameObject.GetComponent<NetworkObject>().TrySetParent(team2Holder);
				dragPlayer.transform.SetAsLastSibling();
			}
			CheckIfPlayersFilled();
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
		CheckIfPlayersFilled();
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
		if (LoadingSceneBool.Value) return;
		ulong senderID = option.Receive.SenderClientId;
		//set the color regardless. just set it to the original if new one is not available.
		int prevColorIndx = ClientID_KEY_ColorIndex_VAL[senderID];
		if (prevColorIndx == targetIndex) return;

		if (ClientID_KEY_ColorIndex_VAL.ContainsValue(targetIndex)) {
			targetIndex = prevColorIndx;
		}
		LobbyPlayer targetPlayerObj = playersObjects.Find(x => x.clientID.Value == senderID);
		if (targetPlayerObj == null) return;

		ClientID_KEY_ColorIndex_VAL[senderID] = targetIndex;

		targetPlayerObj.SetColor(targetIndex);
	}

	#endregion


	#region Entering game
	public static event Action SceneLoadingError, LoadingNextScene;
	public static event Action<bool> LobbyFull;

	public static NetworkVariable<bool> LoadingSceneBool = new NetworkVariable<bool>(false);
	public static NetworkVariable<int> LoadingCountdown = new NetworkVariable<int>();

	Coroutine loadingSceneRoutine = null;
	const int sceneLoadTimer = 3, sceneLoadTimeout = 8;
	public void CheckIfPlayersFilled() {
		LobbyFull?.Invoke(team1.Count > 0 && team2.Count > 0);
		// LobbyFull?.Invoke(team1Max + team2Max == team1.Count + team2.Count);
	}
	public void EnterGame() {
		StopSceneLoading();
		loadingSceneRoutine = StartCoroutine(LoadNextScene());
	}
	IEnumerator LoadNextScene() {
		LoadingSceneBool.Value = true;
		SetupGameDataRPC();

		float timeout = sceneLoadTimeout;
		while (dataRpcConfirmationReceived > 0 && timeout > 0) {
			timeout -= Time.unscaledDeltaTime;
			yield return null;
		}
		if (timeout <= 0) {
			SceneLoadingError?.Invoke();
			StopSceneLoading();
			yield break;
		}

		int countDown = sceneLoadTimer;
		while (countDown > 0) {
			LoadingCountdown.Value = countDown;
			countDown--;
			yield return new WaitForSeconds(1f);
		}
		LoadingCountdown.Value = countDown;

		LoadingNextScene?.Invoke();

		SceneEventProgressStatus sceneStatus = NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
		if (sceneStatus != SceneEventProgressStatus.Started) {
			SceneLoadingError?.Invoke();
			StopSceneLoading();
		} else {
			//change data
			GameData.InSinglePlayerMode = false;
			GameData.allColorOptions = allColorOptions;
			GameData.ClientID_KEY_ColorIndex_VAL = ClientID_KEY_ColorIndex_VAL;
			GameData.ClientID_KEY_LobbyID_VAL = ClientID_KEY_LobbyID_VAL;
			GameData.team1 = team1; GameData.team2 = team2;
		}
	}
	string SerializeHashSet<T>(HashSet<T> set) {
		return JsonConvert.SerializeObject(set);
	}
	string SerializeDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict) {
		return JsonConvert.SerializeObject(dict);
	}
	HashSet<T> DeserializeHashSet<T>(string json) {
		return JsonConvert.DeserializeObject<HashSet<T>>(json);
	}
	Dictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(string json) {
		return JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(json);
	}

	#region Sending team data to clients
	float timeTeamRPCSent;
	const string splitter = "/", teamSplitter = ":";
	int dataRpcConfirmationReceived = 0;
	void SetupGameDataRPC() {
		timeTeamRPCSent = Time.unscaledTime;
		dataRpcConfirmationReceived = team1.Count + team2.Count;

		string teamList = "";
		foreach (ulong clientID in team1) {
			teamList += clientID.ToString() + splitter;
		}
		teamList.Remove(teamList.Length - 1);
		teamList += teamSplitter;
		foreach (ulong clientID in team2) {
			teamList += clientID.ToString() + splitter;
		}
		teamList.Remove(teamList.Length - 1);

		string id_color = SerializeDictionary<ulong, int>(ClientID_KEY_ColorIndex_VAL);

		SendTeamListClientRpc(teamList, timeTeamRPCSent, id_color);
	}
	[ClientRpc]
	void SendTeamListClientRpc(string teamIDs, float timeSent, string id_color) {
		string[] teamLists = teamIDs.Split(teamSplitter);
		string[] team1List = teamLists[0].Split(splitter);
		string[] team2List = teamLists[1].Split(splitter);
		GameData.team1.Clear();
		GameData.team2.Clear();

		Enum.TryParse(LobbyManager.Instance.joinedLobby.Data[LobbyManager.GameMode].Value, out GameMode mode);
		GameData.gameMode = mode;

		foreach (string s in team1List) {
			if (ulong.TryParse(s, out ulong clientID)) GameData.team1.Add(clientID);
		}
		foreach (string s in team2List) {
			if (ulong.TryParse(s, out ulong clientID)) GameData.team2.Add(clientID);
		}


		GameData.ClientID_KEY_ColorIndex_VAL = DeserializeDictionary<ulong, int>(id_color);

		GameData.InSinglePlayerMode = false;
		GameData.allColorOptions = allColorOptions;
		TeamUpdatedServerRpc(timeSent);
	}
	[ServerRpc(RequireOwnership = false)]
	void TeamUpdatedServerRpc(float timeSent) {
		if (timeSent != timeTeamRPCSent) return;
		dataRpcConfirmationReceived--;
	}



	#endregion

	public void StopSceneLoading() {
		if (loadingSceneRoutine != null) StopCoroutine(loadingSceneRoutine);
		loadingSceneRoutine = null;
		LoadingSceneBool.Value = false;
	}

	#endregion



	#region Miscellaneous
	const string PlayerNameKey = "PlayerName";
	public TMP_InputField nameInputField;
	void AuthenticationDone() {
		if (PlayerPrefs.HasKey(PlayerNameKey)) {
			SetNewName(PlayerPrefs.GetString(PlayerNameKey));
		} else {
			SetNewName(LobbyManager.playerName);
		}

	}
	public void SetNewName(string name) {
		LobbyManager.playerName = name;
		PlayerPrefs.SetString(PlayerNameKey, name);
		nameInputField.Set(name);
	}


	#endregion


}
