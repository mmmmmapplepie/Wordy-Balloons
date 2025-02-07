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
	NetworkVariable<bool> joinConfirmed = new NetworkVariable<bool>(false);

	public override void OnNetworkSpawn() {
		SetDropDown();
		ColorChanged(-1, currColorIndex.Value);
		currColorIndex.OnValueChanged += ColorChanged;
		playerName.OnValueChanged += NameChanged;
		joinConfirmed.OnValueChanged += LoadStateChanged;
		clientID.OnValueChanged += ClientIDChanged;
		siblingNum.OnValueChanged += ChangePos;
		if (NetworkManager.Singleton.IsServer) {
			siblingNum.Value = transform.GetSiblingIndex();
		}
		EnableColorpicker();
		loadingCover.SetActive(!joinConfirmed.Value);
		NameChanged(default, playerName.Value);
	}

	public override void OnNetworkDespawn() {
		currColorIndex.OnValueChanged -= ColorChanged;
		playerName.OnValueChanged -= NameChanged;
		joinConfirmed.OnValueChanged -= LoadStateChanged;
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
	void NameChanged(FixedString64Bytes old, FixedString64Bytes newC) {
		playerNameTxt.text = newC.ToString();
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
		if (!MyLobby.CanStopSceneLoading) { print("can't kick now"); return; }
		NetworkManager.Singleton.DisconnectClient(clientID.Value);
	}

	public override void OnDestroy() {
		if (tempObj != null) Destroy(tempObj);
		base.OnDestroy();
	}
	public void RequestColorChange(int value) {
		int targetIndex = int.Parse(colorPicker.options[value].text);
		if (currColorIndex.Value == targetIndex) return;
		if (MyLobby.Instance != null) MyLobby.Instance.RequestColorChange(targetIndex);
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
		swapIndicator.SetActive(false);
	}

	public void OnPointerUp(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		swapIndicator.SetActive(false);
	}
}




















