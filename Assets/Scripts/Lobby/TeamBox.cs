using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TeamBox : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler {
	// Color highlight = new Color32(0x00, 0xFF, 0xAA, 0x64);
	// Color normal = new Color32(0x00, 0x00, 0x00, 0x64);
	public Transform targetT;
	public static event System.Action<Transform, LobbyPlayer, LobbyPlayer> TeamChangeEvent;

	public void OnDrop(PointerEventData eventData) {
		if (!NetworkManager.Singleton.IsServer) return;
		GameObject dragObject = eventData.pointerDrag;
		if (dragObject == null || dragObject.GetComponent<LobbyPlayer>() == null) return;
		


		TeamChangeEvent?.Invoke(targetT, dragObject.GetComponent<LobbyPlayer>(), null);

		// GetComponent<Image>().color = normal;
	}

	public void OnPointerEnter(PointerEventData eventData) {
		// if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<LobbyPlayer>() != null && NetworkManager.Singleton.IsServer) {
		// 	GetComponent<Image>().color = highlight;
		// }
	}

	public void OnPointerExit(PointerEventData eventData) {
		// if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<LobbyPlayer>() != null && NetworkManager.Singleton.IsServer) {
		// 	GetComponent<Image>().color = normal;
		// }
	}
}
