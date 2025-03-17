using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonColorOnEnter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
	Image img;
	public float targetAlpha;
	public float initialAlpha;
	void Awake() {
		img = GetComponent<Image>();
		SetColorAlpha(initialAlpha);
	}

	public void OnPointerEnter(PointerEventData eventData) {
		SetColorAlpha(targetAlpha);
	}

	public void OnPointerExit(PointerEventData eventData) {
		SetColorAlpha(initialAlpha);
	}
	void SetColorAlpha(float a) {
		Color c = img.color;
		c.a = a;
		img.color = c;
	}
}
