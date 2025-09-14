using System.Collections.Generic;
using UnityEngine;

public class TypedInputAudio : MonoBehaviour {
  public AudioClip correctInput, wrongInput, correctWord, wrongWord, skiptickIncrease;

  void Start() {
    AudioPlayer.LoadClipBySettingOnOneShot(correctInput);
    AudioPlayer.LoadClipBySettingOnOneShot(wrongInput);
    AudioPlayer.LoadClipBySettingOnOneShot(correctWord);
    AudioPlayer.LoadClipBySettingOnOneShot(wrongWord);
    AudioPlayer.LoadClipBySettingOnOneShot(skiptickIncrease);

    InputManager.CorrectEntryFinished += CorrectEntry;
    InputManager.WrongEntryFinished += WrongEntry;
    InputManager.IncrementInputFinished += InputChanged;
    InputManager.SkipTickChanged += SkipTickChange;
  }
  void OnDestroy() {
    InputManager.CorrectEntryFinished -= CorrectEntry;
    InputManager.WrongEntryFinished -= WrongEntry;
    InputManager.IncrementInputFinished -= InputChanged;
    InputManager.SkipTickChanged -= SkipTickChange;
  }

  void CorrectEntry() {
    AudioPlayer.PlayOneShot_Static(correctWord, VolumeControl.GetEffectVol() * Random.Range(0.8f, 1f));
  }
  void WrongEntry() {
    AudioPlayer.PlayOneShot_Static(wrongWord, VolumeControl.GetEffectVol() * Random.Range(0.8f, 1f));
  }
  int prevtick = 0;
  void SkipTickChange(int i) {
    if (i <= prevtick) { prevtick = i; return; }
    AudioPlayer.PlayOneShot_Static(skiptickIncrease, VolumeControl.GetEffectVol() * Random.Range(0.8f, 1f));
    prevtick = i;
  }
  void InputChanged(string s) {
    if (s == InputManager.SkipString) return;
    string target = InputManager.Instance.targetString;
    string typedString = InputManager.Instance.typedString;
    string stringToCompare = target.Substring(typedString.Length - s.Length, s.Length);

    if (stringToCompare == s) AudioPlayer.PlayOneShot_Static(correctInput, VolumeControl.GetEffectVol() * Random.Range(0.4f, 0.5f));
    else AudioPlayer.PlayOneShot_Static(wrongInput, VolumeControl.GetEffectVol() * Random.Range(0.4f, 0.5f));
  }







}
