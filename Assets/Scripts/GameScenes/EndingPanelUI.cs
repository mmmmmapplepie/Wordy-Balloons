using System.Collections.Generic;
using UnityEngine;

public class EndingPanelUI : MonoBehaviour {
	void Awake() {
		AudioPlayer.Instance.AddNewSound(victorySound);
		AudioPlayer.Instance.AddNewSound(defeatSound);
	}
	public Sound victorySound, defeatSound;
	public void PlaySound(bool victory) {
		AudioPlayer.Instance.PlaySound(victory ? victorySound.Name : defeatSound.Name, VolumeControl.GetEffectVol());
	}
	public void PlayVictory() {
		PlaySound(true);
	}
	public void PlayDefeat() {
		PlaySound(false);
	}
}
