using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SinglePlayerSetup : NetworkBehaviour {
	public Slider speedSlider;
	public TextMeshProUGUI sliderValue;
	void Start() {
		if (PlayerPrefs.HasKey(AISpeed)) {
			SetAISpeed(PlayerPrefs.GetInt(AISpeed));
			speedSlider.Set(PlayerPrefs.GetInt(AISpeed));
		} else {
			speedSlider.Set(SinglePlayerAI.AISpeed, true);
		}
	}

	const string AISpeed = "aiSpeed";
	public void SetAISpeed(float value) {
		SinglePlayerAI.AISpeed = Mathf.RoundToInt(value);
		PlayerPrefs.SetInt(AISpeed, Mathf.RoundToInt(value));
		sliderValue.text = Mathf.RoundToInt(value).ToString();
	}

	public void EnterGameMode(int i) {
		Enum.TryParse(i.ToString(), out GameMode mode);
		EnterWithGameMode(mode);
	}
	public void EnterWithGameMode(GameMode mode) {
		GameData.gameMode = mode;
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

		GameData.Dictionary = dictionaryToggle.onRightSide ? DictionaryMode.Complete : DictionaryMode.Beginner;

		GameData.team1.Clear();
		GameData.team1.Add(selfID);
		GameData.team2.Clear();
		GameData.team2.Add(computerID);

		GameData.PlayMode = PlayModeEnum.BasicPVE;

		NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
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
}
