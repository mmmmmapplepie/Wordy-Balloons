using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyPlayer : NetworkBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler {
	[SerializeField] TextMeshProUGUI playerNameTxt;
	[SerializeField] GameObject kickBtn, loadingCover, activePlayerCover;
	[SerializeField] TMP_Dropdown colorPicker;

	NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();


	NetworkVariable<ulong> clientIDNetVar = new NetworkVariable<ulong>(ulong.MaxValue);

	NetworkVariable<Color> useColor = new NetworkVariable<Color>(Color.black);

	NetworkVariable<bool> dataCompleteNetVar = new NetworkVariable<bool>(false);

	[HideInInspector] public string lobbyID;
	[HideInInspector] public NetcodeManager netScript;

	Coroutine timeOutRoutine;
	public override void OnNetworkSpawn() {
		useColor.OnValueChanged += ColorChanged;
		dataCompleteNetVar.OnValueChanged += LoadStateChanged;
		clientIDNetVar.OnValueChanged += ClientIDChanged;
		if (NetworkManager.Singleton.IsServer) timeOutRoutine = StartCoroutine(TimeOutRoutine());

		loadingCover.SetActive(!dataCompleteNetVar.Value);
	}
	IEnumerator TimeOutRoutine() {
		float timeoutTime = 5f;
		yield return new WaitForSecondsRealtime(timeoutTime);
		KickPlayer();
	}
	public override void OnNetworkDespawn() {
		useColor.OnValueChanged -= ColorChanged;
		dataCompleteNetVar.OnValueChanged -= LoadStateChanged;
		clientIDNetVar.OnValueChanged -= ClientIDChanged;
	}

	void ColorChanged(Color old, Color newC) {

	}
	void LoadStateChanged(bool old, bool newb) {
		if (newb) {
			loadingCover.SetActive(false);
		}
	}
	void ClientIDChanged(ulong old, ulong newid) {
		if (newid == NetworkManager.Singleton.LocalClientId) {
			activePlayerCover.SetActive(true);
		}
	}




	public void SetupPlayer(PlayerData data) {
		lobbyID = data.LobbyID;
		playerName.Value = data.Name;
		clientIDNetVar.Value = data.ClientID;
		useColor.Value = data.Color;

		if (timeOutRoutine != null) StopCoroutine(timeOutRoutine);
		if (NetworkManager.Singleton.IsServer) {
			if (lobbyID != AuthenticationService.Instance.PlayerId) {
				kickBtn.SetActive(true);
			}
		}

		dataCompleteNetVar.Value = true;
	}
	public void KickPlayer() {
		MyLobby.Instance.KickFromLobby(lobbyID);
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









public struct PlayerData {
	public ulong ClientID;
	public string LobbyID;
	public string Name;
	public Color Color;
	public PlayerData(ulong clientID, string lobbyID, string name, Color color) {
		this.ClientID = clientID;
		this.LobbyID = lobbyID;
		this.Name = name;
		this.Color = color;
	}
}
