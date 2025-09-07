using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TMPHyperlink : MonoBehaviour, IPointerClickHandler {
	public string URL = "https://mmmmapplepie.itch.io/wordy-balloons";
	public string linkID = "DownloadPage";
	private TextMeshProUGUI tmpText;

	void Awake() {
		tmpText = GetComponent<TextMeshProUGUI>();
	}

	public void OnPointerClick(PointerEventData eventData) {
		int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, eventData.pressEventCamera);
		if (linkIndex != -1) {
			TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
			string clickID = linkInfo.GetLinkID();
			if (clickID != linkID) return;
			OpenURL();
		}
	}

	public void OpenURL() {
		Application.OpenURL(URL);
	}
}
