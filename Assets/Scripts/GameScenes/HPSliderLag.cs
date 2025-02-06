using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPSliderLag : MonoBehaviour {
	public Slider laggingSlider;
	float lagTime = 0.5f;
	float speed = 0.3f;

	float latestHitTime = 0f;
	float targetVal = 1f;
	bool moving = false;
	public void NewValueForTargetSlider(float value) {
		if (!moving) latestHitTime = Time.time;
		targetVal = value;
	}
	void Update() {
		if (laggingSlider.value < targetVal) {
			moving = false;
			latestHitTime = Time.time;
			laggingSlider.value = targetVal;
			return;
		}
		if (Time.time - latestHitTime > lagTime && laggingSlider.value != targetVal) {
			moving = true;
			float newVal = laggingSlider.value - speed * Time.deltaTime;
			laggingSlider.value = Mathf.Max(newVal, targetVal);
			if (laggingSlider.value == targetVal) moving = false;
		}
	}
}
