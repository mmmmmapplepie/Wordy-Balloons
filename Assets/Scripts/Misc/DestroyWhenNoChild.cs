using System.Collections.Generic;
using UnityEngine;

public class DestroyWhenNoChild : MonoBehaviour {
	void Update() {
		for (int i = 0; i < transform.childCount; i++) {
			Transform child = transform.GetChild(i);
			if (child.gameObject.hideFlags == HideFlags.None) return;
		}
		Destroy(gameObject);
	}
}
