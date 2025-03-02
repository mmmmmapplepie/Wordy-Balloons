using System;
using UnityEngine;

public class GameModeInputManager : MonoBehaviour {


	void Start() {
		SetupInputFunctionWRTGameMode();
		InputManager.WrongEntryFinished += WrongInput;
	}
	void OnDestroy() {
		InputManager.WrongEntryFinished -= WrongInput;
	}

	Action wrongInputAction;
	void SetupInputFunctionWRTGameMode() {
		switch (GameData.gameMode) {
			case GameMode.Normal:
				wrongInputAction = null;
				break;
			case GameMode.Eraser:
				wrongInputAction = Eraser;
				break;
			case GameMode.OwnEnemy:
				wrongInputAction = OwnEnemy;
				break;
		}
	}

	void WrongInput() {
		wrongInputAction?.Invoke();
	}

	void Eraser() {
		InputManager.Instance.ResetTypedText();
	}

	public BaseManager baseManager;
	void OwnEnemy() {
		baseManager.DamageBaseServerRpc(BalloonManager.team, InputManager.Instance.typedString.Length);
		InputManager.Instance.ResetTypedText();
	}



}
