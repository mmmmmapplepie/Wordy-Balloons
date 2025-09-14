using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AiPlayer : MonoBehaviour {
  Action UpdateMethod;
  void Awake() {
    GameStateManager.GameStartEvent += CheckGameStart;
    GameStateManager.GameResultSetEvent += GameEnd;
    if (GameData.PlayMode == PlayModeEnum.Multiplayer) {
      UpdateMethod = null;
    } else if (GameData.PlayMode == PlayModeEnum.BasicPVE) {
      SetBasicMethod();
    } else if (GameData.PlayMode == PlayModeEnum.Tutorial) {
      UpdateMethod = null;
    }
  }
  void OnDestroy() {
    GameStateManager.GameStartEvent -= CheckGameStart;
    GameStateManager.GameResultSetEvent -= GameEnd;
  }
  bool gameStarted = false;
  bool AIRunning = true;
  void CheckGameStart() {
    gameStarted = true;
  }
  void GameEnd(GameState state) {
    AIRunning = false;
  }

  void Update() {
    if (!AIRunning) return;
    if (UpdateMethod != null && gameStarted) {
      UpdateMethod();
    }
  }









  void SetBasicMethod() {
    UpdateMethod = BasicAI;
    interval = 60f / AISpeed;
  }
  public InputManager inputManager;
  public static int AISpeed = 530;
  float interval = 0;
  float prevaInputTime = -100f;
  void BasicAI() {
    if (Time.time - prevaInputTime < interval) return;
    prevaInputTime = Time.time;
    int currtypedLength = inputManager.typedString.Length;
    if (inputManager.typedString == inputManager.targetString) {
      inputManager.EntrySubmitted();
      return;
    }
    if (inputManager.targetString[..currtypedLength] != inputManager.typedString) {
      inputManager.Backspace();
      return;
    }
    bool inputCorrect = UnityEngine.Random.Range(0, 100) > 4;
    string inputVal = inputManager.targetString.Substring(currtypedLength, 1);
    if (!inputCorrect) inputVal = "!";
    inputManager.IncrementInput(inputVal);
  }
}
