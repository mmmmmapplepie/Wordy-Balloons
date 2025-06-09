using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameData : NetworkBehaviour {
	public static PlayModeEnum PlayMode = PlayModeEnum.Multiplayer;
	public static GameMode gameMode = default;
	public static List<Color> allColorOptions;
	public static Dictionary<ulong, int> ClientID_KEY_ColorIndex_VAL = new Dictionary<ulong, int>();
	public static Dictionary<ulong, string> ClientID_KEY_LobbyID_VAL = new Dictionary<ulong, string>();
	public static HashSet<ulong> team1 = new HashSet<ulong>(), team2 = new HashSet<ulong>();
	public static DictionaryMode Dictionary = DictionaryMode.Complete;

	public static Team GetTeamFromClientID(ulong ID) {
		if (team1.Contains(ID)) return Team.t1;
		return Team.t2;
	}

}
public enum Team { t1, t2 };
//normal is normal, eraser everything is erased when typing something wrong, own enemy - if you fire with mistake you take damage instead.
public enum GameMode { Normal, Eraser, Own_Enemy }
public enum DictionaryMode { Beginner, Complete }
public enum PlayModeEnum { Multiplayer, Tutorial, BasicPVE }
