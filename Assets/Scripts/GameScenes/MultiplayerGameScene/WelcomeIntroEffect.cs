using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WelcomeIntroEffect : MonoBehaviour {
	[HideInInspector] public bool AnimationFinished = false;



	void OnEnable() {
		if (GameData.PlayMode == PlayModeEnum.Tutorial) StartCoroutine(StartOpeningAnimation());
	}


	public Image blackoutImg;
	public GradientColorTypewriter typewriter;
	public AudioClip typingClip, fireworkSound, titleClip;
	public Transform LettersHolder;
	public Color titleExpandedColor;
	public GameObject nxtBtnExtra;
	public List<GameObject> fireworksEffect = new List<GameObject>();
	public Gradient g;

	IEnumerator StartOpeningAnimation() {
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


		yield return new WaitForSecondsRealtime(0.4f);

		for (int i = 0; i < 13; i++) {
			LettersHolder.GetChild(i).gameObject.SetActive(true);
			yield return new WaitForSecondsRealtime(0.25f);
		}
		yield return new WaitForSecondsRealtime(2f);

		Camera.main.orthographic = false;

		float expandTime = 3f;
		t = 0;
		bool confettiFired = false;
		typewriter.gameObject.SetActive(false);
		while (t < expandTime) {
			float r = 1f - Mathf.Pow((1f - t / expandTime), 5f);
			t += Time.unscaledDeltaTime;
			// Color targetC = Color.Lerp(Color.white, titleExpandedColor, t / expandTime);
			for (int i = 0; i < LettersHolder.childCount; i++) {
				Color targetC = Color.Lerp(Color.white, g.Evaluate((float)i / LettersHolder.childCount), r);
				LettersHolder.GetChild(i).GetComponent<WordFlylingIn>().SetBetweenPoints(r);
				LettersHolder.GetChild(i).GetComponent<TextMeshProUGUI>().color = targetC;
			}
			if (!confettiFired && t > 0.1f) {
				AudioPlayer.Instance.PlayOneShot(titleClip, VolumeControl.GetEffectVol());
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

			for (int i = 0; i < LettersHolder.childCount; i++) {
				Color targetC = Color.Lerp(Color.white, g.Evaluate((float)i / LettersHolder.childCount), r / 2f + 0.5f);
				LettersHolder.GetChild(i).GetComponent<WordFlylingIn>().SetBetweenPoints(r);
				LettersHolder.GetChild(i).GetComponent<TextMeshProUGUI>().color = targetC;
			}
			typewriter.gameObject.GetComponent<TextMeshProUGUI>().color = Color.Lerp(Color.clear, Color.white, t / reduceTime);
			BGMManager.instance.SetBGMVolume(t / reduceTime);
			yield return null;
		}
		BGMManager.instance.SetBGMVolume(1f);
		for (int i = 0; i < LettersHolder.childCount; i++) {
			Color targetC = Color.Lerp(Color.white, g.Evaluate((float)i / LettersHolder.childCount), 0.5f);
			LettersHolder.GetChild(i).GetComponent<WordFlylingIn>().SetBetweenPoints(0);
			LettersHolder.GetChild(i).GetComponent<TextMeshProUGUI>().color = targetC;
		}
		StartCoroutine(BobbingTitle());
		Camera.main.orthographic = true;
		t = 0;
		while (t < blackoutTime) {
			t += Time.unscaledDeltaTime;
			blackoutImg.color = Color.Lerp(Color.black, Color.clear, t / blackoutTime);
			yield return null;
		}
		BGMManager.instance.SetBGMVolume(1f);
		blackoutImg.color = Color.clear;
		nxtBtnExtra.SetActive(true);

		AnimationFinished = true;
	}

	IEnumerator FirecrackerEffects() {
		yield return new WaitForSeconds(1f);
		List<Vector2> poses = new List<Vector2>();
		for (int i = -4; i <= 4; i++) {
			poses.Add(GetArcPoint(GetComponent<RectTransform>().rect.height * 0.3f, 0.1f + (float)(i + 4) * 0.1f) - 300f * Vector2.up);
		}
		for (int i = 0; i < poses.Count; i++) {
			StartCoroutine(CreateCracker(poses[i]));
		}
		yield return new WaitForSeconds(0.2f);
		AudioPlayer.PlayOneShot_Static(fireworkSound, VolumeControl.GetEffectVol() * Random.Range(0.6f, 1f));
		yield return new WaitForSeconds(0.1f);
		AudioPlayer.PlayOneShot_Static(fireworkSound, VolumeControl.GetEffectVol() * Random.Range(0.6f, 1f));

	}
	IEnumerator CreateCracker(Vector2 pos) {
		yield return new WaitForSeconds(Random.Range(0, 0.2f));
		GameObject newEffectPrefab = fireworksEffect[Random.Range(0, fireworksEffect.Count - 1)];
		GameObject g = Instantiate(newEffectPrefab, transform);
		g.GetComponent<RectTransform>().anchoredPosition = pos;
	}
	public Vector2 GetArcPoint(float maxHeight, float t) {
		t = Mathf.Clamp01(t); // Ensure t stays within [0,1]

		float width = GetComponent<RectTransform>().rect.width;
		// float centerY = GetComponent<RectTransform>().rect.height / 2f;
		float halfWidth = width / 2f;

		float x = Mathf.Lerp(-halfWidth, halfWidth, t);
		float normalizedX = x / halfWidth; // Range: -1 to 1

		// y = sqrt(1 - (x/a)^2) * b (top half of ellipse)
		float yOffset = Mathf.Sqrt(1f - normalizedX * normalizedX) * maxHeight;
		float y = yOffset;

		return new Vector2(x, y);
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
