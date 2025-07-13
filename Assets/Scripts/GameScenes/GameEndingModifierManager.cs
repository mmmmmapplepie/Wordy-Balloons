using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameEndingModifierManager : NetworkBehaviour {

	public static string GetNameFromEnum(GameEndingMode type) {
		switch (type) {
			case GameEndingMode.Endurance:
				return "Endurance";
			case GameEndingMode.Drain:
				return "Drain";
			case GameEndingMode.SuddenDeath:
				return "Sudden Death";
			case GameEndingMode.Speedup:
				return "Speed Up";
			case GameEndingMode.Damageup:
				return "Damage Up";
		}
		return null;
	}

	public float timerPeriod = 10f;

	void Awake() {
		GameStateManager.GameStartEvent += GameStarted;
	}
	public override void OnDestroy() {
		GameStateManager.GameStartEvent -= GameStarted;
		base.OnDestroy();
	}

	public bool? gameEndModeOn { get; private set; } = null;
	void GameStarted() {
		if (GameData.GameEndingMode == GameEndingMode.Endurance) return;
		gameEndModeOn = false;
		GameEndModeOnEvent?.Invoke();
	}
	float t = 0;
	public static event System.Action GameEndModeOnEvent, EndingModulated;
	void Update() {
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || gameEndModeOn == null) return;
		t += Time.deltaTime;
		if (gameEndModeOn == false && t > GameData.GameEndingModulationTime * 60f) {
			gameEndModeOn = true;
			Modify();
			GameModeOnClientRpc();
			t -= GameData.GameEndingModulationTime * 60f;
		} else if (t > timerPeriod) {
			t -= timerPeriod;
			Modify();
		}
	}

	void SetUpdatePeriod() {
		switch (GameData.GameEndingMode) {
			case GameEndingMode.Drain:
				timerPeriod = 10f;
				break;
			case GameEndingMode.SuddenDeath:
				timerPeriod = float.PositiveInfinity;
				break;
			case GameEndingMode.Speedup:
				timerPeriod = 10f;
				break;
			case GameEndingMode.Damageup:
				timerPeriod = 20f;
				break;
		}
	}
	int calls = 0;
	void Modify() {
		switch (GameData.GameEndingMode) {
			case GameEndingMode.Drain:
				BaseManager.DamageBase(Team.t1, 5);
				BaseManager.DamageBase(Team.t2, 5);
				break;
			case GameEndingMode.SuddenDeath:
				BalloonManager.BallonDamageMultiplier = int.MaxValue;
				break;
			case GameEndingMode.Speedup:
				calls++;
				BalloonManager.Flytime = BalloonManager.BaseFlytime / (1f + (0.1f * calls));
				if (BalloonManager.Flytime == 0) BalloonManager.Flytime = Mathf.Epsilon;
				break;
			case GameEndingMode.Damageup:
				if (BalloonManager.BallonDamageMultiplier != int.MaxValue) BalloonManager.BallonDamageMultiplier += 1;
				break;
		}
		EndingModulatedClientRpc();
	}

	[ClientRpc]
	void EndingModulatedClientRpc() {
		EndingModulated?.Invoke();
	}
	[ClientRpc]
	void GameModeOnClientRpc() {
		SetUpdatePeriod();
		GameEndModeOnEvent?.Invoke();
	}




}
