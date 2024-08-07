using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BasicTextAnimation : MonoBehaviour, ITextAnimator {
	public Color correctColor, wrongColor;
	public float correctScale, wrongScale, wrongJiggleScale;
	public float correctPeriod, wrongPeriod;
	public TMP_FontAsset correctFont, wrongFont;
	List<FontAnimation<float>> animationProgressList = new List<FontAnimation<float>>();

	InputManager input;
	TMP_Text text;

	public void AnimationFunction(InputManager inputManager, TMP_Text tmpText) {
		input = inputManager;
		text = tmpText;

		UpdateFontAndText();
		ManageAnimatingText();

		text.ForceMeshUpdate();

		TMP_TextInfo txtInfo = text.textInfo;

		int charCount = txtInfo.characterCount;


		if (charCount == 0) return;
		for (int i = 0; i < charCount; i++) {
			TMP_CharacterInfo charInfo = txtInfo.characterInfo[i];
			if (!charInfo.isVisible || i >= animationProgressList.Count) continue;
			int charMatIndex = charInfo.materialReferenceIndex;
			int vertStartIndex = charInfo.vertexIndex;
			Color32[] vertCols = txtInfo.meshInfo[charMatIndex].colors32;
			Vector3[] verts = txtInfo.meshInfo[charMatIndex].vertices;
			FontAnimation<float> anim = animationProgressList[i];
			anim.animation(anim.progress, verts, vertCols, vertStartIndex);
			anim.progress += Time.deltaTime;
		}
		text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
		text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
	}

	void ManageAnimatingText() {
		string typed = input.typedString;
		string target = input.targetString;
		int targetTextLength = target.Length;

		// TMP_TextInfo txtInfo = text.textInfo;

		// int charCount = txtInfo.characterCount;

		// if (charCount == 0) return;
		// int index = 0;
		// for (int i = 0; i < charCount; i++) {
		// 	TMP_CharacterInfo charInfo = txtInfo.characterInfo[i];
		// 	if (!charInfo.isVisible || i >= animationProgressList.Count) continue;
		// 	int charMatIndex = charInfo.materialReferenceIndex;
		// 	int vertStartIndex = charInfo.vertexIndex;
		// 	Color32[] vertCols = txtInfo.meshInfo[charMatIndex].colors32;
		// 	Vector3[] verts = txtInfo.meshInfo[charMatIndex].vertices;

		// 	FontAnimation<float> anim = animationProgressList[index];
		// 	anim.animation(anim.progress, verts, vertCols, vertStartIndex);
		// 	index++;
		// 	anim.progress += Time.deltaTime;
		// }

		for (int i = animationProgressList.Count; i < typed.Length; i++) {
			FontAnimation<float> newAnim = new FontAnimation<float>();
			newAnim.progress = 0;
			newAnim.animation = typed[i] == target[i] ? CorrectCharacter : WrongCharacter;
			animationProgressList.Add(newAnim);
		}

		while (animationProgressList.Count > typed.Length) {
			animationProgressList.RemoveAt(typed.Length);
		}
	}

	void UpdateFontAndText() {
		string typed = input.typedString;
		string target = input.targetString;
		string output = "";
		text.text = target;
		// return;

		bool? correct = null;
		if (System.String.IsNullOrEmpty(typed)) {
			text.text = target;
			return;
		}
		for (int i = 0; i < typed.Length; i++) {
			if (typed[i] == target[i]) {
				if (correct == true) {
					output += target[i];
				} else {
					output += "<font=\"" + correctFont.name + "\">" + target[i];
					correct = true;
				}
			} else {
				if (correct == false) {
					output += target[i];
				} else {
					output += "<font=\"" + wrongFont.name + "\">" + target[i];
					correct = false;
				}
			}
		}
		output += "<font=\"default\">" + target.Substring(typed.Length);
		text.text = output;
	}

	void CorrectCharacter(float prog, Vector3[] verts, Color32[] cols, int charIndex) {
		float p = prog / correctPeriod;
		if (prog < correctPeriod) {
			Vector3 center = (verts[charIndex] + verts[charIndex + 2]) / 2f;
			for (int i = charIndex; i < charIndex + 4; i++) {
				Vector3 diff = verts[i] - center;
				verts[i] = center + diff * Mathf.Lerp(correctScale, 1f, p);
			}
		}
		for (int i = charIndex; i < charIndex + 4; i++) {
			cols[i] = (Color32)correctColor;
		}

		prog = prog < correctPeriod ? prog + Time.deltaTime : correctPeriod;
	}
	void WrongCharacter(float prog, Vector3[] verts, Color32[] cols, int charIndex) {
		Vector3 center = (verts[charIndex] + verts[charIndex + 2]) / 2f;
		Vector3 jiggleMagnitude = wrongJiggleScale * (verts[charIndex + 2] - center);
		float jiggleX = jiggleMagnitude.x;
		float jiggleY = jiggleMagnitude.y;

		float p = prog / wrongPeriod;
		if (prog < wrongPeriod) {
			for (int i = charIndex; i < charIndex + 4; i++) {
				Vector3 diff = verts[i] - center;
				verts[i] = center + diff * Mathf.Lerp(wrongScale, 1f, p);
			}
		}
		for (int i = charIndex; i < charIndex + 4; i++) {
			verts[i] += new Vector3(Random.Range(-jiggleX, jiggleX), Random.Range(-jiggleY, jiggleY), 0f);
		}
		for (int i = charIndex; i < charIndex + 4; i++) {
			cols[i] = (Color32)wrongColor;
		}
		prog = prog < wrongPeriod ? prog + Time.deltaTime : wrongPeriod;
	}


}
