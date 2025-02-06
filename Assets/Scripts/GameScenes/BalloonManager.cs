using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BalloonManager : NetworkBehaviour {


	void Start() {
		InputManager.CorrectInputProcess += SpawnBalloon;
	}

	public static Team team = Team.t1;
	public static List<ulong> teamIDs = new List<ulong>();
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		team = GameData.GetTeamNumber(NetworkManager.Singleton.LocalClientId);
		List<ulong> associatedTeamID = team == Team.t1 ? GameData.team1IDList : GameData.team2IDList;
		foreach (ulong id in associatedTeamID) {
			teamIDs.Add(id);
		}
	}


	public override void OnDestroy() {
		base.OnDestroy();
		InputManager.CorrectInputProcess -= SpawnBalloon;
	}

	public void BallonSpawnBtn(int id) {
		SpawnBalloon(Random.Range(1, 20), (ulong)id);
	}

	public void SpawnBalloon(int wordLength, ulong teamID) {
		SpawnBalloonServerRpc(teamID, wordLength);
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
		script.balloonTeam.Value = GameData.GetTeamNumber(teamID);
		script.balloonColor.Value = colorList[GameData.ClientID_KEY_ColorIndex_VAL[teamID]];
		newBalloon.GetComponent<NetworkObject>().Spawn();
	}




}
