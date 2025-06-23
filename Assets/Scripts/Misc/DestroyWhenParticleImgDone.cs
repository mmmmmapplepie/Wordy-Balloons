using System.Collections.Generic;
using AssetKits.ParticleImage;
using UnityEngine;

public class DestroyWhenParticleImgDone : MonoBehaviour {
	ParticleImage img;
	void Start() {
		TryGetComponent<ParticleImage>(out img);
	}
	void Update() {
		if (img.isPlaying || !gameObject.activeInHierarchy) return;
		Destroy(gameObject);
	}
}
