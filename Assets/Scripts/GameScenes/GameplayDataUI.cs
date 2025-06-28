using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameplayDataUI : NetworkBehaviour {


	void Awake() {
		startTime = Time.time;

		UpdateGameMode();

		InputManager.SkipTickChanged += SkipTickChanged;
		InputManager.TypedTextChanged += UpdateAccuracy;
		InputManager.WrongEntryProcess += WrongEntry;
		InputManager.CorrectEntryProcess += CorrectEntry;

		BaseManager.BaseTakenDamage += BaseTakesDamageClientRpc;
		BaseManager.BaseHPSet += BaseHPSet;

		BalloonManager.BalloonSpawned += BalloonFired;

		GameStateManager.GameResultSetEvent += GameResultChange;
		GameUI.SaveData += UpdateStats;
	}

	public override void OnDestroy() {
		InputManager.TypedTextChanged -= UpdateAccuracy;
		InputManager.SkipTickChanged -= SkipTickChanged;
		InputManager.WrongEntryProcess -= WrongEntry;
		InputManager.CorrectEntryProcess -= CorrectEntry;

		BaseManager.BaseTakenDamage -= BaseTakesDamageClientRpc;
		BaseManager.BaseHPSet -= BaseHPSet;

		BalloonManager.BalloonSpawned -= BalloonFired;

		GameStateManager.GameResultSetEvent -= GameResultChange;
		GameUI.SaveData -= UpdateStats;

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
		if (GameData.PlayMode != PlayModeEnum.Multiplayer) {
			playerCountTxt.text = "";
		} else {
			if (NetworkManager.Singleton.IsServer) {
				playerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;
				playerCountTxt.text = "Players: " + playerCount.Value.ToString();
			} else if (NetworkManager.Singleton.IsClient) {
				playerCountTxt.text = "Players: " + playerCount.Value.ToString();
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


	#region AverageSpeed  & Points contributed
	int pointsContributedByMe = 0;
	NetworkVariable<int> team1Points = new NetworkVariable<int>(0);
	NetworkVariable<int> team2Points = new NetworkVariable<int>(0);
	float timeToSample = 10f;
	float startTime = 0;
	List<(float timestamp, int points)> pointsForCurrSpeed = new List<(float timestamp, int points)>();
	public TextMeshProUGUI currSpeedTxt, avgSpeedTxt, totalPointsTxt, pointContributionTxt;
	public void BalloonFired(int count, ulong ID) {
		if (NetworkManager.Singleton.ConnectedClientsIds.Contains(ID)) {
			ClientRpcParams param = new ClientRpcParams {
				Send = new ClientRpcSendParams {
					TargetClientIds = new ulong[] { ID }
				}
			};
			UpdatePointsClientRpc(count, param);
		}

		UpdateTeamPointsServerRpc(GameData.team1.Contains(ID) ? Team.t1 : Team.t2, count);
	}
	[ClientRpc]
	void UpdatePointsClientRpc(int points, ClientRpcParams clientRpcParams = default) {
		pointsContributedByMe += points;
	}

	[ServerRpc(RequireOwnership = false)]
	void UpdateTeamPointsServerRpc(Team team, int addition) {
		if (team == Team.t1) team1Points.Value += addition;
		if (team == Team.t2) team2Points.Value += addition;
	}
	int myWrongEntries = 0, myCorrectEntries = 0;
	NetworkVariable<int> team1WrongEntries = new NetworkVariable<int>(0);
	NetworkVariable<int> team2WrongEntries = new NetworkVariable<int>(0);
	void CorrectEntry(string entry, ulong id) {
		myCorrectEntries++;
		pointsForCurrSpeed.Add((Time.time, entry.Length));
	}
	void WrongEntry() {
		WrongEntryServerRpc(GameData.team1.Contains(NetworkManager.Singleton.LocalClientId) ? Team.t1 : Team.t2);
		myWrongEntries++;
	}
	[ServerRpc(RequireOwnership = false)]
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
	int avgSpd = 0;
	void UpdateSpeeds() {
		for (int i = 0; i < pointsForCurrSpeed.Count;) {
			if (Time.time - pointsForCurrSpeed[i].timestamp > timeToSample) pointsForCurrSpeed.RemoveAt(0);
			else {
				break;
			}
		}
		float sampleRatio = 60f;
		avgSpd = Mathf.FloorToInt(sampleRatio * pointsContributedByMe / (Time.time - startTime));
		avgSpeedTxt.text = avgSpd.ToString();

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
	int avgAccuracy = 0;
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
		avgAccuracy = Mathf.FloorToInt(100f * totalTypedAccurate / totalTyped);
		accuracyTxt.text = avgAccuracy.ToString() + "%";
		prevTyped = tempTyped;
	}
	#endregion


	#region  BaseDamage
	[ClientRpc]
	void BaseTakesDamageClientRpc(Team damageTeam, int remainingHP) {
		BaseTakesDamage(damageTeam, remainingHP);
	}

	void BaseHPSet() {
		BaseTakesDamage(Team.t1, BaseManager.team1MaxHP.Value);
		BaseTakesDamage(Team.t2, BaseManager.team2MaxHP.Value);
	}

	public Slider homeHPSlider, oppHPSlider;
	public TextMeshProUGUI homeHPTxt, awayHPTxt;
	public CameraShaker camShaker;

	void BaseTakesDamage(Team damagedTeam, int remainingHP) {
		float hpRatio = (float)remainingHP / (float)(damagedTeam == Team.t1 ? BaseManager.team1MaxHP.Value : BaseManager.team2MaxHP.Value);
		Slider main;
		TextMeshProUGUI hpTxt = homeHPTxt;
		if (damagedTeam == BalloonManager.team) {
			main = homeHPSlider;
			hpTxt = homeHPTxt;
		} else {
			main = oppHPSlider;
			hpTxt = awayHPTxt;
		}
		if (damagedTeam == BalloonManager.team) {
			float diff = (main.value - hpRatio);
			if (diff > 0) {
				camShaker.StartShaker(Mathf.Lerp(0, 0.5f, diff / 0.4f), Mathf.Lerp(0, 0.5f, diff / 0.4f));
			}
		}
		main.value = hpRatio;
		hpTxt.text = remainingHP.ToString();
	}






	#endregion


	#region gameEnd stats
	[Header("Game End Stats")]
	public TextMeshProUGUI mySpeed;
	public TextMeshProUGUI myAccuracy, ourPoints, ourWrongEntries;
	public TextMeshProUGUI opposingPoints, opposingWrongEntries;
	void GameResultChange(GameStateManager.GameResult result) {
		UpdateStats(result);
		if (result == GameStateManager.GameResult.Draw) return;
		mySpeed.text = avgSpeedTxt.text;
		myAccuracy.text = accuracyTxt.text;
		ourPoints.text = (BalloonManager.team == Team.t1 ? team1Points.Value : team2Points.Value).ToString() + " (" + pointsContributedByMe.ToString() + ")";
		ourWrongEntries.text = (BalloonManager.team == Team.t1 ? team1WrongEntries.Value : team2WrongEntries.Value).ToString() + " (" + myWrongEntries.ToString() + ")";

		opposingPoints.text = (BalloonManager.team == Team.t1 ? team2Points.Value : team1Points.Value).ToString();
		opposingWrongEntries.text = (BalloonManager.team == Team.t1 ? team2WrongEntries.Value : team1WrongEntries.Value).ToString();
	}
	void UpdateStats() {
		GameStateManager.GameResult loss = GameStateManager.GameResult.Team1Win;
		if (BalloonManager.team == Team.t1) loss = GameStateManager.GameResult.Team2Win;
		UpdateStats(loss);
	}
	bool dataUpdated = false;
	void UpdateStats(GameStateManager.GameResult r) {
		if (dataUpdated) return;
		dataUpdated = true;
		if (GameData.PlayMode == PlayModeEnum.Tutorial) return;
		int result = GetWinDrawLossResult(r);

		Stats.totalGames++;
		if (result == 0) Stats.draws++;
		else if (result == 1) Stats.wins++;
		else Stats.losses++;

		if (GameData.PlayMode != PlayModeEnum.Multiplayer) {
			Stats.singlePlayerGames++;
			if (result == 1 && SinglePlayerAI.AISpeed > Stats.highestComputerSpeedDefeated) Stats.highestComputerSpeedDefeated = SinglePlayerAI.AISpeed;
		} else Stats.multiPlayerGames++;

		Stats.rightEntries += myCorrectEntries;
		Stats.wrongEntries += myWrongEntries;
		Stats.pointsCreated += pointsContributedByMe;

		Stats.averageSpeed = (float)((Stats.totalGames - 1) * Stats.averageSpeed + avgSpd) / (float)Stats.totalGames;
		Stats.averageAccuracy = (float)((Stats.totalGames - 1) * Stats.averageAccuracy + avgAccuracy) / (float)Stats.totalGames;
		Stats.AddToSpeedList(avgSpd);
		Stats.AddToAccuracyList(avgAccuracy);

		Stats.SetData();
	}
	int GetWinDrawLossResult(GameStateManager.GameResult result) {
		if (GameStateManager.GameResult.Draw == result) return 0;
		else if (result == GameStateManager.GameResult.Team1Win && BalloonManager.team == Team.t1 || result == GameStateManager.GameResult.Team2Win && BalloonManager.team == Team.t2) return 1;
		else return -1;
	}



	#endregion

}

