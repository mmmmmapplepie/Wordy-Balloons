using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;

public class LoadingAnimation : MonoBehaviour {
	public enum StretchMode { power, sinusoidal, }
	public RectTransform rt, parentRT;
	public float PowerFactor = 1f;
	Image cubeImg;
	[Range(0, 1)]
	public float startProgress = 0f;
	[Range(0, 1)]
	public float maxOpacity = 1f;
	[Range(0, 1)]
	public float stretchFactor = 0.5f;
	public StretchMode stretchMode;
	public float stopTime = 0.25f;
	public float moveTime = 0.5f;
	public float fadeInTime = 0.5f;
	public bool autoTime = false;
	public bool fadeIn = false;
	public bool clockWise = true;
	public bool stickToEdges = false;

	float progress = 0;
	float totalTime = 0;
	float moveRatio;

	void OnEnable() {
		if (Application.isPlaying && fadeIn) {
			StartCoroutine(FadeIn());
		}
		rt.anchorMax = rt.anchorMin = Vector2.zero;
		if (autoTime) stopTime = moveTime / 2f;
		totalTime = 4f * (moveTime + stopTime);
		progress = startProgress;
		moveRatio = moveTime / (stopTime + moveTime);
	}
	void OnDisable() {
		cubeImg = GetComponent<Image>();
		cubeImg.color = new Color(cubeImg.color.r, cubeImg.color.g, cubeImg.color.b, maxOpacity);
	}
	IEnumerator FadeIn() {
		cubeImg = GetComponent<Image>();
		Color c = cubeImg.color;
		cubeImg.color = new Color(c.r, c.g, c.b, 0f);
		Color startC = cubeImg.color;
		Color endC = new Color(c.r, c.g, c.b, 1f);
		float t = 0;
		while (t < fadeInTime) {
			t += Time.deltaTime;
			cubeImg.color = Color.Lerp(startC, endC, t / fadeInTime);
			yield return null;
		}
		cubeImg.color = endC;
	}



	void Update() {
		float change = Time.deltaTime / totalTime;
		if (clockWise) {
			progress += change;
			progress %= 1f;
		} else {
			progress -= change;
			if (progress < 0f) progress = progress + 1f;
		}
		progress = progress % 1f;
		CallPhase();
	}
	void CallPhase() {
		if (progress < 0.25f) {
			BottomLeftStart();
			return;
		}
		if (progress > 0.25f && progress < 0.5f) {
			TopLeftStart();
			return;
		}
		if (progress > 0.5f && progress < 0.75f) {
			TopRightStart();
			return;
		}
		if (progress > 0.75f) {
			BottomRightStart();
			return;
		}
	}

