using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class dragTest : MonoBehaviour {
	public Scroller scroller;
	List<WordHolder> words;
	public Scrollbar scrollbar;
	public List<WordOptionBtn> buttons;
	void Awake() {
		scroller.OnValueChanged(ScrollValChanged);
	}
	public void SetScroller(List<WordHolder> data) {
		words = data;
		int count = words != null ? words.Count : 0;
		scroller.SetTotalCount(count);
		scroller.Position = 0;
		scrollbar.size = Mathf.Clamp(12 / Mathf.Max(words.Count, 1), 0.05f, 1f);
		if (words.Count <= 12) {
			SetupBtnsWithGivenScroll(0f);
		}
	}

	public void ScrollBarMoved(float s) {
		scroller.Position = s * words.Count;
	}

	void ScrollValChanged(float f) {
		if (words == null || words.Count <= 12) return;
		while (f < 0) f += words.Count;
		f %= words.Count;


		SetupBtnsWithGivenScroll(f);
		scrollbar.Set(f / words.Count);
	}

	public float yGap = -70f, startingPos = -40f;

	void SetupBtnsWithGivenScroll(float f) {
		if (words.Count == 0) {
			scrollbar.interactable = false;
			for (int i = 0; i < buttons.Count; i++) {
				WordOptionBtn btn = buttons[i];
				btn.gameObject.SetActive(false);
			}
			return;
		}

		float deltaShift = 10f;

		if (words.Count <= 12) {
			scrollbar.interactable = false;
			for (int i = 0; i < buttons.Count; i++) {
				WordOptionBtn btn = buttons[i];
				if (i < words.Count) {
					btn.gameObject.SetActive(true);
					btn.SetData(words[i]);
					btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, i * yGap + startingPos - deltaShift);
				} else {
					btn.gameObject.SetActive(false);
				}
			}
			return;
		}

		scrollbar.interactable = true;
		deltaShift = GetSignedDecimal(f);

		for (int i = 0; i < buttons.Count; i++) {
			WordOptionBtn btn = buttons[i];
			int btnPosI = i - 1;
			int wordI = Mathf.FloorToInt(f) - 1 + i;
			btn.gameObject.SetActive(true);
			btn.SetData(words[(wordI < 0 ? wordI + words.Count : wordI) % words.Count]);
			btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, (btnPosI - deltaShift) * yGap + startingPos);
		}
	}
	float GetSignedDecimal(float value) {
		double integralPart = value > 0 ? Mathf.Floor(value) : Mathf.Ceil(value);
		return (float)(value - integralPart);
	}
}
