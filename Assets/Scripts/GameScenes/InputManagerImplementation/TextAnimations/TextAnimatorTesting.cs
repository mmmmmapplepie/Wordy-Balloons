using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextAnimatorTesting : MonoBehaviour, ITextAnimator {
	public TMP_Text displayText;
	public InputManager inputManager;
	// public TMP_FontAsset font1, font2;

	void Start() {
		InputManager.NewTextSet += NewTextSet;
	}
	void OnDestroy() {
		InputManager.NewTextSet += NewTextSet;
	}

	string targetText;
	void NewTextSet(string s) {
		targetText = s;
	}

	void Update() {
		Animate();
	}

	List<FontAnimation<float>> animatingText = new List<FontAnimation<float>>();
	int prevTypedLength = 0;
	void Animate() {
		ManageAnimatingText();

		displayText.ForceMeshUpdate();

		TMP_TextInfo txtInfo = displayText.textInfo;
		int charCount = txtInfo.characterCount;

		// print(tmp.text.Length);
		// print(charCount);

		if (charCount == 0) return;
		for (int i = 0; i < charCount; i++) {
			TMP_CharacterInfo charInfo = txtInfo.characterInfo[i];
			if (!charInfo.isVisible || i >= animatingText.Count) continue;
			int charMatIndex = charInfo.materialReferenceIndex;
			int vertStartIndex = charInfo.vertexIndex;
			Color32[] vertCols = txtInfo.meshInfo[charMatIndex].colors32;
			Vector3[] verts = txtInfo.meshInfo[charMatIndex].vertices;

			FontAnimation<float> anim = animatingText[i];
			anim.animation(anim, verts, vertCols, vertStartIndex);
		}
		displayText.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
	}
	void ManageAnimatingText() {
		string typed = inputManager.typedString;
		string displayString = targetText;

		int targetTextLength = targetText.Length;

		displayText.text = displayString;
		if (typed.Length > targetTextLength) {
			typed = typed.Substring(0, targetTextLength);
		}

		for (int i = animatingText.Count; i < typed.Length; i++) {
			FontAnimation<float> newAnim = new FontAnimation<float>();
			newAnim.progress = 0;
			newAnim.animation = typed[i] == displayText.text[i] ? CorrectCharacter : WrongCharacter;
			animatingText.Add(newAnim);
		}

		while (animatingText.Count > typed.Length) {
			animatingText.RemoveAt(typed.Length);
		}

		prevTypedLength = typed.Length;
	}

	void CorrectCharacter(FontAnimation<float> prog, Vector3[] verts, Color32[] cols, int charIndex) {
		float period = 1f;

		float p = prog.progress / period;
		if (prog.progress < period) {
			Vector3 center = (verts[charIndex] + verts[charIndex + 2]) / 2f;
			float factor = 2f;
			for (int i = charIndex; i < charIndex + 4; i++) {
				Vector3 diff = verts[i] - center;
				verts[i] = center + diff * Mathf.Lerp(factor, 1f, p);
			}
		}
		for (int i = charIndex; i < charIndex + 4; i++) {
			cols[i] = (Color32)Color.cyan;
		}

		prog.progress = prog.progress < period ? prog.progress + Time.deltaTime : period;
	}
	void WrongCharacter(FontAnimation<float> prog, Vector3[] verts, Color32[] cols, int charIndex) {
		float period = 0.4f;
		Vector3 center = (verts[charIndex] + verts[charIndex + 2]) / 2f;
		float jiggleFactor = 0.3f;
		Vector3 jiggleMagnitude = jiggleFactor * (verts[charIndex + 2] - center);
		float jiggleX = jiggleMagnitude.x;
		float jiggleY = jiggleMagnitude.y;

		float p = prog.progress / period;
		if (prog.progress < period) {
			float sizeFactor = 1.4f;
			for (int i = charIndex; i < charIndex + 4; i++) {
				Vector3 diff = verts[i] - center;
				verts[i] = center + diff * Mathf.Lerp(sizeFactor, 1f, p);
			}
		}
		for (int i = charIndex; i < charIndex + 4; i++) {
			verts[i] += new Vector3(Random.Range(-jiggleX, jiggleX), Random.Range(-jiggleY, jiggleY), 0f);
		}
		for (int i = charIndex; i < charIndex + 4; i++) {
			cols[i] = (Color32)Color.red;
		}
		prog.progress = prog.progress < period ? prog.progress + Time.deltaTime : period;
	}







	public class FontAnimation<T> {
		public T progress;
		public int Index;
		public System.Action<FontAnimation<T>, Vector3[], Color32[], int> animation;
	}




	#region otherstuff
	void MyUpdate() {
		displayText.ForceMeshUpdate();
		TMP_TextInfo txtInfo = displayText.textInfo;
		Vector3[] verts = displayText.mesh.vertices;
		Debug.LogWarning("loop");
		for (int i = 0; i < txtInfo.characterCount; i++) {

			TMP_CharacterInfo charInfo = txtInfo.characterInfo[i];
			if (!charInfo.isVisible) continue;
			Color32[] cols = txtInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
			print(cols.Length);
			for (int j = 0; j < cols.Length; j++) {
				cols[j] = Color.red;
			}
			Vector3 center = Vector3.zero;
			int index = charInfo.vertexIndex;
			for (int j = 0; j < 4; j++) {
				center += verts[index + j];
			}
			center /= 4f;
			for (int j = 0; j < 4; j++) {
				verts[index + j] = (verts[index + j] - center) * 1.2f + center;
			}
		}
	}




	void TheirUpdate() {
		displayText.ForceMeshUpdate();
		TMP_TextInfo txtInfo = displayText.textInfo;
		for (int i = 0; i < txtInfo.characterCount; i++) {
			TMP_CharacterInfo charInfo = txtInfo.characterInfo[i];
			if (!charInfo.isVisible) continue;
			Vector3[] verts = txtInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
			for (int j = 0; j < 4; ++j) {
				Vector3 orig = verts[charInfo.vertexIndex + j];
				verts[charInfo.vertexIndex + j] = orig + new Vector3(0, Mathf.Sin(Time.time * 2f + orig.x * 0.01f) * 10f, 0);
			}
		}

		for (int i = 0; i < txtInfo.meshInfo.Length; ++i) {
			TMP_MeshInfo meshInfo = txtInfo.meshInfo[i];
			meshInfo.mesh.vertices = meshInfo.vertices;
			Mesh mesh = displayText.mesh;
			displayText.canvasRenderer.SetMesh(mesh);
			// txtComp.UpdateGeometry(meshInfo.mesh, i);

		}
	}

	public void AnimationFunction(InputManager input, TMP_Text text) {
		throw new System.NotImplementedException();
	}
	#endregion
}
