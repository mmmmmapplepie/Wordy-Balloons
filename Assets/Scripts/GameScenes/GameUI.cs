using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour {
	void OnEnable() {
		GameStateManager.countDownChanged += ChangeCountDown;
	}
	void OnDisable() {
		GameStateManager.countDownChanged -= ChangeCountDown;
	}
















	//countdonw
	public TextMeshProUGUI countdownTxt;
	void ChangeCountDown(int val) {
		countdownTxt.text = val.ToString();
		countdownTxt.transform.parent.gameObject.SetActive(val == 0 ? false : true);
	}

}
