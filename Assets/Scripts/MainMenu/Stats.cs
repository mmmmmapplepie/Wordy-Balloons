using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using WebSocketSharp;

public class Stats : MonoBehaviour {

	void Awake() {
		LoadData();
	}
	void LoadData() {
		LoadGamesData();
		LoadSpeedData();
		LoadAccuracyAndPointsData();
		dataLoaded?.Invoke();
	}

	public static void SetData() {
		SetGamesPlayedData();
		SetSpeedData();
		SetAccuracyAndPoints();
	}

	public static event System.Action dataLoaded;


	#region wins losses draws total games
	const string Games = "Games", Wins = "Wins", Loss = "Loss", Draw = "Draw", Single = "Single", Multi = "Multi";
	public static int totalGames, wins, losses, draws, singlePlayerGames, multiPlayerGames;
	void LoadGamesData() {
		totalGames = GetPlayerPrefFromKey<int>(Games);
		wins = GetPlayerPrefFromKey<int>(Wins);
		losses = GetPlayerPrefFromKey<int>(Loss);
		draws = GetPlayerPrefFromKey<int>(Draw);
		singlePlayerGames = GetPlayerPrefFromKey<int>(Single);
		multiPlayerGames = GetPlayerPrefFromKey<int>(Multi);
	}
	static void SetGamesPlayedData() {
		SetPlayerPref<int>(Games, totalGames);
		SetPlayerPref<int>(Wins, wins);
		SetPlayerPref<int>(Loss, losses);
		SetPlayerPref<int>(Draw, draws);
		SetPlayerPref<int>(Single, singlePlayerGames);
		SetPlayerPref<int>(Multi, multiPlayerGames);
	}

	#endregion

	#region speed stats

	public static int highestComputerSpeedDefeated;
	public static float averageSpeed;
	public static List<int> lastFiftySpeed { get; private set; } = new List<int>();
	const string ComputerSpeed = "ComputerSpeed", AvgSpeed = "AvgSpeed", LastFiftySpeed = "LastFiftySpeed";
	const char DataSeparator = ':';
	void LoadSpeedData() {
		highestComputerSpeedDefeated = GetPlayerPrefFromKey<int>(ComputerSpeed);
		averageSpeed = GetPlayerPrefFromKey<float>(AvgSpeed);
		lastFiftySpeed.Clear();
		string lastFiftyStr = GetPlayerPrefFromKey<string>(LastFiftySpeed);
		if (!lastFiftyStr.IsNullOrEmpty()) lastFiftySpeed = lastFiftyStr.Split(DataSeparator).Select(int.Parse).ToList();
	}
	public static void AddToSpeedList(int value) {
		lastFiftySpeed.Add(value);
		while (lastFiftySpeed.Count > 50) lastFiftySpeed.RemoveAt(0);
	}

	static void SetSpeedData() {
		SetPlayerPref<int>(ComputerSpeed, highestComputerSpeedDefeated);
		SetPlayerPref<float>(AvgSpeed, averageSpeed);
		StringBuilder s = new StringBuilder();
		foreach (int i in lastFiftySpeed) {
			s.Append(i.ToString()).Append(DataSeparator);
		}
		if (s.Length > 0) s.Remove(s.Length - 1, 1);
		SetPlayerPref<string>(LastFiftySpeed, s.ToString());
	}

	#endregion

	#region accuracy and points
	// 	global stats:
	// -latest accuracy(and avg of last 50 games)
	// -balloons fired(right entries, wrong entries)
	// -points created
	public static float averageAccuracy;
	public static int rightEntries, wrongEntries, pointsCreated;
	public static List<int> lastFiftyAccuracy { get; private set; } = new List<int>();
	const string RightEntries = "RightEntries", PointsCreated = "PointsCreated", WrongEntries = "WrongEntries"
	, AvgAccuracy = "AvgAccuracy", LastFiftyAccuracy = "LastFiftyAccuracy";

