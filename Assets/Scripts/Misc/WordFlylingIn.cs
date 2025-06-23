using System.Collections;
using TMPro;
using UnityEngine;

public class WordFlylingIn : MonoBehaviour {
	public float width, height;
	public float period;

	Vector3 iniPos;
	Vector2 iniRectPos;
	RectTransform rt;
	TextMeshProUGUI txt;
	void Start() {
		iniPos = transform.localPosition;
		rt = GetComponent<RectTransform>();
		iniRectPos = rt.anchoredPosition;
		txt = GetComponent<TextMeshProUGUI>();
		PlayAnimation();
	}
	public void PlayAnimation(float time = 1.5f) {
		period = time;
		StartCoroutine(Fling());
	}

	public AudioClip whooshSFX, splashSFX;
	public GameObject splashEffect;
	IEnumerator Fling() {
		float t = 0;
		Invoke(nameof(PlaySound), 0.3f);
		Invoke(nameof(PlaySplashSound), 1.4f);
		while (t < period) {
			float r = t / period;
			Vector3 pos = iniPos + Vector3.right * (1 - r) * width;
			pos += height * PowerLerp(r) * Vector3.up;
			transform.localPosition = pos;
			transform.localRotation = Quaternion.Euler(0, 0, width * (1 - r) * 4f);
			txt.color = Color.Lerp(Color.clear, Color.white, r);
			transform.localScale = Vector3.one * PowerLerp(r, 3f);
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		txt.color = Color.white;
		transform.localPosition = iniPos;
		transform.localRotation = Quaternion.identity;
		transform.localScale = Vector3.one;
		GameObject newEffect = Instantiate(splashEffect, transform.position - Camera.main.transform.forward, Quaternion.identity, transform);
		newEffect.transform.localScale = Vector3.one * 1.5f;

	}
	void PlaySound() {
		AudioPlayer.PlayOneShot_Static(whooshSFX, VolumeControl.GetEffectVol());
	}
	void PlaySplashSound() {
		AudioPlayer.PlayOneShot_Static(splashSFX, VolumeControl.GetEffectVol() * 0.7f);
	}


	float PowerLerp(float t) {
		t = Mathf.Clamp01(t);
		return 1f - 16f * Mathf.Pow((t - 0.5f), 4f);
	}

	float PowerLerp(float t, float power = 3f) {
		t = Mathf.Clamp01(t);
		return Mathf.Pow(t, power);
	}


	public Vector2 rectTargetPos;
	public Vector3 targetRot;
	public float targetScale;
	public void SetBetweenPoints(float f) {
		rt.anchoredPosition = Vector2.Lerp(iniRectPos, rectTargetPos, f);
		transform.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(targetRot), f);
		transform.localScale = Vector3.one * Mathf.Lerp(1f, targetScale, f);
	}
	[ContextMenu("Copy")]
	public void CopyStuff() {
		rectTargetPos = GetComponent<RectTransform>().anchoredPosition;
		targetRot = transform.localEulerAngles;
		targetScale = transform.localScale.x;
	}







}
