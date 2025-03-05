using System.Diagnostics.Tracing;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TeamBox : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler {
	Color highlight = new Color(1f, 1f, 0f, 1f);
	Color normal = Color.white;
	public Transform targetT;
	public static event System.Action<Transform, LobbyPlayer, LobbyPlayer> TeamChangeEvent;

	void Start() {
		LobbyPlayer.DragBegin += DragBegin;
	}
	void OnDestroy() {
		LobbyPlayer.DragBegin -= DragBegin;
	}

	void DragBegin() {
		if (RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), Input.mousePosition, Camera.main)) {
			GetComponent<Image>().color = highlight;
		}
	}


	public void OnDrop(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		GameObject dragObject = eventData.pointerDrag;
		if (dragObject == null || dragObject.GetComponent<LobbyPlayer>() == null) return;

		TeamChangeEvent?.Invoke(targetT, dragObject.GetComponent<LobbyPlayer>(), null);
		GetComponent<Image>().color = normal;
	}

	public void OnPointerEnter(PointerEventData eventData) {
		if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<LobbyPlayer>() != null && NetworkManager.Singleton.IsServer) {
			GetComponent<Image>().color = highlight;
		}
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<LobbyPlayer>() != null && NetworkManager.Singleton.IsServer) {
			GetComponent<Image>().color = normal;
		}
	}
	void Update() {
		if (Input.GetMouseButtonUp(0)) {
			GetComponent<Image>().color = normal;
		}
	}
}
