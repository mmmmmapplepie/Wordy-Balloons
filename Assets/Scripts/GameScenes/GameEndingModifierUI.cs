using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameEndingModifierUI : MonoBehaviour {
	int modulatedCalls = 0;


	void Awake() {
		GameEndingModifierManager.GameEndModeOnEvent += EndModeOn;
		GameEndingModifierManager.EndingModulated += EndingModulated;
	}

	void OnDestroy() {
		GameEndingModifierManager.GameEndModeOnEvent -= EndModeOn;
		GameEndingModifierManager.EndingModulated -= EndingModulated;
	}

	public GameObject endModeUI;
	public Slider slider;
	public GameEndingModifierManager endingManager;
	void EndModeOn() {
		t = 0f;
		endModeUI.SetActive(true);
		period = endingManager.gameEndModeOn == false ? GameData.GameEndingModulationTime * 60f : endingManager.timerPeriod;
		SetText();
	}
	float period = 10f;
	float t = 0;
	void Update() {
		t += Time.deltaTime;
		slider.value = Mathf.Clamp01((period - t) / period);
	}


	public TextMeshProUGUI mainTxt, pulseTxt;
	void SetText() {
		switch (GameData.GameEndingMode) {
			case GameEndingMode.Drain:
				mainTxt.text = "Draining HP";
				pulseTxt.text = "Draining HP";
				break;
			case GameEndingMode.SuddenDeath:
				slider.gameObject.SetActive(false);
				mainTxt.text = "Sudden Death";
				pulseTxt.text = "Sudden Death";
				break;
			case GameEndingMode.Speedup:
				mainTxt.text = $"Speed {100 + (10 * modulatedCalls)}%";
				pulseTxt.text = $"Speed {100 + (10 * modulatedCalls)}%";
				break;
			case GameEndingMode.Damageup:
				mainTxt.text = $"Damage X {modulatedCalls + 1}";
				pulseTxt.text = $"Damage X {modulatedCalls + 1}";
				break;
		}
	}


	void EndingModulated() {
		modulatedCalls++;
		SetText();
	}









}
