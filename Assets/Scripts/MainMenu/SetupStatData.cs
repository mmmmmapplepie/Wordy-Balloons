using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SetupStatData : MonoBehaviour {
	void Start() {
		Stats.dataLoaded += SetupStats;
		SetupStats();
	}
	void OnDestroy() {
		Stats.dataLoaded -= SetupStats;
	}



	public TextMeshProUGUI totalGames, single, multi, wins, losses, draw, highestComputer, rightEntries, wrongEntries, points, avgSpd, avgAcc;
	public UIGraphManager speedGraph, accuracyGraph;
	public void SetupStats() {
		totalGames.text = Stats.totalGames.ToString();
		single.text = Stats.singlePlayerGames.ToString();
		multi.text = Stats.multiPlayerGames.ToString();
		wins.text = Stats.wins.ToString();
		losses.text = Stats.losses.ToString();
		draw.text = Stats.draws.ToString();
		highestComputer.text = Stats.highestComputerSpeedDefeated.ToString();
		rightEntries.text = Stats.rightEntries.ToString();
		wrongEntries.text = Stats.wrongEntries.ToString();
		points.text = Stats.pointsCreated.ToString();
		avgSpd.text = Stats.averageSpeed.ToString("F2");
		avgAcc.text = Stats.averageAccuracy.ToString("F2");

		List<(float, float, string)> speedHistory = new List<(float, float, string)>();
		for (int i = 50; i > 0; i--) {
			if (i > Stats.lastFiftySpeed.Count) { speedHistory.Add((i, 0, "-")); continue; }
			speedHistory.Add((i, Stats.lastFiftySpeed[Stats.lastFiftySpeed.Count - i], ""));
		}
		speedGraph.SetGraph(speedHistory);
		List<(float, float, string)> accHistory = new List<(float, float, string)>();
		// List<(float, float, string)> dummyList = new List<(float, float, string)> {
		// 	(1f,1f,"6"),
		// 	(2f,200f,"5"),
		// 	(3f,150f,"4"),
		// 	(4f,0f,"3"),
		// 	(5f,100f,"2"),
		// 	(-5f,300f,"1"),
		// };
		// for (int i = 50; i > 0; i--) {
		// 	if (i > dummyList.Count) { accHistory.Add((i, 0, "-")); continue; }
		// 	accHistory.Add(dummyList[dummyList.Count - i]);
		// }
		for (int i = 50; i > 0; i--) {
			if (i > Stats.lastFiftyAccuracy.Count) { accHistory.Add((i, 0, "-")); continue; }
			accHistory.Add((i, Stats.lastFiftyAccuracy[Stats.lastFiftyAccuracy.Count - i], ""));
		}
		accuracyGraph.SetGraph(accHistory);
	}


}
