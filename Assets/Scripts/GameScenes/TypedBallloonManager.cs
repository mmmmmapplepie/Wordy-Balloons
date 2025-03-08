using System.Collections.Generic;
using UnityEngine;

public class TypedBallloonManager : MonoBehaviour {
	public GameObject typedBalloonPrefab, waterLeakEffect, balloonPopEffect;
	TypedBalloonAnimations balloonInControl;
	void Start() {
		InputManager.InputProcessFinished += InputChanged;
		InputManager.WrongEntryFinished += WrongEntry;
		InputManager.CorrectEntryFinished += CorrectEntry;
		InputManager.NewTextSet += NewTextSet;
	}
	void OnDestroy() {
		InputManager.InputProcessFinished -= InputChanged;
		InputManager.WrongEntryFinished -= WrongEntry;
		InputManager.CorrectEntryFinished -= CorrectEntry;
		InputManager.NewTextSet -= NewTextSet;
	}
	float minScale = 0.5f, maxScale = 3f;
	int prevTypedSize = 0;
	float impulseSize = 5f;
	void InputChanged() {
		if (balloonInControl == null) return;
		int currTypedSize = InputManager.Instance.typedString.Length;
		if (currTypedSize == prevTypedSize) return;

		float fillRatio = (float)currTypedSize / (float)InputManager.Instance.targetString.Length;
		balloonInControl.scaleFactor = fillRatio;
		if (currTypedSize < prevTypedSize) {
			Instantiate(waterLeakEffect, balloonInControl.transform.position, Quaternion.identity);
		}
		balloonInControl.AddImpulse(Random.Range(-impulseSize, impulseSize));
		prevTypedSize = currTypedSize;
	}
	void WrongEntry() {
		Instantiate(balloonPopEffect, balloonInControl.transform.position, Quaternion.identity);
	}
	void CorrectEntry() {
		balloonInControl.CorrectEntryAnimation();
		balloonInControl = null;
	}
	void NewTextSet(string txt) {
		if (balloonInControl != null) {
			Instantiate(balloonPopEffect, balloonInControl.transform.position, Quaternion.identity);
			Destroy(balloonInControl.transform.root.gameObject);
		}
		GameObject newBalloon = Instantiate(typedBalloonPrefab, transform.position, Quaternion.identity);
		balloonInControl = newBalloon.GetComponentInChildren<TypedBalloonAnimations>();
		prevTypedSize = 0;
	}




}

