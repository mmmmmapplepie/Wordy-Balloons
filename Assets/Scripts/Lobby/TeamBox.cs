using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TeamBox : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler {
	public int team = 1;
	Color highlight = new Color32(0x00, 0xFF, 0xAA, 0x64);
	Color normal = new Color32(0x00, 0x00, 0x00, 0x64);
	public static event System.Action<int, LobbyPlayer> teamChange;

	public void OnDrop(PointerEventData eventData) {
		GameObject dragObject = eventData.pointerDrag;
		if (dragObject != null && dragObject.GetComponent<LobbyPlayer>() != null && NetworkManager.Singleton.IsServer) {
			if (dragObject.transform.parent.parent == transform) { GetComponent<Image>().color = normal; return; }
			dragObject.transform.SetParent(transform.GetChild(0));
			dragObject.transform.SetAsLastSibling();
			teamChange?.Invoke(team, eventData.pointerDrag.GetComponent<LobbyPlayer>());

		}
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
}
