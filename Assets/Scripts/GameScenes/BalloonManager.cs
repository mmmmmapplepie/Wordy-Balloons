using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BalloonManager : NetworkBehaviour {

	public Transform balloonStartPosition;
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
		InputManager.CorrectEntryProcess -= SpawnBalloon;
		base.OnDestroy();
	}

	public void SpawnBalloon(int length, ulong teamID) {
		StartCoroutine(SpawnBalloonWithDelay(length, teamID));
	}
	public void SpawnBalloon(string word, ulong teamID) {
		SpawnBalloon(word.Length, teamID);
	}
	public static event System.Action<int, ulong> BalloonSpawned;
	IEnumerator SpawnBalloonWithDelay(int count, ulong teamID) {
		yield return new WaitForSeconds(TypedBalloonAnimations.animationTime);
		if (!GameStateManager.IsGameRunning()) yield break;
		SpawnBalloonServerRpc(teamID, count);
	}

	public GameObject balloonPrefab;
	public Transform BalloonHolder;
	[ServerRpc(RequireOwnership = false)]
	void SpawnBalloonServerRpc(ulong teamID, int count) {
		GameObject newBalloon = NetworkBehaviour.Instantiate(balloonPrefab, BalloonHolder);
		Balloon script = newBalloon.GetComponent<Balloon>();
		script.startPos = balloonStartPosition.position;
		script.endPos = -balloonStartPosition.position;
		script.tempPower = count;
		script.tempTeam = GameData.GetTeamFromClientID(teamID);
		script.tempColor = GameData.allColorOptions[GameData.ClientID_KEY_ColorIndex_VAL[teamID]];
		newBalloon.GetComponent<NetworkObject>().Spawn();
		BalloonSpawned?.Invoke(count, teamID);
	}




}
