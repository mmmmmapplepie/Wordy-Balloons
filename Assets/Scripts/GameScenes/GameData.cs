using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour {
	public static List<Color> allColorOptions;
	public static Dictionary<ulong, int> ClientID_KEY_ColorIndex_VAL = new Dictionary<ulong, int>();
	public static Dictionary<string, ulong> LobbyID_KEY_ClientID_VAL = new Dictionary<string, ulong>();
	public static HashSet<string> team1 = new HashSet<string>(), team2 = new HashSet<string>();
	public static bool GameFinished = true;
}
