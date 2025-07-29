using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : NetworkBehaviour {
	void Awake() {
		CountdownFinished = false;
		CurrGameResult = GameResult.Undecided;
	}
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		NetworkManager.SceneManager.OnLoadEventCompleted += SceneLoadedForAll;
		countDown_NV.OnValueChanged += CountDownChanged;

		BaseManager.TeamLose += TeamLoss;

		IngameNetcodeAndSceneManager.DisconnectingEvent += Disconnected;
		IngameNetcodeAndSceneManager.TeamEmptyEvent += TeamEmpty;
	}
	public override void OnNetworkDespawn() {
		if (NetworkManager.SceneManager != null) NetworkManager.SceneManager.OnLoadEventCompleted -= SceneLoadedForAll;
		countDown_NV.OnValueChanged -= CountDownChanged;

		BaseManager.TeamLose -= TeamLoss;

		IngameNetcodeAndSceneManager.DisconnectingEvent -= Disconnected;
		IngameNetcodeAndSceneManager.TeamEmptyEvent -= TeamEmpty;
		base.OnNetworkDespawn();
	}

	const int countDownTime = 3;
	private void SceneLoadedForAll(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
		if (NetworkManager.Singleton.IsServer) {
			StartCoroutine(StartCountDown());
		}
	}

	NetworkVariable<int> countDown_NV = new NetworkVariable<int>(countDownTime + 1);
	IEnumerator StartCountDown() {
		if (GameStateManager.CurrGameResult != GameResult.Undecided) yield break;
		yield return new WaitForSeconds(0.3f);
		int t = countDownTime;
		// countDown_NV.Value = 0;
		while (t > 0) {
			if (GameStateManager.CurrGameResult != GameResult.Undecided) yield break;
			countDown_NV.Value = t;
			t--;
			yield return new WaitForSeconds(1);
		}
		countDown_NV.Value = 0;
	}
	public static event System.Action<int> countDownChanged;
	public static event System.Action GameStartEvent;
	public static event System.Action<GameResult> GameResultSetEvent;
	public static bool CountdownFinished { get; private set; }
	public static GameResult CurrGameResult;
	void CountDownChanged(int prev, int newVal) {
		if (CurrGameResult != GameResult.Undecided) {
			StopAllCoroutines();
			return;
		}
		countDownChanged?.Invoke(newVal);
		if (newVal == 0) {
			CountdownFinished = true;
			GameStartEvent?.Invoke();
		}
	}
	void Update() {
		if (CountdownFinished == false && countDown_NV.Value == 0 && IsGameRunning()) {
			CountdownFinished = true;
			GameStartEvent?.Invoke();
		}
	}
	public static bool IsGameRunning() {
		return CurrGameResult == GameResult.Undecided && !GameData.GamePaused;
	}

	void TeamLoss(Team? t) {
		if (t == null) GameResultSetClientRpc(GameResult.Draw);
		GameResult r = GameResult.Team1Win;
		if (t == Team.t1) r = GameResult.Team2Win;
		GameResultSetClientRpc(r);
	}

	[ClientRpc]
	void GameResultSetClientRpc(GameResult r) {
		print("Game Set from RPC: " + r.ToString());
		SetGameResult(r);
	}
	void SetGameResult(GameResult r) {
		if (CurrGameResult != GameResult.Undecided) return;
		CurrGameResult = r;
		GameResultSetEvent?.Invoke(r);
	}


	void Disconnected() {
		SetGameResult(GameResult.Disconnect);
	}

	void TeamEmpty(Team nonEmptyTeam) {
		if (nonEmptyTeam == Team.t1) {
			GameResultSetClientRpc(GameResult.Team1Win);
		} else {
			GameResultSetClientRpc(GameResult.Team2Win);
		}
	}
}


