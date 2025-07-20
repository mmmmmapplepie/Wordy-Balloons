using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyOption : MonoBehaviour {
	public TextMeshProUGUI lobbyName, lobbyMode, dictionary, lobbyPopulation, gameEndMode;
	public void SetOption(LobbyUI lobbyui, Lobby lobby) {
		GetComponent<Button>().onClick.AddListener(() => lobbyui.JoinLobby(lobby));
		lobbyName.text = lobby.Name;
		lobbyMode.text = lobby.Data[LobbyManager.GameMode].Value == ((GameMode)2).ToString() ? "Own Enemy" : lobby.Data[LobbyManager.GameMode].Value;
		dictionary.text = lobby.Data[LobbyManager.Dictionary].Value;
		gameEndMode.text = lobby.Data[LobbyManager.GameEndMode].Value + "\n(" + lobby.Data[LobbyManager.GameEndTime].Value + ")";
		if (lobby.Data[LobbyManager.GameEndMode].Value == "Endurance") gameEndMode.text = "Endurance";
		lobbyPopulation.text = (lobby.MaxPlayers - lobby.AvailableSlots) + "/" + lobby.MaxPlayers;
	}
}
