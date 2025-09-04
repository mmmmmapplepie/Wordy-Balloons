using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PulseEffect : MonoBehaviour {
	public float startScale, endScale, yFactor = 1f;
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
			float factor = Mathf.Lerp(startScale, endScale, t / pulsePeriod);
			Vector3 tscale = Vector3.one * factor;
			tscale.y = startScale + yFactor * (factor - startScale);
			transform.localScale = tscale;
			txt.color = colorOverTime.Evaluate(t / pulsePeriod);
			yield return null;
		}
	}

	void OnDisable() {
		StopAllCoroutines();
	}
}
