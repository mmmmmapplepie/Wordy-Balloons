using System.Collections.Generic;
using UnityEngine;

public class BackgroundAnimation : MonoBehaviour {
	public List<Animator> animators = new List<Animator>();
	public Sprite backgroundSprite;
	public float scrollSpeed = 1f;
	const string slide = "backgroundSlide";
	public float heightDelta = 0f;
	void Start() {
		float height = Camera.main.orthographicSize * 2f;
		float screenAspect = (float)Screen.width / Screen.height;
		float spriteAspect = backgroundSprite.textureRect.width / backgroundSprite.textureRect.height;
		float width = screenAspect * height;

		if (screenAspect > spriteAspect) {
			height = width / spriteAspect;
		} else {
			width = height * spriteAspect;
		}
		Vector3 scale = new Vector3(width, height, 1) * (height + heightDelta) / height;
		transform.localScale = scale;

		if (animators.Count == 0) return;
		float gap = 1f / animators.Count;
		for (int i = 0; i < animators.Count; i++) {
			Animator a = animators[i];
			a.speed = scrollSpeed;
			a.Play(slide, 0, i * gap);
		}
	}
}
