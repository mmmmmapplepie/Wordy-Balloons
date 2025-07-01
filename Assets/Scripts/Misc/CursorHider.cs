using System.Collections.Generic;
using UnityEngine;

public class CursorHider : MonoBehaviour {
	public float hideTime = 5f;
	float t = 0;
	Vector2 prevPos;
	void Update() {
		Vector2 cursorPos = Input.mousePosition;
		if (prevPos == cursorPos) {
			t += Time.unscaledDeltaTime;
		} else {
			t = 0;
			Cursor.visible = true;
		}
		if (t > hideTime) Cursor.visible = false;
		prevPos = cursorPos;
	}

	void OnDestroy() {
		Cursor.visible = true;
	}
}
