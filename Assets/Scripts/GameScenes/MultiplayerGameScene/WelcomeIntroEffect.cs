using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WelcomeIntroEffect : MonoBehaviour {
	[HideInInspector] public bool AnimationFinished = false;



	void OnEnable() {
		StartCoroutine(StartOpeningAnimation());
	}


	public Image blackoutImg;
	public GradientColorTypewriter typewriter;
	public AudioClip typingClip, fireworkSound, titleClip;
	public Transform LettersHolder;
	public Color titleExpandedColor;
	public GameObject nxtBtnExtra;
	public GameObject fireworksEffect;

	IEnumerator StartOpeningAnimation() {
		//blackout
		float blackoutTime = 2f;
		float t = 0f;
		while (t < blackoutTime) {
			t += Time.unscaledDeltaTime;
			blackoutImg.color = Color.Lerp(Color.clear, Color.black, t / blackoutTime);
			BGMManager.instance.SetBGMVolume((1 - t / blackoutTime));
			yield return null;
		}
		blackoutImg.color = Color.black;
		BGMManager.instance.SetBGMVolume(0);

		yield return new WaitForSecondsRealtime(1f);

		for (int i = 0; i < 7; i++) {
			typewriter.AnimateNext();
			yield return new WaitForSecondsRealtime(0.05f);
			AudioPlayer.PlayOneShot_Static(typingClip, VolumeControl.GetEffectVol());
			yield return new WaitForSecondsRealtime(0.05f);
		}
		yield return new WaitForSecondsRealtime(0.5f);
		for (int i = 0; i < 2; i++) {
			typewriter.AnimateNext();
			yield return new WaitForSecondsRealtime(0.05f);
			AudioPlayer.PlayOneShot_Static(typingClip, VolumeControl.GetEffectVol());
			yield return new WaitForSecondsRealtime(0.05f);
		}


		yield return new WaitForSecondsRealtime(1f);

		for (int i = 0; i < 13; i++) {
			LettersHolder.GetChild(i).gameObject.SetActive(true);
			yield return new WaitForSecondsRealtime(0.3f);
		}
		yield return new WaitForSecondsRealtime(2f);

		Camera.main.orthographic = false;

		float expandTime = 4f;
		t = 0;
		bool confettiFired = false;
		typewriter.gameObject.SetActive(false);
		while (t < expandTime) {
			float r = 1f - Mathf.Pow((1f - t / expandTime), 5f);
			t += Time.unscaledDeltaTime;
			Color targetC = Color.Lerp(Color.white, titleExpandedColor, t / expandTime);
			foreach (Transform tr in LettersHolder) {
				tr.GetComponent<WordFlylingIn>().SetBetweenPoints(r);
				tr.GetComponent<TextMeshProUGUI>().color = targetC;
			}
			if (!confettiFired && t > 0.1f) {
				AudioPlayer.Instance.PlayOneShot(titleClip);
				confettiFired = true;
				StartCoroutine(FirecrackerEffects());
			}
			yield return null;
		}

		typewriter.gameObject.SetActive(true);
		typewriter.gameObject.GetComponent<TextMeshProUGUI>().color = Color.clear;
		float reduceTime = 2f;
		t = 0;
		while (t < reduceTime) {
			float r = Mathf.Pow((1f - t / reduceTime), 3f);
			t += Time.unscaledDeltaTime;
			Color targetC = Color.Lerp(Color.white, titleExpandedColor, 1f - t / reduceTime);
			foreach (Transform tr in LettersHolder) {
				tr.GetComponent<WordFlylingIn>().SetBetweenPoints(r);
				tr.GetComponent<TextMeshProUGUI>().color = targetC;
			}
			typewriter.gameObject.GetComponent<TextMeshProUGUI>().color = Color.Lerp(Color.clear, Color.white, t / reduceTime);
			yield return null;
		}
		foreach (Transform tr in LettersHolder) {
			tr.GetComponent<WordFlylingIn>().SetBetweenPoints(0);
			tr.GetComponent<TextMeshProUGUI>().color = Color.white;
		}
		StartCoroutine(BobbingTitle());
		Camera.main.orthographic = true;
		t = 0;
		while (t < blackoutTime) {
			t += Time.unscaledDeltaTime;
			blackoutImg.color = Color.Lerp(Color.black, Color.clear, t / blackoutTime);
			BGMManager.instance.SetBGMVolume(t / blackoutTime);
			yield return null;
		}
		BGMManager.instance.SetBGMVolume(1f);
		blackoutImg.color = Color.clear;
		nxtBtnExtra.SetActive(true);

		AnimationFinished = true;
	}

	IEnumerator FirecrackerEffects() {
		yield return new WaitForSeconds(1.5f);
		AudioPlayer.PlayOneShot_Static(fireworkSound);
		//create firecracker effects
	}

	IEnumerator BobbingTitle() {
		while (true) {
			LettersHolder.localScale = Vector3.one + Vector3.up * Mathf.Sin(Time.unscaledTime * Mathf.Deg2Rad * 180f) * 0.05f;
			yield return null;
		}
	}

	public void DisableObj() {
		nxtBtnExtra.SetActive(false);
	}


}
