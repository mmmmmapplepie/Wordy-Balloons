using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyOption : MonoBehaviour {
	public TextMeshProUGUI lobbyName, lobbyMode, dictionary, lobbyPopulation;
	public void SetOption(LobbyUI lobbyui, Lobby lobby) {
		GetComponent<Button>().onClick.AddListener(() => lobbyui.JoinLobby(lobby));
		lobbyName.text = lobby.Name;
		lobbyMode.text = lobby.Data[LobbyManager.GameMode].Value;
		dictionary.text = lobby.Data[LobbyManager.Dictionary].Value;
		lobbyPopulation.text = (lobby.MaxPlayers - lobby.AvailableSlots) + "/" + lobby.MaxPlayers;
	}
}
