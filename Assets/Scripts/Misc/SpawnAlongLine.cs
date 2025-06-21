using System.Collections.Generic;
using UnityEngine;

public class SpawnAlongLine : MonoBehaviour {
	public float interval = 1f;
	public float scale = 1f;
	public GameObject objectToSpawn;
	public bool vertical;
	float range = 1f;
	void OnEnable() {
		if (objectToSpawn == null) return;
		TryGetComponent<RectTransform>(out RectTransform rt);
		if (rt) {
			Vector3[] corners = new Vector3[4];
			rt.GetWorldCorners(corners);
			Vector3 bottomLeft = corners[0];
			Vector3 topLeft = corners[1];
			Vector3 topRight = corners[2];
			float width = (topRight - topLeft).magnitude;
			float height = (topLeft - bottomLeft).magnitude;
			range = (vertical ? height : width) * scale;
		} else {
			range = (vertical ? transform.localScale.y : transform.localScale.x) * scale;
		}
		range /= 2;
		InvokeRepeating(nameof(SpawnObject), 0f, interval);
	}

	void SpawnObject() {
		Instantiate(objectToSpawn, transform.position + Random.Range(-1, 1) * range * (vertical ? transform.up : transform.right), Quaternion.identity, transform);
	}
	void OnDisable() {
		CancelInvoke();
	}


}
