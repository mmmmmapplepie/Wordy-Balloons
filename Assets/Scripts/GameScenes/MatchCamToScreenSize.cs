using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-100)]
public class MatchCamToScreenSize : MonoBehaviour {
	public static float camSize;
	void Awake() {
		float w = Screen.width;
		float h = Screen.height;
		float aspect = w / h;

		if (aspect >= 2) {
			camSize = GetComponent<Camera>().orthographicSize;
			return;
		}
		float targetH = 20 / aspect;
		GetComponent<Camera>().orthographicSize = targetH / 2f;
		camSize = GetComponent<Camera>().orthographicSize;
	}
}
