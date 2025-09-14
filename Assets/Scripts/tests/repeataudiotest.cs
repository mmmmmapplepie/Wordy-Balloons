using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class repeataudiotest : MonoBehaviour {
  public Sound s;
  void Start() {
    AudioPlayer.Instance.AddNewSound(s);
  }

  float period = 0.3f;
  float lastT = -100f;
  [Range(0.5f, 1.5f)]
  public float pitch = 1f;
  public float vol = 1f;
  void Update() {

    // AudioPlayer.Instance.SetPitch(s.Name, pitch);
    // if (Time.time - lastT < period) return;
    // // AudioPlayer.Instance.StopSound(s.Name);
    // AudioPlayer.Instance.PlaySound(s.Name);
    // lastT = Time.time;
    // AudioPlayer.Instance.SetPitch(s.Name, Random.Range(-3f, 3f));

    if (Time.time - lastT < period) return;
    // AudioPlayer.Instance.StopSound(s.Name);
    AudioPlayer.PlayOneShot_Static(s.clip, vol, pitch);
    lastT = Time.time;
  }
}
