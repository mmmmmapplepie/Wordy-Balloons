using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIImageWobble : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler {
	public Material mat;
	Material newMat;
	Image img;
	void Awake() {
		img = GetComponent<Image>();
		newMat = new Material(mat);
		img.material = newMat;
		initialColor = img.color;
		Update();
		latestSizeResetTime = Time.unscaledTime;
	}
	public float Rate, Magnitude, maxMultiplier;
	public int bumps;
	bool resetCalled = false;
	void Update() {
		if (!resetCalled) {
			resetCalled = true;
			ResetMagnitude();
		}
		newMat.SetFloat("_Bumps", bumps);
		newMat.SetFloat("_MaxDistortion", Mathf.Sin((Rate / (2f * Mathf.PI)) * Time.unscaledTime) * Magnitude);
		newMat.SetColor("_Color", img.color);
		newMat.SetFloat("_Rotation", (Rate * Time.unscaledTime % 360f));
		if (increaseSize && resetCalled) IncreaseMagnitude();
	}

	void OnEnable() {
		ResetMagnitude();
		img.color = initialColor;
		Update();
	}
	public bool increaseSize = false;
	float increaseTime = 5f, latestSizeResetTime, delay = 5f, runtimeMag = 1;
	void IncreaseMagnitude() {
		float elapsed = Time.unscaledTime - latestSizeResetTime;
		float ratio = elapsed > delay ? (elapsed - delay) / increaseTime : 0;
		runtimeMag = Mathf.Lerp(1, maxMultiplier, Mathf.Min(1, ratio));
		transform.localScale = Vector3.one * runtimeMag;
	}
	public void ResetMagnitude() {
		runtimeMag = 1;
		transform.localScale = Vector3.one;
		latestSizeResetTime = Time.unscaledTime;
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
