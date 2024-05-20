using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyOption : MonoBehaviour {
	public void SetOption(Lobby lobby, MyLobby lobbyScript) {
		GetComponent<Button>().onClick.AddListener(() => lobbyScript.JoinLobbyByID(lobby.Id));
		transform.Find("Name").GetComponent<TextMeshProUGUI>().text = lobby.Name;
		transform.Find("Mode").GetComponent<TextMeshProUGUI>().text = lobby.Data[MyLobby.GameMode].Value;
	}
}
