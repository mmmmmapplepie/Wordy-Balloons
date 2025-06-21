using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour {
	public static float MasterVolume = 1, EffectVolume = 1, BGMVolume = 1;

	const string MasterVol = "MasterVol", EffectVol = "EffectVol", BGMVol = "BGMVol";

	public Slider masterSlider, effectSlider, bgmSlider;
	void Awake() {
		if (PlayerPrefs.HasKey(MasterVol)) {
			MasterVolume = PlayerPrefs.GetFloat(MasterVol);
			masterSlider.Set(MasterVolume);
			EffectVolume = PlayerPrefs.GetFloat(EffectVol);
			effectSlider.Set(EffectVolume);
			BGMVolume = PlayerPrefs.GetFloat(BGMVol);
			bgmSlider.Set(BGMVolume);
			VolumeChanged?.Invoke();
		} else {
			PlayerPrefs.SetFloat(MasterVol, 1);
			PlayerPrefs.SetFloat(EffectVol, 1);
			PlayerPrefs.SetFloat(BGMVol, 1);
		}
	}
	public static event System.Action VolumeChanged;
	public void SetMasterVolume(float f) {
		MasterVolume = f;
		PlayerPrefs.SetFloat(MasterVol, f);
		VolumeChanged?.Invoke();
	}
	public void SetEffectVolume(float f) {
		EffectVolume = f;
		PlayerPrefs.SetFloat(EffectVol, f);
		VolumeChanged?.Invoke();
	}
	public void SetBGMVolume(float f) {
		BGMVolume = f;
		PlayerPrefs.SetFloat(BGMVol, f);
		VolumeChanged?.Invoke();
	}
	public GameObject volumePanel;
	public void VolumePanelToggle(bool open) {
		volumePanel.SetActive(open);
	}


	public static float GetBGMVol() {
		return VolumeControl.BGMVolume * VolumeControl.MasterVolume;
	}
	public static float GetEffectVol() {
		return VolumeControl.EffectVolume * VolumeControl.MasterVolume;
	}

}
