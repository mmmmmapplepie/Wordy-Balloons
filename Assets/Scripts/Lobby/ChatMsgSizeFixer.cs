using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatMsgSizeFixer : MonoBehaviour {
	public float padding;
	void Start() {
		StartCoroutine(WaitForEndOfFrame());
	}
	IEnumerator WaitForEndOfFrame() {
		yield return new WaitForEndOfFrame();
		RectTransform childRT = transform.GetChild(0).GetComponent<RectTransform>();
		RectTransform rt = GetComponent<RectTransform>();
		float childHeight = childRT.rect.height;
		rt.sizeDelta = new Vector2(rt.rect.width, childHeight + padding);
	}
}
