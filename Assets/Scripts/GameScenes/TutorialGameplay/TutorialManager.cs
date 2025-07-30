using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour {
	public const string TutorialClearedPlayerPrefKey = "TutorialCleared";
	public TextMeshProUGUI guideTxt;
	public RectTransform txtHolder;
	public GameObject nxtBtn, skipBtn;
	public PinholeShaderEditor shadowEditor;
	List<string> typeWordsList = new List<string>() { "WordyBalloons", "Are", "Fun", "WOW This Word Is So Long!!!", "When", "Filled", "With", "Water" };
	List<PinholeShape> shapeList = new List<PinholeShape>();
	void Awake() {
		if (GameData.PlayMode != PlayModeEnum.Tutorial) return;
		GameStateManager.GameStartEvent += GameStarted;
		InputManager.IncrementInputFinished += InputTyped;
		InputManager.CorrectEntryProcess += BalloonFired;
		InputManager.SkipAttemptResult += SkipAttempted;



		Balloon.BalloonCreated += BalloonSpawned;
		Balloon.BalloonDestroyed += BalloonDestroyed;

		GameStateManager.GameResultSetEvent += GameSet;

		menuBtn.SetActive(false);
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

		GameStateManager.GameResultSetEvent -= GameSet;
		OnDestroy();
	}
	void OnDestroy() {
		StopAllCoroutines();
		if (GameData.PlayMode == PlayModeEnum.Tutorial) Time.timeScale = 1;
	}

	void GameStarted() {
		if (GameData.PlayMode == PlayModeEnum.Tutorial) {
			StartCoroutine(TutorialRoutine());
		} else {
			gameObject.SetActive(false);
		}
	}


	IEnumerator TutorialRoutine() {
		yield return StartCoroutine(IntroRoutine());
		yield return StartCoroutine(InputGuideRoutine());
		yield return StartCoroutine(BalloonInteractionsRoutine());
		yield return StartCoroutine(OtherInputsActionsRoutine());
		yield return StartCoroutine(MiscAndEndRoutine());
	}


	#region Opening-GamePurpose
	public GameObject menuBtn, timer;
	public WelcomeIntroEffect welcomeIntro;
	// <color=#FE25DF><b><i></color></b></i>
	// <color=#FF4100><b></color></b>
	// #FE25DF - pink,  #FF4100 - orange

	IEnumerator IntroRoutine() {
		inputManager.canUseSkip = false;
		// if (!PlayerPrefs.HasKey(TutorialManager.TutorialClearedPlayerPrefKey)) {
		skipBtn.SetActive(false);
		welcomeIntro.gameObject.SetActive(true);
		yield return new WaitForSecondsRealtime(3f);
		SetupHighlights(null);
		// yield return new WaitUntil(() => welcomeIntro.AnimationFinished);
		// ShowPanelWithText("<color=#FF4100>Welcome to wordy balloons!", 80f, default, 1000f);
		yield return StartCoroutine(WaitForNext(false));

		// }
		welcomeIntro.gameObject.SetActive(false);
		skipBtn.SetActive(true);

		ShowPanelWithText("Hello there!\n\nthis guide will provide you everything you need for the game!", 30f, default, 900f);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("in the background you will see two bases. <color=#FF4100><b>The one on your left is your base.", 30f);

		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(new Vector2(7.5f, 0)), GetWorldSizeInCanvasRectSize(new Vector2(5f, 10f)), 50f, PinholeShaderEditor.Shape.Ellipse));
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(new Vector2(-7.5f, 0)), GetWorldSizeInCanvasRectSize(new Vector2(5f, 10f)), 50f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList);

		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("your aim is to <color=#FF4100><b>launch water balloons</color></b> at the opposing base and destroy it!", 30f);
		yield return StartCoroutine(WaitForNext());
		SetupHighlights(null);
		ShowPanelWithText("let's get a more hands-on experience!\n\n<color=#FE25DF><b><i>(The interactive instructions will be shown in this color)", 30f);
		yield return StartCoroutine(WaitForNext());
	}
	#endregion



	#region Input-Firing-Balloon
	public InputManager inputManager;
	public RectTransform inputRect;
	public TMP_FontAsset inputfont;
	IEnumerator InputGuideRoutine() {
		inputManager.canTakeInput = true;
		inputManager.SetNewTargetText(typeWordsList[0]);
		inputManager.canTakeInput = false;
		ShowPanelWithText("When the game starts, you will be given a <color=#FF4100><b>word</color></b> (shown below).", 30f);
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(inputRect), inputRect.rect.size, 20f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("Typing out this word will start filling up the balloon that you can launch!", 30f);
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(new Vector2(-7.16f, -3.792f)), GetWorldSizeInCanvasRectSize(new Vector2(2f, 2f)), 50f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("<color=#FE25DF><b><i>try typing the first letter of this word.", 30f);
		shadowEditor.gameObject.SetActive(false);
		inputManager.canTakeInput = true;
		nxtBtn.SetActive(false);
		while (!inputTyped) yield return null;
		inputManager.canTakeInput = false;
		yield return new WaitForSecondsRealtime(0.5f);
		Time.timeScale = 0;
		ShowPanelWithText($"Good job!\nCorrectly typed letters will show in <color=#00B8FF><font={inputfont.name}><b>blue</font></color> while wrong letters will appear in <color=#E50000><font={inputfont.name}><b>red</font></b></color>.\n\nLetters are <color=#FF4100><b>case-sensitive</color></b>, so you will need to match capital letters where required.", 30f);
		SetupHighlights(null);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("you can try to launch the balloon by pressing <color=#FF4100><b>Enter</color></b>.\n\nThe launch will only be successful if your <color=#FF4100><b>input exactly matches the target word</color></b>.\n\nEven a single mistake will cause the launch to fail and may trigger side effects depending on the game mode.", 30f);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("if you type something wrong you can delete the last letter by pressing <color=#FF4100><b>backspace</color></b>.\n\n<color=#FE25DF><b><i>Try typing the word and launching it.</color></b></i>\n(to launch, type out the full word correctly and hit <color=#FF4100><b>Enter</color></b>)", 30f);
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
		ShowPanelWithText("good job! you have successfully launched a balloon! It's power will match the length of the <color=#FF4100><b>word you typed</color></b>.", 30f, new Vector2(100f, balloonPos.y));
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
		while (t < 1.5f) {
			Color backgroundC = darkBackgroundColor;
			if (t > 1f) {
				backgroundC = Color.Lerp(darkBackgroundColor, Color.clear, (t - 1) / 0.5f);
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
		balloonManager.SpawnBalloon(10, 1);
		while (!opposingBalloonSpawned) yield return null;

		yield return new WaitForSecondsRealtime(0.25f);
		Time.timeScale = 0;
		ShowPanelWithText("The opposition have also launched a balloon!\n\nwhen opposing balloons collide, each balloon will damage the other with the amount equaling its own power. balloons with power <color=#FF4100><b>0 or less are destroyed</color></b>.", 30f, new Vector2(-300f, latestBalloon.transform.position.y));
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(latestBalloon.transform.position), GetWorldSizeInCanvasRectSize(new Vector2(1.5f, 1.5f)), 100f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList);
		yield return StartCoroutine(WaitForNext());
		txtHolder.gameObject.SetActive(false);
		shadowEditor.gameObject.SetActive(false);
		nxtBtn.SetActive(false);
		Time.timeScale = 1;
		destroyedWithBalloon = false;
		destroyedOnBase = false;
		while (!destroyedWithBalloon) yield return null;

		yield return new WaitForSecondsRealtime(0.2f);
		Time.timeScale = 0;
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetWorldPointToCanvasPoint(latestDestroyedBalloonPos), GetWorldSizeInCanvasRectSize(new Vector2(1.5f, 1.5f)), 100f, PinholeShaderEditor.Shape.Ellipse));
		SetupHighlights(shapeList);
		ShowPanelWithText("your balloon was stronger and so survived! Now the balloon is about to hit and damage the opposing base. The damage it does will equal its <color=#FF4100><b>remaining power</color></b>.", 30f, new Vector2(-300f, 0));
		yield return StartCoroutine(WaitForNext());
		txtHolder.gameObject.SetActive(false);
		nxtBtn.SetActive(false);
		shadowEditor.gameObject.SetActive(false);
		Time.timeScale = 1;
		while (!destroyedOnBase) yield return null;

		yield return new WaitForSecondsRealtime(0.1f);
		Time.timeScale = 0;
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(team1HP), team1HP.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		shapeList.Add(new PinholeShape(GetCenterOfRect(team2HP), team2HP.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		ShowPanelWithText("The <color=#FF4100><b>health</color></b> of the bases are shown in their respective HP bars.\n\nwhen this HP gets to 0, the team with 0hp will lose the game.", 30f);
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
		ShowPanelWithText("There is one final trick you can use, but before that, go ahead and launch a few more balloons.\n\n<color=#FE25DF><b><i>(type out and launch more balloons, be mindful of letter-case and hit \"enter\" to launch.)", 30f);
		Time.timeScale = 1;
		nxtBtn.SetActive(false);
		inputManager.canTakeInput = true;
		newBalloonedFiredByPlayer = false;
		while (!newBalloonedFiredByPlayer) yield return null;
		inputManager.SetNewTargetText(typeWordsList[2]);
		newBalloonedFiredByPlayer = false;
		while (!newBalloonedFiredByPlayer) yield return null;
		inputManager.SetNewTargetText(typeWordsList[3]);
		yield return new WaitForSecondsRealtime(1f);
		Time.timeScale = 0;
		inputManager.canTakeInput = false;
		ShowPanelWithText("You can skip the target word by typing <color=#FF4100><b>///</color></b> (three slashes in a row)!\n\nPerforming a skip will use a <color=#FF4100><b>skip charge</color></b> (shown below).\neach time you type \"/\", a <color=#FF4100>!<b></color></b> mark will appear below the skip charge number - matching the number of \"/\" currently typed in a row. ", 30f);
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(skipChargeUI), skipChargeUI.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		shapeList.Add(new PinholeShape(GetCenterOfRect(inputRect), inputRect.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		nxtBtn.SetActive(true);
		yield return StartCoroutine(WaitForNext());
		nxtBtn.SetActive(false);
		shadowEditor.gameObject.SetActive(false);
		ShowPanelWithText("<color=#FE25DF><b><i>Try skipping this text by typing ///(the key with \"?\" without holding shift.)", 30f, new Vector2(0f, -100f));
		Time.timeScale = 1;
		inputManager.canUseSkip = true;
		inputManager.canTakeInput = true;
		inputManager.canAttemptFire = false;
		skipCalled = false;
		while (!skipCalled) yield return null;
		inputManager.SetNewTargetText(typeWordsList[4]);
		Time.timeScale = 0;
		inputManager.canTakeInput = false;
		SetupHighlights(null);
		ShowPanelWithText("<color=#FF4100><b>Skip charges recharge whenever you successfully launch a balloon.</color></b>\n\nGood job! You have mastered everything you need gameplay-wise!", 30f);
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
		ShowPanelWithText("You can also see your <color=#FF4100><b>live game stats</color></b> down here. These display your:<align=left>\n•average speed (based on successfully launched balloons)\n•balloon points launched (contribution to team points)\n•average typing accuracy", 30f, new Vector2(350f, -100f));
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(statsRect), statsRect.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("One of the word's <color=#FF4100><b>dictionary meaning</color></b> will also be shown here which you can read if curious.", 30f);
		shapeList.Clear();
		shapeList.Add(new PinholeShape(GetCenterOfRect(meaningRect), meaningRect.rect.size, 50f, PinholeShaderEditor.Shape.Rectangle));
		SetupHighlights(shapeList);
		yield return StartCoroutine(WaitForNext());
		ShowPanelWithText("Alright, you're ready! Happy typing!!!\n\n\n<color=#FE25DF><b><i>The tutorial has been cleared!</color></b></i>", 30f, Vector2.down * 150f);
		InputManager.CorrectEntryProcess += NewEntryNeeded;
		InputManager.SkipAttemptResult += NewEntryNeeded;
		PlayerPrefs.SetInt(TutorialManager.TutorialClearedPlayerPrefKey, 1);
		nxtBtn.SetActive(false);
		Time.timeScale = 1;
		shadowEditor.gameObject.SetActive(false);
		inputManager.canTakeInput = true;
		inputManager.canAttemptFire = true;
		float t = 0;
		CanvasGroup g = txtHolder.AddComponent<CanvasGroup>();
		skipBtn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "FINISH TUTORIAL";
		while (t < 3) {
			t += Time.deltaTime;
			yield return null;
		}
		t = 0;
		while (t < 4) {
			t += Time.deltaTime;
			g.alpha = 1 - t / 4;
			yield return null;
		}
		txtHolder.gameObject.SetActive(false);
	}
	int currIndex = 4;
	void NewEntryNeeded(bool skipped) {
		if (!gameObject.activeInHierarchy) return;
		if (skipped) {
			needNew = true;
		}
	}
	void NewEntryNeeded(string s, ulong id) {
		if (!gameObject.activeInHierarchy) return;
		needNew = true;
	}
	bool needNew = false;
	void LateUpdate() {
		if (needNew) {
			SetNewTargetContinuous();
			needNew = false;
		}
	}
	void SetNewTargetContinuous() {
		currIndex++;
		currIndex %= typeWordsList.Count;
		if (currIndex == 3) currIndex++;
		inputManager.SetNewTargetText(typeWordsList[currIndex]);
	}
	void GameSet(GameState result) {
		StopAllCoroutines();
		gameObject.SetActive(false);
	}

	#endregion





	public Color highlightDefaultColor, darkBackgroundColor;

	void SetupHighlights(List<PinholeShape> highlightsArea = null, Color? background = null, Color? highlight = null) {
		shadowEditor.gameObject.SetActive(true);
		shadowEditor.backgroundColor = background != null ? (Color)background : darkBackgroundColor;
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
		size.x = prefWidth;
		size.y += 100f;
		txtHolder.sizeDelta = size;
		txtHolder.anchoredPosition = pos;
		LayoutRebuilder.ForceRebuildLayoutImmediate(txtHolder);
	}
	IEnumerator WaitForNext(bool setBtnActive = true) {
		nxtBtn.SetActive(setBtnActive);
		waitingForNext = true;
		while (waitingForNext) yield return null;
	}

	public void NxtClicked() {
		waitingForNext = false;
		nxtBtn.GetComponent<UIImageWobble>().ResetMagnitude();
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
