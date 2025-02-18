using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour {
	void OnEnable() {
		GameStateManager.countDownChanged += ChangeCountDown;
		GameStateManager.GameStartEvent += GameStart;

		IngameNetcodeAndSceneManager.GameResultChange += GameResultChange;

		InputManager.NewWordChosen += NewText;
	}
	void OnDisable() {
		GameStateManager.countDownChanged -= ChangeCountDown;
		GameStateManager.GameStartEvent -= GameStart;

		IngameNetcodeAndSceneManager.GameResultChange -= GameResultChange;

		InputManager.NewWordChosen -= NewText;
	}












	[Header("Game State")] public TextMeshProUGUI countdownTxt;
	void ChangeCountDown(int val) {
		countdownTxt.text = val.ToString();
		countdownTxt.transform.parent.gameObject.SetActive(val == 0 ? false : true);
	}
	void GameStart() {
		//show Game start text. and sound effect.
	}


	[Header("Word meaning")] public TextMeshProUGUI wordExplanationTxt;
	void NewText(DictionaryEntry entry) {
		int explanations = entry.description.Count;
		wordExplanationTxt.text = entry.description[Random.Range(0, explanations)];
	}


	#region Menus btn

	[Header("Menu")] public GameObject menusPanel;
	public GameObject guidePanel, gameNotPausedTxt;
	public void ToggleMenu() {
		menusPanel.SetActive(!menusPanel.activeInHierarchy);
		guidePanel.SetActive(false);

		if (GameData.InSinglePlayerMode) {
			Time.timeScale = menusPanel.activeInHierarchy ? 0 : 1f;
			gameNotPausedTxt.SetActive(false);
		}
	}
	public void ToggleGuide() {
		guidePanel.SetActive(!guidePanel.activeInHierarchy);
	}
	public void GoToScene(string s) {
		NetworkManager.Singleton.Shutdown();
		SceneManager.LoadScene(s);
	}


	#endregion



	#region Game Finish
	[Header("Game Finish")] public TextMeshProUGUI VictoryDefeatText;
	public GameObject menuBtn, ConnectionLost;



	void GameResultChange(GameStateManager.GameResult result) {
		if (GameStateManager.CurrGameResult != GameStateManager.GameResult.Undecided) return;
		if (result == GameStateManager.GameResult.Draw) {
			ConnectionLost.SetActive(true);
		} else {
			VictoryDefeatText.text = result.ToString();
			VictoryDefeatText.transform.parent.gameObject.SetActive(true);
		}
		menusPanel.SetActive(true);
		menuBtn.SetActive(false);
	}




	#endregion





}
