using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;

public class MainMenuUI : MonoBehaviour {
#if UNITY_EDITOR
	public void GoToScene(UnityEditor.SceneAsset scene) {
		SceneManager.LoadScene(scene.name);
	}
#endif

	void Start() {
		// if (!PlayerPrefs.HasKey(TutorialManager.TutorialClearedPlayerPrefKey)) {
		LoadTutorial();
		// }
	}
	public void GoToScene(string name) {
		if (SceneManager.GetSceneByName(name) == null) return;
		SceneManager.LoadScene(name);
	}

	public void ActivateObject(GameObject o) {
		o.SetActive(true);
	}
	public void DeactivateObject(GameObject o) {
		o.SetActive(false);
	}

	public void CloseGame() {
		Application.Quit();
	}
	public GameObject creditPanel;
	public void OpenCredits(bool open) {
		creditPanel.SetActive(open);
	}


	public void LoadTutorial() {
		StartCoroutine(StartGameRoutine());
	}

	public SliderToggle dictionaryToggle;
	void ShutdownNetwork() {
		if (NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.ShutdownInProgress)
			NetworkManager.Singleton.Shutdown();
	}

	IEnumerator StartGameRoutine() {
		while (NetworkManager.Singleton.IsConnectedClient) {
			ShutdownNetwork();
			yield return null;
		}

		GameData.gameMode = GameMode.Normal;

		FindObjectOfType<UnityTransport>().SetConnectionData("127.0.0.1", 7777);

		NetworkManager.Singleton.StartHost();

		ulong selfID = NetworkManager.Singleton.LocalClientId;
		ulong computerID = selfID + 1;

		List<Color> colors = new List<Color>() { Color.cyan, Color.red };
		GameData.allColorOptions = colors;

		GameData.ClientID_KEY_ColorIndex_VAL.Clear();
		GameData.ClientID_KEY_LobbyID_VAL.Clear();
		GameData.ClientID_KEY_ColorIndex_VAL.Add(selfID, 0);
		GameData.ClientID_KEY_ColorIndex_VAL.Add(computerID, 1);

		GameData.team1.Clear();
		GameData.team1.Add(selfID);
		GameData.team2.Clear();
		GameData.team2.Add(computerID);

		GameData.PlayMode = PlayModeEnum.Tutorial;

		NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
	}
}
