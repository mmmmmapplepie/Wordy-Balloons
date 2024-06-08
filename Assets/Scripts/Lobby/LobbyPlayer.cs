using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyPlayer : NetworkBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler {
	[SerializeField] TextMeshProUGUI playerNameTxt;
	[SerializeField] GameObject kickBtn, loadingCover, activePlayerCover;
	[SerializeField] TMP_Dropdown colorPicker;

	NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();
	public NetworkVariable<ulong> clientID = new NetworkVariable<ulong>(ulong.MaxValue);
	public NetworkVariable<int> currColorIndex = new NetworkVariable<int>(0);
	public NetworkVariable<int> siblingNum = new NetworkVariable<int>();
	NetworkVariable<bool> dataCompleteNetVar = new NetworkVariable<bool>(false);
	[HideInInspector] public string lobbyID;
	[HideInInspector] public NetcodeManager netScript;

	Coroutine timeOutRoutine;
	public override void OnNetworkSpawn() {
		SetDropDown();
		ColorChanged(-1, currColorIndex.Value);
		currColorIndex.OnValueChanged += ColorChanged;
		dataCompleteNetVar.OnValueChanged += LoadStateChanged;
		clientID.OnValueChanged += ClientIDChanged;
		siblingNum.OnValueChanged += ChangePos;
		if (NetworkManager.Singleton.IsServer) {
			timeOutRoutine = StartCoroutine(TimeOutRoutine());
			siblingNum.Value = transform.GetSiblingIndex();
		}
		EnableColorpicker();
		loadingCover.SetActive(!dataCompleteNetVar.Value);
	}
	IEnumerator TimeOutRoutine() {
		float timeoutTime = 10f;
		yield return new WaitForSecondsRealtime(timeoutTime);
		KickPlayer();
	}
	public override void OnNetworkDespawn() {
		currColorIndex.OnValueChanged -= ColorChanged;
		dataCompleteNetVar.OnValueChanged -= LoadStateChanged;
		clientID.OnValueChanged -= ClientIDChanged;
		siblingNum.OnValueChanged -= ChangePos;
	}

	public void SetColor(int newIndex) {
		currColorIndex.Value = newIndex + 1;
		currColorIndex.Value = newIndex;
	}

	void ColorChanged(int old, int newC) {
		colorPicker.Set(newC);
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
		EnableColorpicker();
	}

	public void SetupPlayer(PlayerData data) {
		playerName.Value = data.Name;
		clientID.Value = data.ClientID;
		currColorIndex.Value = data.ColorIndex;
		colorPicker.value = currColorIndex.Value;

		if (timeOutRoutine != null) StopCoroutine(timeOutRoutine);
		if (NetworkManager.Singleton.IsServer) {
			if (lobbyID != AuthenticationService.Instance.PlayerId) {
				kickBtn.SetActive(true);
			}
		}
		dataCompleteNetVar.Value = true;
	}

	void EnableColorpicker() {
		colorPicker.interactable = clientID.Value == NetworkManager.Singleton.LocalClientId;

	}

	void SetDropDown() {
		colorPicker.ClearOptions();

		List<TMP_Dropdown.OptionData> items = new List<TMP_Dropdown.OptionData>();
		List<Color> options = NetcodeManager.allColorOptions;
		for (int i = 0; i < options.Count; i++) {
			Sprite s = NetcodeManager.allColorOptionSprites[i];
			string index = i.ToString();
			TMP_Dropdown.OptionData item = new TMP_Dropdown.OptionData(index, s);
			items.Add(item);
		}
		colorPicker.AddOptions(items);
	}
	public void KickPlayer() {
		if (!NetcodeManager.CanStopSceneLoading) { print("can't kick now"); return; }
		MyLobby.Instance.KickFromLobby(lobbyID);
	}

	public override void OnDestroy() {
		if (tempObj != null) Destroy(tempObj);
		base.OnDestroy();
	}
	public void RequestColorChange(int value) {
		int targetIndex = int.Parse(colorPicker.options[value].text);
		if (currColorIndex.Value == targetIndex) return;
		if (NetcodeManager.Instance != null) NetcodeManager.Instance.RequestColorChange(targetIndex);
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
	public int ColorIndex;
	public PlayerData(ulong clientID, string lobbyID, string name, int colorIndex) {
		this.ClientID = clientID;
		this.LobbyID = lobbyID;
		this.Name = name;
		this.ColorIndex = colorIndex;
	}
}











