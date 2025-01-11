using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour {
	void OnEnable() {
		GameStateManager.countDownChanged += ChangeCountDown;
		GameStateManager.GameStartEvent += GameStart;
	}
	void OnDisable() {
		GameStateManager.countDownChanged -= ChangeCountDown;
		GameStateManager.GameStartEvent -= GameStart;
	}
















	//countdonw
	public TextMeshProUGUI countdownTxt;
	void ChangeCountDown(int val) {
		countdownTxt.text = val.ToString();
		countdownTxt.transform.parent.gameObject.SetActive(val == 0 ? false : true);
	}
	void GameStart() {
		//show Game start text. and sound effect.
	}

}
