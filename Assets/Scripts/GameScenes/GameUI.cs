using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using WebSocketSharp;

public class GameUI : MonoBehaviour {
  void Awake() {
    ExitGamePressed = false;
    GameData.GamePaused = false;
  }

  void Start() {
    SetupChangeSceneOption();
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
  public AudioClip tickAudio, startAudio;
  void ChangeCountDown(int val) {
    if (val > 0) AudioPlayer.PlayOneShot_Static(tickAudio, VolumeControl.GetEffectVol() * 0.2f);
    countdownTxt.text = val.ToString();
  }
  void GameStart() {
    AudioPlayer.PlayOneShot_Static(startAudio, 0.5f * VolumeControl.GetEffectVol());
    countdownTxt.transform.parent.gameObject.SetActive(false);
  }


  [Header("Word meaning")] public TextMeshProUGUI wordExplanationTxt;
  void NewText(DictionaryEntry entry) {
    int explanations = entry.description.Count;
    wordExplanationTxt.text = entry.description[Random.Range(0, explanations)];
  }


  #region Menus btn
  void SetupChangeSceneOption() {
    if (GameData.PlayMode == PlayModeEnum.Multiplayer) return;
    if (GameData.PlayMode == PlayModeEnum.Tutorial) {
      moveSceneBtnTxt.transform.parent.gameObject.SetActive(false);
      return;
    }
    moveSceneBtnTxt.text = "Single Player Menu";
    targetSceneName = "SinglePlayer";
  }

  string targetSceneName = "LobbyScene";

  public TextMeshProUGUI moveSceneBtnTxt;
  [Header("Menu")] public GameObject menusPanel;
  public GameObject guidePanel, gameNotPausedTxt;
  public void ToggleMenu() {
    menusPanel.SetActive(!menusPanel.activeInHierarchy);
    guidePanel.SetActive(false);

    if (GameData.PlayMode != PlayModeEnum.Multiplayer) {
      Time.timeScale = menusPanel.activeInHierarchy ? 0 : 1f;
      gameNotPausedTxt.SetActive(false);
      GameData.GamePaused = menusPanel.activeInHierarchy;
    }
  }
  public void ToggleGuide() {
    guidePanel.SetActive(!guidePanel.activeInHierarchy);
  }
  public static bool ExitGamePressed = false;
  public void GoToScene(string s) {
    ExitGamePressed = true;
    if (GameStateManager.CurrGameState == GameState.InPlay) {
      SaveData?.Invoke();
    }
    NetworkManager.Singleton.Shutdown();
    SceneManagerAsync.Singleton.LoadSceneAsync(s.IsNullOrEmpty() == true ? targetSceneName : s);
  }
  public static event System.Action SaveData;


  #endregion



  #region Game Finish
  [Header("Game Finish")] public GameObject menuBtn;
  public GameObject drawPanel, victoryPanel, defeatPanel, disconnectPanel, endingPanel, gameplayUI;
  void GameResultSet(GameState result) {
    if (result == GameState.InPlay) return;
    gameNotPausedTxt.transform.parent.gameObject.SetActive(false);
    gameplayUI.SetActive(false);
    menuBtn.SetActive(false);
    menusPanel.SetActive(false);
    // print(result);
    StartCoroutine(DelayedUIShow(result));
  }
  IEnumerator DelayedUIShow(GameState result) {
    if (result != GameState.Disconnect) yield return new WaitForSeconds(BaseManager.BaseDestroyAnimationTime);
    endingPanel.SetActive(true);
    menusPanel.SetActive(true);
    gameNotPausedTxt.transform.parent.gameObject.SetActive(false);

    if (result == GameState.Disconnect) {
      disconnectPanel.SetActive(true);
      endingPanel.GetComponent<EndingPanelUI>().PlayDraw();
      yield break;
    } else if (result == GameState.Draw) {
      drawPanel.SetActive(true);
      endingPanel.GetComponent<Animator>().Play("Draw");
      yield break;
    } else {
      DisplayTeamWinning(result == GameState.Team1Win ? Team.t1 : Team.t2);
    }
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




