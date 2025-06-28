using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour {
	public CameraShake shakeController;

	Coroutine shakeRoutine;
	public void StartShaker(float period, float startMagnitude) {
		StopShake();
		shakeRoutine = StartCoroutine(ShakeRoutine(period, startMagnitude));
	}

	public void StopShake() {
		if (shakeRoutine != null) StopCoroutine(shakeRoutine);
		shakeController.magnitude = 0;
	}

	IEnumerator ShakeRoutine(float period, float magnitude) {
		shakeController.magnitude = magnitude;
		float t = 0;
		while (t < period) {
			shakeController.magnitude = Mathf.Lerp(magnitude, 0, t / period);
			yield return null;
			t += Time.unscaledDeltaTime;
		}
		shakeController.magnitude = 0;
	}




}
