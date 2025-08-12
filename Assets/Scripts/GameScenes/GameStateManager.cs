using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : NetworkBehaviour {
	void Awake() {
		CountdownFinished = false;
		CurrGameState = GameState.Countdown;
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
		if (GameStateManager.CurrGameState != GameState.Countdown) yield break;
		yield return new WaitForSeconds(0.3f);
		int t = countDownTime;
		// countDown_NV.Value = 0;
		while (t > 0) {
			if (GameStateManager.CurrGameState != GameState.Countdown) yield break;
			countDown_NV.Value = t;
			t--;
			yield return new WaitForSeconds(1);
		}
		countDown_NV.Value = 0;
	}
	public static event System.Action<int> countDownChanged;
	public static event System.Action GameStartEvent;
	public static event System.Action<GameState> GameResultSetEvent;
	public static bool CountdownFinished { get; private set; }
	public static GameState CurrGameState;
	void CountDownChanged(int prev, int newVal) {
		if (CurrGameState != GameState.Countdown) {
			StopAllCoroutines();
			return;
		}
		countDownChanged?.Invoke(newVal);
		if (newVal == 0) {
			CountdownFinished = true;
			GameStateManager.CurrGameState = GameState.InPlay;
			GameStartEvent?.Invoke();
		}
	}
	// void Update() {
	// 	if (CountdownFinished == false && countDown_NV.Value <= 0 && !GameData.GamePaused && CurrGameState == GameState.Countdown) {
	// 		CountdownFinished = true;
	// 		GameStateManager.CurrGameState = GameState.InPlay;
	// 		GameStartEvent?.Invoke();
	// 	}
	// }
	public static bool IsGameRunning() {
		return CurrGameState == GameState.InPlay && !GameData.GamePaused;
	}

	void TeamLoss(Team? t) {
		if (t == null) GameResultSetClientRpc(GameState.Draw);
		GameState r = GameState.Team1Win;
		if (t == Team.t1) r = GameState.Team2Win;
		GameResultSetClientRpc(r);
	}

	[ClientRpc]
	void GameResultSetClientRpc(GameState r) {
		print("Game Set from RPC: " + r.ToString());
		SetGameResult(r);
	}
	void SetGameResult(GameState r) {
		if (CurrGameState != GameState.InPlay && CurrGameState != GameState.Countdown) return;
		CurrGameState = r;
		print("game result set: " + r.ToString());
		GameResultSetEvent?.Invoke(r);
	}


	void Disconnected() {
		SetGameResult(GameState.Disconnect);
	}

	void TeamEmpty(Team nonEmptyTeam) {
		if (nonEmptyTeam == Team.t1) {
			GameResultSetClientRpc(GameState.Team1Win);
		} else {
			GameResultSetClientRpc(GameState.Team2Win);
		}
	}
}