	void LoadAccuracyAndPointsData() {
		rightEntries = GetPlayerPrefFromKey<int>(RightEntries);
		wrongEntries = GetPlayerPrefFromKey<int>(WrongEntries);
		pointsCreated = GetPlayerPrefFromKey<int>(PointsCreated);
		averageAccuracy = GetPlayerPrefFromKey<float>(AvgAccuracy);
		lastFiftyAccuracy.Clear();
		string lastFiftyAcc = GetPlayerPrefFromKey<string>(LastFiftyAccuracy);
		if (!lastFiftyAcc.IsNullOrEmpty()) lastFiftyAccuracy = lastFiftyAcc.Split(DataSeparator).Select(int.Parse).ToList();
	}
	public static void AddToAccuracyList(int value) {
		lastFiftyAccuracy.Add(value);
		while (lastFiftyAccuracy.Count > 50) lastFiftyAccuracy.RemoveAt(0);
	}

	static void SetAccuracyAndPoints() {
		SetPlayerPref<int>(RightEntries, rightEntries);
		SetPlayerPref<int>(WrongEntries, wrongEntries);
		SetPlayerPref<int>(PointsCreated, pointsCreated);
		SetPlayerPref<float>(AvgAccuracy, averageAccuracy);
		StringBuilder s = new StringBuilder();
		foreach (int i in lastFiftyAccuracy) {
			s.Append(i.ToString()).Append(DataSeparator);
		}
		if (s.Length > 0) s.Remove(s.Length - 1, 1);
		SetPlayerPref<string>(LastFiftyAccuracy, s.ToString());

	}







	#endregion

	static T GetPlayerPrefFromKey<T>(string key) {
		if (!PlayerPrefs.HasKey(key)) {
			if (typeof(T) == typeof(string)) PlayerPrefs.SetString(key, default);
			if (typeof(T) == typeof(int)) PlayerPrefs.SetInt(key, default);
			if (typeof(T) == typeof(float)) PlayerPrefs.SetFloat(key, default);
			return default;
		}

		if (typeof(T) == typeof(string)) return (T)(object)PlayerPrefs.GetString(key);
		if (typeof(T) == typeof(int)) return (T)(object)PlayerPrefs.GetInt(key);
		if (typeof(T) == typeof(float)) return (T)(object)PlayerPrefs.GetFloat(key);

		throw new System.NotSupportedException($"Type {typeof(T)} is not supported.");
	}
	static bool SetPlayerPref<T>(string key, T value) {
		if (!PlayerPrefs.HasKey(key)) return false;

		if (typeof(T) == typeof(string)) {
			PlayerPrefs.SetString(key, (string)(object)value);
			return true;
		}

		if (typeof(T) == typeof(int)) {
			PlayerPrefs.SetInt(key, (int)(object)value);
			return true;
		}

		if (typeof(T) == typeof(float)) {
			PlayerPrefs.SetFloat(key, (float)(object)value);
			return true;
		}

		return false;
	}
	public void ClearAllData() {
		bool tutorialClear = PlayerPrefs.HasKey(TutorialManager.TutorialClearedPlayerPrefKey);
		float masterVol = PlayerPrefs.GetFloat(VolumeControl.MasterVol);
		float effectVol = PlayerPrefs.GetFloat(VolumeControl.EffectVol);
		float bgmVol = PlayerPrefs.GetFloat(VolumeControl.BGMVol);

		PlayerPrefs.DeleteAll();
		LoadData();

		if (tutorialClear) PlayerPrefs.SetInt(TutorialManager.TutorialClearedPlayerPrefKey, 1);
		PlayerPrefs.SetFloat(VolumeControl.MasterVol, masterVol);
		PlayerPrefs.SetFloat(VolumeControl.EffectVol, effectVol);
		PlayerPrefs.SetFloat(VolumeControl.BGMVol, bgmVol);
	}

}
