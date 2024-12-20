using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class SetAnchors : MonoBehaviour {
	public bool MatchHeight = true, MatchWidth = true, Center = false;

	//require parent object to be a panel encompassing the entire cam view
	[ContextMenu("SetAnchors")]
	public void Set() {
		RectTransform rt = GetComponent<RectTransform>();
		Vector2 iniAnchorMin = rt.anchorMin;
		Vector2 iniAnchorMax = rt.anchorMax;

		Vector2 initialAnchorPos = rt.anchoredPosition;
		Vector2 anchorCenter = (iniAnchorMax + iniAnchorMin) / 2f;
		Vector2 parentRectSize = transform.parent.GetComponent<RectTransform>().rect.size;
		Vector2 centerPos = initialAnchorPos + (anchorCenter * parentRectSize);

		Vector2 max = rt.rect.max;
		Vector2 min = rt.rect.min;

		max += centerPos;
		min += centerPos;


		Canvas c = null;
		Transform par = transform.parent;
		while (c == null && par != null) {
			par.TryGetComponent<Canvas>(out c);
			par = par.parent;
		}
		if (c == null || transform.parent == null) return;

		float cwidth = transform.parent.GetComponent<RectTransform>().rect.width;
		float cheight = transform.parent.GetComponent<RectTransform>().rect.height;

		Vector2 iniSizeDelta = rt.sizeDelta;

		if (Center) {
			rt.anchorMax = new Vector2(centerPos.x / parentRectSize.x, centerPos.y / parentRectSize.y);
			rt.anchorMin = new Vector2(centerPos.x / parentRectSize.x, centerPos.y / parentRectSize.y);
			rt.anchoredPosition = Vector2.zero;
			
			return;
		}


		if (MatchHeight) {
			rt.anchorMax = new Vector2(rt.anchorMax.x, max.y / cheight);
			rt.anchorMin = new Vector2(rt.anchorMin.x, min.y / cheight);
			iniSizeDelta.y = 0f;
		}
		if (MatchWidth) {
			rt.anchorMax = new Vector2(max.x / cwidth, rt.anchorMax.y);
			rt.anchorMin = new Vector2(min.x / cwidth, rt.anchorMin.y);
			iniSizeDelta.x = 0f;
		}

		Vector2 anchorPosChange = Vector2.zero;
		anchorPosChange.x = ((rt.anchorMax.x + rt.anchorMin.x) / 2 - (iniAnchorMax.x + iniAnchorMin.x) / 2) * transform.parent.GetComponent<RectTransform>().rect.size.x;
		anchorPosChange.y = ((rt.anchorMax.y + rt.anchorMin.y) / 2 - (iniAnchorMax.y + iniAnchorMin.y) / 2) * transform.parent.GetComponent<RectTransform>().rect.size.y;

		rt.sizeDelta = iniSizeDelta;
		rt.anchoredPosition = initialAnchorPos - anchorPosChange;

		if (!MatchHeight || !MatchWidth) SetPosition();
	}



	public enum AlignPos { Center, RightOrTop, LeftOrBottom };
	[Header("Alignment")]
	[Tooltip("Only applicable when match height and width are not both set")]
	public AlignPos alignment = AlignPos.Center;

	// [ContextMenu("SetPos")]
	public void SetPosition() {
		if (MatchHeight == MatchWidth) return;
		RectTransform rt = GetComponent<RectTransform>();
		Vector2 iniAnchorMin = rt.anchorMin;
		Vector2 iniAnchorMax = rt.anchorMax;

		Vector2 initialAnchorPos = rt.anchoredPosition;
		Vector2 anchorCenter = (iniAnchorMax + iniAnchorMin) / 2f;
		Vector2 centerPos = initialAnchorPos + (anchorCenter * transform.parent.GetComponent<RectTransform>().rect.size);

		Vector2 maxPos = rt.rect.max;
		Vector2 minPos = rt.rect.min;

		maxPos += centerPos;
		minPos += centerPos;

		Canvas c = null;
		Transform par = transform.parent;
		while (c == null && par != null) {
			par.TryGetComponent<Canvas>(out c);
			par = par.parent;
		}
		if (c == null) return;


		float cwidth = c.GetComponent<RectTransform>().rect.width;
		float cheight = c.GetComponent<RectTransform>().rect.height;
		Vector2 iniSizeDelta = rt.sizeDelta;


		if (!MatchHeight) {
			//align to top/bottom
			float targetPos = GetTargetAnchorPos(maxPos.y / cheight, minPos.y / cheight);
			rt.anchorMax = new Vector2(rt.anchorMax.x, targetPos);
			rt.anchorMin = new Vector2(rt.anchorMin.x, targetPos);
		}
		if (!MatchWidth) {
			//align to right/left
			float targetPos = GetTargetAnchorPos(maxPos.x / cwidth, minPos.x / cwidth);
			rt.anchorMax = new Vector2(targetPos, rt.anchorMax.y);
			rt.anchorMin = new Vector2(targetPos, rt.anchorMin.y);
		}

		Vector2 anchorPosChange = Vector2.zero;
		anchorPosChange.x = ((rt.anchorMax.x + rt.anchorMin.x) / 2 - (iniAnchorMax.x + iniAnchorMin.x) / 2) * transform.parent.GetComponent<RectTransform>().rect.size.x;
		anchorPosChange.y = ((rt.anchorMax.y + rt.anchorMin.y) / 2 - (iniAnchorMax.y + iniAnchorMin.y) / 2) * transform.parent.GetComponent<RectTransform>().rect.size.y;

		rt.sizeDelta = iniSizeDelta;
		rt.anchoredPosition = initialAnchorPos - anchorPosChange;
	}

	float GetTargetAnchorPos(float max, float min) {
		switch (alignment) {
			case AlignPos.Center:
				return (max + min) / 2f;
			case AlignPos.RightOrTop:
				return max;
			case AlignPos.LeftOrBottom:
				return min;
		}
		return 0f;
	}

}
