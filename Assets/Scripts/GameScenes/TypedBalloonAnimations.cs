using System.Collections.Generic;
using UnityEngine;

public class TypedBalloonAnimations : MonoBehaviour {
	public HarmonicOscillator oscillator;
	[Range(0, 1)] public float minScaleNorm, maxScaleNorm;
	public float scaleFactor;
	float oscillatorRange;
	[Range(-1, 1)]
	public float pivot = -1;//1 is make the bottom stationary, -1 make bottom stationary
	void Start() {
		oscillatorRange = oscillator.maxPos - oscillator.minPos;
	}
	void Update() {
		SetObjectScale();
		SetObjectPivot();
	}

	void SetObjectScale() {
		float r = (oscillator.position - oscillator.minPos) / oscillatorRange;
		float vertScale;
		if (r >= 0.5f) {
			vertScale = 1f + Mathf.Lerp(0, maxScaleNorm, 2f * r - 1f);
		} else {
			vertScale = Mathf.Lerp(minScaleNorm, 1f, 2f * r);
		}
		float horScale = 1 / vertScale;
		transform.localScale = new Vector3(horScale * scaleFactor, vertScale * scaleFactor, 1f);
	}
	void SetObjectPivot() {
		transform.localPosition = new Vector3(0, 0.5f * transform.localScale.y * pivot, 0);
	}

	public void AddImpulse(float k) {
		oscillator.AddImpulse(k);
	}

	public const float animationTime = 1f;
	public void CorrectEntryAnimation() {

	}

}
