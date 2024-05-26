using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyPlayer : NetworkBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler {
	[SerializeField] TextMeshProUGUI playerNameTxt;
	[SerializeField] GameObject kickBtn, loadingCover, activePlayerCover;
	[SerializeField] TMP_Dropdown colorPicker;

	NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();
	NetworkVariable<ulong> clientIDNetVar = new NetworkVariable<ulong>(ulong.MaxValue);
	public NetworkVariable<Color> useColor = new NetworkVariable<Color>(Color.black);
	public NetworkVariable<int> siblingNum = new NetworkVariable<int>();
	NetworkVariable<bool> dataCompleteNetVar = new NetworkVariable<bool>(false);
	[HideInInspector] public string lobbyID;
	[HideInInspector] public NetcodeManager netScript;

	Coroutine timeOutRoutine;
	public override void OnNetworkSpawn() {
		useColor.OnValueChanged += ColorChanged;
		dataCompleteNetVar.OnValueChanged += LoadStateChanged;
		clientIDNetVar.OnValueChanged += ClientIDChanged;
		siblingNum.OnValueChanged += ChangePos;
		if (NetworkManager.Singleton.IsServer) {
			timeOutRoutine = StartCoroutine(TimeOutRoutine());
			siblingNum.Value = transform.GetSiblingIndex();
		}
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
		siblingNum.OnValueChanged -= ChangePos;
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










	public GameObject swapIndicator;
	GameObject tempObj;
	public static event System.Action<Transform, LobbyPlayer, LobbyPlayer> TeamChangeEvent;
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
			Vector2 mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
			mousePos.x = GetComponent<RectTransform>().position.x;
			tempObj.GetComponent<RectTransform>().position = mousePos;
		}
	}
	public void OnEndDrag(PointerEventData eventData) {
		Destroy(tempObj);
	}

	public void OnDrop(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		GameObject dragObject = eventData.pointerDrag;
		if (dragObject == null || dragObject.GetComponent<LobbyPlayer>() == null) return;
		if (dragObject == gameObject) return;

		TeamChangeEvent?.Invoke(transform.parent, dragObject.GetComponent<LobbyPlayer>(), this);
	}

	void ChangePos(int old, int newPos) {
		transform.SetSiblingIndex(newPos);
	}

	public void OnPointerEnter(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		GameObject dragObject = eventData.pointerDrag;
		if (dragObject == null || dragObject.GetComponent<LobbyPlayer>() == null) return;
		if (dragObject == gameObject) return;
		swapIndicator.SetActive(true);
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		swapIndicator.SetActive(false);
	}

	public void OnPointerUp(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		swapIndicator.SetActive(false);
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
