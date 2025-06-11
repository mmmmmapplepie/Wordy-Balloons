using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIImageWobble : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler {
	public Material mat;
	Material newMat;
	Image img;
	void Start() {
		img = GetComponent<Image>();
		newMat = new Material(mat);
		img.material = newMat;
		initialColor = img.color;
		Update();
	}
	public float Rate, Magnitude;
	public int bumps;
	void Update() {
		newMat.SetFloat("_Bumps", bumps);
		newMat.SetFloat("_MaxDistortion", Magnitude);
		newMat.SetColor("_Color", img.color);
		newMat.SetFloat("_Rotation", (Rate * Time.time % 360f));

	}

	public Color highlightColor = new Color(1f, 1f, 150f / 255f, 1f);
	public Color clickColor = new Color(1f, 150f / 255f, 100f / 255f, 1f);
	Color initialColor;
	Color enteredColor;

	public bool IncreaseSizeOnEnter = false;
	public float sizeFactor = 1.1f;
	public void OnPointerEnter(PointerEventData eventData) {
		if (IncreaseSizeOnEnter) {
			transform.localScale *= sizeFactor;
		}
		enteredColor = highlightColor;
		if (!Interactable() || clicked) return;
		img.color = enteredColor;
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (IncreaseSizeOnEnter) {
			transform.localScale = Vector3.one;
		}
		enteredColor = initialColor;
		if (!Interactable() || clicked) return;
		img.color = enteredColor;
	}
	bool clicked = false;
	public void OnPointerDown(PointerEventData eventData) {
		clicked = true;
		if (!Interactable()) return;
		img.color = clickColor;
	}

	public void OnPointerUp(PointerEventData eventData) {
		clicked = false;
		if (!Interactable()) return;
		img.color = enteredColor;
	}

	bool Interactable() {
		if (!TryGetComponent<Button>(out Button b)) return false;
		if (!b.interactable) return false;
		return true;
	}
}
