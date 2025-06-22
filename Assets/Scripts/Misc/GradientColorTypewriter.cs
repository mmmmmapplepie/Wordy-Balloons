using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GradientColorTypewriter : MonoBehaviour {
	//value smaller than 1 makes it "pause" half way as technically TMP only sets 4 points of color. so we cant have a sharp change in color.
	[Range(1, 50)]
	public float gradientRelativeWidth = 1f;
	public bool useUnscaledTime = false;
	public float animTime = 0.5f;
	public bool animDirectionLeftToRight = true;
	public bool ignoreInvisible = true;
	public Color32 iniCol, finCol;

	int curr = 0;
	[ContextMenu("anim next")]
	public void AnimateNext() {
		AnimateLetterAtIndex(curr);
		curr++;
	}



	TextMeshProUGUI textH;
	string s;
	void Start() {
		TryGetComponent<TextMeshProUGUI>(out textH);
		SetString();
	}
	List<VertData> vertData = new List<VertData>();
	List<int> invisiblePosList = new List<int>();
	public void SetString() {
		if (textH == null) return;
		if (s != textH.text) {
			NewTextSet(textH.text.Length);
		}
		s = textH.text;
		SetInvisiblePosList();
		MatchTextProToCurrData();
	}
	void SetInvisiblePosList() {
		invisiblePosList.Clear();
		TMP_Text text = textH;

		text.ForceMeshUpdate();

		TMP_TextInfo txtInfo = text.textInfo;

		int charCount = txtInfo.characterCount;

		if (charCount == 0) return;
		int visibles = 0;
		for (int i = 0; i < charCount; i++) {
			TMP_CharacterInfo charInfo = txtInfo.characterInfo[i];
			visibles += charInfo.isVisible ? 1 : 0;
			invisiblePosList.Add(visibles);
		}
	}
	int GetNextNonInvisible(int inx) {
		if (inx >= s.Length) return s.Length;
		if (invisiblePosList[inx] > inx) return inx;
		for (int i = inx + 1; i < s.Length; i++) {
			if (invisiblePosList[i] > inx) return i;
		}
		return s.Length;
	}

	void NewTextSet(int count) {
		StopAllCoroutines();
		vertData.Clear();
		VertData initialColorData = new VertData(Enumerable.Repeat<Color32>(iniCol, 4).ToArray());
		runningAnimations.Clear();
		runningAnimations = Enumerable.Repeat<(Coroutine, int)>(default, count).ToList();
		vertData = Enumerable.Repeat<VertData>(initialColorData, count).ToList();
	}


	public void AnimateLetterAtIndex(int inx) {
		inx = ignoreInvisible ? GetNextNonInvisible(inx) : inx;
		if (inx >= s.Length) return;
		if (runningAnimations[inx].anim != null) {
			StopCoroutine(runningAnimations[inx].anim);
		}
		(Coroutine, int) newAnim = (StartCoroutine(AlterLetterData(inx)), inx);
		runningAnimations[inx] = newAnim;
	}
	List<(Coroutine anim, int inx)> runningAnimations = new List<(Coroutine, int)>();
	IEnumerator AlterLetterData(int inx) {

		if (animTime > 0) {
			float t = 0;
			while (t < 1) {
				t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / animTime;
				SetColorForGivenIndex(inx, t);
				yield return null;
			}
		}
		SetColorForGivenIndex(inx, 1);
	}
	const float normWidth = 1;
	void SetColorForGivenIndex(int inx, float ratio) {
		if (vertData.Count <= inx) return;
		float graidentWidth = normWidth * gradientRelativeWidth;
		float leftedge = (animDirectionLeftToRight ? (-0.5f - gradientRelativeWidth) * normWidth : 0.5f * normWidth) +
		(normWidth * ratio * (1f + gradientRelativeWidth) * (animDirectionLeftToRight ? 1f : -1f));
		float rightedge = leftedge + graidentWidth;

		Color32 rightColor = animDirectionLeftToRight ? iniCol : finCol;
		Color32 leftColor = animDirectionLeftToRight ? finCol : iniCol;

		Color32 leftEdgeCol, rightEdgeCol;
		if (-normWidth / 2f <= leftedge) leftEdgeCol = leftColor;
		else if (-normWidth / 2f >= rightedge) leftEdgeCol = rightColor;
		else {
			leftEdgeCol = Color32.Lerp(leftColor, rightColor, (-normWidth / 2f - leftedge) / graidentWidth);
		}

		if (normWidth / 2f <= leftedge) rightEdgeCol = leftColor;
		else if (normWidth / 2f >= rightedge) rightEdgeCol = rightColor;
		else {
			rightEdgeCol = Color32.Lerp(leftColor, rightColor, (normWidth / 2f - leftedge) / graidentWidth);
		}
		Color32[] cols = new Color32[4];
		cols[0] = cols[1] = leftEdgeCol;
		cols[2] = cols[3] = rightEdgeCol;
		vertData[inx] = new VertData(cols);
	}



	void LateUpdate() {
		MatchTextProToCurrData();
	}
	void MatchTextProToCurrData() {
		TMP_Text text = textH;

		text.ForceMeshUpdate();

		TMP_TextInfo txtInfo = text.textInfo;

		int charCount = txtInfo.characterCount;

		if (charCount == 0) return;
		for (int i = 0; i < charCount; i++) {
			TMP_CharacterInfo charInfo = txtInfo.characterInfo[i];

			if (!charInfo.isVisible) continue;
			int charMatIndex = charInfo.materialReferenceIndex;
			int vertStartIndex = charInfo.vertexIndex;
			Color32[] vertCols = txtInfo.meshInfo[charMatIndex].colors32;
			for (int j = 0; j < 4; j++) {
				vertCols[vertStartIndex + j] = vertData[i].vertColor[j];
			}
		}
		text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
	}







}

public struct VertData {
	public Color32[] vertColor;
	public VertData(Color32[] vertColor) {
		this.vertColor = vertColor;
	}
}