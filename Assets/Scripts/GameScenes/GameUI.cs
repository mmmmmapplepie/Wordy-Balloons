using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour {



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
	public AudioClip tickAudio, startAudio;
	void ChangeCountDown(int val) {
		if (val > 0) AudioPlayer.PlayOneShot_Static(tickAudio, 0.2f);
		countdownTxt.text = val.ToString();
	}
	void GameStart() {
		AudioPlayer.PlayOneShot_Static(startAudio, 0.5f);
		countdownTxt.transform.parent.gameObject.SetActive(false);
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
		if (GameStateManager.CurrGameResult == GameStateManager.GameResult.Undecided) {
			SaveData?.Invoke();
		}
		NetworkManager.Singleton.Shutdown();
		SceneManager.LoadScene(s, LoadSceneMode.Single);
	}
	public static event System.Action SaveData;


	#endregion



	#region Game Finish
	[Header("Game Finish")] public GameObject menuBtn;
	public GameObject connectionLost, victoryPanel, defeatPanel, endingPanel, gameplayUI;
	void GameResultSet(GameStateManager.GameResult result) {
		menuBtn.SetActive(false);
		gameplayUI.SetActive(false);
		if (result == GameStateManager.GameResult.Draw) {
			menusPanel.SetActive(true);
			connectionLost.SetActive(true);
			return;
		}
		StartCoroutine(DelayedUIShow(result));
	}
	IEnumerator DelayedUIShow(GameStateManager.GameResult result) {
		yield return new WaitForSeconds(BaseManager.BaseDestroyAnimationTime);
		endingPanel.SetActive(true);
		menusPanel.SetActive(true);
		DisplayTeamWinning(result == GameStateManager.GameResult.Team1Win ? Team.t1 : Team.t2);
	}



	void DisplayTeamWinning(Team t) {
		bool victory = t == BalloonManager.team;
		if (victory) {
			victoryPanel.SetActive(true);
			endingPanel.GetComponent<Animator>().Play("Victory");
		} else {
			defeatPanel.SetActive(true);
			endingPanel.GetComponent<Animator>().Play("Defeat");
		}
	}




	#endregion





}
