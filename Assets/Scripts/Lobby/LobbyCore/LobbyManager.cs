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
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class LobbyManager : MonoBehaviour {
	public const string RelayCode = "RelayCode";
	public const string LobbyID = "LobbyID";
	public const string GameMode = "GameMode";
	public const string PlayerName = "PlayerName";

	CancellationTokenSource ExitScene;
	CancellationToken ExitToken;
	public static LobbyManager Instance;
	void Awake() {
		Instance = this;
		ExitScene = new CancellationTokenSource();
		ExitToken = ExitScene.Token;
	}
	void Start() {
		Authentication();
	}
	public async void Authentication(string name = null) {
		AuthenticationBegin?.Invoke();
		try {
			if (UnityServices.State == ServicesInitializationState.Uninitialized) {
				InitializationOptions options = new InitializationOptions();
				playerName = (name == null) ? "Player" + UnityEngine.Random.Range(0, 10000) : name;
				options.SetProfile(playerName);
				await UnityServices.InitializeAsync(options);
			}
			if (!AuthenticationService.Instance.IsSignedIn) {
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				if (AuthenticationService.Instance != null) authenticationID = AuthenticationService.Instance.PlayerId;
				print(playerName);
			}
			AuthenticationSuccess?.Invoke();
		} catch (Exception e) {
			print(e);
			AuthenticationFailure?.Invoke();
		}
	}

	Queue<string> createdLobbyIds = new Queue<string>();


	[HideInInspector] public string authenticationID;
	[HideInInspector] public static string playerName;
	public Lobby hostLobby, joinedLobby;
	// DateTime latestLobbyInteraction;

	#region Events

	//others
	public static event Action AuthenticationBegin, AuthenticationSuccess, AuthenticationFailure;
	public static event Action LobbyManagerResetEvent;

	//creating lobby
	public static event Action LobbyCreationBegin, CreatedLobby, LobbyCreationFailure;
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

		//you may need polling for edges cases where the events dont work for some reason.
		// LobbyPoll();
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

	#endregion


	#region  lobbyCreation
	public async void CreateLobby(string lobbyName, string mode, int lobbyMaxPlayerNumber) {
		if (hostLobby != null) return;
		LobbyCreationBegin?.Invoke();
		if (NGOConnected()) {
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
					{RelayCode, new DataObject(DataObject.VisibilityOptions.Member, RelayCode)}
				}
			};
			hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, lobbyMaxPlayerNumber, lobbyDetails);
			// latestLobbyInteraction = hostLobby.LastUpdated;
			createdLobbyIds.Enqueue(hostLobby.Id);
			//assign relay
			Allocation relayAlloc = await AllocateRelay(lobbyMaxPlayerNumber);
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(relayAlloc, "dtls"));
			string relayCode = await GetRelayCode(relayAlloc);

			//update lobby with relay
			hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				Data = new Dictionary<string, DataObject> {
					{RelayCode, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
				}
			});
			joinedLobby = hostLobby;
			await SubscribeToLobbyEvents();
			//have to make your own player objects (as you won't be getting events for players being added as that "already happened") -- only for the host.
			CreatedLobby?.Invoke();
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
		try {
			lobbyCallback = new LobbyEventCallbacks();
			lobbyCallback.LobbyChanged += LobbyChanged;
			lobbyCallback.KickedFromLobby += KickedFromLobby;
			lobbyCallback.LobbyDeleted += LobbyDeleted;
			LobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(joinedLobby.Id, lobbyCallback);
		} catch (Exception e) {
			print(e);
			throw e;
		}
	}
	async Task UnsubscribeFromLobbyEvents() {
		try {
			ILobbyEvents temp = LobbyEvents;
			if (temp == null) return;
			await temp.UnsubscribeAsync();
		} catch (Exception e) {
			print(e);
			throw e;
		}
		lobbyCallback = null;
		LobbyEvents = null;
	}

	void LobbyChanged(ILobbyChanges changes) {
		if (joinedLobby == null) return;
		//in case it fails unsub to the lobby events? idk
		// DateTime now = DateTime.Now;
		// TimeSpan changesTimespan = now - changes.LastUpdated.Value;
		// TimeSpan latestInteractionTimespan = now - latestLobbyInteraction;
		// if (changesTimespan > latestInteractionTimespan) return;

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

			joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyID, options);
			await JoinLobby();
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

			joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, options);

			await JoinLobby();
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
			joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
			await JoinLobby();
		} catch (Exception e) {
			print(e);
			LeaveLobby();
			LobbyJoinFailure?.Invoke();
		}
	}
	// bool WaitingForNGO = false;
	async Task JoinLobby() {
		// WaitingForNGO = true;
		// latestLobbyInteraction = joinedLobby.LastUpdated;
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
	// public void JoinLobbySuccessful(bool succesful) {
	// 	if (succesful) {
	// 		LobbyJoinSuccess?.Invoke();
	// 	} else {
	// 		LobbyJoinFailure?.Invoke();
	// 	}
	// 	// WaitingForNGO = false;
	// }

	#endregion




	#region Kick/LeavingLobby

	public async void KickFromLobby(string id) {
		if (hostLobby == null) return;
		if (joinedLobby != null) {
			try {
				await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, id);
			} catch (LobbyServiceException e) {
				print(e.Reason);
			}
		}
	}
	public void KickedFromLobby() {
		// if (joinedLobby == null) return;
		// if (WaitingForNGO) {
		// 	LobbyJoinFailure?.Invoke();
		// } else {
		// 	AuthenticationSuccess?.Invoke();
		// }
		KickedFromLobbyEvent?.Invoke();
		LeaveLobby();
	}
	public void LeaveLobby(bool SendEvents = true) {
		LeaveLobby(authenticationID, SendEvents);
	}
	public async void LeaveLobby(string authenticationID, bool SendEvents = true) {
		if (SendEvents) LeaveLobbyBegin?.Invoke();

		try {
			await UnsubscribeFromLobbyEvents();
		} catch (Exception e) {
			print(e);
		}
		if (joinedLobby != null) {
			try {
				// WaitingForNGO = false;
				if (authenticationID == joinedLobby.HostId) {
					await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
				} else {
					await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, authenticationID);
				}
			} catch (Exception e) {
				print(e);
			}
		}
		joinedLobby = null;
		hostLobby = null;
		if (SendEvents) LeaveLobbyComplete?.Invoke();
	}
	void LobbyDeleted() {
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
			ListLobbyFailure?.Invoke(null);
		}
	}

	// public async void UpdateLobbyMode(string mode) {
	// 	try {
	// 		Dictionary<string, DataObject> data = new Dictionary<string, DataObject>();
	// 		data[GameMode] = new DataObject(DataObject.VisibilityOptions.Public, mode, DataObject.IndexOptions.S1);
	// 		hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
	// 			Data = data
	// 		});
	// 		joinedLobby = hostLobby;
	// 	} catch (LobbyServiceException e) {
	// 		print(e.Reason);
	// 		LeaveLobby();
	// 	}
	// }
	public async void MakeLobbyPublic() {
		try {
			hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				IsPrivate = false
			});
			joinedLobby = hostLobby;
			// LobbyCreationSuccess?.Invoke();
		} catch (LobbyServiceException e) {
			print(e.Reason);
			LeaveLobby();
			LobbyUpdateFailure?.Invoke();
		}
	}
	#endregion





	void OnDestroy() {
		ResetLobbyManager();
		AuthenticationService.Instance.SignOut();
		ExitScene.Cancel();
		ExitScene.Dispose();
		Instance = null;
	}

	//if references and such not properly nulled when leaving etc (or stuck)
	void ResetLobbyManager() {
		LeaveLobby(true);
		while (createdLobbyIds.TryDequeue(out string lobbyId)) {
			try {
				LobbyService.Instance.DeleteLobbyAsync(lobbyId);
			} catch (LobbyServiceException e) {
				print(e);
			}
		}
		LobbyManagerResetEvent?.Invoke();
	}



	bool NGOConnected() {
		NetworkManager nm = NetworkManager.Singleton;
		return nm.ShutdownInProgress || nm.IsClient || nm.IsServer;
	}
}



