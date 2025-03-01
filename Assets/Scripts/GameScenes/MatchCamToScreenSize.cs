using System.Collections.Generic;
using UnityEngine;

public class MatchCamToScreenSize : MonoBehaviour {
	void Awake() {
		float w = Screen.width;
		float h = Screen.height;
		float aspect = w / h;

		if (aspect >= 2) return;
		float targetH = 20 / aspect;
		GetComponent<Camera>().orthographicSize = targetH / 2f;

	}
}
