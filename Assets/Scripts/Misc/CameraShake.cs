using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour {
	public bool shakeInX, shakeInY, shakeInZ;

	public float magnitude;
	public float period;
	Vector3 center;
	public void SetPivot() {
		center = transform.position;
	}

	public bool shake = false;
	float t = 0;
	void Update() {
		if (shake) {
			if (period > 0) {
				t += Time.unscaledDeltaTime;
				if (t < period) {
					return;
				} else t = 0;
			}
			Vector3 shakeDisplacement = default;
			if (shakeInX) shakeDisplacement.x = magnitude * Random.Range(-1f, 1f);
			if (shakeInY) shakeDisplacement.y = magnitude * Random.Range(-1f, 1f);
			if (shakeInZ) shakeDisplacement.z = magnitude * Random.Range(-1f, 1f);
			transform.localPosition = shakeDisplacement;
		} else transform.position = center;
	}
}
