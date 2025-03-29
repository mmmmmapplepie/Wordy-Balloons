using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : NetworkBehaviour {
	void Awake() {
		GameRunning = GameData.InSinglePlayerMode;
		CurrGameResult = GameResult.Undecided;
	}
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		NetworkManager.SceneManager.OnLoadEventCompleted += SceneLoadedForAll;
		countDown_NV.OnValueChanged += CountDownChanged;

		BaseManager.TeamLose += TeamLoss;

		IngameNetcodeAndSceneManager.GameResultChangeByConnection += SetGameResult;
	}
	public override void OnNetworkDespawn() {
		if (NetworkManager.SceneManager != null) NetworkManager.SceneManager.OnLoadEventCompleted -= SceneLoadedForAll;
		countDown_NV.OnValueChanged -= CountDownChanged;

		BaseManager.TeamLose -= TeamLoss;

		IngameNetcodeAndSceneManager.GameResultChangeByConnection -= SetGameResult;
		base.OnNetworkDespawn();
	}

	float countDownTime = 3;
	// float countDownTime = 0.1f;
	private void SceneLoadedForAll(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
		if (NetworkManager.Singleton.IsServer) StartCoroutine(StartCountDown());
	}

	NetworkVariable<int> countDown_NV = new NetworkVariable<int>(0);
	IEnumerator StartCountDown() {
		if (GameStateManager.CurrGameResult != GameStateManager.GameResult.Undecided) yield break;
		float t = countDownTime;
		countDown_NV.Value = Mathf.CeilToInt(t);
		while (t > 0f) {
			t -= Time.deltaTime;
			if (GameStateManager.CurrGameResult != GameStateManager.GameResult.Undecided) yield break;
			countDown_NV.Value = Mathf.CeilToInt(t);
			yield return null;
		}
		countDown_NV.Value = 0;
	}
	public static event System.Action<int> countDownChanged;
	public static event System.Action GameStartEvent;
	public static event System.Action<GameResult> GameResultSetEvent;
	public static bool GameRunning { get; private set; }
	public static GameResult CurrGameResult;
	void CountDownChanged(int prev, int newVal) {
		if (CurrGameResult != GameResult.Undecided) {
			StopAllCoroutines();
			return;
		}
		countDownChanged?.Invoke(newVal);
		if (newVal == 0) {
			GameRunning = true;
			GameStartEvent?.Invoke();
		}
	}

	public static bool IsGameRunning() {
		return CurrGameResult == GameResult.Undecided && Time.timeScale != 0;
	}

	void TeamLoss(Team t) {
		GameResult r = GameResult.Team1Win;
		if (t == Team.t1) r = GameResult.Team2Win;
		GameResultSetClientRpc(r);
	}

	[ClientRpc]
	void GameResultSetClientRpc(GameResult r) {
		SetGameResult(r);
	}
	void SetGameResult(GameResult r) {
		if (CurrGameResult != GameResult.Undecided) return;
		CurrGameResult = r;
		GameResultSetEvent?.Invoke(r);
		IngameNetcodeAndSceneManager.ShutDownNetwork();
		GameRunning = false;
	}


	public enum GameResult { Undecided, Team1Win, Team2Win, Draw }
}
