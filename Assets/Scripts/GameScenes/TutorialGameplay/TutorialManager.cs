using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour {
	public const string TutorialClearedPlayerPrefKey = "TutorialCleared";
	public TextMeshProUGUI guideTxt;
	public RectTransform txtHolder;
	public GameObject nxtBtn, skipBtn;
	public PinholeShaderEditor shadowEditor;
	List<string> typeWordsList = new List<string>() { "Balloons", "Are", "Fun", "WOW This Word Is So Long!!!", "When", "Filled", "With", "Water" };
	List<PinholeShape> shapeList = new List<PinholeShape>();
	void Awake() {
		GameStateManager.GameStartEvent += GameStarted;
		InputManager.IncrementInputFinished += InputTyped;
		InputManager.CorrectEntryProcess += BalloonFired;
		InputManager.CorrectEntryProcess += NewEntryNeeded;
		InputManager.SkipAttemptResult += SkipAttempted;
		InputManager.SkipAttemptResult += NewEntryNeeded;


		Balloon.BalloonCreated += BalloonSpawned;
		Balloon.BalloonDestroyed += BalloonDestroyed;

		GameStateManager.GameResultSetEvent += GameSet;
	}
	void OnDisable() {
		GameStateManager.GameStartEvent -= GameStarted;
		InputManager.IncrementInputFinished -= InputTyped;
		InputManager.CorrectEntryProcess -= BalloonFired;
		InputManager.CorrectEntryProcess -= NewEntryNeeded;
		InputManager.SkipAttemptResult -= SkipAttempted;
		InputManager.SkipAttemptResult -= NewEntryNeeded;


		Balloon.BalloonCreated -= BalloonSpawned;
		Balloon.BalloonDestroyed -= BalloonDestroyed;

		GameStateManager.GameResultSetEvent += GameSet;

	}
	void OnDestroy() {
		StopAllCoroutines();
		Time.timeScale = 1;
	}

	void GameStarted() {
		StartCoroutine(TutorialRoutine());
	}


	IEnumerator TutorialRoutine() {
		yield return StartCoroutine(IntroRoutine());
		yield return StartCoroutine(InputGuideRoutine());
		yield return StartCoroutine(BalloonInteractionsRoutine());
		yield return StartCoroutine(OtherInputsActionsRoutine());
		yield return StartCoroutine(MiscAndEndRoutine());
	}


	#region Opening-GamePurpose
	public GameObject menuBtn;

	IEnumerator IntroRoutine() {
		menuBtn.SetActive(false);
		skipBtn.SetActive(true);
		inputManager.canUseSkip = false;
		SetupHighlights(null);
		ShowPanelWithText("Welcome to wordy balloons!", 60f, default, 1200f);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("this guide will provide you everything you need to play the game!", 30f);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("in the background there are two bases. The one on your left is your home base.", 30f);

		//higlight bases
		shapeList.Clear();
		// shapeList.Add(new PinholeShape(820f * Vector2.right, new Vector2(600f, 1000f), 100f, PinholeShaderEditor.Shape.Ellipse));
		// shapeList.Add(new PinholeShape(-820f * Vector2.right, new Vector2(600f, 1000f), 100f, PinholeShaderEditor.Shape.Ellipse));
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(new Vector2(7.5f, 0)), GetWorldSizeInCanvasRectSize(new Vector2(6f, 10f)), 100f, PinholeShaderEditor.Shape.Ellipse));
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(new Vector2(-7.5f, 0)), GetWorldSizeInCanvasRectSize(new Vector2(6f, 10f)), 100f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList);

		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("your aim is to fire water balloons at the opposing base from your base and secure the win!", 30f);
		SetupHighlights(null);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("lets get a more \"hands on\" experience!", 30f);
		yield return StartCoroutine(WaitForNext());
	}
	#endregion



	#region Input-Firing-Balloon
	public InputManager inputManager;
	public RectTransform inputRect;
	IEnumerator InputGuideRoutine() {
		inputManager.canTakeInput = true;
		inputManager.SetNewTargetText(typeWordsList[0]);
		inputManager.canTakeInput = false;
		ShowPanelWithText("When the game starts, you will be given a word here!", 30f, 100f * Vector2.down);
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(inputRect), inputRect.rect.size, 20f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList, darkBackgroundColor);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("Typing out this word will start filling up the balloon that you can fire!", 30f, 100f * Vector2.down);
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(new Vector2(-7.16f, -3.792f)), GetWorldSizeInCanvasRectSize(new Vector2(2f, 2f)), 100f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList, darkBackgroundColor);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("You can fill up a new water balloon by typing out this word!\ntry typing the first letter of this word", 30f, 100f * Vector2.down);
		inputManager.canTakeInput = true;
		nxtBtn.SetActive(false);
		while (!inputTyped) yield return null;
		inputManager.canTakeInput = false;
		yield return new WaitForSecondsRealtime(0.5f);
		Time.timeScale = 0;
		ShowPanelWithText("if you type the correct letter it will show up \"blue\" while a wrong letter will show up as \"red\". The letters are also case sensitive so you will have to add the capital letters wherever required.", 30f);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("to fire the balloon, press \"Enter\". If what you have typed matches the target word then a balloon will be fired, but if you try to fire the balloon without having typed the word exactly the balloon fire will fail with possible side effects depending on the game mode.", 30f);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("Try to type out the word correctly and fire it.\nif you type something wrong you can delete the latest letter by pressing \"backspace\"", 30f);
		inputManager.canTakeInput = true;
		Time.timeScale = 1;
		nxtBtn.SetActive(false);
		shadowEditor.gameObject.SetActive(false);
		newBalloonedFiredByPlayer = false;
		playerBalloonSpawned = false;
		while (!newBalloonedFiredByPlayer) yield return null;
		inputManager.canTakeInput = false;
		while (!playerBalloonSpawned) yield return null;
		yield return new WaitForSecondsRealtime(0.25f);
		Vector2 balloonPos = GetWorldPointToCanvasPoint(latestBalloon.transform.position);
		ShowPanelWithText("good job! you have successfully fired a balloon! the power of the balloon will match the length of the word you typed to fire the balloon\nthe new balloon will automatically fly towards the opposing base.", 30f, new Vector2(100f, balloonPos.y));
		shapeList.Clear();
		shapeList.Add(new PinholeShape(balloonPos, GetWorldSizeInCanvasRectSize(new Vector2(1.5f, 1.5f)), 100f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList);
		Time.timeScale = 0;
		yield return StartCoroutine(WaitForNext());
		nxtBtn.SetActive(false);
	}
	bool inputTyped = false;
	void InputTyped(string input) {
		inputTyped = true;
	}

	bool newBalloonedFiredByPlayer = false;
	void BalloonFired(string s, ulong clientID) {
		if (BalloonManager.teamIDs.Contains(clientID)) newBalloonedFiredByPlayer = true;
	}

	bool playerBalloonSpawned = false, opposingBalloonSpawned = false;
	Balloon latestBalloon;
	void BalloonSpawned(Team t, Balloon b) {
		latestBalloon = b;
		if (t == BalloonManager.team) {
			playerBalloonSpawned = true;
		} else {
			opposingBalloonSpawned = true;
		}
	}

	#endregion



	#region BalloonInteraction
	public BalloonManager balloonManager;
	public RectTransform team1HP, team2HP;
	IEnumerator BalloonInteractionsRoutine() {
		float t = 0;
		Time.timeScale = 1;
		inputManager.SetNewTargetText(typeWordsList[1]);
		ShowPanelWithText("The balloon takes 5 seconds to go across the bases.", 30f, new Vector2(0f, -100f));
		while (t < 2.5f) {
			Color backgroundC = backgroundDefaultColor;
			if (t > 1f) {
				backgroundC = Color.Lerp(backgroundDefaultColor, Color.clear, (t - 1) / 1.5f);
			}
			Vector2 tempPos = GetWorldPointToCanvasPoint(latestBalloon.transform.position);
			shapeList.Clear();
			shapeList.Add(new PinholeShape(tempPos, GetWorldSizeInCanvasRectSize(new Vector2(1.5f, 1.5f)), 100f, PinholeShaderEditor.Shape.Ellipse));
			SetupHighlights(shapeList, backgroundC);
			t += Time.deltaTime;
			yield return null;
		}
		shadowEditor.gameObject.SetActive(false);
		opposingBalloonSpawned = false;
		balloonManager.SpawnBalloon(5, 1);
		while (!opposingBalloonSpawned) yield return null;

		yield return new WaitForSecondsRealtime(0.25f);
		Time.timeScale = 0;
		ShowPanelWithText("The opposing base have also fired a balloon!\nif a balloon hits an opposing balloon then they will reduce the power of each other by their own power and the balloons with 0 power are destroyed.Lets see what happens when the two balloons collide.", 30f, new Vector2(-300f, latestBalloon.transform.position.y));
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(latestBalloon.transform.position), GetWorldSizeInCanvasRectSize(new Vector2(1.5f, 1.5f)), 100f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList);
		yield return StartCoroutine(WaitForNext());
		Time.timeScale = 1;
		destroyedWithBalloon = false;
		destroyedOnBase = false;
		while (!destroyedWithBalloon) yield return null;

		yield return new WaitForSecondsRealtime(0.2f);
		Time.timeScale = 0;
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(latestDestroyedBalloonPos), GetWorldSizeInCanvasRectSize(new Vector2(1.5f, 1.5f)), 100f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList);
		ShowPanelWithText("your balloon was stronger and so survived! Now the balloon is about to hit and damage the opposing base. The damage it does will be equal to the balloons remaining power.", 30f, new Vector2(-300f, 0));
		yield return StartCoroutine(WaitForNext());
		shadowEditor.gameObject.SetActive(false);
		Time.timeScale = 1;
		while (!destroyedOnBase) yield return null;

		yield return new WaitForSecondsRealtime(0.1f);
		Time.timeScale = 0;
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(team1HP), team1HP.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		shapeList.Add(new PinholeShape(GetCenterOfRect(team2HP), team2HP.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		ShowPanelWithText("The HP of the bases are shown in their respective HP bars.\nwhen this HP gets to 0, the team with 0hp will lose the game.", 30f);
		yield return StartCoroutine(WaitForNext());
		shadowEditor.gameObject.SetActive(false);
	}
	Vector3 latestDestroyedBalloonPos;
	bool destroyedOnBase = false;
	bool destroyedWithBalloon = false;
	void BalloonDestroyed(bool onbase, Balloon b) {
		latestDestroyedBalloonPos = b.transform.position;
		if (onbase) destroyedOnBase = true;
		else destroyedWithBalloon = true;
	}



	#endregion


	#region OtherInputActions
	public RectTransform skipChargeUI;
	IEnumerator OtherInputsActionsRoutine() {
		ShowPanelWithText("There is one final trick you can use, but before that let's fire a few more balloons", 30f, 100f * Vector2.down);
		Time.timeScale = 1;
		nxtBtn.SetActive(false);
		inputManager.canTakeInput = true;
		newBalloonedFiredByPlayer = false;
		inputManager.SetNewTargetText(typeWordsList[1]);
		while (!newBalloonedFiredByPlayer) yield return null;
		inputManager.SetNewTargetText(typeWordsList[2]);
		newBalloonedFiredByPlayer = false;
		while (!newBalloonedFiredByPlayer) yield return null;
		inputManager.SetNewTargetText(typeWordsList[3]);
		yield return new WaitForSecondsRealtime(1f);
		Time.timeScale = 0;
		inputManager.canTakeInput = false;
		ShowPanelWithText("you may end up getting difficult words to type in a pinch. In those cases you can skip the word by typing \"///\"(\"/\" three times in a row)! Performing a skip will use up a skip charge shown below and each time you type \"/\" a small mark will appear to notify you of how many times youve typed \"/\". ", 30f);
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(skipChargeUI), skipChargeUI.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		shapeList.Add(new PinholeShape(GetCenterOfRect(inputRect), inputRect.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		nxtBtn.SetActive(true);
		yield return StartCoroutine(WaitForNext());
		nxtBtn.SetActive(false);
		shadowEditor.gameObject.SetActive(false);
		ShowPanelWithText("Try skipping this text by typing \"///\"", 30f, new Vector2(0f, -100f));
		Time.timeScale = 1;
		inputManager.canUseSkip = true;
		inputManager.canTakeInput = true;
		inputManager.canAttemptFire = false;
		skipCalled = false;
		while (!skipCalled) yield return null;
		inputManager.SetNewTargetText(typeWordsList[4]);
		Time.timeScale = 0;
		inputManager.canTakeInput = false;
		ShowPanelWithText("Skip charges return when you fire a balloon successfully so you can use skips freely.\nGood job! Now you've mastered everything you need for success! Now go ahead and clear this tutorial!", 30f);
		yield return StartCoroutine(WaitForNext());
	}
	bool skipCalled = false;
	void SkipAttempted(bool success) {
		if (success) skipCalled = true;
	}



	#endregion


	#region OtherMiscThings
	public RectTransform statsRect, meaningRect;
	IEnumerator MiscAndEndRoutine() {
		Time.timeScale = 1;
		inputManager.canTakeInput = true;
		txtHolder.gameObject.SetActive(false);
		yield return new WaitForSeconds(0);
		Time.timeScale = 0;
		inputManager.canTakeInput = false;
		ShowPanelWithText("Oh right! You can also see your live game stats down here. You can see your average speed (based on balloons succesfully fired), balloon points fired and average accuracy of your typing!", 30f, new Vector2(500f, -100f));
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(statsRect), statsRect.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("And if you are curious (and also very very fast!) you can see one of the meanings of the word you are typing over here.", 30f, 100f * Vector2.down);
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(meaningRect), meaningRect.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("Alright, now that is indeed all there is to it! Happy typing!!!", 30f, 100f * Vector2.down);
		PlayerPrefs.SetInt(TutorialManager.TutorialClearedPlayerPrefKey, 1);

		nxtBtn.SetActive(false);
		Time.timeScale = 1;
		shadowEditor.gameObject.SetActive(false);
		inputManager.canTakeInput = true;
		inputManager.canAttemptFire = true;
		tutorialDone = true;


		yield return new WaitForSecondsRealtime(5f);
		txtHolder.gameObject.SetActive(false);
	}
	int currIndex = 4;
	bool tutorialDone = false;
	void NewEntryNeeded(bool skipped) {
		if (!tutorialDone) return;
		if (skipped) {
			SetNewTargetContinuous();
		}
	}
	void NewEntryNeeded(string s, ulong id) {
		if (!tutorialDone) return;
		SetNewTargetContinuous();
	}
	void SetNewTargetContinuous() {
		currIndex++;
		currIndex %= typeWordsList.Count;
		if (currIndex == 3) currIndex++;
		inputManager.SetNewTargetText(typeWordsList[currIndex]);
	}
	void GameSet(GameStateManager.GameResult result) {
		StopAllCoroutines();
		gameObject.SetActive(false);
	}

	#endregion





	public Color backgroundDefaultColor, highlightDefaultColor, lightBackgroundColor, darkBackgroundColor;

	void SetupHighlights(List<PinholeShape> highlightsArea = null, Color? background = null, Color? highlight = null) {
		shadowEditor.gameObject.SetActive(true);
		shadowEditor.backgroundColor = background != null ? (Color)background : backgroundDefaultColor;
		shadowEditor.maskColor = highlight != null ? (Color)highlight : highlightDefaultColor;
		shadowEditor.maskShapes = highlightsArea;
		shadowEditor.SetMat();
	}


	bool waitingForNext = false;
	void ShowPanelWithText(string txt, float txtSize = -1, Vector2 pos = default, float prefWidth = -1) {
		txtHolder.gameObject.SetActive(true);
		guideTxt.text = txt;
		guideTxt.fontSize = txtSize > 0 ? txtSize : guideTxt.fontSize;
		if (prefWidth < 0) prefWidth = txtHolder.rect.width;
		Vector2 size = guideTxt.GetPreferredValues(prefWidth - 100f, Mathf.Infinity);
		size.x = txtHolder.rect.width;
		size.y += 100f;
		txtHolder.sizeDelta = size;
		txtHolder.anchoredPosition = pos;
		LayoutRebuilder.ForceRebuildLayoutImmediate(txtHolder);
	}
	Coroutine waitingForNextRoutine = null;
	IEnumerator WaitForNext() {
		nxtBtn.SetActive(true);
		waitingForNext = true;
		while (waitingForNext) yield return null;
	}

	public void NxtClicked() {
		waitingForNext = false;
	}

	public Vector2 GetCenterOfRect(RectTransform rt) {
		Vector2 localPos = rt.localPosition;
		Vector2 size = rt.rect.size;
		return localPos - (rt.pivot - 0.5f * Vector2.one) * size;
	}
	public Vector2 GetWorldPointToCanvasPoint(Vector2 worldPoint) {
		Vector2 screenPt = Camera.main.WorldToScreenPoint(worldPoint) / new Vector2(Screen.width, Screen.height) * transform.root.GetComponent<RectTransform>().rect.size;
		screenPt -= transform.root.GetComponent<RectTransform>().rect.size * 0.5f;
		return screenPt;
	}

	public Vector2 GetWorldSizeInCanvasRectSize(Vector2 size) {
		return 0.5f * size * (transform.root.GetComponent<RectTransform>().rect.height / MatchCamToScreenSize.camSize);
	}

	public void SkipTutorial() {
		PlayerPrefs.SetInt(TutorialManager.TutorialClearedPlayerPrefKey, 1);

		SceneManager.LoadScene("MainMenu");
	}
}
