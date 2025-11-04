using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SnapEdgesToTargets : MonoBehaviour {
	public Snap rightEdge;
	public Snap leftEdge;
	public Snap topEdge;
	public Snap bottomEdge;
	RectTransform t;
	void Awake() {
		t = GetComponent<RectTransform>();
	}

	void OnValidate() {
		rightEdge.edgeToSet = Snap.Edge.Right;
		leftEdge.edgeToSet = Snap.Edge.Left;
		topEdge.edgeToSet = Snap.Edge.Top;
		bottomEdge.edgeToSet = Snap.Edge.Bottom;
	}
	void LateUpdate() {
		rightEdge.SetEdgeValues(t);
		leftEdge.SetEdgeValues(t);
		topEdge.SetEdgeValues(t);
		bottomEdge.SetEdgeValues(t);
	}


	readonly Vector3[] corners = new Vector3[4];
	void SetScreenCorners(RectTransform rt, Camera cam) {
		rt.GetWorldCorners(corners);
		for (int i = 0; i < 4; i++) {
			corners[i] = cam.WorldToScreenPoint(corners[i]);
		}
	}


}







[System.Serializable]
public class Snap {
	public enum Edge { Top, Bottom, Right, Left };

	public RectTransform target;
	[HideInInspector] public Edge edgeToSet;
	public Edge targetEdge;
	[Tooltip("Positive value will produce up/right displacement")]
	public float delta = 0f;
	bool done = false;

	public void SetEdgeValues(RectTransform rectToSet) {
		if (target == null || rectToSet == null || Camera.main == null || rectToSet.parent == null) return;

		if (done) return;

		Camera cam = Camera.main;

		Vector3 targetPos = GetMatchingCornerScreenPos(target, targetEdge, cam);
		targetPos += delta * GetAxisValue(targetEdge);

		Vector2 normDelta = GetNormSizeDeltaFromScreenSpace(rectToSet, targetPos, edgeToSet, cam);

		Vector3 childLocal = rectToSet.localScale;
		Vector2 realDelta = normDelta * rectToSet.rect.size;
		if ((edgeToSet == Edge.Top || edgeToSet == Edge.Bottom) && rectToSet.rect.size.y == 0) {
			realDelta = normDelta;
		}
		if ((edgeToSet == Edge.Right || edgeToSet == Edge.Left) && rectToSet.rect.size.x == 0) {
			realDelta = normDelta;
		}

		rectToSet.sizeDelta += realDelta;

		Vector2 anchorShift = Vector2.zero;
		switch (edgeToSet) {
			case Edge.Left:
				anchorShift.x = realDelta.x * (rectToSet.pivot.x - 1f) * childLocal.x;
				break;
			case Edge.Right:
				anchorShift.x = realDelta.x * rectToSet.pivot.x * childLocal.x;
				break;
			case Edge.Bottom:
				anchorShift.y = realDelta.y * (rectToSet.pivot.y - 1f) * childLocal.y;
				break;
			case Edge.Top:
				anchorShift.y = realDelta.y * rectToSet.pivot.y * childLocal.y;
				break;
		}
		rectToSet.anchoredPosition += anchorShift;
	}
	public static Vector2 ScaleDivide(Vector2 numerator, Vector2 denominator) {
		return new Vector2(
				denominator.x == 0f ? 0f : numerator.x / denominator.x,
				denominator.y == 0f ? 0f : numerator.y / denominator.y
		);
	}
	readonly Vector3[] corners = new Vector3[4];
	void SetScreenCorners(RectTransform rt, Camera cam) {
		rt.GetWorldCorners(corners);
		for (int i = 0; i < 4; i++) {
			corners[i] = cam.WorldToScreenPoint(corners[i]);
		}
	}

	Vector3 GetMatchingCornerScreenPos(RectTransform rt, Edge edge, Camera c) {
		SetScreenCorners(rt, c);
		int targetCornerIndex = 0;
		if (edge == Edge.Top || edge == Edge.Right) targetCornerIndex = 2;
		return corners[targetCornerIndex];
	}

	Vector2 GetNormSizeDeltaFromScreenSpace(RectTransform rectToSet, Vector2 targetPos, Edge edge, Camera cam) {
		SetScreenCorners(rectToSet, cam);



		float width = corners[2].x - corners[0].x;
		float height = corners[1].y - corners[0].y;
		width = width == 0 ? 1f : width;
		height = height == 0 ? 1f : height;
		Vector2 normDelta = Vector2.zero;
		switch (edge) {
			case Edge.Right:
				normDelta.x = (targetPos.x - corners[2].x) / width;
				break;
			case Edge.Left:
				normDelta.x = -(targetPos.x - corners[0].x) / width;
				break;
			case Edge.Top:
				normDelta.y = (targetPos.y - corners[2].y) / height;
				break;
			case Edge.Bottom:
				normDelta.y = -(targetPos.y - corners[0].y) / height;
				break;
		}
		return normDelta;
	}

	Vector3 GetAxisValue(Edge edge) {
		switch (edge) {
			case Edge.Right:
			case Edge.Left: return Vector2.right;
			case Edge.Top:
			case Edge.Bottom: return Vector2.up;
			default: return Vector2.zero;
		}
	}




}







[CustomPropertyDrawer(typeof(Snap))]
public class SnapSettingsDrawer : PropertyDrawer {
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		label = EditorGUI.BeginProperty(position, label, property);

		// draw the label + indent
		float prevLabelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 120f;
		position = EditorGUI.PrefixLabel(position, label);
		EditorGUI.indentLevel++;

		var targetProp = property.FindPropertyRelative("target");
		var sideProp = property.FindPropertyRelative("edgeToSet");
		var targetEdge = property.FindPropertyRelative("targetEdge");
		var deltaProp = property.FindPropertyRelative("delta");

		float line = EditorGUIUtility.singleLineHeight;
		float pad = 2f;

		Rect rTarget = new Rect(position.x, position.y, position.width, line);
		Rect rEdge = new Rect(position.x, position.y + line + pad, position.width, line);
		Rect rDelta = new Rect(position.x, position.y + (line + pad) * 2, position.width, line);

		EditorGUI.PropertyField(rTarget, targetProp);

		Snap.Edge side = (Snap.Edge)sideProp.enumValueIndex;
		Snap.Edge[] filtered = (side == Snap.Edge.Left || side == Snap.Edge.Right)
				? new[] { Snap.Edge.Left, Snap.Edge.Right }
				: new[] { Snap.Edge.Top, Snap.Edge.Bottom };

		string[] names = filtered.Select(e => e.ToString()).ToArray();
		int currentIdx = Array.IndexOf(filtered, (Snap.Edge)targetEdge.enumValueIndex);
		if (currentIdx < 0) currentIdx = 0;

		int newIdx = EditorGUI.Popup(rEdge, "TargetEdge", currentIdx, names);
		targetEdge.enumValueIndex = (int)filtered[newIdx];

		EditorGUI.PropertyField(rDelta, deltaProp);

		// restore label width
		EditorGUIUtility.labelWidth = prevLabelWidth;
		EditorGUI.indentLevel--;

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			=> EditorGUIUtility.singleLineHeight * 3 + 4;
}
