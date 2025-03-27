using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGraphManager : MonoBehaviour {
	public GameObject barPrefab, labelPrefab;
	public RectTransform container, barContainer, labelContainer;

	public float barSize = 1f;
	public float labelSize = 1f;
	public float edgeGap = 0.1f;
	public Color barColor, labelColor, valueColor;
	public bool ignoreXValues = true;

	// void Update() {
	// 	SetGraph(data);
	// }

	// void Start() {
	// 	SetGraph(new List<(float, float, string)> {
	// 		(1f,1f,""),
	// 		(2f,2f,""),
	// 		(3f,1.5f,""),
	// 		(4f,0f,""),
	// 		(5f,1f,""),
	// 		(-5f,1.5f,""),
	// 	});
	// }

	List<(float, float, string)> data;
	///<summary>
	///list shoudl contain (x, y, string for x label);
	///</summary>
	public void SetGraph(List<(float, float, string)> inputData) {
		ClearGraph();
		data = inputData;

		if (inputData == null || inputData.Count == 0) return;

		SetGraphMinMax();

		CreateGraphVisuals();
	}

	void ClearGraph() {
		foreach (Transform t in barContainer) Destroy(t.gameObject);
		foreach (Transform t in labelContainer) Destroy(t.gameObject);
	}

	[HideInInspector] public float xMin, yMin, xMax, yMax;

	float xExpNum = 1f, yExpNum = 1f;
	void SetGraphMinMax() {
		xMin = data[0].Item1; yMin = data[0].Item2; xMax = data[0].Item1; yMax = data[0].Item2;
		foreach ((float, float, string) point in data) {
			if (point.Item1 > xMax) xMax = point.Item1;
			if (point.Item1 < xMin) xMin = point.Item1;
			if (point.Item2 > yMax) yMax = point.Item2;
			if (point.Item2 < yMin) yMin = point.Item2;
		}

		if (xMin == xMax) {
			xMin--;
			xMax++;
			xExpNum = 1f;
		} else {
			float xExp = Mathf.CeilToInt(Mathf.Log10(Mathf.Abs(xMax - xMin))) - 1;
			xExpNum = Mathf.Pow(10f, xExp);
			float xD = (xMax - xMin) * edgeGap;
			xMin = Mathf.Floor((xMin - xD) / xExpNum) * xExpNum;
			xMax = Mathf.Ceil((xMax + xD) / xExpNum) * xExpNum;
		}

		if (yMin == yMax) {
			yMax += 2;
			yExpNum = 1;
		} else {
			float yExp = Mathf.CeilToInt(Mathf.Log10(Mathf.Abs(yMax - yMin))) - 1;
			yExpNum = Mathf.Pow(10f, yExp);
			float yD = (yMax - yMin) * edgeGap;
			yMin = Mathf.Floor((yMin - yD) / yExpNum) * yExpNum;
			yMax = Mathf.Ceil((yMax + yD) / yExpNum) * yExpNum;
		}
	}

	void CreateGraphVisuals() {
		CreateGraphPoints();
		CreateGraphAxis();
	}
	void CreateGraphPoints() {
		Vector2 rectSize = container.rect.size;
		int xIndex = 1;
		float xRange = xMax - xMin > 0 ? xMax - xMin : 1f;
		float yRange = yMax - yMin > 0 ? yMax - yMin : 1f;
		foreach ((float, float, string) point in data) {
			GameObject newBar = Instantiate(barPrefab, barContainer);
			Image img = newBar.GetComponent<Image>();
			RectTransform rt = newBar.GetComponent<RectTransform>();
			img.color = barColor;

			rt.anchorMax = Vector2.zero;
			rt.anchorMin = Vector2.zero;

			rt.pivot = new Vector2(0.5f, 0f);

			float xPos = ignoreXValues ? xIndex * rectSize.x / (data.Count + 1) : rectSize.x * (point.Item1 - xMin) / xRange;
			rt.anchoredPosition = new Vector2(xPos, 0f);
			rt.sizeDelta = new Vector2(barSize, rectSize.y * (point.Item2 - yMin) / yRange);




			GameObject newBarX = Instantiate(labelPrefab, labelContainer);
			TextMeshProUGUI txt = newBarX.GetComponent<TextMeshProUGUI>();
			RectTransform rtTxt = newBarX.GetComponent<RectTransform>();
			txt.text = point.Item3 == "-" ? "" : point.Item2.ToString();
			txt.color = valueColor;

			rtTxt.anchorMax = Vector2.zero;
			rtTxt.anchorMin = Vector2.zero;
			txt.enableWordWrapping = true;

			rtTxt.pivot = new Vector2(0.5f, 0.5f);
			Vector2 size = labelSize * Vector2.one;
			size.x = 0;
			rtTxt.sizeDelta = size;
			rtTxt.anchoredPosition = new Vector2(xPos, Mathf.Max(0.5f * rectSize.y * (point.Item2 - yMin) / yRange, labelSize / 2f));




			xIndex++;
		}

	}
	public int gapBetweenLabels;
	public int startGapLabelIndex;

	void CreateGraphAxis() {
		labelPrefab.GetComponent<TextMeshProUGUI>().color = labelColor;
		Vector2 rectSize = container.rect.size;
		float xRange = xMax - xMin > 0 ? xMax - xMin : 1f;
		float yRange = yMax - yMin > 0 ? yMax - yMin : 1f;
		if (ignoreXValues) {
			int xIndex = 1;
			int skipIndex = startGapLabelIndex;
			int gap = Mathf.Max(1, gapBetweenLabels);
			foreach ((float, float, string) point in data) {
				if (skipIndex % gap != 0) { skipIndex++; xIndex++; continue; }
				GameObject newBarX = Instantiate(labelPrefab, labelContainer);
				TextMeshProUGUI txt = newBarX.GetComponent<TextMeshProUGUI>();
				RectTransform rt = newBarX.GetComponent<RectTransform>();
				txt.text = String.IsNullOrEmpty(point.Item3) ? point.Item1.ToString() : point.Item3;

				rt.anchorMax = Vector2.zero;
				rt.anchorMin = Vector2.zero;

				rt.pivot = new Vector2(0.5f, 0f);

				rt.sizeDelta = labelSize * Vector2.one;
				rt.anchoredPosition = new Vector2(xIndex * rectSize.x / (data.Count + 1), -labelSize);
				xIndex++;
				skipIndex++;
			}
		} else {
			float xPoints = xRange / xExpNum;
			for (int i = 0; i <= xPoints; i++) {
				GameObject newBarX = Instantiate(labelPrefab, labelContainer);
				TextMeshProUGUI txt = newBarX.GetComponent<TextMeshProUGUI>();
				RectTransform rt = newBarX.GetComponent<RectTransform>();
				txt.text = (xMin + i * xExpNum).ToString("F0");

				rt.anchorMax = Vector2.zero;
				rt.anchorMin = Vector2.zero;

				rt.pivot = new Vector2(0.5f, 0f);

				rt.anchoredPosition = new Vector2(rectSize.x * i * xExpNum / xRange, -labelSize);
				rt.sizeDelta = labelSize * Vector2.one;
			}
		}
		float yPoints = yRange / yExpNum;
		for (int i = 0; i <= yPoints; i++) {
			GameObject newBarY = Instantiate(labelPrefab, labelContainer);
			TextMeshProUGUI txt = newBarY.GetComponent<TextMeshProUGUI>();
			RectTransform rt = newBarY.GetComponent<RectTransform>();
			txt.text = (yMin + i * yExpNum).ToString("F0");

			rt.anchorMax = Vector2.zero;
			rt.anchorMin = Vector2.zero;

			rt.pivot = new Vector2(0f, 0.5f);

			rt.anchoredPosition = new Vector2(-labelSize - 20f, rectSize.y * i * yExpNum / yRange);
			rt.sizeDelta = labelSize * Vector2.one;
		}



		//do y labels

	}






}
