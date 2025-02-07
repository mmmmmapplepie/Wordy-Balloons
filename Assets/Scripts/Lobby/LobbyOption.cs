using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyOption : MonoBehaviour {
	public TextMeshProUGUI lobbyName, lobbyMode, lobbyPopulation;
	public void SetOption(Lobby lobby) {
		GetComponent<Button>().onClick.AddListener(() => LobbyManager.Instance.JoinLobbyByID(lobby.Id));
		lobbyName.text = lobby.Name;
		lobbyMode.text = lobby.Data[LobbyManager.GameMode].Value;
		lobbyPopulation.text = (lobby.MaxPlayers - lobby.AvailableSlots) + "/" + lobby.MaxPlayers;
	}
}
