using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FancyButton : MonoBehaviour {
	public bool publicVersion = true;
	float clickT = -100f;
	Coroutine animRoutine;
	public void Clicked() {
		Clicked(-1);
	}
	public void Clicked(float animPeriod = -1, bool soundEvent = true) {
		if (Time.unscaledTime - clickT < reclickTime) return;
		clickT = Time.unscaledTime;
		publicVersion = !publicVersion;
		if (soundEvent) ButtonStateChanged?.Invoke();
		if (animRoutine != null) StopCoroutine(animRoutine);
		animRoutine = StartCoroutine(ClickedRoutine(animPeriod));
	}
	public event System.Action ButtonStateChanged;
	public Transform btnSpinVisualObj;
	public TextMeshProUGUI publicTxt, privateTxt;
	public Color activeColor, inactiveColor;
	public float period = 1f;
	public float reclickTime = 2f;
	IEnumerator ClickedRoutine(float animPeriod = -1) {
		bool targetPos = publicVersion;
		float targetRot = publicVersion ? 0 : 180f;
		float startRot = btnSpinVisualObj.localEulerAngles.z;
		startRot %= 360f;
		if (targetRot < startRot) targetRot += 360f;
		Quaternion startRotQ = Quaternion.Euler(0, 0, startRot);
		Quaternion endRotQ = Quaternion.Euler(0, 0, targetRot);
		if (animPeriod < 0) animPeriod = period;
		float t = 0f;
		while (t < animPeriod) {
			t += Time.unscaledDeltaTime;
			float finalRot = Mathf.Lerp(startRot, targetRot, t / animPeriod);
			btnSpinVisualObj.rotation = Quaternion.Euler(0, 0, finalRot);
			publicTxt.color = publicVersion ? Color.Lerp(inactiveColor, activeColor, t / animPeriod) : Color.Lerp(activeColor, inactiveColor, t / animPeriod);
			privateTxt.color = !publicVersion ? Color.Lerp(inactiveColor, activeColor, t / animPeriod) : Color.Lerp(activeColor, inactiveColor, t / animPeriod);
			yield return null;
		}
		publicTxt.color = publicVersion ? activeColor : inactiveColor;
		privateTxt.color = !publicVersion ? activeColor : inactiveColor;
		btnSpinVisualObj.rotation = Quaternion.Euler(0, 0, targetRot);
	}
}
