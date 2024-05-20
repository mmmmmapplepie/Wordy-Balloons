using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Chatmanager : NetworkBehaviour {
	public TMP_InputField input;
	public GameObject chatPrefab;
	public Transform chatBox;
	public void SendChat() {
		if (input.text.Length == 0) return;
		SendChatServerRpc(input.text);


		input.text = "";
	}

	[ServerRpc(RequireOwnership = false)]
	void SendChatServerRpc(string chat) {
		GetChatClientRpc(chat);
	}
	[ClientRpc]
	void GetChatClientRpc(string chat) {
		GameObject newchat = Instantiate(chatPrefab, chatBox);
		newchat.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = chat;
	}
}
