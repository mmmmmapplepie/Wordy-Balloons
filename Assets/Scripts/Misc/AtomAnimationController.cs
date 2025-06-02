using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AtomAnimationController : MonoBehaviour {
	List<(Image, Color)> initialColors = new List<(Image, Color)>();
	ElectronOrbit[] orbitAnims;
	public Color stoppedColor;
	void Start() {
		Image[] imgs = gameObject.GetComponentsInChildren<Image>();
		foreach (Image im in imgs) {
			initialColors.Add((im, im.color));
		}
		orbitAnims = gameObject.GetComponentsInChildren<ElectronOrbit>();
		AnimateStop();
	}

	Coroutine changingAnimation;
	float progress = 0f;
	[ContextMenu("play")]
	public void AnimateStart() {
		if (changingAnimation != null) StopCoroutine(changingAnimation);
		changingAnimation = StartCoroutine(AnimationChange(true));
	}
	[ContextMenu("stopp")]
	public void AnimateStop() {
		if (changingAnimation != null) StopCoroutine(changingAnimation);
		changingAnimation = StartCoroutine(AnimationChange(false));
	}

	public float animPeriod = 2f;
	public float targetSpeed = 200f;

	IEnumerator AnimationChange(bool start) {
		float targetProgress = start ? 1f : 0f;

		while (start ? progress < targetProgress : progress > targetProgress) {
			progress += start ? Time.deltaTime / animPeriod : -Time.deltaTime / animPeriod;
			UpdateSpeeds(start, progress);
			UpdateColors(start, progress);
			yield return null;
		}
		progress = targetProgress;
		UpdateSpeeds(start, progress);
		UpdateColors(start, progress);
	}
	void UpdateSpeeds(bool start, float progress) {
		if (orbitAnims == null) return;
		foreach (ElectronOrbit orbit in orbitAnims) {
			orbit.orbitSpeed = Mathf.Lerp(0f, targetSpeed, progress);
		}
	}

	void UpdateColors(bool start, float progress) {
		foreach ((Image, Color) item in initialColors) {
			Color stopCol = stoppedColor;
			stopCol.a = item.Item2.a;
			item.Item1.color = Color.Lerp(stopCol, item.Item2, progress);
		}
	}


}
