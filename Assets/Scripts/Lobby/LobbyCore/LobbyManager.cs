using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Services.Relay.Models;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class LobbyManager : MonoBehaviour {
	public const string RelayCode = "RelayCode";
	public const string Dictionary = "Dictionary";
	public const string LobbyID = "LobbyID";
	public const string GameMode = "GameMode";
	public const string PlayerName = "PlayerName";

	public static LobbyManager Instance;
	void Awake() {
		Instance = this;
		InternetConnectivityCheck.ConnectedStateEvent += ConnectionChanged;
		LobbyNetcodeManager.TransportFailureEvent += TransportFail;
	}
	public void Start() {
		Authenticate();
	}
	bool attemptingToAuthenticate = false;
	public async void Authenticate(string name = null) {
		if (attemptingToAuthenticate) return;
		AuthenticationBegin?.Invoke();
		attemptingToAuthenticate = true;
		try {
			InitializationOptions options = new InitializationOptions();
			playerName = (name == null) ? "Player" + UnityEngine.Random.Range(0, 10000) : name;
			options.SetProfile(playerName);
			if (UnityServices.State != ServicesInitializationState.Initialized) await TaskTimeout.AddTimeout(UnityServices.InitializeAsync(options));

			if (AuthenticationService.Instance.IsSignedIn) {
				await TaskTimeout.AddTimeout(CleanupLobbies());
				AuthenticationService.Instance.SignOut(true);
			}
			AuthenticationService.Instance.ClearSessionToken();

			await TaskTimeout.AddTimeout(AuthenticationService.Instance.SignInAnonymouslyAsync());
			authenticationID = AuthenticationService.Instance.PlayerId;
			await TaskTimeout.AddTimeout(CleanupLobbies(true));
			AuthenticationSuccess?.Invoke();
		} catch (Exception e) {
			print(e);
			AuthenticationFailure?.Invoke();
		}
		attemptingToAuthenticate = false;

	}

	async Task CleanupLobbies(bool clearList = false) {
		for (int i = 0; i < lobbyEventsToCleanup.Count;) {
			try {
				await TaskTimeout.AddTimeout(lobbyEventsToCleanup[i].UnsubscribeAsync());
				lobbyEventsToCleanup.RemoveAt(i);
				print("cleanedup lobby event link");
			} catch (Exception e) {
				print(e);
				i++;
			}
		}
		for (int i = 0; i < lobbiesToCleanup.Count;) {
			try {
				await LobbyService.Instance.RemovePlayerAsync(lobbiesToCleanup[i], authenticationID);
				lobbiesToCleanup.RemoveAt(i);
				print("cleaned up lobby link");
			} catch (Exception e) {
				print(e);
				i++;
			}
		}
		if (clearList) {
			lobbiesToCleanup.Clear();
			lobbyEventsToCleanup.Clear();
		}
	}


	bool conn = true;

	void ConnectionChanged(bool connected) {
		if (conn != connected && connected == true) {
			Authenticate();
		}
		conn = connected;
	}
	void TransportFail() {
		ConnectionChanged(false);
	}



	[HideInInspector] public string authenticationID;
	public static string playerName;
	public Lobby hostLobby, joinedLobby;
	// DateTime latestLobbyInteraction;

	#region Events

	//others
	public static event Action AuthenticationBegin, AuthenticationSuccess, AuthenticationFailure;

	//creating lobby
	public static event Action LobbyCreationBegin, CreatedLobbyEvent, LobbyCreationFailure;
	public static event Action RelayFailure;

	//joining lobby
	public static event Action LobbyJoinBegin, LobbyJoinFailure, JoinedLobby;
	public static event Action<string> PlayerJoinedLobby;

	//maintaining lobby
	ILobbyEvents LobbyEvents = null;
	LobbyEventCallbacks lobbyCallback = null;
	public static event Action<ILobbyChanges> LobbyChangedEvent;
	public static event Action LobbyUpdateFailure;
	public static event Action HearbeatFailure;

	//leaving lobby
	public static event Action LeaveLobbyBegin, LeaveLobbyComplete;
	public static event Action<List<Player>> PlayersRemainingInLobbyAfterLeaver;
	public static event Action KickedFromLobbyEvent;

	//getting available lobbies
	public static event Action<List<Lobby>> ListLobbySuccess, ListLobbyFailure;
	#endregion

	#region lobby heartbeat & autoPoll? & autoAvailableListRefresh

	float heartBeatElapsed = 0, heartBeatPeriod = 15f;
	void Update() {
		LobbyHeartbeat();
		LobbyPoll();
	}
	async void LobbyHeartbeat() {
		if (hostLobby == null) { heartBeatElapsed = 0; return; }
		if (heartBeatElapsed > heartBeatPeriod) {
			heartBeatElapsed = 0f;
			try {
				await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
			} catch (Exception e) {
				print(e);
				LeaveLobby();
				HearbeatFailure?.Invoke();
			}
		} else {
			heartBeatElapsed += Time.deltaTime;
		}
	}

	float lobbyPollElapsed = 0, lobbyPollPeriod = 10f;
	void LobbyPoll() {
		if (joinedLobby != null || hostLobby != null) { lobbyPollElapsed = lobbyPollPeriod - 1f; return; }
		if (lobbyPollElapsed > lobbyPollPeriod) {
			lobbyPollElapsed = 0f;
			ListLobbies();
		} else {
			lobbyPollElapsed += Time.deltaTime;
		}
	}

	#endregion


	#region  lobbyCreation
	public async void CreateLobby(string lobbyName, string mode, int lobbyMaxPlayerNumber, DictionaryMode dictionary) {
		if (hostLobby != null) return;
		LobbyCreationBegin?.Invoke();
		if (NGOConnected()) {
			LeaveLobby();
			LobbyCreationFailure?.Invoke();
			return;
		}
		try {
			//create lobby
			CreateLobbyOptions lobbyDetails = new CreateLobbyOptions {
				IsPrivate = true,
				Player = GetNewPlayer(playerName),
				Data = new Dictionary<string, DataObject> {
					{GameMode, new DataObject(DataObject.VisibilityOptions.Public, mode)},
					{RelayCode, new DataObject(DataObject.VisibilityOptions.Member, RelayCode)},
					{Dictionary, new DataObject(DataObject.VisibilityOptions.Public, dictionary.ToString())}
				}
			};
			hostLobby = await TaskTimeout.AddTimeout<Lobby>(LobbyService.Instance.CreateLobbyAsync(lobbyName, lobbyMaxPlayerNumber, lobbyDetails));

			//assign relay
			Allocation relayAlloc = await TaskTimeout.AddTimeout<Allocation>(AllocateRelay(lobbyMaxPlayerNumber), TimeSpan.FromSeconds(5));
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(relayAlloc, "dtls"));
			string relayCode = await TaskTimeout.AddTimeout<String>(GetRelayCode(relayAlloc), TimeSpan.FromSeconds(5));

			//update lobby with relay
			hostLobby = await TaskTimeout.AddTimeout<Lobby>(LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				Data = new Dictionary<string, DataObject> {
					{RelayCode, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
				}
			}), TimeSpan.FromSeconds(5));
			joinedLobby = hostLobby;
			await TaskTimeout.AddTimeout(SubscribeToLobbyEvents(), TimeSpan.FromSeconds(5));
			CreatedLobbyEvent?.Invoke();
		} catch (Exception e) {
			print(e);
			LeaveLobby();
			LobbyCreationFailure?.Invoke();
		}
	}
	Player GetNewPlayer(string name) {
		return new Player {
			Data = new Dictionary<string, PlayerDataObject>{
				{PlayerName,  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name)},
			}
		};
	}

	async Task SubscribeToLobbyEvents() {
		//add a 5 sec timer here for timeout incase
		try {
			lobbyCallback = new LobbyEventCallbacks();
			lobbyCallback.LobbyChanged += LobbyChanged;
			lobbyCallback.KickedFromLobby += KickedFromLobby;
			lobbyCallback.LobbyDeleted += LobbyDeleted;
			LobbyEvents = await TaskTimeout.AddTimeout<ILobbyEvents>(Lobbies.Instance.SubscribeToLobbyEventsAsync(joinedLobby.Id, lobbyCallback));
		} catch (Exception e) {
			print(e);
			throw e;
		}
	}
	List<ILobbyEvents> lobbyEventsToCleanup = new List<ILobbyEvents>();
	async Task UnsubscribeFromLobbyEvents() {
		try {
			if (!InternetConnectivityCheck.connected && LobbyEvents != null && !lobbyEventsToCleanup.Contains(LobbyEvents)) lobbyEventsToCleanup.Add(LobbyEvents);
			ILobbyEvents temp = LobbyEvents;
			if (temp == null) return;
			lobbyCallback = null;
			LobbyEvents = null;
			await TaskTimeout.AddTimeout(temp.UnsubscribeAsync());
		} catch (Exception e) {
			lobbyCallback = null;
			LobbyEvents = null;
			print(e);
			throw e;
		}
	}

	void LobbyChanged(ILobbyChanges changes) {
		if (joinedLobby == null) return;

		changes.ApplyToLobby(joinedLobby);
		if (hostLobby != null) hostLobby = joinedLobby;
		LobbyChangedEvent?.Invoke(changes);

		CheckPlayersJoinedLobby(changes);
		CheckPlayersLeftLobby(changes);
	}
	void CheckPlayersLeftLobby(ILobbyChanges changes) {
		if (changes.PlayerLeft.Value != null) {
			PlayersRemainingInLobbyAfterLeaver?.Invoke(joinedLobby.Players);
		}
	}
	void CheckPlayersJoinedLobby(ILobbyChanges changes) {
		if (changes.PlayerJoined.Value != null) {
			foreach (LobbyPlayerJoined p in changes.PlayerJoined.Value) {
				PlayerJoinedLobby?.Invoke(p.Player.Id);
			}
		}
	}
	#endregion


	#region Relay
	public async Task<Allocation> AllocateRelay(int playerCount, bool hosting = true) {
		try {
			//you can set region in this allocation as well if you want (it does happen automatically as well)
			//set the player count -1 as it does not include hosts.playerCount;
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerCount - 1);
			return allocation;
		} catch (RelayServiceException e) {
			print(e.Reason);
			RelayFailure?.Invoke();
			throw e;
		}
	}
	public async Task<string> GetRelayCode(Allocation allocation) {
		try {
			string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			return relayCode;
		} catch (RelayServiceException e) {
			print(e.Reason);
			RelayFailure?.Invoke();
			throw e;
		}
	}
	public async Task<JoinAllocation> JoinRelay(string relayCode) {
		try {
			JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
			return allocation;
		} catch (RelayServiceException e) {
			print(e.Reason);
			RelayFailure?.Invoke();
			throw e;
		}
	}
	#endregion



	#region joiningLobby
	public async void JoinLobbyByID(string lobbyID) {
		if (joinedLobby != null) { LobbyJoinFailure?.Invoke(); return; }
		LobbyJoinBegin?.Invoke();
		try {
			if (NGOConnected()) {
				throw new Exception("NGO still connected");
			}
			JoinLobbyByIdOptions options = new JoinLobbyByIdOptions {
				Player = GetNewPlayer(playerName)
			};

			joinedLobby = await TaskTimeout.AddTimeout<Lobby>(Lobbies.Instance.JoinLobbyByIdAsync(lobbyID, options));
			await TaskTimeout.AddTimeout(JoinLobby());
		} catch (Exception e) {
			print(e);
			LeaveLobby();
			LobbyJoinFailure?.Invoke();
		}
	}

	public async void JoinLobbyByCode(string code) {
		if (joinedLobby != null) { LobbyJoinFailure?.Invoke(); return; }
		LobbyJoinBegin?.Invoke();
		try {
			if (NGOConnected()) {
				throw new Exception("NGO still connected");
			}
			JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions {
				Player = GetNewPlayer(playerName)
			};

			joinedLobby = await TaskTimeout.AddTimeout<Lobby>(Lobbies.Instance.JoinLobbyByCodeAsync(code, options));

			await TaskTimeout.AddTimeout(JoinLobby());
		} catch (Exception e) {
			print(e);
			LeaveLobby();
			LobbyJoinFailure?.Invoke();
		}
	}

	public async void QuickJoinLobby() {
		if (joinedLobby != null) { LobbyJoinFailure?.Invoke(); return; }
		LobbyJoinBegin?.Invoke();
		try {
			if (NGOConnected()) {
				throw new Exception("NGO still connected");
			}
			QuickJoinLobbyOptions options = new QuickJoinLobbyOptions {
				Player = GetNewPlayer(playerName)
			};
			joinedLobby = await TaskTimeout.AddTimeout<Lobby>(LobbyService.Instance.QuickJoinLobbyAsync(options));
			await TaskTimeout.AddTimeout(JoinLobby());
		} catch (Exception e) {
			print(e);
			LeaveLobby();
			LobbyJoinFailure?.Invoke();
		}
	}
	async Task JoinLobby() {
		string relayCode = joinedLobby.Data[RelayCode].Value;
		try {
			JoinAllocation joinRelayAlloc = await JoinRelay(relayCode);
			await SubscribeToLobbyEvents();
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinRelayAlloc, "dtls"));
			JoinedLobby?.Invoke();
		} catch (Exception e) {
			print(e);
			LeaveLobby();
			throw e;
		}
	}

	#endregion




	#region Kick/LeavingLobby

	public async void KickFromLobby(string id) {
		if (hostLobby != null) {
			try {
				await LobbyService.Instance.RemovePlayerAsync(hostLobby.Id, id);
			} catch (LobbyServiceException e) {
				print(e.Reason);
			}
		}
	}
	public void KickedFromLobby() {
		if (MyLobby.LoadingSceneBool.Value) return;
		KickedFromLobbyEvent?.Invoke();
		LeaveLobby();
	}
	public void LeaveLobby(bool SendEvents = true) {
		LeaveLobby(authenticationID, SendEvents);
	}
	List<string> lobbiesToCleanup = new List<string>();
	public async void LeaveLobby(string authenticationID, bool SendEvents = true) {
		if (SendEvents) LeaveLobbyBegin?.Invoke();
		try {
			await UnsubscribeFromLobbyEvents();
		} catch (Exception e) {
			print(e);
		}
		if (!InternetConnectivityCheck.connected && hostLobby != null && !lobbiesToCleanup.Contains(hostLobby.Id)) lobbiesToCleanup.Add(hostLobby.Id);
		try {
			if (hostLobby != null) {
				await TaskTimeout.AddTimeout(LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id));
			} else if (joinedLobby != null) {
				await TaskTimeout.AddTimeout(LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, authenticationID));
			}
		} catch (Exception e) {
			print(e);
		}
		joinedLobby = null;
		hostLobby = null;
		if (SendEvents) LeaveLobbyComplete?.Invoke();
	}
	async void DeleteLobby() {
		try {
			await UnsubscribeFromLobbyEvents();
		} catch (Exception e) {
			print(e);
		}
		try {
			await TaskTimeout.AddTimeout(LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id));
		} catch (Exception e) {
			print(e);
		}
	}
	void LobbyDeleted() {
		if (MyLobby.LoadingSceneBool.Value) return;
		LeaveLobby();
	}
	#endregion






	#region otherLobbyMethods
	const int ListLobbyMax = 25;
	public async Task ListLobbies() {
		try {
			QueryLobbiesOptions filter = new QueryLobbiesOptions {
				Count = ListLobbyMax,
				Filters = new List<QueryFilter> {
					new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
					},
				Order = new List<QueryOrder> {
					new QueryOrder(false, QueryOrder.FieldOptions.Created)
				}
			};

			QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(filter);
			ListLobbySuccess?.Invoke(response.Results);
		} catch (LobbyServiceException e) {
			print(e.Reason);
			if (e.ErrorCode == (int)LobbyExceptionReason.RateLimited) return;
			ListLobbyFailure?.Invoke(null);
		}
	}
	public async void MakeLobbyPublic(bool makePublic = true) {
		try {
			hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				IsPrivate = !makePublic
			});
			joinedLobby = hostLobby;
		} catch (LobbyServiceException e) {
			print(e.Reason);
			LeaveLobby();
			LobbyUpdateFailure?.Invoke();
		}
	}
	#endregion



	void OnDestroy() {
		InternetConnectivityCheck.ConnectedStateEvent -= ConnectionChanged;
		LobbyNetcodeManager.TransportFailureEvent -= TransportFail;
		Instance = null;
		DeleteLobby();
		if (MyLobby.LoadingSceneBool.Value != true) LeaveLobby();
	}

	bool NGOConnected() {
		NetworkManager nm = NetworkManager.Singleton;
		return nm.ShutdownInProgress || nm.IsClient || nm.IsServer;
	}
}



