using System;
using TMPro;
using UnityEngine;

public class TextAnimator : MonoBehaviour {

	public TMP_Text text;
	InputManager inputManager;
	void Start() {
		inputManager = InputManager.Instance;
		// InputManager.NewTextSet += SetNewText;
		InputManager.InputProcessFinished += AnimateText;
		// Invoke(nameof(RemoveAnimator), 5f);
	}
	void OnDestroy() {
		// InputManager.NewTextSet -= SetNewText;
		InputManager.InputProcessFinished -= AnimateText;
	}
	// void SetNewText(string newTxt) {
	// }

	void RemoveAnimator() {
		if (TryGetComponent<ITextAnimator>(out ITextAnimator animScript)) {
			MonoBehaviour monoScript = animScript as MonoBehaviour;
			Destroy(monoScript);
		}
	}

	// public static event Func<Action<InputManager, TMP_Text>> FindTextAnimationFunction;
	void AnimateText() {
		text.text = inputManager.displayString;
		if (inputManager == null) return;
		Action<InputManager, TMP_Text> animFtn = NoAnimation;
		if (TryGetComponent<ITextAnimator>(out ITextAnimator animScript)) {
			animFtn = animScript.AnimationFunction;
		}
		animFtn(inputManager, text);
	}
	void NoAnimation(InputManager inputManager, TMP_Text text) {
		text.ForceMeshUpdate();
	}











}

public class FontAnimation<T> {
	public T progress;

	//animationProgress, vertexList, vertexColorList, vertexStartIndex
	public System.Action<T, Vector3[], Color32[], int> animation;
}
