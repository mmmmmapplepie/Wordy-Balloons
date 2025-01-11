using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : NetworkBehaviour {
	public override void OnNetworkSpawn() {
		NetworkManager.SceneManager.OnLoadEventCompleted += SceneLoadedForAll;
		countDown_NV.OnValueChanged += CountDownChanged;

	}
	public override void OnNetworkDespawn() {
		countDown_NV.OnValueChanged -= CountDownChanged;
		if (NetworkManager.SceneManager != null) NetworkManager.SceneManager.OnLoadEventCompleted -= SceneLoadedForAll;
	}

	float countDownTime = 3;
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
	public static bool GameRunning;
	void CountDownChanged(int prev, int newVal) {
		countDownChanged?.Invoke(newVal);
		if (newVal == 0) {
			GameRunning = true;
			GameStartEvent?.Invoke();
		}
	}

}
