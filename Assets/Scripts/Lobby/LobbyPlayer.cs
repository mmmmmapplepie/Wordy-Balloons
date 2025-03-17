using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class LobbyPlayer : NetworkBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler {
	[SerializeField] TextMeshProUGUI playerNameTxt;
	[SerializeField] GameObject kickBtn, loadingCover, activePlayerCover;
	[SerializeField] TMP_Dropdown colorPicker;

	NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();
	[HideInInspector] public NetworkVariable<ulong> clientID = new NetworkVariable<ulong>(ulong.MaxValue);
	[HideInInspector] public NetworkVariable<int> currColorIndex = new NetworkVariable<int>(0);
	[HideInInspector] public NetworkVariable<int> siblingNum = new NetworkVariable<int>();
	NetworkVariable<bool> joinConfirmed = new NetworkVariable<bool>(false);


	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		SetDropDown();
		ColorChanged(-1, currColorIndex.Value);
		clientID.OnValueChanged += CheckActivePlayer;
		currColorIndex.OnValueChanged += ColorChanged;
		playerName.OnValueChanged += NameChanged;
		joinConfirmed.OnValueChanged += LoadStateChanged;
		siblingNum.OnValueChanged += ChangePos;

		if (NetworkManager.Singleton.IsServer) {
			siblingNum.Value = transform.GetSiblingIndex();
		}

		loadingCover.SetActive(!joinConfirmed.Value);
		NameChanged(default, playerName.Value);
	}

	public override void OnNetworkDespawn() {
		clientID.OnValueChanged -= CheckActivePlayer;
		currColorIndex.OnValueChanged -= ColorChanged;
		playerName.OnValueChanged -= NameChanged;
		joinConfirmed.OnValueChanged -= LoadStateChanged;
		siblingNum.OnValueChanged -= ChangePos;
		base.OnNetworkDespawn();
	}

	public void SetColor(int newIndex) {
		currColorIndex.Value = newIndex;
	}

	void ColorChanged(int old, int newC) {
		colorPicker.Set(newC);
	}
	void NameChanged(FixedString64Bytes old, FixedString64Bytes newC) {
		playerNameTxt.text = newC.ToString();
	}
	void LoadStateChanged(bool old, bool newb) {
		if (newb) {
			loadingCover.SetActive(false);
		}
	}

	public void ConfirmJoin(string name) {
		playerName.Value = name;
		joinConfirmed.Value = true;
	}

	public void SetupPlayer(ulong clientID, int colorInd) {
		this.clientID.Value = clientID;
		currColorIndex.Value = colorInd;

		if (NetworkManager.Singleton.IsServer) {
			if (this.clientID.Value != NetworkManager.Singleton.LocalClientId) {
				kickBtn.SetActive(true);
			}
		}

		EnableColorpicker();
	}

	void CheckActivePlayer(ulong id, ulong clientID) {
		if (clientID == NetworkManager.Singleton.LocalClientId) {
			activePlayerCover.SetActive(true);
		}
	}

	void EnableColorpicker() {
		colorPicker.interactable = clientID.Value == NetworkManager.Singleton.LocalClientId;
	}

	void SetDropDown() {
		colorPicker.ClearOptions();

		List<TMP_Dropdown.OptionData> items = new List<TMP_Dropdown.OptionData>();
		List<Color> options = MyLobby.allColorOptions;
		for (int i = 0; i < options.Count; i++) {
			Sprite s = MyLobby.allColorOptionSprites[i];
			string index = i.ToString();
			TMP_Dropdown.OptionData item = new TMP_Dropdown.OptionData(index, s);
			items.Add(item);
		}
		colorPicker.AddOptions(items);
	}
	public void KickPlayer() {
		if (MyLobby.LoadingSceneBool.Value) { print("can't kick now"); return; }
		NetworkManager.Singleton.DisconnectClient(clientID.Value);
	}

	public override void OnDestroy() {
		if (tempObj != null) Destroy(tempObj);
		base.OnDestroy();
	}
	public void RequestColorChange(int value) {
		int targetIndex = value;
		if (currColorIndex.Value == targetIndex) return;
		colorPicker.Set(currColorIndex.Value);
		if (MyLobby.Instance != null) MyLobby.Instance.RequestColorChange(targetIndex);
	}









	public GameObject swapIndicator;
	GameObject tempObj;
	public static event System.Action<Transform, LobbyPlayer, LobbyPlayer> TeamChangeEvent;
	RectTransform tempHolder;
	Vector2 minMaxY;
	void SetupTempHolder() {
		tempHolder = transform.parent.parent.parent.GetComponent<RectTransform>();
		Vector3[] corners = new Vector3[4];
		tempHolder.GetLocalCorners(corners);
		minMaxY.x = corners[0].y + GetComponent<RectTransform>().rect.height * 0.5f;
		minMaxY.y = corners[1].y - GetComponent<RectTransform>().rect.height * 0.5f;
	}
	public static event Action DragBegin;
	public void OnBeginDrag(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		if (tempHolder == null) SetupTempHolder();
		RectTransform rt = GetComponent<RectTransform>();
		DragBegin?.Invoke();
		tempObj = Instantiate(gameObject, tempHolder);
		tempObj.GetComponent<RectTransform>().anchorMin = Vector2.one * 0.5f;
		tempObj.GetComponent<RectTransform>().anchorMax = Vector2.one * 0.5f;
		tempObj.transform.SetAsLastSibling();
		tempObj.GetComponent<RectTransform>().sizeDelta = new Vector2(rt.rect.size.x, rt.rect.size.y);
		OnDrag(eventData);
		CanvasGroup group = tempObj.AddComponent<CanvasGroup>();
		group.blocksRaycasts = false;
		swapIndicator.SetActive(true);
	}

	public void OnDrag(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		if (tempObj != null) {
			RectTransformUtility.ScreenPointToLocalPointInRectangle(tempHolder, Input.mousePosition, Camera.main, out Vector2 mousePos);
			mousePos.x = GetComponent<RectTransform>().position.x;
			mousePos.y = Mathf.Clamp(mousePos.y, minMaxY.x, minMaxY.y);
			tempObj.GetComponent<RectTransform>().localPosition = mousePos;
		}
	}
	public void OnEndDrag(PointerEventData eventData) {
		swapIndicator.SetActive(false);
		Destroy(tempObj);
	}

	public void OnDrop(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		GameObject dragObject = eventData.pointerDrag;
		if (dragObject == null) return;
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
		GameObject dragObject = eventData.pointerDrag;
		if (dragObject == null || dragObject.GetComponent<LobbyPlayer>() == null) return;
		if (dragObject == gameObject) return;
		swapIndicator.SetActive(false);
	}


	void Update() {
		if (!NetworkManager.Singleton.IsServer) return;
		if (Input.GetMouseButtonUp(0)) {
			swapIndicator.SetActive(false);
		}
	}
}




















