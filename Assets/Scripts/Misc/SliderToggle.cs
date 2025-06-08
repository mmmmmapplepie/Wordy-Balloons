using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderToggle : MonoBehaviour {
	public Transform leftPoint, rightPoint;
	public TextMeshProUGUI text;
	public string rightText, leftText;
	public Image sliderObj;
	public Color rightSideColor, leftSideColor;
	public float slideTime = 0.5f;
	public bool onRightSide = true;

	void OnValidate() {
		if (!gameObject.activeInHierarchy) return;
		StartCoroutine(SlideAnim(0));
	}

	public void SliderToggleClicked() {
		if (!gameObject.activeInHierarchy) return;
		if (anim != null) StopCoroutine(anim);
		onRightSide = !onRightSide;
		anim = StartCoroutine(SlideAnim(slideTime));
	}

	Coroutine anim;
	IEnumerator SlideAnim(float p) {
		Color sColor = sliderObj.color;
		Color tarColor = onRightSide ? rightSideColor : leftSideColor;
		Vector3 sPos = sliderObj.transform.position;
		Vector3 tarPos = onRightSide ? rightPoint.position : leftPoint.position;
		text.text = onRightSide ? rightText : leftText;

		float t = 0;
		while (t <= p) {
			t += Time.unscaledDeltaTime;
			float r = Mathf.SmoothStep(0, 1f, t / p);
			sliderObj.transform.position = Vector3.Lerp(sPos, tarPos, r);
			sliderObj.color = Color.Lerp(sColor, tarColor, r);
			yield return null;
		}
		sliderObj.transform.position = Vector3.Lerp(sPos, tarPos, 1);
		sliderObj.color = Color.Lerp(sColor, tarColor, 1);
	}
}
