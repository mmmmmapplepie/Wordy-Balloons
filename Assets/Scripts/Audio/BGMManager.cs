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
		VolumeControl.VolumeChanged += VolumeChanged;
		AudioPlayer.Instance.PlaySound(menuBGM.Name, VolumeControl.GetBGMVol());
		currBGM = menuBGM.Name;

		if (instance == null) {
			instance = this;
			DontDestroyOnLoad(gameObject.transform.root.gameObject);
		}
	}
	void OnDestroy() {
		SceneManager.sceneLoaded -= SceneLoaded;
		VolumeControl.VolumeChanged -= VolumeChanged;
	}
	void VolumeChanged() {
		string sceneName = SceneManager.GetActiveScene().name;
		AudioPlayer.Instance.SetVolume(currBGM, VolumeControl.GetBGMVol());
	}
	string currBGM;


	void SceneLoaded(Scene scene, LoadSceneMode mode) {
		if (scene.name == "MainMenu" || scene.name == "LobbyScene" || scene.name == "SinglePlayer") {
			if (AudioPlayer.Instance.IsPlaying(battleBGM.Name)) AudioPlayer.Instance.StopSound(battleBGM.Name, 0.5f);
			if (!AudioPlayer.Instance.IsPlaying(menuBGM.Name)) { AudioPlayer.Instance.PlaySound(menuBGM.Name, VolumeControl.GetBGMVol(), 1f); currBGM = menuBGM.Name; }
		} else if (scene.name == "MultiplayerGameScene") {
			if (AudioPlayer.Instance.IsPlaying(menuBGM.Name)) AudioPlayer.Instance.StopSound(menuBGM.Name);
			if (!AudioPlayer.Instance.IsPlaying(battleBGM.Name)) { AudioPlayer.Instance.PlaySound(battleBGM.Name, VolumeControl.GetBGMVol(), 1f); currBGM = battleBGM.Name; }
		}
	}
}
