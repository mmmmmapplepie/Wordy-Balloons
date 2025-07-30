using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TypedBallloonManager : MonoBehaviour {
	public GameObject typedBalloonPrefab, waterLeakEffect, balloonPopEffect, starEffect;
	public Transform typedBalloonTarget;
	TypedBalloonAnimations balloonInControl;
	void Awake() {
		InputManager.InputProcessFinished += InputChanged;
		InputManager.WrongEntryFinished += WrongEntry;
		InputManager.CorrectEntryFinished += CorrectEntry;
		InputManager.NewTextSet += NewTextSet;

		GameStateManager.GameResultSetEvent += GameResultSet;
	}

	void OnDestroy() {
		InputManager.InputProcessFinished -= InputChanged;
		InputManager.WrongEntryFinished -= WrongEntry;
		InputManager.CorrectEntryFinished -= CorrectEntry;
		InputManager.NewTextSet -= NewTextSet;

		GameStateManager.GameResultSetEvent -= GameResultSet;
	}
	float minScale = 0.5f, maxScale = 2f;
	int prevTypedSize = -1;
	float impulseSize = 5f;
	void InputChanged() {
		if (balloonInControl == null) return;
		int currTypedSize = InputManager.Instance.typedString.Length;
		if (prevTypedSize == currTypedSize) return;
		int targetSize = InputManager.Instance.targetString.Length;
		float fillRatio = (float)currTypedSize / (float)targetSize;
		float targetMaxSize = Mathf.Max(minScale, targetSize * maxScale / 15f);
		targetMaxSize = Mathf.Min(maxScale, targetMaxSize);
		balloonInControl.scaleFactor = Mathf.Lerp(minScale, targetMaxSize, fillRatio);
		if (currTypedSize < prevTypedSize) {
			Instantiate(waterLeakEffect, transform.position, Quaternion.identity);
		}
		balloonInControl.AddImpulse(Random.Range(-impulseSize, impulseSize));
		prevTypedSize = currTypedSize;
	}
	void WrongEntry() {
		Instantiate(balloonPopEffect, balloonInControl.transform.position, Quaternion.identity);
	}
	void CorrectEntry() {
		Instantiate(starEffect, balloonInControl.transform.position, Quaternion.identity);
		balloonInControl.CorrectEntryAnimation(typedBalloonTarget.localPosition);
		balloonInControl = null;
	}
	void NewTextSet(string txt) {
		if (balloonInControl != null) {
			Instantiate(balloonPopEffect, balloonInControl.transform.position, Quaternion.identity);
			Destroy(balloonInControl.gameObject);
		}
		CreateNewTypedBalloon();
		InputChanged();
	}

	void CreateNewTypedBalloon() {
		GameObject newBalloon = Instantiate(typedBalloonPrefab, transform.position, Quaternion.identity, transform);
		balloonInControl = newBalloon.GetComponentInChildren<TypedBalloonAnimations>();
		prevTypedSize = -1;
	}

	private void GameResultSet(GameState result) {
		if (result == GameState.InPlay) return;
		if (balloonInControl != null) Destroy(balloonInControl.gameObject);
	}


}

