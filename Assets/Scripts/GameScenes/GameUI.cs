using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour {
	void Start() {
		AudioPlayer.Instance.AddNewSound(victorySound);
		AudioPlayer.Instance.AddNewSound(defeatSound);
	}


	void OnEnable() {
		GameStateManager.countDownChanged += ChangeCountDown;
		GameStateManager.GameStartEvent += GameStart;

		InputManager.NewWordChosen += NewText;

		GameStateManager.GameResultSetEvent += GameResultSet;

	}
	void OnDisable() {
		GameStateManager.countDownChanged -= ChangeCountDown;
		GameStateManager.GameStartEvent -= GameStart;

		InputManager.NewWordChosen -= NewText;

		GameStateManager.GameResultSetEvent -= GameResultSet;

		Time.timeScale = 1;

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
	[Header("Game Finish")] public GameObject menuBtn;
	public GameObject connectionLost, victoryPanel, defeatPanel, endingPanel, gameplayUI;
	public Sound victorySound, defeatSound;
	void GameResultSet(GameStateManager.GameResult result) {
		if (result == GameStateManager.GameResult.Draw) {
			connectionLost.SetActive(true);
			return;
		}
		menuBtn.SetActive(false);
		gameplayUI.SetActive(false);
		StartCoroutine(DelayedUIShow(result));
	}
	IEnumerator DelayedUIShow(GameStateManager.GameResult result) {
		yield return new WaitForSeconds(BaseManager.BaseDestroyAnimationTime);
		DisplayTeamWinning(result == GameStateManager.GameResult.Team1Win ? Team.t1 : Team.t2);
		endingPanel.SetActive(true);
		menusPanel.SetActive(true);
	}



	void DisplayTeamWinning(Team t) {
		bool victory = t == BalloonManager.team;
		if (victory) {
			victoryPanel.SetActive(true);
			AudioPlayer.Instance.PlaySound(victorySound.Name);
		} else {
			defeatPanel.SetActive(true);
			AudioPlayer.Instance.PlaySound(defeatSound.Name);
		}
	}


	#endregion





}
