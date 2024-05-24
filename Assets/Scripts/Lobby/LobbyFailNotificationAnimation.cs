using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyFailNotificationAnimation : MonoBehaviour {
	void OnEnable() {
		StartCoroutine(fade());
	}

	IEnumerator fade() {
		CanvasGroup g = GetComponent<CanvasGroup>();
		g.alpha = 1;
		yield return new WaitForSeconds(2f);
		float period = 2f;
		float t = period;
		while (t > 0) {
			t -= Time.deltaTime;
			g.alpha = t / period;
			yield return null;
		}
		gameObject.SetActive(false);
	}

}
