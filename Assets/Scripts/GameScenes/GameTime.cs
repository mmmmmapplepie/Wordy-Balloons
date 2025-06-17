using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameTime : MonoBehaviour {
	float startT = -1;
	void Awake() {
		GameStateManager.GameStartEvent += GameStart;
	}
	void OnDestroy() {
		GameStateManager.GameStartEvent -= GameStart;
	}
	void GameStart() {
		startT = Time.time;
	}
	public TextMeshProUGUI txt;
	void Update() {
		if (startT < 0) return;
		float diff = Time.time - startT;
		int mins = Mathf.FloorToInt(diff / 60f);
		int secs = Mathf.FloorToInt(diff % 60f);
		string minsS = mins < 10 ? "0" + mins : mins.ToString();
		string secsS = secs < 10 ? "0" + secs : secs.ToString();
		txt.text = minsS + ":" + secsS;
	}
}
