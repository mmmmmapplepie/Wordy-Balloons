using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public class InputManager : MonoBehaviour {
	[HideInInspector] public bool canTakeInput = false;
	public static InputManager Instance;
	void Awake() {
		Instance = this;
	}
	void OnDestroy() {
		Instance = null;
		_skipCharges = 3;
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
	public string displayString { get; set; } = "";
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
	public static event Action<string, ulong> CorrectInputProcess;
	public static event Action CorrectInputFinished;
	void ProcessCorrectInput() {
		Func<List<string>> balloonCreator = FindBalloonCreator?.Invoke();
		if (balloonCreator == null) { balloonCreator = NormalCreate; }
		List<string> newBalloons = balloonCreator();
		foreach (string balloonTxt in newBalloons) {
			CorrectInputProcess?.Invoke(balloonTxt, NetworkManager.Singleton.LocalClientId);
		}
		CorrectInputFinished?.Invoke();
		skipCharges++;
		SetNewTargetText();
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
		ProcessDisplayString();
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
	int skipRequirement = 3;
	public static event Action<int> skipTickChanged;
	// essentially typing "///" will try to skip - typing "/" four times in a row
	bool IncrementSkip(string input) {
		if (input != "/" || skipCharges == 0) {
			skipTick = 0;
			return false;
		}
		skipTick++;

		if (skipTick >= skipRequirement) {
			return TrySkip();
		}
		return false;
	}
	static int _skipCharges = 3;
	public static int skipCharges {
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
	public void TrySkipBtn() {
		TrySkip();
	}
	bool TrySkip() {
		skipTick = 0;
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
		if (typedString.Length > skipRequirement && typedString.Length > targetString.Length) {
			typedString = typedString.Substring(0, targetString.Length);
		}
		ProcessDisplayString();
	}

	void ProcessDisplayString() {
		displayString = "";
		for (int i = 0; i < typedString.Length; i++) {
			if (char.IsWhiteSpace(targetString[i]) && targetString[i] != typedString[i]) {
				displayString += "_";
			} else {
				displayString += targetString[i];
			}
		}
		displayString += targetString.Substring(typedString.Length);
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
		displayString = targetString;
		ResetEvent?.Invoke();
	}
	#endregion



	#region Producing new target text
	//takes in a string and return true if it is allowed.
	public static event Func<Func<string>> FindTextTargetMethod;
	public static event Func<Func<string, bool>> FindTextApprovalMethod;
	public static event Action<string> NewTextSet;



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
		NewTextSet?.Invoke(ranTxt);
	}
	List<DictionaryEntry> DictionaryList = null;
	public static event Action<DictionaryEntry> NewWordChosen;
	string PickRandomText() {
		if (DictionaryList == null) DictionaryList = EnglishDictionary.Instance.GetDictionaryList();
		if (DictionaryList == null) return "Random text has been set bruh!";
		int ranWord = UnityEngine.Random.Range(0, DictionaryList.Count);
		NewWordChosen?.Invoke(DictionaryList[ranWord]);
		return DictionaryList[ranWord].word;
	}

	#endregion







}
