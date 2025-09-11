using System;
using TMPro;
using UnityEngine;

public class TextAnimator : MonoBehaviour {

  public TMP_Text text;
  InputManager inputManager;
  void Start() {
    inputManager = InputManager.Instance;
    // InputManager.InputProcessFinished += AnimateText;
    animFtn = NoAnimation;
    if (TryGetComponent<ITextAnimator>(out ITextAnimator animScript)) {
      animFtn = animScript.AnimationFunction;
    }
  }
  void OnDestroy() {
    // InputManager.InputProcessFinished -= AnimateText;
  }

  void LateUpdate() {
    AnimateText();
  }

  Action<InputManager, TMP_Text> animFtn;
  void AnimateText() {
    if (inputManager == null || animFtn == null) return;
    text.text = inputManager.displayString;
    animFtn(inputManager, text);
  }
  void NoAnimation(InputManager inputManager, TMP_Text text) {
    // text.ForceMeshUpdate();
  }











}

public class FontAnimation<T> {
  public T progress;

  //animationProgress, vertexList, vertexColorList, vertexStartIndex
  public System.Action<T, Vector3[], Color32[], int> animation;
}
