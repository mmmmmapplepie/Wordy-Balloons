using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FocusKeyboard : MonoBehaviour {
	public TMP_InputField inputField;
	void Update() {
		if (Input.anyKeyDown) {
			inputField.ActivateInputField();
		}
	}
}
