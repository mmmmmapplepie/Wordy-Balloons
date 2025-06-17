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
#if UNITY_EDITOR
	void OnValidate() {
		if (!gameObject.activeInHierarchy) return;
		StartCoroutine(SlideAnim(0));
	}
#endif
	public void SliderToggleClicked() {
		if (!gameObject.activeInHierarchy) return;
		if (anim != null) StopCoroutine(anim);
		onRightSide = !onRightSide;
		anim = StartCoroutine(SlideAnim(slideTime));
	}

	Coroutine anim;
	IEnumerator SlideAnim(float p) {
		if (rightPoint == null || leftPoint == null || sliderObj == null) yield break;
		Color sColor = sliderObj.color;
		Color tarColor = onRightSide ? rightSideColor : leftSideColor;
		Vector3 sPos = sliderObj.transform.localPosition;
		Vector3 tarPos = onRightSide ? rightPoint.localPosition : leftPoint.localPosition;
		text.text = onRightSide ? rightText : leftText;

		float t = 0;
		while (t <= p) {
			t += Time.unscaledDeltaTime;
			float r = Mathf.SmoothStep(0, 1f, t / p);
			sliderObj.transform.localPosition = Vector3.Lerp(sPos, tarPos, r);
			sliderObj.color = Color.Lerp(sColor, tarColor, r);
			yield return null;
		}
		sliderObj.transform.localPosition = Vector3.Lerp(sPos, tarPos, 1);
		sliderObj.color = Color.Lerp(sColor, tarColor, 1);
	}
}
