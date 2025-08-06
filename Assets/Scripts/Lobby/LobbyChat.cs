using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using WebSocketSharp;

public class LobbyChat : NetworkBehaviour {
	public Color nameColor;
	string hexColor;
	void Awake() {
		chatHolderRT = chatHolder.GetComponent<RectTransform>();
		chatList = new NetworkList<FixedString512Bytes>();
	}

	public override void OnNetworkSpawn() {
		ClearChat();
		chatList.OnListChanged += ChatListChanged;
	}

	public override void OnNetworkDespawn() {
		base.OnNetworkDespawn();
		ClearChat();
		chatList.OnListChanged -= ChatListChanged;
	}

	void Start() {
		hexColor = ColorUtility.ToHtmlStringRGB(nameColor);
		NetcodeManager.ClientStartSuccess += JoinedLobby;
	}
	public override void OnDestroy() {
		base.OnDestroy();
		NetcodeManager.ClientStartSuccess -= JoinedLobby;
	}

	void JoinedLobby() {
		UpdateChatList();
	}
	void UpdateChatList() {
		foreach (FixedString512Bytes chat in chatList) {
			CreateChatObject(chat.ToString());
		}
	}

	void ClearChat() {
		try {
			if (chatList != null && NetworkManager.Singleton.IsServer) chatList.Clear();
		} catch (Exception e) {
			print(e);
		}
		foreach (Transform t in chatHolder) {
			Destroy(t.gameObject);
		}
		chatHolderRT.anchoredPosition = Vector2.zero;
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.Return) && chatInput.IsInteractable() && !chatInput.isFocused) {
			EventSystem.current.SetSelectedGameObject(chatInput.gameObject);
		}
	}

	public TMP_InputField chatInput;
	public void SendChat() {
		string chatToSend = chatInput.text;
		if (chatToSend.IsNullOrEmpty()) { StartCoroutine(EndOfFrameCall(ClearInputField)); return; }
		SendChatServerRpc(chatToSend, LobbyManager.playerName);
		StartCoroutine(EndOfFrameCall(ClearInputField));
	}
	IEnumerator EndOfFrameCall(System.Action ftn) {
		yield return new WaitForEndOfFrame();
		ftn();
	}
	void ClearInputField() {
		chatInput.Set("");
	}

	NetworkList<FixedString512Bytes> chatList;
	[ServerRpc(RequireOwnership = false)]
	void SendChatServerRpc(string chatToSend, string senderName) {
		StringBuilder chatString = new StringBuilder();
		chatString.Append("<color=#" + hexColor + ">").Append(senderName).Append("</color>\n").Append(chatToSend);
		FixedString512Bytes newChat = new FixedString512Bytes();
		newChat.Append(chatString.ToString().Substring(0, Mathf.Min(chatString.Length, newChat.Capacity)));
		chatList.Add(newChat);
	}
	void ChatListChanged(NetworkListEvent<FixedString512Bytes> changeEvent) {
		if (changeEvent.Type == NetworkListEvent<FixedString512Bytes>.EventType.Add) CreateChatObject(changeEvent.Value.ToString());
	}
	public float topBottomPadding = 5f;
	public float rightLeftPadding = 20f;
	public Transform chatHolder;
	RectTransform chatHolderRT;
	public GameObject chatMsgItemPrefab;
	void CreateChatObject(string msg) {
		float initialHolderYPos = chatHolderRT.anchoredPosition.y;

		GameObject newChatItem = Instantiate(chatMsgItemPrefab, chatHolder);
		TextMeshProUGUI newChatTextTxt = newChatItem.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		RectTransform textRT = newChatTextTxt.GetComponent<RectTransform>();
		RectTransform txtHolderRT = newChatItem.GetComponent<RectTransform>();

		newChatTextTxt.text = msg;
		Vector2 prefSize = newChatTextTxt.GetPreferredValues(chatHolderRT.rect.width - rightLeftPadding * 2f, Mathf.Infinity);
		Vector2 targetSize = new Vector2(chatHolderRT.rect.width - rightLeftPadding * 2f, prefSize.y);
		textRT.sizeDelta = new Vector2(chatHolderRT.rect.width - rightLeftPadding * 2f, prefSize.y);
		targetSize = new Vector2(chatHolderRT.rect.width, prefSize.y + 2f * topBottomPadding);
		txtHolderRT.sizeDelta = targetSize;

		if (Mathf.Abs(initialHolderYPos) < 0.05f) {
			chatHolderRT.anchoredPosition = new Vector2(chatHolderRT.anchoredPosition.x, 0f);
			LayoutRebuilder.ForceRebuildLayoutImmediate(chatHolderRT);
			return;
		}

		chatHolderRT.anchoredPosition = new Vector2(chatHolderRT.anchoredPosition.x, initialHolderYPos - targetSize.y - chatHolder.GetComponent<VerticalLayoutGroup>().spacing);
		LayoutRebuilder.ForceRebuildLayoutImmediate(chatHolderRT);
	}
}
