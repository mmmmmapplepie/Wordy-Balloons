using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour {
	public Sound menuBGM, battleBGM;
	static BGMManager instance;
	void Start() {
		if (instance != null) {
			Destroy(gameObject); return;
		}
		AudioPlayer.Instance.AddNewSound(menuBGM);
		AudioPlayer.Instance.AddNewSound(battleBGM);

		SceneManager.sceneLoaded += SceneLoaded;
		AudioPlayer.Instance.PlaySound(menuBGM.Name);

		if (instance == null) {
			instance = this;
			DontDestroyOnLoad(gameObject.transform.root.gameObject);
		}
	}
	void OnDestroy() {
		SceneManager.sceneLoaded -= SceneLoaded;
	}


	void SceneLoaded(Scene scene, LoadSceneMode mode) {
		if (scene.name == "MainMenu" || scene.name == "LobbyScene") {
			if (AudioPlayer.Instance.CheckPlaying(battleBGM.Name)) AudioPlayer.Instance.StopSound(battleBGM.Name, 0.5f);
			if (!AudioPlayer.Instance.CheckPlaying(menuBGM.Name)) { AudioPlayer.Instance.PlaySound(menuBGM.Name, 1f); }
		} else if (scene.name == "MultiplayerGameScene") {
			if (AudioPlayer.Instance.CheckPlaying(menuBGM.Name)) AudioPlayer.Instance.StopSound(menuBGM.Name);
			if (!AudioPlayer.Instance.CheckPlaying(battleBGM.Name)) AudioPlayer.Instance.PlaySound(battleBGM.Name, 1f);
		}
	}
}
