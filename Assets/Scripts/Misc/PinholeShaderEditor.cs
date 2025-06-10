using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PinholeShaderEditor : MonoBehaviour {
	public enum Shape { Ellipse, Rectangle }


	public Material pinholeShaderMat;
	public Image targetImg;
	public Color maskColor, backgroundColor;

	[Tooltip("Max shapes is 8. Anything more will be automatically deleted")]
	public List<PinholeShape> maskShapes = new List<PinholeShape>();
	void Start() {
		SetMat();
	}
	public void SetMat() {
		SetMatValues();
	}
	const int MaxShapesForShader = 8;
	void SetMatValues() {
		if (maskShapes.Count > MaxShapesForShader) {
			maskShapes.RemoveRange(MaxShapesForShader, maskShapes.Count - MaxShapesForShader);
		}
		if (targetImg == null || pinholeShaderMat == null) return;


		Material newMat = new Material(pinholeShaderMat);
		targetImg.material = newMat;

		newMat.SetColor("_MaskColor", maskColor);
		newMat.SetColor("_BackgroundColor", backgroundColor);
		Vector2 size = targetImg.rectTransform.rect.size;
		newMat.SetVector("_CanvasSize", new Vector4(size.x, size.y, 0, 0));

		newMat.SetInt("_ShapeCount", maskShapes.Count);



		List<Vector4> shapeData = new List<Vector4>();
		// float4 data1 = _Shapes[shapeIndex * 2];     // x, y, width, height -- all in UV coordinates
		// float4 data2 = _Shapes[shapeIndex * 2 + 1]; // rotation, fade(0 - no fade, 1 - maximum fade with fade distance = minimum among width/height), shapeType (0 is ellipse, 1 is rectangle), weightMask (clamped to 0, 1)
		foreach (PinholeShape shape in maskShapes) {
			Vector4 data1 = new Vector4(shape.center.x, shape.center.y, shape.dimension.x, shape.dimension.y);
			Vector4 data2 = new Vector4(shape.rotation, shape.fade, (int)shape.shape, shape.maskWeight);
			shapeData.Add(data1);
			shapeData.Add(data2);
		}

		if (maskShapes.Count > 0) newMat.SetVectorArray("_Shapes", shapeData);

	}

}


[Serializable]
public struct PinholeShape {
	[Tooltip("center in canvas pixel units wrt center of the rect")]
	public Vector2 center;
	[Tooltip("size in canvas pixel units")]
	public Vector2 dimension;
	[Range(0, 360)]
	public float rotation;
	[Tooltip("fade distance in canvas pixel units")]
	public float fade;
	public PinholeShaderEditor.Shape shape;
	[Range(0, 1)]
	public float maskWeight;

}




[CustomEditor(typeof(PinholeShaderEditor))]
public class PinholeShderEditorInspector : Editor {
	public override void OnInspectorGUI() {
		// Draw default fields
		DrawDefaultInspector();

		PinholeShaderEditor controller = (PinholeShaderEditor)target;

		if (GUILayout.Button("Apply Changes To Mat")) {
			controller.SetMat();
		}
	}
}