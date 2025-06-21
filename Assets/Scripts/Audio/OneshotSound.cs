using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(100)]
public class OneshotSound : MonoBehaviour {
	public AudioClip clip;
	public float volume = 1;
	void Awake() {
		if (AudioPlayer.Instance == null) return;
		AudioPlayer.SetOneshotClip(clip);
	}
	public void PlayOneshotClip() {
		AudioPlayer.PlayOneShot_Static(clip, volume * VolumeControl.GetEffectVol());
	}
}
