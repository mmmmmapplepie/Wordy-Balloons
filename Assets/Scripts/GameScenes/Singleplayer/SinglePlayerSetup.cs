using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SinglePlayerSetup : NetworkBehaviour {

	IEnumerator Start() {
		if (NetworkManager.Singleton.IsConnectedClient) NetworkManager.Singleton.Shutdown();

		while (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.ShutdownInProgress) yield return null;

		FindObjectOfType<UnityTransport>().SetConnectionData("127.0.0.1", 7777);

		NetworkManager.Singleton.StartHost();

		ulong selfID = NetworkManager.Singleton.LocalClientId;
		ulong computerID = selfID + 1;

		// 	public static List<Color> allColorOptions;
		List<Color> colors = new List<Color>() { Color.cyan, Color.red };
		GameData.allColorOptions = colors;
		// public static Dictionary<ulong, int> ClientID_KEY_ColorIndex_VAL = new Dictionary<ulong, int>();
		GameData.ClientID_KEY_ColorIndex_VAL.Clear();
		GameData.ClientID_KEY_ColorIndex_VAL.Add(selfID, 0);
		GameData.ClientID_KEY_ColorIndex_VAL.Add(computerID, 1);

		// public static List<ulong> team1IDList = new List<ulong>(), team2IDList = new List<ulong>();
		// public static HashSet<string> team1 = new HashSet<string>(), team2 = new HashSet<string>();
		GameData.team1IDList.Clear();
		GameData.team1IDList.Add(selfID);
		GameData.team2IDList.Clear();
		GameData.team2IDList.Add(computerID);
		GameData.team1.Clear();
		GameData.team1.Add(selfID.ToString());
		GameData.team2.Clear();
		GameData.team2.Add(computerID.ToString());

		GameData.InSinglePlayerMode = true;

		NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
		//maybe not required.
		// public static Dictionary<string, ulong> LobbyID_KEY_ClientID_VAL = new Dictionary<string, ulong>();
	}
	// IEnumerator WaitForNetcode() {
	// 	while (NetworkManager.Singleton.IsHost)
	// }
}
