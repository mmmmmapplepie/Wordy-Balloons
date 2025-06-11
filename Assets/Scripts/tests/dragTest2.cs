using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class dragTest2 : MonoBehaviour, IDragHandler {
	public void OnDrag(PointerEventData eventData) {
		print("being dragged");
	}
}
