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
		NetworkManager.SceneManager.OnLoadEventCompleted += SceneLoadedForAll;
		countDown_NV.OnValueChanged += CountDownChanged;

		IngameNetcodeAndSceneManager.GameResultChange += GameResultChange;
	}
	public override void OnNetworkDespawn() {
		if (NetworkManager.SceneManager != null) NetworkManager.SceneManager.OnLoadEventCompleted -= SceneLoadedForAll;
		countDown_NV.OnValueChanged -= CountDownChanged;

		IngameNetcodeAndSceneManager.GameResultChange += GameResultChange;
	}

	float countDownTime = 3;
	// float countDownTime = 0.1f;
	private void SceneLoadedForAll(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
		if (NetworkManager.Singleton.IsServer) StartCoroutine(StartCountDown());
	}

	NetworkVariable<int> countDown_NV = new NetworkVariable<int>(0);
	IEnumerator StartCountDown() {
		float t = countDownTime;
		countDown_NV.Value = Mathf.CeilToInt(t);
		while (t > 0f) {
			t -= Time.deltaTime;
			countDown_NV.Value = Mathf.CeilToInt(t);
			yield return null;
		}
		countDown_NV.Value = 0;
	}
	public static event System.Action<int> countDownChanged;
	public static event System.Action GameStartEvent, GameFinishEvent;
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


	void GameResultChange(GameResult result) {
		GameRunning = false;
	}

	public enum GameResult { Undecided, Team1Win, Team2Win, Draw }
}
