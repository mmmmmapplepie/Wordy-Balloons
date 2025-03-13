using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Guide : MonoBehaviour {

	public RectTransform panelContainer; // The parent container holding the panels
	List<RectTransform> panels = new List<RectTransform>(); // List of panels
	float panelWidth = 500f; // Width of each panel
	public float shiftDuration = 0.5f; // Animation duration
	public float fadedAlpha = 0.2f; // Transparency for non-centered panels

	private bool isShifting = false;
	int panelsInCenterAndRight = 0;
	int centerIndex = 0;

	private void Start() {
		foreach (Transform t in panelContainer) {
			panels.Add(t.GetComponent<RectTransform>());
		}
		panelWidth = panels[0].rect.width;
		EnsureMinimumPanels();
		CorrectPanelPositions();
	}

	private void EnsureMinimumPanels() {
		int count = panels.Count;
		if (count < 5) {
			int multiplier = Mathf.CeilToInt(5f / (float)count) - 1;
			int copiesNeeded = multiplier * count;
			for (int i = 0; i < copiesNeeded; i++) {
				panels.Add(Instantiate(panels[i % count], panelContainer));
			}
		}
		centerIndex = Mathf.CeilToInt(panels.Count / 2f) - 1;
		panelsInCenterAndRight = Mathf.FloorToInt(panels.Count / 2f) + 1;
		for (int i = 0; i < panels.Count - panelsInCenterAndRight; i++) {
			RectTransform rt = panels[panels.Count - 1];
			panels.RemoveAt(panels.Count - 1);
			panels.Insert(0, rt);
		}
	}


	private void CorrectPanelPositions() {
		for (int i = 0; i < panels.Count; i++) {
			int relativeIndex = i - centerIndex;
			panels[i].anchoredPosition = new Vector2(relativeIndex * panelWidth, 0);
		}
		panelContainer.anchoredPosition = Vector2.zero;
		SetTransparency();
	}
	private void SetTransparency() {
		for (int i = 0; i < panels.Count; i++) {
			float distanceFromCenter = Mathf.Abs(panels[i].anchoredPosition.x);
			float alpha = fadedAlpha;
			if (distanceFromCenter < panelWidth) {
				alpha = Mathf.Lerp(1f, fadedAlpha, distanceFromCenter / panelWidth);
			}
			CanvasGroup canvasGroup = panels[i].GetComponent<CanvasGroup>();
			if (canvasGroup == null) {
				canvasGroup = panels[i].gameObject.AddComponent<CanvasGroup>();
			}
			canvasGroup.alpha = alpha;
		}
	}
	public void ShiftPanels(bool right) {
		if (isShifting) return;
		StartCoroutine(ShiftPanelsCoroutine(right));
	}

	private IEnumerator ShiftPanelsCoroutine(bool right) {
		isShifting = true;
		float elapsedTime = 0f;
		int direction = right ? 1 : -1;
		Vector2 startPosition = panelContainer.anchoredPosition;
		Vector2 targetPosition = startPosition + new Vector2(-direction * panelWidth, 0);
		CanvasGroup fade = panels[centerIndex].GetComponent<CanvasGroup>();
		CanvasGroup show = panels[centerIndex + (right ? 1 : -1)].GetComponent<CanvasGroup>();


		while (elapsedTime < shiftDuration) {
			elapsedTime += Time.deltaTime;
			float t = elapsedTime / shiftDuration;
			t = Mathf.SmoothStep(0, 1, t);

			panelContainer.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
			fade.alpha = Mathf.Lerp(1f, fadedAlpha, t);
			show.alpha = Mathf.Lerp(fadedAlpha, 1f, t);
			yield return null;
		}

		panelContainer.anchoredPosition = targetPosition;
		RearrangePanels(right);
		CorrectPanelPositions();
		isShifting = false;
	}

	private void RearrangePanels(bool right) {
		if (!right) {
			RectTransform rt = panels[panels.Count - 1];
			panels.RemoveAt(panels.Count - 1);
			panels.Insert(0, rt);
		} else {
			RectTransform rt = panels[0];
			panels.RemoveAt(0);
			panels.Add(rt);
		}
	}



	public void CloseGuide() {
		gameObject.SetActive(false);
	}

}
