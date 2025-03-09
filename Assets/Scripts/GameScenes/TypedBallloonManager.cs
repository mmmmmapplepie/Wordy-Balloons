using System.Collections.Generic;
using Unity.Netcode;
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
	float minScale = 0.5f, maxScale = 2.5f;
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
		balloonInControl.CorrectEntryAnimation();
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
		newBalloon.transform.GetChild(newBalloon.transform.childCount - 1).GetComponent<SpriteRenderer>().color = GameData.allColorOptions[GameData.ClientID_KEY_ColorIndex_VAL[NetworkManager.Singleton.LocalClientId]];
		prevTypedSize = -1;
	}




}

