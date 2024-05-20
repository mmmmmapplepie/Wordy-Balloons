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
	public const string GameMode = "GameMode";
	public const string PlayerName = "PlayerName";
	bool Free = true;


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
	void OnDestroy() {
		ExitScene.Cancel();
		ExitScene.Dispose();
	}

	public static MyLobby Instance;
	[HideInInspector] public string authenticationID;
	[HideInInspector] public string playerName;
	[HideInInspector] public string lobbyCode;
	public Lobby hostLobby;
	public Lobby joinedLobby;

	#region Events
	ILobbyEvents LobbyEvents = null;
	LobbyEventCallbacks lobbyCallback = null;
	public event Action<ILobbyChanges> LobbyChangedEvent;

	public event Action AuthenticationBegin, AuthenticationSuccess;
	public event Action<RequestFailedException> UnityServiceFailure;
	public event Action<AuthenticationException> AuthenticationFailure;


	public event Action LobbyCreationBegin, LobbyCreationSuccess;
	public event Action<LobbyServiceException> LobbyCreationFailure;
	public event Action LobbyCreationFailureRelay;
	public event Action<RelayServiceException> RelayFailure;


	// public event Action<Lobby> lobbyPulled;
	// public event Action<LobbyServiceException> LobbyPullFailure;
	public event Action HearbeatFailure;


	public event Action LobbyJoinBegin, LobbyJoinSuccess;
	public event Action<LobbyServiceException> LobbyJoinFailure;


	public event Action LeaveLobbyBegin, LeaveLobbySuccess;
	public event Action<LobbyServiceException> LeaveLobbyFailure;


	public event Action DeleteLobbyBegin, DeleteLobbySuccess;
	public event Action<LobbyServiceException> DeleteLobbyFailure;


	public event Action<List<Lobby>> ListLobbySuccess;
	public event Action<LobbyServiceException> ListLobbyFailure;
	#endregion

	#region lobby heartbeat & pull

	float heartBeatElapsed = 0, heartBeatPeriod = 15f;
	// float updateElapsed = 0, updatePeriod = 1.5f;
	void Update() {
		LobbyHeartbeat();
		// LobbyPull();
	}
	async void LobbyHeartbeat() {
		if (hostLobby == null) return;
		if (heartBeatElapsed > heartBeatPeriod) {
			heartBeatElapsed = 0f;
			try {
				await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
			} catch (LobbyServiceException e) {
				print(e);
				HearbeatFailure?.Invoke();
			}
		} else {
			heartBeatElapsed += Time.deltaTime;
		}
	}

	// async void LobbyPull() {
	// 	if (joinedLobby == null) return;
	// 	if (updateElapsed > updatePeriod) {
	// 		updateElapsed = 0f;
	// 		try {
	// 			joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
	// 			if (hostLobby != null) {
	// 				hostLobby = joinedLobby;
	// 			}
	// 			lobbyPulled?.Invoke(joinedLobby);
	// 		} catch (LobbyServiceException e) {
	// 			print(e);
	// 			LobbyPullFailure?.Invoke(e);
	// 		}
	// 	} else {
	// 		updateElapsed += Time.deltaTime;
	// 	}
	// }
	#endregion

	public async void Authentication(string name = null) {
		if (!Free) return;
		Free = false;
		AuthenticationBegin?.Invoke();
		try {
			if (UnityServices.State == ServicesInitializationState.Uninitialized) {
				InitializationOptions options = new InitializationOptions();
				playerName = (name == null) ? "Player" + UnityEngine.Random.Range(0, 10000) : name;
				options.SetProfile(playerName);
				await UnityServices.InitializeAsync(options);
			}
		} catch (RequestFailedException e) {
			print(e);
			UnityServiceFailure?.Invoke(e);
		}

		try {
			if (!AuthenticationService.Instance.IsSignedIn) {
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				if (AuthenticationService.Instance != null) authenticationID = AuthenticationService.Instance.PlayerId;
				print(playerName);
			}
			AuthenticationSuccess?.Invoke();
		} catch (AuthenticationException e) {
			print(e);
			AuthenticationFailure?.Invoke(e);
		}
		Free = true;
	}


	public void CL() {
		CreateLobby("ye", "YEP", 4);
	}
	public void LeaveLobby() {
		LeaveLobby(authenticationID);
	}




	public async void CreateLobby(string lobbyName, string mode, int lobbyMaxPlayerNumber) {
		if (hostLobby != null) return;
		if (!Free) return;
		Free = false;
		LobbyCreationBegin?.Invoke();
		try {
			//create lobby
			CreateLobbyOptions lobbyDetails = new CreateLobbyOptions {
				IsPrivate = false,
				Player = GetNewPlayer(playerName),
				Data = new Dictionary<string, DataObject> {
					{GameMode, new DataObject(DataObject.VisibilityOptions.Public, mode, DataObject.IndexOptions.S1)},
					{RelayCode, new DataObject(DataObject.VisibilityOptions.Member, "RelayCode")}
				}
			};
			hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, lobbyMaxPlayerNumber, lobbyDetails);

			//assign relay
			Allocation relayAlloc = await AllocateRelay(lobbyMaxPlayerNumber);
			if (relayAlloc == default) {
				LeaveLobby(authenticationID);
				LobbyCreationFailureRelay?.Invoke();
				Free = true;
				return;
			}
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(relayAlloc, "dtls"));
			string relayCode = await GetRelayCode(relayAlloc);
			if (relayCode == default) {
				LeaveLobby(authenticationID);
				LobbyCreationFailureRelay?.Invoke();
				Free = true;
				return;
			}

			//update lobby with relay
			hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				Data = new Dictionary<string, DataObject> {
					{RelayCode, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
				}
			});
			joinedLobby = hostLobby;
			bool subscribed = await SubscribeToLobbyEvents(true);
			//have to make your own player objects (as you won't be getting events for players being added as that "already happened") -- only for the host.
			if (subscribed) {
				//move this to the end of host starting
				LobbyCreationSuccess?.Invoke();
			} else {
				LeaveLobby(authenticationID);
			}
		} catch (LobbyServiceException e) {
			LobbyCreationFailure?.Invoke(e);
			print(e);
		}
		Free = true;
	}

	Player GetNewPlayer(string name) {
		return new Player {
			Data = new Dictionary<string, PlayerDataObject>{
				{PlayerName,  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name)},
			}
		};
	}

	async Task<bool> SubscribeToLobbyEvents(bool subscribe) {
		try {
			if (subscribe) {
				lobbyCallback = new LobbyEventCallbacks();
				lobbyCallback.LobbyChanged += LobbyChanged;
				LobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(joinedLobby.Id, lobbyCallback);
			} else {
				// if (lobbyCallback != null) lobbyCallback.LobbyChanged -= LobbyChanged;
				await LobbyEvents.UnsubscribeAsync();
				lobbyCallback = null;
				LobbyEvents = null;
			}
			return true;
		} catch (LobbyServiceException e) {
			print(e);
			return false;
		}
	}

	void LobbyChanged(ILobbyChanges changes) {
		if (joinedLobby == null) return;
		changes.ApplyToLobby(joinedLobby);
		if (hostLobby != null) hostLobby = joinedLobby;
		LobbyChangedEvent?.Invoke(changes);





		//lobby deleted/kicked
		if (changes.LobbyDeleted || joinedLobby.Players.Find(x => x.Id == authenticationID) == null) {
			LeaveLobby(authenticationID, true);
			return;
		}

		if (changes.PlayerJoined.Value != null && hostLobby != null) {
			foreach (LobbyPlayerJoined p in changes.PlayerJoined.Value) {
				Player player = p.Player;
				print(player.Id);
				//make the player object etc
			}
		}

		if (changes.Data.Value != null) {
			//change appropriately look up documentation for details on the data type passed around.
		}
	}

	public async void JoinLobbyByID(string lobbyID) {
		if (joinedLobby != null) return;
		if (!Free) return;
		Free = false;
		LobbyJoinBegin?.Invoke();
		try {
			JoinLobbyByIdOptions options = new JoinLobbyByIdOptions {
				Player = GetNewPlayer(playerName)
			};

			joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyID, options);

			await JoinLobby();
		} catch (LobbyServiceException e) {
			print(e);
			LobbyJoinFailure?.Invoke(e);
		}
		Free = true;
	}

	public async void JoinLobbyByCode(string code) {
		if (joinedLobby != null) return;
		if (!Free) return;
		Free = false;
		LobbyJoinBegin?.Invoke();
		try {
			JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions {
				Player = GetNewPlayer(playerName)
			};

			joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, options);

			await JoinLobby();
		} catch (LobbyServiceException e) {
			print(e);
			LobbyJoinFailure?.Invoke(e);
		}
		Free = true;
	}

	public async void QuickJoinLobby() {
		if (joinedLobby != null) return;
		if (!Free) return;
		Free = false;
		LobbyJoinBegin?.Invoke();
		try {
			QuickJoinLobbyOptions options = new QuickJoinLobbyOptions {
				Player = GetNewPlayer(playerName)
			};
			joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
			await JoinLobby();
		} catch (LobbyServiceException e) {
			print(e);
			LobbyJoinFailure?.Invoke(e);
		}
		Free = true;
	}
	async Task JoinLobby() {
		string relayCode = joinedLobby.Data[RelayCode].Value;
		JoinAllocation joinRelayAlloc = await JoinRelay(relayCode);
		if (joinRelayAlloc == default) {
			Free = true;
			LeaveLobby(authenticationID);
			return;
		}
		if (await SubscribeToLobbyEvents(true)) {
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinRelayAlloc, "dtls"));
			LobbyJoinSuccess?.Invoke();
		} else {
			LeaveLobby(authenticationID);
			Free = true;
		}
	}








	public async void KickFromLobby(string id) {
		if (hostLobby != null) return;
		if (joinedLobby != null) {
			try {
				await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, id);
			} catch (LobbyServiceException e) {
				print(e);
			}
		}
	}
	public async void LeaveLobby(string id, bool kicked = false) {
		LeaveLobbyBegin?.Invoke();
		if (joinedLobby != null) {
			try {
				await SubscribeToLobbyEvents(false);
				if (id == joinedLobby.HostId) {
					await DeleteLobby();
				} else {
					if (!kicked) await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, id);
				}
				joinedLobby = null;
				hostLobby = null;
				LeaveLobbySuccess?.Invoke();
			} catch (LobbyServiceException e) {
				LeaveLobbyFailure?.Invoke(e);
				print(e);
			}
		}
	}
	public async Task DeleteLobby() {
		DeleteLobbyBegin?.Invoke();
		try {
			if (hostLobby != null) {
				await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
			}
			DeleteLobbySuccess?.Invoke();
		} catch (LobbyServiceException e) {
			print(e);
			DeleteLobbyFailure?.Invoke(e);
		}
	}








	public async void ListLobbies() {
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
			ListLobbyFailure?.Invoke(e);
			print(e);
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
			print(e);
		}
	}





	#region Relay
	public async Task<Allocation> AllocateRelay(int playerCount, bool hosting = true) {
		try {
			//you can set region in this allocation as well if you want (it does happen automatically as well)
			//set the player count -1 as it does not include hosts.
			int relayInputPlayerCount = hosting ? playerCount - 1 : playerCount;
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(relayInputPlayerCount);
			return allocation;
		} catch (RelayServiceException e) {
			RelayFailure?.Invoke(e);
			print(e);
			return default;
		}
	}
	public async Task<string> GetRelayCode(Allocation allocation) {
		try {
			string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			return relayCode;
		} catch (RelayServiceException e) {
			RelayFailure?.Invoke(e);
			print(e);
			return default;
		}
	}
	public async Task<JoinAllocation> JoinRelay(string relayCode) {
		try {
			JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
			return allocation;
		} catch (RelayServiceException e) {
			print(e);
			RelayFailure?.Invoke(e);
			return default;
		}
	}
	#endregion
}



public struct PlayerData {
	public ulong ClientID;
	public string LobbyID;
	public string Name;
	public PlayerData(ulong clientID, string lobbyID, string name) {
		this.ClientID = clientID;
		this.LobbyID = lobbyID;
		this.Name = name;
	}
}