using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

public class InputManager : MonoBehaviour {
	bool canTakeInput = false;
	public static InputManager Instance;
	void Awake() {
		Instance = this;
	}
	void OnEnable() {
		GameStateManager.countDownChanged += CheckGameStart;
	}
	void OnDisable() {
		GameStateManager.countDownChanged -= CheckGameStart;
	}
	void CheckGameStart(int count) {
		if (count != 0) return;
		SetNewTargetText();
		canTakeInput = true;
	}
	public static event Action InputProcessFinished;
	void Update() {
		ProcessInput();
		InputProcessFinished?.Invoke();
	}


	public string targetString { get; set; } = "";
	public string typedString { get; set; } = "";
	public static event Action TypedTextChanged;
	void ProcessInput() {
		if (!canTakeInput) return;
		string input = Input.inputString;
		if (input == null || input.Length == 0) return;
		if (input.Contains("\r")) {
			InputSubmitted();
		} else if (input.Contains("\b")) {
			Backspace();
		} else {
			IncrementInput(input);
		}
		TypedTextChanged?.Invoke();
	}

	#region InputSubmit
	public void InputSubmitted() {
		if (typedString == targetString) {
			ProcessCorrectInput();
		} else {
			ProcessWrongInput();
		}
	}

	public static event Func<Func<List<string>>> FindBalloonCreator;
	public static event Action<string> CreateBalloon;
	public static event Action CorrectInputFinished;
	void ProcessCorrectInput() {
		Func<List<string>> balloonCreator = FindBalloonCreator?.Invoke();
		if (balloonCreator == null) { balloonCreator = NormalCreate; }
		List<string> newBalloons = balloonCreator();
		foreach (string balloonTxt in newBalloons) {
			CreateBalloon.Invoke(balloonTxt);
		}
		CorrectInputFinished?.Invoke();
	}
	List<string> NormalCreate() {
		return new List<string> { typedString };
	}




	public static event Action WrongInputProcess, WrongInputFinished;
	void ProcessWrongInput() {
		WrongInputProcess?.Invoke();
		WrongInputFinished?.Invoke();
	}
	#endregion


	#region TypedInputUpdate
	public static event Func<Action> FindBackspaceProcess, BackspaceFinished;
	public void Backspace() {
		Action backSpaceFtn = FindBackspaceProcess?.Invoke();
		if (backSpaceFtn == null) backSpaceFtn = BasicBackspace;
		backSpaceFtn();
		BackspaceFinished?.Invoke();
	}
	void BasicBackspace() {
		if (typedString.Length <= 0) return;
		typedString = typedString.Substring(0, typedString.Length - 1);
	}


	public static event Func<Action<string>> FindIncrementInputProcess;
	public static event Action IncrementInputFinished;
	void IncrementInput(string input) {
		bool skip = IncrementSkip(input);
		if (skip) return;

		Action<string> incrementAction = FindIncrementInputProcess?.Invoke();
		if (incrementAction == null) incrementAction = BaseIncrement;
		incrementAction(input);
		IncrementInputFinished?.Invoke();
	}
	int _skipTick = 0;
	int skipTick {
		get {
			return _skipTick;
		}
		set {
			_skipTick = value < 0 ? 0 : value;
			skipTickChanged?.Invoke(_skipTick);
		}
	}
	public static event Action<int> skipTickChanged;
	// essentially typing "////" will try to skip - typing "/" four times in a row
	bool IncrementSkip(string input) {
		if (input != "/") {
			skipTick = 0;
			return false;
		}
		skipTick++;

		if (skipTick == 4) {
			skipTick = 0;
			return TrySkip();
		}
		return false;
	}
	int _skipCharges = 3;
	public int skipCharges {
		get {
			return _skipCharges;
		}
		set {
			_skipCharges = value < 0 ? 0 : value;
		}
	}
	public static event Func<Func<bool>> FindCheckSkipFunction;
	public static event Func<Action> FindSkipFunction;
	public static event Action<bool> SkipAttemptResult;
	bool TrySkip() {
		Func<bool> canSkip = FindCheckSkipFunction?.Invoke();
		if (canSkip == null) canSkip = NormalSkipCheck;
		if (!canSkip()) { SkipAttemptResult?.Invoke(false); return false; }

		Action skipFtn = FindSkipFunction?.Invoke();
		if (skipFtn == null) skipFtn = NormalSkip;
		skipFtn.Invoke();

		SkipAttemptResult?.Invoke(true);
		return true;
	}
	bool NormalSkipCheck() {
		if (skipCharges == 0) return false;
		return true;
	}
	void NormalSkip() {
		skipCharges--;
		SetNewTargetText();
	}

	void BaseIncrement(string input) {
		typedString = String.Concat(typedString, input);
		if (typedString.Length > 3 && typedString.Length > targetString.Length) {
			typedString = typedString.Substring(0, targetString.Length);
		}
	}
	#endregion


	#region General Functions
	public void ToggleInputEnabledState() {
		canTakeInput = !canTakeInput;
	}
	public static event Action ResetEvent;
	public void ResetTypedText() {
		skipTick = 0;
		typedString = "";
		ResetEvent?.Invoke();
	}
	#endregion



	#region Producing new target text
	//takes in a string and return true if it is allowed.
	public static event Func<Func<string>> FindTextTargetMethod;
	public static event Func<Func<string, bool>> FindTextApprovalMethod;
	public static event Action NewTextSet;



	void SetNewTargetText() {
		Func<string> RandomizerMethod = FindTextTargetMethod?.Invoke();
		if (RandomizerMethod == null) RandomizerMethod = PickRandomText;

		System.Func<string, bool> filter = FindTextApprovalMethod?.Invoke();
		if (filter == null) filter = (string input) => true;

		bool txtPassed = false;
		string ranTxt = "";
		while (!txtPassed) {
			ranTxt = RandomizerMethod();
			if (filter(ranTxt)) txtPassed = true;
		}

		targetString = ranTxt;
		ResetTypedText();
		NewTextSet?.Invoke();
	}

	string PickRandomText() {
		return "Random text has been set bruh!";
	}

	#endregion







}
