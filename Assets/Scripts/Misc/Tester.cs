using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tester : MonoBehaviour {
	public Transform t;
	public RectTransform overlay;
	void Update() {
		GetWorldPointToCanvasPoint(t.position);
	}


	public Vector2 GetWorldPointToCanvasPoint(Vector2 worldPoint) {
		Vector2 screenPt;
		print(worldPoint);
		print(Camera.main.WorldToViewportPoint(worldPoint));

		print(screenPt = Camera.main.WorldToScreenPoint(worldPoint));
		RectTransformUtility.ScreenPointToWorldPointInRectangle(overlay, screenPt, Camera.main, out Vector3 rectPt);
		print(rectPt);
		print(RectTransformUtility.WorldToScreenPoint(Camera.main, worldPoint));
		print("===========");

		return default;
	}
}
