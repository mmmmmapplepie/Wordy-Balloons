using System.Collections.Generic;
using UnityEngine;

public class EndingPanelUI : MonoBehaviour {
	void Awake() {
		AudioPlayer.Instance.AddNewSound(victorySound);
		AudioPlayer.Instance.AddNewSound(defeatSound);
		AudioPlayer.Instance.AddNewSound(drawSound);
	}
	public Sound victorySound, defeatSound, drawSound;
	public void PlaySound(Sound s) {
		AudioPlayer.Instance.PlaySound(s.Name, VolumeControl.GetEffectVol());
	}
	public void PlayVictory() {
		PlaySound(victorySound);
	}
	public void PlayDefeat() {
		PlaySound(defeatSound);
	}
	public void PlayDraw() {
		PlaySound(drawSound);
	}
}
