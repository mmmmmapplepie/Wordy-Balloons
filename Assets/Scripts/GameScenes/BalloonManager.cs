using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BalloonManager : NetworkBehaviour {


	void Start() {
		InputManager.CorrectEntryProcess += SpawnBalloon;
	}

	public static Team team = Team.t1;
	public static HashSet<ulong> teamIDs = new HashSet<ulong>();
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		team = GameData.GetTeamFromClientID(NetworkManager.Singleton.LocalClientId);
		HashSet<ulong> associatedTeamID = team == Team.t1 ? GameData.team1 : GameData.team2;
		foreach (ulong id in associatedTeamID) {
			teamIDs.Add(id);
		}
	}


	public override void OnDestroy() {
		base.OnDestroy();
		InputManager.CorrectEntryProcess -= SpawnBalloon;
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
		script.balloonTeam.Value = GameData.GetTeamFromClientID(teamID);
		script.balloonColor.Value = colorList[GameData.ClientID_KEY_ColorIndex_VAL[teamID]];
		newBalloon.GetComponent<NetworkObject>().Spawn();
	}




}
