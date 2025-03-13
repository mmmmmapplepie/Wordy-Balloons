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

		InputManager.SkipTickChanged += SkipTickChanged;
		InputManager.TypedTextChanged += UpdateAccuracy;
		InputManager.CorrectEntryProcess += InputFired;
		InputManager.WrongEntryProcess += WrongEntry;

		BaseManager.BaseTakenDamage += BaseTakesDamageClientRpc;

		GameStateManager.GameResultSetEvent += GameResultChange;
	}
	public override void OnDestroy() {
		InputManager.TypedTextChanged -= UpdateAccuracy;
		InputManager.SkipTickChanged -= SkipTickChanged;
		InputManager.CorrectEntryProcess -= InputFired;
		InputManager.WrongEntryProcess -= WrongEntry;

		BaseManager.BaseTakenDamage -= BaseTakesDamageClientRpc;

		GameStateManager.GameResultSetEvent -= GameResultChange;

		base.OnDestroy();
	}

	void Update() {
		if (!GameStateManager.IsGameRunning()) return;
		UpdatePlayerCount();
		UpdatePointAndPointContribution();
		UpdateSpeeds();
		UpdateSkipCharges();
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
	public TextMeshProUGUI skipCharges;
	void UpdateSkipCharges() {
		skipCharges.text = InputManager.skipCharges.ToString();
	}
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
	public void AIInput(int count, ulong ID) {
		InputFired(new string('*', count), ID);
	}
	void InputFired(string s, ulong localID) {
		pointsContributedByMe += s.Length;
		UpdateTeamPointsServerRpc(GameData.team1.Contains(localID) ? Team.t1 : Team.t2, s.Length);
		pointsForCurrSpeed.Add((Time.time, s.Length));
	}
	[ServerRpc(RequireOwnership = false)]
	void UpdateTeamPointsServerRpc(Team team, int addition) {
		if (team == Team.t1) team1Points.Value += addition;
		if (team == Team.t2) team2Points.Value += addition;
	}
	int myWrongEntries = 0;
	NetworkVariable<int> team1WrongEntries = new NetworkVariable<int>(0);
	NetworkVariable<int> team2WrongEntries = new NetworkVariable<int>(0);
	void WrongEntry() {
		myWrongEntries++;
		WrongEntryServerRpc(GameData.team1.Contains(NetworkManager.Singleton.LocalClientId) ? Team.t1 : Team.t2);
	}
	[ServerRpc]
	void WrongEntryServerRpc(Team t) {
		if (t == Team.t1) team1WrongEntries.Value++;
		else team2WrongEntries.Value++;
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
	[ClientRpc]
	void BaseTakesDamageClientRpc(Team damageTeam, float ratio) {
		BaseTakesDamage(damageTeam, ratio);
	}

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



	#region gameEnd stats
	[Header("Game End Stats")]
	public TextMeshProUGUI mySpeed;
	public TextMeshProUGUI myAccuracy, ourPoints, ourWrongEntries;
	public TextMeshProUGUI opposingPoints, opposingWrongEntries;
	void GameResultChange(GameStateManager.GameResult result) {
		if (result == GameStateManager.GameResult.Draw) return;
		mySpeed.text = avgSpeedTxt.text;
		myAccuracy.text = accuracyTxt.text;
		ourPoints.text = (BalloonManager.team == Team.t1 ? team1Points.Value : team2Points.Value).ToString() + " (" + pointsContributedByMe.ToString() + ")";
		ourWrongEntries.text = (BalloonManager.team == Team.t1 ? team1WrongEntries.Value : team2WrongEntries.Value).ToString() + " (" + myWrongEntries.ToString() + ")";

		opposingPoints.text = (BalloonManager.team == Team.t1 ? team2Points.Value : team1Points.Value).ToString();
		opposingWrongEntries.text = (BalloonManager.team == Team.t1 ? team2WrongEntries.Value : team1WrongEntries.Value).ToString();
	}



	#endregion



}
