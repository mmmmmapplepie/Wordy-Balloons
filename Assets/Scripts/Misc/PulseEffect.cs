using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PulseEffect : MonoBehaviour {
	public float startScale, endScale;
	public float pulsePeriod;
	public ParticleSystem.MinMaxGradient colorOverTime;
	TextMeshProUGUI txt;
	void Awake() {
		txt = GetComponent<TextMeshProUGUI>();
	}

	void OnEnable() {
		StartCoroutine(Pulse());
	}
	IEnumerator Pulse() {
		float t = 0;
		while (true) {
			t += Time.deltaTime;
			if (t > pulsePeriod) {
				t = 0;
			}
			transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t / pulsePeriod);
			txt.color = colorOverTime.Evaluate(t / pulsePeriod);
			yield return null;
		}
	}

	void OnDisable() {
		StopAllCoroutines();
	}
}
