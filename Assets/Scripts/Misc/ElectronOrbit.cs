using System.Collections.Generic;
using UnityEngine;

public class ElectronOrbit : MonoBehaviour {
	public Transform nucleus;  // Reference to the nucleus
	public float orbitSpeed = 100f;  // Speed of the orbit
	public float orbitRadius = 100f;  // Radius of the electron's orbit
	public float phase = 0f;

	void Start() {
		orbitRadius = orbitRadius * 1000f * transform.root.localScale.x;
		phase = Random.Range(0, 360f);
		transform.position = new Vector3(nucleus.position.x + orbitRadius, nucleus.position.y, nucleus.position.z);
		transform.RotateAround(nucleus.position, transform.parent.up, phase);
		transform.localRotation = Quaternion.identity;
		transform.parent.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
	}

	void Update() {
		transform.RotateAround(nucleus.position, transform.parent.up, orbitSpeed * Time.deltaTime);
		transform.localRotation = Quaternion.identity;
		if (transform.localPosition.z > 0) transform.parent.SetAsFirstSibling();
		else transform.parent.SetAsLastSibling();
	}

}
