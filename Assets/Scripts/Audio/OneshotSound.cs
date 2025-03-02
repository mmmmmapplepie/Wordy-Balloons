using System.Collections.Generic;
using UnityEngine;

public class OneshotSound : MonoBehaviour {
	public AudioClip clip;
	public float volume = 1;
	void Start() {
		if (AudioPlayer.Instance == null) return;
		AudioPlayer.SetOneshotClip(clip);
	}
	public void PlayOneshotClip() {
		AudioPlayer.PlayOneShot_Static(clip, volume);
	}
}
