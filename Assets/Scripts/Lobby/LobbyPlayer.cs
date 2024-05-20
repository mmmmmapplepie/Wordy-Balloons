using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyPlayer : NetworkBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler {
	public string lobbyID;
	public ulong clientID;
	[SerializeField] TextMeshProUGUI playerNameTxt;
	[SerializeField] GameObject kickBtn;
	[SerializeField] TMP_Dropdown colorPicker;
	MyLobby lobbyScript;



	public DragAndDropVisualMode dragAndDropVisualMode => throw new System.NotImplementedException();
	NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();
	NetworkVariable<ulong> clientIDNetVar = new NetworkVariable<ulong>();
	NetworkVariable<Color> playerColor = new NetworkVariable<Color>();
	public override void OnNetworkSpawn() {
		if (lobbyScript != null) {
			playerName.Value = nameTemp;
			playerColor.Value = colorTemp;
			clientIDNetVar.Value = clientID;
		}
		if (clientIDNetVar.Value == NetworkManager.Singleton.LocalClientId) {
			colorPicker.gameObject.SetActive(true);
		}
		playerNameTxt.text = playerName.Value.ToString();
		if (NetworkManager.Singleton.IsServer && lobbyID != AuthenticationService.Instance.PlayerId) {
			kickBtn.SetActive(true);
		}
	}
	FixedString64Bytes nameTemp;
	Color colorTemp;
	public void SetupPlayer(PlayerData data, MyLobby myLobby) {
		nameTemp = data.Name;
		colorTemp = Color.white;
		lobbyID = data.LobbyID;
		clientID = data.ClientID;
		lobbyScript = myLobby;

	}
	public void KickPlayer() {
		lobbyScript.LeaveLobby(lobbyID);
	}

	public override void OnDestroy() {
		if (tempObj != null) Destroy(tempObj);
		base.OnDestroy();
	}
	GameObject tempObj;
	Vector2 difference = Vector2.zero;
	public void OnBeginDrag(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		tempObj = Instantiate(gameObject, transform.parent.parent.parent);
		tempObj.transform.SetAsLastSibling();
		tempObj.GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().rect.size.x, GetComponent<RectTransform>().rect.size.y);
		CanvasGroup group = tempObj.AddComponent<CanvasGroup>();
		group.blocksRaycasts = false;
	}

	public void OnDrag(PointerEventData eventData) {
		if (tempObj != null) {
			tempObj.GetComponent<RectTransform>().position = difference + (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
		}
	}
	public void OnEndDrag(PointerEventData eventData) {
		Destroy(tempObj);
	}

}
