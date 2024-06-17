using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

public class InputManager : MonoBehaviour {
	public TextMeshProUGUI targetText;
	public TMP_InputField input;
	public bool textSet = false;
	void OnEnable() {
		GameStateManager.countDownChanged += CheckStart;
		SetInputAvailabilityEvent += ChangeInputAvailability;
	}
	void OnDisable() {
		GameStateManager.countDownChanged -= CheckStart;
		SetInputAvailabilityEvent = ChangeInputAvailability;
	}

	void Update() {
	}
	static event Action<bool> SetInputAvailabilityEvent;
	public static void SetInputAvailability(bool enable) {
		print(enable);
		SetInputAvailabilityEvent?.Invoke(enable);
	}
	void ChangeInputAvailability(bool enable) {
		input.interactable = enable;
	}


	//takes in a string and return true if it is allowed.
	public static event System.Func<System.Func<string, bool>> LookForTextFilterMethodEvent;
	public static event System.Func<Func<string>> newTextPreset;

	void CheckStart(int count) {
		if (count != 0) return;
		ProduceText();
	}

	void ProduceText() {
		Func<string> inputTxtInitial = newTextPreset?.Invoke();
		if (inputTxtInitial == null) inputTxtInitial = () => "";
		input.text = inputTxtInitial();

		System.Func<string, bool> filter = LookForTextFilterMethodEvent?.Invoke();
		if (filter == null) filter = (string input) => true;


		//loop random stuff from dictionary until filter approves.
		textSet = true;
	}




	public static event Func<Action<string>> InputChangedEvent;
	public static event Func<Func<string, List<string>>> InputSubmitEvent;
	public static event Action<string> ballonCreated;
	string prevInput = "";
	int prevstringPos = 0;
	public void InputChanged(string s) {
		if (s == prevInput) return;

		if (input.textComponent.GetTextInfo(s).lineCount > 2 || input.textComponent.isTextOverflowing) {
			input.text = prevInput;
			input.stringPosition = prevstringPos;
		}
		prevInput = input.text;
		prevstringPos = input.stringPosition;

		Action<string> changedCheck = InputChangedEvent?.Invoke();
		if (changedCheck == null) return;
		changedCheck(s);
	}

	public void InputSubmitted(string s) {
		if (!textSet) return;

		//check for skip part.
		if (s == "///") {
			bool canSkip = CheckSkip();
			if (canSkip) {
				Skip();
				return;
			}
		}

		Func<string, List<string>> balloonCreator = InputSubmitEvent?.Invoke();
		if (balloonCreator == null) { balloonCreator = NormalCreate; }
		List<string> newBalloons = balloonCreator(s);
		foreach (string balloonTxt in newBalloons) {
			ballonCreated.Invoke(balloonTxt);
		}

		if (s == targetText.text) {
			textSet = false;
			ProduceText();
		}
	}
	int _skipCharges = 3;
	public int skipCharges {
		get {
			return _skipCharges;
		}
		set {
			skipCharges = value < 0 ? 0 : value;
		}
	}

	bool CheckSkip() {
		if (skipCharges == 0) return false;
		return true;
	}
	void Skip() {
		skipCharges--;
		textSet = false;
		ProduceText();
	}
	List<string> NormalCreate(string s) {
		if (s == targetText.text) {
			return new List<string> { s };
		} else {
			return new List<string>();
		}
	}


}
