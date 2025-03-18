using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TypedBalloonAnimations : MonoBehaviour {
	public HarmonicOscillator oscillator;
	[Range(0, 1)] public float minScaleNorm, maxScaleNorm;
	public float scaleFactor;
	float oscillatorRange;
	[Range(-1, 1)]
	public float pivot = -1;//1 is make the bottom stationary, -1 make bottom stationary
	Vector3 centerPos;
	void Start() {
		oscillatorRange = oscillator.maxPos - oscillator.minPos;
		transform.GetChild(transform.childCount - 1).GetComponent<SpriteRenderer>().color = GameData.allColorOptions[GameData.ClientID_KEY_ColorIndex_VAL[NetworkManager.Singleton.LocalClientId]];
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
		transform.localPosition = centerPos + new Vector3(0, 0.5f * transform.localScale.y * pivot, 0);
	}

	public void AddImpulse(float k) {
		oscillator.AddImpulse(k);
	}

	public const float animationTime = 1f;
	float expPow = 8f;
	public void CorrectEntryAnimation(Vector3 target) {
		StartCoroutine(EntryAnimation(target));
	}
	IEnumerator EntryAnimation(Vector3 target) {
		float t = 0;
		float initialScale = scaleFactor;
		float rotAdd = Random.Range(-180f, 180f);
		while (t < animationTime) {
			t += Time.deltaTime;
			float r = 1f - 2f * Mathf.Pow(3f, -expPow * t / animationTime);
			pivot = Mathf.Lerp(-1, 0, t / animationTime);
			centerPos = Vector3.Lerp(Vector3.zero, target, r);
			scaleFactor = Mathf.Lerp(initialScale, 0.3f, t / animationTime);
			transform.rotation *= Quaternion.Euler(0, 0, rotAdd * Time.deltaTime);

			foreach (Transform tr in transform) {
				SpriteRenderer s = tr.GetComponent<SpriteRenderer>();
				Color i = s.color;
				i.a = 1f - Mathf.Pow(t / animationTime, 10f);
				s.color = i;
			}

			yield return null;
		}
		Destroy(gameObject);
	}

}
