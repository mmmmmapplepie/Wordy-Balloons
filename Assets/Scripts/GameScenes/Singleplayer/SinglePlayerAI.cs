using System;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerAI : MonoBehaviour {
	Action UpdateMethod;
	void Awake() {
		GameStateManager.GameStartEvent += CheckGameStart;
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
	}
	bool gameStarted = false;
	public bool AIRunning = true;
	void CheckGameStart() {
		gameStarted = true;
	}

	void Update() {
		if (!AIRunning) return;
		if (UpdateMethod != null && gameStarted) {
			UpdateMethod();
		}
	}









	void SetBasicMethod() {
		UpdateMethod = BasicAI;
	}

	List<DictionaryEntry> DictionaryList = null;
	public static int AISpeed = 80;
	float cumulativePower = 0;
	int wordLength = -1;
	public BalloonManager balloonManager;

	void BasicAI() {
		if (wordLength < 1) {
			if (DictionaryList == null) DictionaryList = EnglishDictionary.Instance.GetDictionaryList();
			if (DictionaryList == null) return;
			wordLength = DictionaryList[UnityEngine.Random.Range(0, DictionaryList.Count)].word.Length;
		}
		cumulativePower += Time.deltaTime * AISpeed / 60f;
		if (cumulativePower >= wordLength && wordLength > 0) {
			cumulativePower -= wordLength;
			this.balloonManager.SpawnBalloon(wordLength, 1);
			wordLength = 0;
		}
	}
}