	//the "wait" will happen at the end of each cycle
	void BottomLeftStart() {
		float parentHeight = parentRT.rect.height;
		float moveLimit = 0.25f * moveRatio;
		if (progress < moveLimit) {
			Vector2 rtSize = rt.rect.size;

			float t = GetStretchProgress(progress, moveLimit);
			float stretch = Mathf.Lerp(1f, parentHeight / rtSize.y, stretchFactor);
			Vector3 finalScale = new Vector3(1f, stretch, 1f);
			rt.localScale = Vector3.Lerp(Vector3.one, finalScale, t);

			float rtAnchorHeight = rtSize.y * 0.5f;
			float yLerp = Mathf.Lerp(rtAnchorHeight, parentHeight - rtAnchorHeight, progress / moveLimit);
			float yPos = 0f;
			if (!stickToEdges) {
				yPos = progress < moveLimit / 2f ? Mathf.Max(yLerp, rt.localScale.y * rtAnchorHeight) : Mathf.Min(yLerp, parentHeight - rt.localScale.y * rtAnchorHeight);
			} else {
				yPos = progress < moveLimit / 2f ? Mathf.Clamp(Mathf.Min(yLerp, rt.localScale.y * rtAnchorHeight), rt.localScale.y * rtAnchorHeight, Mathf.Infinity) : Mathf.Clamp(Mathf.Max(yLerp, parentHeight - rt.localScale.y * rtAnchorHeight), 0f, parentHeight - rt.localScale.y * rtAnchorHeight);
			}

			rt.anchoredPosition = new Vector2(rtSize.x * 0.5f, yPos);

		} else {
			rt.anchoredPosition = new Vector2(rt.rect.size.x * 0.5f, parentHeight - rt.rect.size.y * 0.5f);
			rt.localScale = Vector3.one;
		}
	}
	void TopLeftStart() {
		float parentHeight = parentRT.rect.height;
		float parentWidth = parentRT.rect.width;
		float moveLimit = 0.25f * moveRatio;
		float progressTemp = progress - 0.25f;
		if (progressTemp < moveLimit) {
			Vector2 rtSize = rt.rect.size;

			float stretch = Mathf.Lerp(1f, parentWidth / rtSize.x, stretchFactor);
			Vector3 finalScale = new Vector3(stretch, 1f, 1f);

			float t = GetStretchProgress(progressTemp, moveLimit);
			rt.localScale = Vector3.Lerp(Vector3.one, finalScale, t);
			float rtAnchorWidth = rtSize.x * 0.5f;
			float xLerp = Mathf.Lerp(rtAnchorWidth, parentWidth - rtAnchorWidth, (progressTemp / moveLimit));
			float xPos = 0f;
			if (!stickToEdges) {
				xPos = progressTemp < moveLimit / 2f ? Mathf.Max(xLerp, rt.localScale.x * rtAnchorWidth) : Mathf.Min(xLerp, parentWidth - rt.localScale.x * rtAnchorWidth);
			} else {
				xPos = progressTemp < moveLimit / 2f ? Mathf.Clamp(Mathf.Min(xLerp, rt.localScale.x * rtAnchorWidth), rt.localScale.x * rtAnchorWidth, Mathf.Infinity) : Mathf.Clamp(Mathf.Max(xLerp, parentWidth - rt.localScale.x * rtAnchorWidth), 0f, parentWidth - rt.localScale.x * rtAnchorWidth);
			}
			rt.anchoredPosition = new Vector2(xPos, parentHeight - rtSize.y * 0.5f);

		} else {
			rt.anchoredPosition = new Vector2(parentWidth - rt.rect.size.x * 0.5f, parentHeight - rt.rect.size.y * 0.5f);
			rt.localScale = Vector3.one;
		}

	}
	void TopRightStart() {
		float parentHeight = parentRT.rect.height;
		float parentWidth = parentRT.rect.width;
		float moveLimit = 0.25f * moveRatio;
		float progressTemp = progress - 0.5f;
		if (progressTemp < moveLimit) {
			Vector2 rtSize = rt.rect.size;

			float stretch = Mathf.Lerp(1f, parentHeight / rtSize.y, stretchFactor);
			Vector3 finalScale = new Vector3(1f, stretch, 1f);

			float t = GetStretchProgress(progressTemp, moveLimit);
			rt.localScale = Vector3.Lerp(Vector3.one, finalScale, t);

			float rtAnchorHeight = rtSize.y * 0.5f;
			float yLerp = Mathf.Lerp(rtAnchorHeight, parentHeight - rtAnchorHeight, 1 - (progressTemp / moveLimit));
			float yPos = 0f;
			if (!stickToEdges) {
				yPos = progressTemp < moveLimit / 2f ? Mathf.Min(yLerp, parentHeight - rt.localScale.y * rtAnchorHeight) : Mathf.Max(yLerp, rt.localScale.y * rtAnchorHeight);
			} else {
				yPos = progressTemp < moveLimit / 2f ? Mathf.Clamp(Mathf.Max(yLerp, parentHeight - rt.localScale.y * rtAnchorHeight), 0f, parentHeight - rt.localScale.y * rtAnchorHeight) : Mathf.Clamp(Mathf.Min(yLerp, rt.localScale.y * rtAnchorHeight), rt.localScale.y * rtAnchorHeight, Mathf.Infinity);
			}

			rt.anchoredPosition = new Vector2(parentWidth - rtSize.x * 0.5f, yPos);

		} else {
			rt.anchoredPosition = new Vector2(parentWidth - rt.rect.size.x * 0.5f, rt.rect.size.y * 0.5f);
			rt.localScale = Vector3.one;
		}

	}
	void BottomRightStart() {
		float parentHeight = parentRT.rect.height;
		float parentWidth = parentRT.rect.width;
		float moveLimit = 0.25f * moveRatio;
		float progressTemp = progress - 0.75f;
		if (progressTemp < moveLimit) {
			Vector2 rtSize = rt.rect.size;

			float stretch = Mathf.Lerp(1f, parentWidth / rtSize.x, stretchFactor);
			Vector3 finalScale = new Vector3(stretch, 1f, 1f);

			float t = GetStretchProgress(progressTemp, moveLimit);
			rt.localScale = Vector3.Lerp(Vector3.one, finalScale, t);
			float rtAnchorWidth = rtSize.x * 0.5f;
			float xLerp = Mathf.Lerp(rtAnchorWidth, parentWidth - rtAnchorWidth, 1 - (progressTemp / moveLimit));
			float xPos = 0f;
			if (!stickToEdges) {
				xPos = progressTemp < moveLimit / 2f ? Mathf.Min(xLerp, parentWidth - rt.localScale.x * rtAnchorWidth) : Mathf.Max(xLerp, rt.localScale.x * rtAnchorWidth);
			} else {
				xPos = progressTemp < moveLimit / 2f ? Mathf.Clamp(Mathf.Max(xLerp, parentWidth - rt.localScale.x * rtAnchorWidth), 0f, parentWidth - rt.localScale.x * rtAnchorWidth) : Mathf.Clamp(Mathf.Min(xLerp, rt.localScale.x * rtAnchorWidth), rt.localScale.x * rtAnchorWidth, Mathf.Infinity);
			}
			rt.anchoredPosition = new Vector2(xPos, rtSize.y * 0.5f);

		} else {
			rt.anchoredPosition = rt.rect.size * 0.5f;
			rt.localScale = Vector3.one;
		}
	}



	float GetStretchProgress(float curr, float total) {
		if (stretchMode == StretchMode.power) {
			if (curr < total / 2f) {
				return Mathf.Pow(curr / (total / 2f), PowerFactor);
			} else {
				return Mathf.Pow((total - curr) / (total / 2f), PowerFactor);
			}
		}
		if (stretchMode == StretchMode.sinusoidal) {
			return Mathf.Sin(Mathf.PI * (curr / total));
		}
		return 0;
	}
}
