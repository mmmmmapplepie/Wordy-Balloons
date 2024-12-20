using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetFont : MonoBehaviour {
	public TMP_FontAsset font;
	[ContextMenu("set")]
	void SetFontForAllChildren() {
		SetFontInTransform(transform);
	}

	void SetFontInTransform(Transform t) {
		if (t.TryGetComponent<TMP_Text>(out TMP_Text tm)) tm.font = font;
		foreach (Transform newT in t) {
			print(newT.name);
			SetFontInTransform(newT);
		}
	}
}
