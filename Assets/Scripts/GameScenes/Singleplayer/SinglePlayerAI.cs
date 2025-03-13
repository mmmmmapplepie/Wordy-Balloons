using System;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerAI : MonoBehaviour {
	public static Action UpdateMethod;
	void Awake() {
		GameStateManager.countDownChanged += CheckGameStart;
		if (!GameData.InSinglePlayerMode) {
			UpdateMethod = null;
		} else {
			if (UpdateMethod == null) {
				SetBasicMethod();
			}
		}
	}
	void OnDestroy() {
		GameStateManager.countDownChanged -= CheckGameStart;
	}
	bool gameStarted = false;
	void CheckGameStart(int count) {
		if (count != 0) return;
		gameStarted = true;
	}

	void Update() {
		if (UpdateMethod != null && gameStarted) {
			UpdateMethod();
		}
	}









	void SetBasicMethod() {
		UpdateMethod = BasicAI;
	}

	List<DictionaryEntry> DictionaryList = null;
	int lettersPerMinute = 100;
	float cumulativePower = 0;
	int wordLength = -1;
	public BalloonManager balloonManager;
	public GameplayDataUI dataHolder;

	void BasicAI() {
		if (wordLength < 1) {
			if (DictionaryList == null) DictionaryList = EnglishDictionary.Instance.GetDictionaryList();
			if (DictionaryList == null) return;
			wordLength = DictionaryList[UnityEngine.Random.Range(0, DictionaryList.Count)].word.Length;
		}
		cumulativePower += Time.deltaTime * lettersPerMinute / 60f;
		if (cumulativePower >= wordLength && wordLength > 0) {
			cumulativePower -= wordLength;
			balloonManager.SpawnBalloon(wordLength, 1);
			dataHolder.AIInput(wordLength, 1);
			wordLength = 0;
		}
	}








}
