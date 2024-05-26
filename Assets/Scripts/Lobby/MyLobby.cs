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

[DefaultExecutionOrder(-100)]
public class MyLobby : MonoBehaviour {
	//use for lobby data keys
	public const string RelayCode = "RelayCode";
	public const string LobbyID = "LobbyID";
	public const string GameMode = "GameMode";
	public const string PlayerName = "PlayerName";

	CancellationTokenSource ExitScene;
	CancellationToken ExitToken;

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
	void OnDestroy() {
		while (createdLobbyIds.TryDequeue(out string lobbyId)) {
			try {
				LobbyService.Instance.DeleteLobbyAsync(lobbyId);
			} catch (LobbyServiceException e) {
				print(e);
			}
		}
		AuthenticationService.Instance.SignOut();
		ExitScene.Cancel();
		ExitScene.Dispose();
		if (Instance == this) Instance = null;
	}

	public static MyLobby Instance;
	[HideInInspector] public string authenticationID;
	[HideInInspector] public string playerName;
	public Lobby hostLobby, joinedLobby;
	DateTime latestLobbyInteraction;

	#region Events
	ILobbyEvents LobbyEvents = null;
	LobbyEventCallbacks lobbyCallback = null;
	public event Action<ILobbyChanges> LobbyChangedEvent;

	public event Action AuthenticationBegin, AuthenticationSuccess, AuthenticationFailure;


	public event Action LobbyCreationBegin, LobbyCreated, LobbyCreationSuccess, LobbyCreationFailure;

	public event Action RelayFailure;

	public event Action HearbeatFailure;

	public event Action LobbyJoinBegin, LobbyJoinSuccess, LobbyJoinFailure, JoinLobbyNetcode;
	public event Action<string> LobbyJoined;

	public event Action LeaveLobbyBegin;
	// public event Action LeaveLobbySuccess, LeaveLobbyFailure;
	public event Action<List<Player>> PlayersLeft;


	public event Action<List<Lobby>> ListLobbySuccess;
	public event Action ListLobbyFailure;
	#endregion

	#region lobby heartbeat & poll & listRefresh

