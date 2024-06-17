using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : NetworkBehaviour {
	public override void OnNetworkSpawn() {
		NetworkManager.SceneManager.OnLoadEventCompleted += SceneLoadedForAll;
		countDown.OnValueChanged += CountDownChanged;

	}
	public override void OnNetworkDespawn() {
		countDown.OnValueChanged -= CountDownChanged;
		if (NetworkManager.SceneManager != null) NetworkManager.SceneManager.OnLoadEventCompleted -= SceneLoadedForAll;
	}

	float countDownTime = 3;
	private void SceneLoadedForAll(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
		if (NetworkManager.Singleton.IsServer) StartCoroutine(StartCountDown());
	}

	NetworkVariable<int> countDown = new NetworkVariable<int>(0);
	IEnumerator StartCountDown() {
		float t = countDownTime;
		countDown.Value = Mathf.CeilToInt(t);
		while (t > 0f) {
			t -= Time.deltaTime;
			countDown.Value = Mathf.CeilToInt(t);
			yield return null;
		}
		countDown.Value = 0;
	}
	public static event System.Action<int> countDownChanged;
	void CountDownChanged(int prev, int newVal) {
		if (newVal == 0) GameData.GameRunning = true;
		countDownChanged?.Invoke(newVal);
	}

}
