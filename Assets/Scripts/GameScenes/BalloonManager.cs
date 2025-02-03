using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BalloonManager : NetworkBehaviour {


	void Start() {
		InputManager.CorrectInputProcess += SpawnBalloon;
		//subscribe to successfull fire 
	}

	public static int teamNumber = 1;
	public static List<ulong> teamIDs = new List<ulong>();
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		teamNumber = GameData.team1IDList.Contains(NetworkManager.Singleton.LocalClientId) ? 1 : 2;
		List<ulong> associatedTeamID = teamNumber == 1 ? GameData.team1IDList : GameData.team2IDList;
		foreach (ulong id in associatedTeamID) {
			teamIDs.Add(id);
		}
	}

	public override void OnDestroy() {
		base.OnDestroy();
		InputManager.CorrectInputProcess += SpawnBalloon;
		//subscribe to successfull fire 


	}



	public void SpawnBalloon(string word, ulong teamID) {
		SpawnBalloonServerRpc(teamID, word.Length);
	}

	public GameObject balloonPrefab;
	public List<Color> colorList;
	public Transform BalloonHolder;
	[ServerRpc(RequireOwnership = false)]
	void SpawnBalloonServerRpc(ulong teamID, int count) {
		GameObject newBalloon = NetworkBehaviour.Instantiate(balloonPrefab, BalloonHolder);
		Balloon script = newBalloon.GetComponent<Balloon>();
		script.flyProgress.Value = 0f;
		script.power.Value = count;
		script.balloonTeamID.Value = teamID;
		script.balloonColor.Value = colorList[GameData.ClientID_KEY_ColorIndex_VAL[teamID]];
		newBalloon.GetComponent<NetworkObject>().Spawn();
	}




}
