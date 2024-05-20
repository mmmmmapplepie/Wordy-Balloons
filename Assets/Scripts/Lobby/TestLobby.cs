using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class TestLobby : MonoBehaviour {
	//use for lobby data keys
	const string RelayCode = "RelayCode";


	void Start() {
		AuthenticationService.Instance.SignedIn += PlayerSignedIn;


		Authentication();
	}
	void PlayerSignedIn() {
		//enable the buttons etc for making lobby etc
	}


	string playerName = "PlayerName";
	string playerID;
	async void Authentication(string name = null) {
		if (UnityServices.State == ServicesInitializationState.Uninitialized) {
			InitializationOptions options = new InitializationOptions();
			if (name == null) playerName = "Player" + Random.Range(0, 10000);
			options.SetProfile(playerName);
			await UnityServices.InitializeAsync(options);
		}
		// you can use another sign in method if you wish.
		if (!AuthenticationService.Instance.IsSignedIn) {
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			playerID = AuthenticationService.Instance.PlayerId;
		}
	}




	Lobby hostLobby;
	Lobby joinedLobby;
	float heartBeatElapsed = 0, heartBeatPeriod = 15f;
	float updateElapsed = 0, updatePeriod = 2f;
	void Update() {
		LobbyHeartbeat();
		LobbyPull();
	}
	async void LobbyHeartbeat() {
		//lobbies becomes inactive in ~30 secs and "new" players not in lobby wont be able to find it. So use this to continuously ping the lobby so that it stays alive in the lobbiesList kinda stuff.
		if (hostLobby == null) return;
		if (heartBeatElapsed > heartBeatPeriod) {
			heartBeatElapsed = 0f;
			try {
				await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
			} catch (LobbyServiceException e) {
				print(e);
			}
		} else {
			heartBeatElapsed += Time.deltaTime;
		}
	}


	//lobby does not update in real time so you have to pull for updates separately for lobby. each service etc has different limits for how many Pulls can be made per second.
	async void LobbyPull() {
		//updating lobby changes
		if (joinedLobby == null) return;
		if (updateElapsed > updatePeriod) {
			updateElapsed = 0f;
			try {
				//if player is kicked the return value is null (apparently)
				joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
				if (joinedLobby != null) HostSetting(joinedLobby.HostId == AuthenticationService.Instance.PlayerId);
			} catch (LobbyServiceException e) {
				print(e);
			}
		} else {
			updateElapsed += Time.deltaTime;
		}
	}
	void HostSetting(bool isHost) {
		//if host then enable buttons etc matching that for the host.
	}

	public async void CreateLobby() {
		try {
			//setting lobby details && creating lobby
			string lobbyName = "Lobby Name";
			int maxPlayers = 4;

			CreateLobbyOptions lobbyDetails = new CreateLobbyOptions {
				IsPrivate = true,
				//passing in more details about the player; in this case the player name
				Player = GetNewPlayer(),
				//lobby data ie gamemode kinda thing
				Data = new Dictionary<string, DataObject> {
					{"GameMode", new DataObject(DataObject.VisibilityOptions.Public, "CaptureTheFlag", DataObject.IndexOptions.S1)}
				}
			};
			hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyDetails);

			Allocation relayAlloc = await AllocateRelay(maxPlayers);
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(relayAlloc, "dtls"));
			string relayCode = await GetRelayCode(relayAlloc);

			hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				Data = new Dictionary<string, DataObject> {
					{RelayCode, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
				}
			});
			HostSetting(true);

			joinedLobby = hostLobby;
			NetworkManager.Singleton.StartHost();
		} catch (LobbyServiceException e) {
			print(e);
		}
	}

	#region relay
	async Task<Allocation> AllocateRelay(int playerCount, bool hosting = true) {
		try {
			//you can set region in this allocation as well if you want (it does happen automatically as well)
			//set the player count -1 as it does not include hosts.
			int relayInputPlayerCount = hosting ? playerCount - 1 : playerCount;
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(relayInputPlayerCount);
			return allocation;
		} catch (RelayServiceException e) {
			print(e);
			return default;
		}
	}
	async Task<string> GetRelayCode(Allocation allocation) {
		try {
			string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			return relayCode;
		} catch (RelayServiceException e) {
			print(e);
			return default;
		}
	}
	async Task<JoinAllocation> JoinRelay(string relayCode) {
		try {
			JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
			return allocation;
		} catch (RelayServiceException e) {
			print(e);
			return default;
		}
	}

	#endregion

	public async void ListLobbies() {
		try {
			//filter for lobby search
			QueryLobbiesOptions filter = new QueryLobbiesOptions {
				//number of lobbies to show
				Count = 25,

				Filters = new List<QueryFilter> {
					//QueryFilter.OpOptions.GT mean GT==>Greater-Than, EQ==>Equal, etcetc.
					new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
					new QueryFilter(QueryFilter.FieldOptions.S1, "CaptureTheFlag", QueryFilter.OpOptions.EQ)
					},
				Order = new List<QueryOrder> {
					new QueryOrder(false, QueryOrder.FieldOptions.Created)
				}
			};

			QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(filter);
			foreach (Lobby lobby in response.Results) {
				DisplayLobby(lobby);
			}
		} catch (LobbyServiceException e) {
			print(e);
		}
	}
	void DisplayLobby(Lobby lobby) {

	}

	public async void JoinLobbyByID(string lobbyID) {
		try {
			JoinLobbyByIdOptions options = new JoinLobbyByIdOptions {
				Player = GetNewPlayer()
			};
			QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync();

			joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyID, options);

			string relayCode = joinedLobby.Data[RelayCode].Value;
			JoinAllocation joinRelayAlloc = await JoinRelay(relayCode);
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinRelayAlloc, "dtls"));
			HostSetting(false);
			NetworkManager.Singleton.StartClient();
		} catch (LobbyServiceException e) {
			print(e);
		}
	}
	public async void JoinLobbyByCode(string code) {
		try {
			//what dis for??
			JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions {
				Player = GetNewPlayer()
			};

			//join by code, code can be found by:
			// Lobby.LobbyCode;
			// joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, options);

			joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, options);

			string relayCode = joinedLobby.Data[RelayCode].Value;
			JoinAllocation joinRelayAlloc = await JoinRelay(relayCode);
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinRelayAlloc, "dtls"));
			HostSetting(false);
			NetworkManager.Singleton.StartClient();
		} catch (LobbyServiceException e) {
			print(e);
		}
	}

	Player GetNewPlayer() {
		return new Player {
			Data = new Dictionary<string, PlayerDataObject>{
						{"PlayerName",  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName )}
					}
		};
	}


	//just join any random public lobby.
	public async void QuickJoinLobby() {
		try {
			//quickjoin also has filter options.
			QuickJoinLobbyOptions options = new QuickJoinLobbyOptions {
				Player = GetNewPlayer()
			};
			joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

			string relayCode = joinedLobby.Data[RelayCode].Value;
			JoinAllocation joinRelayAlloc = await JoinRelay(relayCode);
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinRelayAlloc, "dtls"));
			HostSetting(false);
			NetworkManager.Singleton.StartClient();
		} catch (LobbyServiceException e) {
			print(e);
		}
	}

	public async void ChangeLobbyData(string gameMode) {
		try {
			Dictionary<string, DataObject> data = hostLobby.Data;
			data["GameMode"] = new DataObject(DataObject.VisibilityOptions.Public, gameMode, DataObject.IndexOptions.S1);
			hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				//it does not update in real time so you have to pull for updates separately for lobby. each service etc has different limits for how many Pulls can be made per second.
				Data = data
			});
			joinedLobby = hostLobby;

		} catch (LobbyServiceException e) {
			print(e);
		}
	}

	public async void UpdatePlayerData() {
		try {
			string name = "NewName";
			playerName = name;
			await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
				Data = new Dictionary<string, PlayerDataObject>{
						{"PlayerName",  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name)}
					}
			});
		} catch (LobbyServiceException e) {
			print(e);
		}
	}

	// id should be the value of: AuthenticationService.Instance.PlayerId;
	public async void LeaveLobby(string id) {
		try {
			//as it takes the playerID to remove from lobby, the same function can be used to kick players from the lobby (as host)
			//generally a host leaving will make 1 of the remaining people host on random.
			await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, id);
			if (id == AuthenticationService.Instance.PlayerId) {
				//stop networkmanager and relay?
			}
			ShutDownNetwork();
			joinedLobby = null;
			hostLobby = null;
		} catch (LobbyServiceException e) {
			print(e);
		}
	}

	// NGO host migration is not really a thing. so you might wanna avoid dis.
	public async void ChangeLobbyHost(string id) {
		try {
			hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
				HostId = id
			});
			hostLobby = null;
			//the other player will have to "pull" the changes i think
			HostSetting(false);
			//have to change server host?
		} catch (LobbyServiceException e) {
			print(e);
		}
	}
	public async void DeleteLobby(bool stopNetwork = false) {
		try {
			if (hostLobby != null) await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
			joinedLobby = null;
			hostLobby = null;
			if (stopNetwork) ShutDownNetwork();
		} catch (LobbyServiceException e) {
			print(e);
		}
	}
	void ShutDownNetwork() {
		if (!NetworkManager.Singleton.ShutdownInProgress) {
			NetworkManager.Singleton.Shutdown();
		}
	}
}
