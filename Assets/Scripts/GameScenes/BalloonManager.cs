using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BalloonManager : NetworkBehaviour {


	void Start() {
		InputManager.CreateBalloon += SpawnBalloon;
		//subscribe to successfull fire 
	}

	public static List<ulong> teamIDs = new List<ulong>();
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		List<ulong> associatedTeamID = GameData.team1IDList.Contains(NetworkManager.Singleton.LocalClientId) ? GameData.team1IDList : GameData.team2IDList;
		foreach (ulong id in associatedTeamID) {
			teamIDs.Add(id);
		}
	}

	public override void OnDestroy() {
		base.OnDestroy();
		InputManager.CreateBalloon += SpawnBalloon;
		//subscribe to successfull fire 


	}



	public void SpawnBalloon(string word) {
		ulong teamID = NetworkManager.Singleton.LocalClientId;
		print(NetworkManager.Singleton.IsServer);
		SpawnBalloonServerRpc(teamID, word.Length);
	}

	public GameObject balloonPrefab;
	public List<Color> colorList;
	public Transform BalloonHolder;
	[ServerRpc(RequireOwnership = false)]
	void SpawnBalloonServerRpc(ulong teamID, int count) {
		print("rpc Called");
		GameObject newBalloon = NetworkBehaviour.Instantiate(balloonPrefab, BalloonHolder);
		Balloon script = newBalloon.GetComponent<Balloon>();
		script.flyProgress.Value = 0f;
		script.power.Value = count;
		script.balloonTeamID.Value = teamID;
		script.balloonColor.Value = colorList[GameData.ClientID_KEY_ColorIndex_VAL[teamID]];
		newBalloon.GetComponent<NetworkObject>().Spawn();
	}




}
