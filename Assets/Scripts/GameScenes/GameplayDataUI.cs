using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameplayDataUI : NetworkBehaviour {



	void Start() {
		startTime = Time.time;

		UpdateGameMode();

		InputManager.skipTickChanged += SkipTickChanged;
		InputManager.TypedTextChanged += UpdateAccuracy;
		InputManager.CorrectInputProcess += InputFired;

		BaseManager.BaseTakenDamage += BaseTakesDamage;
	}
	public override void OnDestroy() {
		InputManager.TypedTextChanged -= UpdateAccuracy;
		InputManager.skipTickChanged -= SkipTickChanged;
		InputManager.CorrectInputProcess -= InputFired;

		BaseManager.BaseTakenDamage += BaseTakesDamage;

		base.OnDestroy();
	}

	void Update() {
		UpdatePlayerCount();
		UpdatePointAndPointContribution();
		UpdateSpeeds();
	}





	#region PlayerCount
	public TextMeshProUGUI playerCountTxt;
	NetworkVariable<int> playerCount = new NetworkVariable<int>();
	void UpdatePlayerCount() {
		if (GameData.InSinglePlayerMode) {
			playerCountTxt.text = "";
		} else {
			if (NetworkManager.Singleton.IsServer) {
				playerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;
			}
			if (NetworkManager.Singleton.IsClient) {
				playerCountTxt.text = "Players: " + playerCount.ToString();
			}

		}
	}
	#endregion



	#region Gamemode
	public TextMeshProUGUI gameModeTxt;
	void UpdateGameMode() {
		gameModeTxt.text = "Mode: " + GameData.gameMode.ToString();
	}
	#endregion



	#region Skip
	public GameObject tick1, tick2;
	void SkipTickChanged(int ticks) {
		tick1.SetActive(ticks > 0);
		tick2.SetActive(ticks > 1);
	}
	#endregion
	//skips remaining, skip charges hit


	#region AverageSpeed - in characters/permin & Points contributed
	int pointsContributedByMe = 0;
	NetworkVariable<int> team1Points = new NetworkVariable<int>(0);
	NetworkVariable<int> team2Points = new NetworkVariable<int>(0);
	float timeToSample = 10f;
	float startTime = 0;
	List<(float timestamp, int points)> pointsForCurrSpeed = new List<(float timestamp, int points)>();
	public TextMeshProUGUI currSpeedTxt, avgSpeedTxt, totalPointsTxt, pointContributionTxt;
	void InputFired(string s, ulong localID) {
		pointsContributedByMe += s.Length;
		if (GameData.team1IDList.Contains(localID)) team1Points.Value += s.Length;
		else team2Points.Value += s.Length;

		pointsForCurrSpeed.Add((Time.time, s.Length));
	}

	void UpdatePointAndPointContribution() {
		totalPointsTxt.text = pointsContributedByMe.ToString();
		int teamPoints = BalloonManager.team == Team.t1 ? team1Points.Value : team2Points.Value;
		if (teamPoints == 0) return;
		pointContributionTxt.text = ((pointsContributedByMe / teamPoints) * 100).ToString("f0") + "%";
	}

	void UpdateSpeeds() {
		for (int i = 0; i < pointsForCurrSpeed.Count;) {
			if (Time.time - pointsForCurrSpeed[i].timestamp > timeToSample) pointsForCurrSpeed.RemoveAt(0);
			else {
				break;
			}
		}
		float sampleRatio = 60f;
		avgSpeedTxt.text = (sampleRatio * pointsContributedByMe / (Time.time - startTime)).ToString("f0");

		if (pointsForCurrSpeed.Count == 0) { currSpeedTxt.text = "0"; return; }

		int latestTotalPoints = pointsForCurrSpeed.Sum(x => x.points);
		float oldestTime = pointsForCurrSpeed[0].timestamp;
		float calculationPeriod = Time.time - oldestTime < 1 ? 1 : Time.time - oldestTime;
		currSpeedTxt.text = (sampleRatio * latestTotalPoints / calculationPeriod).ToString("f0"); ;
	}
	#endregion



	#region Accuracy
	public TextMeshProUGUI accuracyTxt;
	string targetText = "";
	string prevTyped = "";
	int totalTyped = 0;
	int totalTypedAccurate = 0;
	void UpdateAccuracy() {
		if (targetText != InputManager.Instance.targetString) {
			targetText = InputManager.Instance.targetString;
			prevTyped = "";
			return;
		}
		string tempTyped = InputManager.Instance.typedString;
		if (tempTyped.Length <= prevTyped.Length) {
			prevTyped = tempTyped;
			return;
		}

		for (int i = prevTyped.Length; i < tempTyped.Length; i++) {
			if (tempTyped[i] == targetText[i]) totalTypedAccurate++;
			totalTyped++;
		}

		accuracyTxt.text = (100f * totalTypedAccurate / totalTyped).ToString("f0") + "%";

		prevTyped = tempTyped;
	}
	#endregion


	#region  BaseDamage


	public Slider homeHPSlider, oppHPSlider;
	void BaseTakesDamage(Team damagedTeam, float hpRatio) {
		Slider main;
		if (damagedTeam == BalloonManager.team) {
			main = homeHPSlider;
		} else {
			main = oppHPSlider;
		}
		main.value = hpRatio;
	}







	#endregion



}