	float heartBeatElapsed = 0, heartBeatPeriod = 15f;
	float lobbyListRefreshElapsed = 0, lobbyListRefreshPeriod = 5f;
	float updateElapsed = 0, updatePeriod = 3f;
	void Update() {
		LobbyHeartbeat();

		//you will probably need polling for edges cases where the events dont work for some reason.
		LobbyPoll();

		// LobbyListRefresh()
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
	async void LobbyListRefresh() {
		if (joinedLobby != null) { lobbyListRefreshElapsed = 0; return; }
		if (lobbyListRefreshElapsed > lobbyListRefreshPeriod) {
			lobbyListRefreshElapsed = 0f;
			try {
				await ListLobbies();
			} catch (Exception e) {
				print(e);
				LeaveLobby();
				HearbeatFailure?.Invoke();
			}
		} else {
			lobbyListRefreshElapsed += Time.deltaTime;
		}
	}

	async void LobbyPoll() {
		if (joinedLobby == null) { updateElapsed = 0; return; }
		if (updateElapsed > updatePeriod) {
			updateElapsed = 0f;
			try {
				joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
				if (hostLobby != null) {
					hostLobby = joinedLobby;
				}
			} catch (LobbyServiceException e) {
				print(e);
				OutOfLobby(e.Reason != LobbyExceptionReason.LobbyNotFound);
			}
		} else {
			updateElapsed += Time.deltaTime;
		}
	}
	void OutOfLobby(bool kicked) {
		if (!NetworkManager.Singleton.IsConnectedClient && kicked) {
			LobbyJoinFailure?.Invoke();
		} else {
			LobbyJoinSuccess?.Invoke();
		}
		LeaveLobby();
	}
	#endregion

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
					{GameMode, new DataObject(DataObject.VisibilityOptions.Public, mode, DataObject.IndexOptions.S1)},
					{RelayCode, new DataObject(DataObject.VisibilityOptions.Member, "RelayCode")}
				}
			};
			hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, lobbyMaxPlayerNumber, lobbyDetails);
			latestLobbyInteraction = hostLobby.LastUpdated;
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
			await SubscribeToLobbyEvents(true);
			//have to make your own player objects (as you won't be getting events for players being added as that "already happened") -- only for the host.
			LobbyCreated?.Invoke();
		} catch (Exception e) {
			print(e);
			LeaveLobby();
			LobbyCreationFailure?.Invoke();
		}
	}
	public void LobbyCreationSuccessful(bool succesful) {
		if (succesful) {
			LobbyCreationSuccess?.Invoke();
		} else {
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

	async Task SubscribeToLobbyEvents(bool subscribe) {
		try {
			if (subscribe) {
				lobbyCallback = new LobbyEventCallbacks();
				lobbyCallback.LobbyChanged += LobbyChanged;
				lobbyCallback.KickedFromLobby += KickedFromLobby;
				LobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(joinedLobby.Id, lobbyCallback);
			} else {
				// if (lobbyCallback != null) lobbyCallback.LobbyChanged -= LobbyChanged;
				if (LobbyEvents == null) return;
				ILobbyEvents temp = LobbyEvents;
				lobbyCallback = null;
				LobbyEvents = null;
				await temp.UnsubscribeAsync();
			}
		} catch (Exception e) {
			print(e);
			throw e;
		}
	}

	void LobbyChanged(ILobbyChanges changes) {
		if (joinedLobby == null) return;

		//in case i fail unsub to the lobby events
		DateTime now = DateTime.Now;
		TimeSpan changesTimespan = now - changes.LastUpdated.Value;
		TimeSpan latestInteractionTimespan = now - latestLobbyInteraction;
		if (changesTimespan > latestInteractionTimespan) return;

		changes.ApplyToLobby(joinedLobby);

		if (hostLobby != null) hostLobby = joinedLobby;
		LobbyChangedEvent?.Invoke(changes);

		if (hostLobby != null) {
			if (changes.PlayerJoined.Value != null) {
				foreach (LobbyPlayerJoined p in changes.PlayerJoined.Value) {
					LobbyJoined?.Invoke(p.Player.Id);
				}
			}
			if (changes.PlayerLeft.Value != null) {
				PlayersLeft(hostLobby.Players);
			}
		}
	}

	void KickedFromLobby() {
		OutOfLobby(true);
	}



	public async void JoinLobbyByID(string lobbyID) {
		if (joinedLobby != null) return;
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
			LobbyJoinFailure?.Invoke();
		}
	}

	public async void JoinLobbyByCode(string code) {
		if (joinedLobby != null) return;
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
			LobbyJoinFailure?.Invoke();
		}
	}

	public async void QuickJoinLobby() {
		if (joinedLobby != null) return;
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
			LobbyJoinFailure?.Invoke();
		}
	}
	async Task JoinLobby() {
		latestLobbyInteraction = joinedLobby.LastUpdated;
		string relayCode = joinedLobby.Data[RelayCode].Value;
		try {
			JoinAllocation joinRelayAlloc = await JoinRelay(relayCode);
			await SubscribeToLobbyEvents(true);
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinRelayAlloc, "dtls"));
			JoinLobbyNetcode?.Invoke();
		} catch (Exception e) {
			LeaveLobby();
			throw e;
		}
	}
	public void JoinLobbySuccessful(bool succesful) {
		if (succesful) {
			LobbyJoinSuccess?.Invoke();
		} else {
			LobbyJoinFailure?.Invoke();
		}
	}





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
	public void LeaveLobby() {
		LeaveLobby(authenticationID);
	}
	public async void LeaveLobby(string playerID) {
		if (joinedLobby != null) {
			LeaveLobbyBegin?.Invoke();
			try {
				string lobbyID = joinedLobby.Id;
				string lobbyhostID = joinedLobby.HostId;
				joinedLobby = null;
				hostLobby = null;
				await SubscribeToLobbyEvents(false);
				if (playerID == lobbyhostID) {
					await LobbyService.Instance.DeleteLobbyAsync(lobbyID);
				} else {
					await LobbyService.Instance.RemovePlayerAsync(lobbyID, playerID);
				}
				// LeaveLobbySuccess?.Invoke();
			} catch (Exception e) {
				print(e);
				// LeaveLobbyFailure?.Invoke();
			}
		}
	}








	public async Task ListLobbies() {
		try {
			QueryLobbiesOptions filter = new QueryLobbiesOptions {
				Count = 25,
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
			ListLobbyFailure?.Invoke();
			print(e.Reason);
		}
	}

	public async void UpdateLobbyMode(string mode) {
		try {
			Dictionary<string, DataObject> data = new Dictionary<string, DataObject>();
			data[GameMode] = new DataObject(DataObject.VisibilityOptions.Public, mode, DataObject.IndexOptions.S1);
			hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				Data = data
			});
			joinedLobby = hostLobby;
		} catch (LobbyServiceException e) {
			print(e.Reason);
			LeaveLobby();
		}
	}
	public async void MakeLobbyPublic() {
		try {
			hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				IsPrivate = false
			});
			joinedLobby = hostLobby;
			LobbyCreationSuccess?.Invoke();
		} catch (LobbyServiceException e) {
			print(e.Reason);
			LeaveLobby();
			LobbyCreationFailure?.Invoke();
		}
	}





	#region Relay
	public async Task<Allocation> AllocateRelay(int playerCount, bool hosting = true) {
		try {
			//yo
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







	bool NGOConnected() {
		NetworkManager nm = NetworkManager.Singleton;
		return nm.ShutdownInProgress || nm.IsClient || nm.IsServer;
	}
}



