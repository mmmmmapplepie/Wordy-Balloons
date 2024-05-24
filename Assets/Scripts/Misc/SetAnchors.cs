using UnityEngine;

[ExecuteInEditMode]
public class SetAnchors : MonoBehaviour {
	public bool MatchHeight, MatchWidth;

	//require parent object to be a panel encompassing the entire cam view
	[ContextMenu("SetAnchors")]
	public void Set() {
		RectTransform rt = GetComponent<RectTransform>();
		Vector2 iniAnchorMin = rt.anchorMin;
		Vector2 iniAnchorMax = rt.anchorMax;

		Vector2 initialAnchorPos = rt.anchoredPosition;
		Vector2 center = initialAnchorPos;
		Vector2 anchorCenter = (iniAnchorMax + iniAnchorMin) / 2f;
		center += (anchorCenter * transform.parent.GetComponent<RectTransform>().rect.size);

		Vector2 max = rt.rect.max;
		Vector2 min = rt.rect.min;

		max += center;
		min += center;


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


	}
}
