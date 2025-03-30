using System.Collections.Generic;
using TMPro;
using UnityEngine;
[ExecuteAlways]
public class TextInCircle : MonoBehaviour {
	public GameObject textHolder;
	TMP_Text text;
	public float radius;
	public bool useRadiusAsRatioOfTextWidth = false;
	public float gapRatio = 1f;
	void Update() {
		AnimateText();
	}
	void AnimateText() {
		if (text == null && !textHolder.TryGetComponent<TMP_Text>(out text)) {
			return;
		}

		float r = radius;
		if (r == 0) return;


		text.ForceMeshUpdate();
		TMP_TextInfo txtInfo = text.textInfo;
		int charCount = txtInfo.characterCount;
		Vector3 center = text.bounds.center;
		float width = text.bounds.max.x - text.bounds.min.x;
		if (useRadiusAsRatioOfTextWidth) {
			r = radius * width;
		}
		if (r == 0) return;

		Vector3[] vertices;
		Matrix4x4 transformation;

		float textSize = text.fontSize;
		float targetGap = textSize * gapRatio / 20f;

		Vector3 circleCenter = center - Vector3.up * r;
		int characterCount = txtInfo.characterCount;
		for (int i = 0; i < characterCount; i++) {


			if (!txtInfo.characterInfo[i].isVisible) continue;

			int vertexIndex = txtInfo.characterInfo[i].vertexIndex;
			int materialIndex = txtInfo.characterInfo[i].materialReferenceIndex;
			vertices = txtInfo.meshInfo[materialIndex].vertices;

			Vector3 charCenter = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2f;
			charCenter.y = txtInfo.characterInfo[i].baseLine;

			vertices[vertexIndex + 0] -= charCenter;
			vertices[vertexIndex + 1] -= charCenter;
			vertices[vertexIndex + 2] -= charCenter;
			vertices[vertexIndex + 3] -= charCenter;
			float baseline = txtInfo.characterInfo[i].baseLine;
			float circ = (baseline - circleCenter.y) * Mathf.PI * 2f;
			float angle = 360f * -targetGap * (charCenter.x - circleCenter.x) / circ;
			Quaternion rotation = Quaternion.Euler(0, 0, angle);
			Vector3 finalDir = rotation * Vector3.up * (baseline - circleCenter.y);
			transformation = Matrix4x4.TRS(default, rotation, Vector3.one);
			vertices[vertexIndex + 0] = transformation.MultiplyPoint3x4(vertices[vertexIndex + 0]);
			vertices[vertexIndex + 1] = transformation.MultiplyPoint3x4(vertices[vertexIndex + 1]);
			vertices[vertexIndex + 2] = transformation.MultiplyPoint3x4(vertices[vertexIndex + 2]);
			vertices[vertexIndex + 3] = transformation.MultiplyPoint3x4(vertices[vertexIndex + 3]);
			vertices[vertexIndex + 0] += finalDir - Vector3.up * r;
			vertices[vertexIndex + 1] += finalDir - Vector3.up * r;
			vertices[vertexIndex + 2] += finalDir - Vector3.up * r;
			vertices[vertexIndex + 3] += finalDir - Vector3.up * r;
		}
		text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
	}


}
