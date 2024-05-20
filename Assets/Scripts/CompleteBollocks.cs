using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CompleteBollocks : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
	public void OnBeginDrag(PointerEventData eventData) {
		GetComponent<BoxCollider>().enabled = false;
		MoveToMousePos();
	}

	public void OnDrag(PointerEventData eventData) {
		MoveToMousePos();
	}

	public void OnEndDrag(PointerEventData eventData) {
		GetComponent<BoxCollider>().enabled = true;
	}
	void MoveToMousePos() {
		Vector3 mousepos = Input.mousePosition;
		mousepos.z = 10f;
		mousepos = Camera.main.ScreenToWorldPoint(mousepos);
		print(mousepos);
		transform.position = mousepos;
	}
}
