using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FancyButton : MonoBehaviour {
	public bool isPublic = true;
	float clickT = -100f;
	Coroutine animRoutine;
	public void Clicked() {
		Clicked(-1);
	}
	public void Clicked(float animPeriod = -1) {
		clickT = Time.unscaledTime;
		isPublic = !isPublic;
		if (animRoutine != null) StopCoroutine(animRoutine);
		animRoutine = StartCoroutine(ClickedRoutine(animPeriod));
	}
	public Transform btnSpinVisualObj;
	public TextMeshProUGUI publicTxt, privateTxt;
	public Color activeColor, inactiveColor;
	public float period = 1f;
	IEnumerator ClickedRoutine(float animPeriod = -1) {
		bool targetPos = isPublic;
		float targetRot = isPublic ? 0 : 180f;
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
			publicTxt.color = isPublic ? Color.Lerp(inactiveColor, activeColor, t / animPeriod) : Color.Lerp(activeColor, inactiveColor, t / animPeriod);
			privateTxt.color = !isPublic ? Color.Lerp(inactiveColor, activeColor, t / animPeriod) : Color.Lerp(activeColor, inactiveColor, t / animPeriod);
			yield return null;
		}
		publicTxt.color = isPublic ? activeColor : inactiveColor;
		privateTxt.color = !isPublic ? activeColor : inactiveColor;
		btnSpinVisualObj.rotation = Quaternion.Euler(0, 0, targetRot);
	}
}
