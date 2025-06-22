using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchChildLetterPosToTextPos : MonoBehaviour {
	TextMeshProUGUI txt;
	public bool ignoreInvisible = true;
	void OnEnable() {
		TryGetComponent<TextMeshProUGUI>(out txt);
		if (txt) {
			SetChildPos();
		}
	}


	void SetChildPos() {
		TMP_Text text = txt;

		text.ForceMeshUpdate();

		TMP_TextInfo txtInfo = text.textInfo;

		int charCount = txtInfo.characterCount;
		int inx = 0;
		if (charCount == 0) return;
		for (int i = 0; i < Mathf.Max(charCount, transform.childCount); i++) {
			if (i >= charCount) break;
			TMP_CharacterInfo charInfo = txtInfo.characterInfo[i];
			if (ignoreInvisible && !charInfo.isVisible) continue;
			int charMatIndex = charInfo.materialReferenceIndex;
			int vertStartIndex = charInfo.vertexIndex;
			Vector3[] vertCols = txtInfo.meshInfo[charMatIndex].vertices;
			Vector3 center = (vertCols[vertStartIndex] + vertCols[vertStartIndex + 2]) / 2f;
			center = txt.transform.TransformPoint(center);
			if (transform.childCount > inx) {
				transform.GetChild(inx).position = center;
			} else {
				break;
			}
			inx++;
		}
	}
}
