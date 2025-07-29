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
	void EndModeOn(bool endMode) {
		started = true;
		t = 0f;
		endModeUI.SetActive(true);
		period = endMode == false ? GameData.GameDecidingChangesStartTime * 60f : endingManager.timerPeriod;
		SetText();
	}
	bool started = false;
	float period = 10f;
	float t = 0;
	void Update() {
		if (!started) return;
		t += Time.deltaTime;
		slider.value = Mathf.Clamp01((period - t) / period);
	}


	public TextMeshProUGUI mainTxt, pulseTxt;
	void SetText(bool effectsOn = false) {
		if (effectsOn) {
			pulseTxt.GetComponent<PulseEffect>().endScale = 1.2f;
		}
		switch (GameData.GameEndingMode) {
			case GameEndingMode.Drain:
				SetTexts(effectsOn ? "DRAINING HP" : "HP DRAIN INCOMING");
				break;
			case GameEndingMode.SuddenDeath:
				if (effectsOn) {
					slider.gameObject.SetActive(false);
					SetTexts("SUDDEN DEATH");
					mainTxt.transform.parent.GetComponent<RectTransform>().anchoredPosition = Vector2.down * 50f;
				} else { SetTexts("SUDDEN DEATH INCOMING"); }
				break;
			case GameEndingMode.Speedup:
				SetTexts(effectsOn ? $"SPEED {100 + (10 * modulatedCalls)}%" : "SPEED-UP INCOMING");
				break;
			case GameEndingMode.Damageup:
				SetTexts(effectsOn ? $"DAMAGE X {modulatedCalls + 1}" : "DAMAGE-UP INCOMING");
				break;
		}
	}
	void SetTexts(string txt) {
		mainTxt.text = txt;
		pulseTxt.text = txt;
	}


	void EndingModulated() {
		modulatedCalls++;
		t = 0f;
		SetText(true);
	}









}
