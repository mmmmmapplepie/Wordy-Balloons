using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-100)]
public class AudioPlayer : MonoBehaviour {
  public List<Sound> sounds;
  public bool UseClipNames = true;
  public bool Singleton = true;
  public static AudioPlayer Instance;
  void Awake() {
    if (Singleton) {
      if (Instance == null) {
        Instance = this;
        DontDestroyOnLoad(transform.root);
      } else Destroy(gameObject);
    }

    if (Singleton && Instance != this) return;

    SetupAudioSources();
    SetupOneShotSoundPlayer();
  }
  void SetupAudioSources() {
    foreach (Sound sound in sounds) {
      AddNewSound(sound);
    }
  }
  public void AddNewSound(Sound sound) {
    sound.Name = GetNameFromSound(sound);
    if (sounds.Find(x => x.Name == sound.Name) != null) return;
    AudioSource src = sound.audioSource = gameObject.AddComponent<AudioSource>();
    sounds.Add(sound);
    src.clip = sound.clip;
    src.priority = sound.priority;
    src.volume = sound.volume;
    src.pitch = sound.pitch;
    src.loop = sound.loop;
    src.playOnAwake = sound.playOnAwake;
    src.spatialBlend = sound.spatialBlend;
    src.minDistance = sound.minDistance;
    src.maxDistance = sound.maxDistance;
    if (src.playOnAwake == true) {
      src.Play();
    } else {
      src.Stop();
    }
  }
  public void RemoveSound(Sound sound) {
    StopSound(sound.Name);
    int ind = sounds.FindIndex(x => x.Name == sound.Name);
    if (ind < 0) return;
    Destroy(sounds[ind].audioSource);
    sounds.RemoveAt(ind);
  }
  string GetNameFromSound(Sound sound) {
    return UseClipNames ? sound.clip.name : sound.Name;
  }
  public void StopSoundsAndRoutines(Sound s, bool stopAll = false) {
    string name = UseClipNames ? s.clip.name : s.Name;
    StopSoundsAndRoutines(name, stopAll);
  }
  public void StopSoundsAndRoutines(string name, bool stopAll = false) {
    if (stopAll) {
      StopFadeRoutines("", true);
      foreach (Sound s in sounds) {
        s.audioSource.Stop();
      }
    } else {
      StopFadeRoutines(name);
      FindSound(name).audioSource.Stop();
    }
  }
  public Sound FindSound(string name) {
    return sounds.Find(x => x.Name == name);
  }
  public bool IsPlaying(string name) {
    return FindSound(name).audioSource.isPlaying;
  }
  public void SetLooping(string name, bool LoopIsTrue = true) {
    FindSound(name).audioSource.loop = LoopIsTrue ? true : false;
  }
  List<FadeSound> fadeSounds = new List<FadeSound>();
  public void PlaySound(string name, float finalVolume = 1f, float fadeInTime = 0f, bool scaleVolumeWithSoundVolume = true, bool stopAllRoutines = false) {
    Sound sound = FindSound(name);
    if (sound == null) return;
    if (sound.audioSource == null) return;
    StopFadeRoutines(name, stopAllRoutines);
    float vol = sound.volume;
    if (scaleVolumeWithSoundVolume) {
      finalVolume *= vol;
    }
    if (fadeInTime == 0) {
      sound.audioSource.volume = finalVolume;
      sound.audioSource.Play();
    } else {
      FadeSound fadeSound = new FadeSound(StartCoroutine(FadeInRoutine(sound, fadeInTime, finalVolume)), name);
      fadeSounds.Add(fadeSound);
    }
  }
  IEnumerator FadeInRoutine(Sound sound, float fadeInTime, float volumeFinal) {
    float VolDiff = volumeFinal;
    float StartTime = Time.unscaledTime;
    sound.audioSource.Play();
    while (Time.unscaledTime < StartTime + fadeInTime) {
      float ratio = ((Time.unscaledTime - StartTime - fadeInTime) / fadeInTime) + 1f;
      sound.audioSource.volume = VolDiff * ratio;
      yield return null;
    }
    sound.audioSource.volume = volumeFinal;
  }
  public void StopSound(string name, float fadeOutTime = 0f, bool stopAllRoutines = false) {
    Sound sound = FindSound(name);
    if (sound == null) return;
    if (sound.audioSource == null) return;
    StopFadeRoutines(name, stopAllRoutines);
    if (fadeOutTime == 0) {
      sound.audioSource.volume = 0f;
      sound.audioSource.Stop();
    } else {
      FadeSound fadeSound = new FadeSound(StartCoroutine(FadeOutRoutine(sound, fadeOutTime)), name);
      fadeSounds.Add(fadeSound);
    }
  }
  IEnumerator FadeOutRoutine(Sound sound, float fadeOutTime) {
    float InitialVol = sound.audioSource.volume;
    float FinalVol = 0f;
    float VolDiff = FinalVol - InitialVol;
    float StartTime = Time.unscaledTime;
    while (Time.unscaledTime < StartTime + fadeOutTime) {
      float ratio = ((Time.unscaledTime - StartTime - fadeOutTime) / fadeOutTime) + 1f;
      sound.audioSource.volume = InitialVol + VolDiff * ratio;
      yield return null;
    }
    sound.audioSource.volume = FinalVol;
    sound.audioSource.Stop();
  }
  public void StopFadeRoutines(string name, bool stopAllRoutines = false) {
    List<FadeSound> fadesounds = null;
    if (stopAllRoutines) {
      fadesounds = fadeSounds;
    } else {
      fadesounds = fadeSounds.FindAll(x => x.Name == name);
    }
    for (int i = 0; i < fadesounds.Count; i++) {
      if (fadesounds[i] != null) {
        StopCoroutine(fadesounds[i].Routine);
        fadesounds[i] = null;
      }
    }
    fadesounds.RemoveAll(x => x == null);
  }
  public class FadeSound {
    public Coroutine Routine;
    public string Name;
    public FadeSound(Coroutine routine, string name) {
      this.Routine = routine;
      this.Name = name;
    }
  }
  public void SetVolume(string name, float targetVolume = 1f, float changeTime = 0f, bool stopAllRoutines = false, bool limitVolume = true, bool changeEvenIfNotPlaying = false) {
    Sound sound = FindSound(name);
    if (sound == null || sound.audioSource == null) return;
    if (!sound.audioSource.isPlaying && !changeEvenIfNotPlaying) return;
    float volM = sound.volume;
    if (limitVolume) {
      targetVolume *= volM;
    }
    StopFadeRoutines(name, stopAllRoutines);
    if (changeTime == 0f) {
      sound.audioSource.volume = targetVolume;
    } else {
      FadeSound fadeSound = new FadeSound(StartCoroutine(VolumeRoutine(sound, targetVolume, changeTime)), name);
      fadeSounds.Add(fadeSound);
    }
  }
  IEnumerator VolumeRoutine(Sound sound, float FinalVol, float changeTime) {
    float InitialVol = sound.audioSource.volume;
    float VolDiff = FinalVol - InitialVol;
    float StartTime = Time.unscaledTime;
    while (Time.unscaledTime < StartTime + changeTime) {
      float ratio = ((Time.unscaledTime - StartTime - changeTime) / changeTime) + 1f;
      sound.audioSource.volume = InitialVol + VolDiff * ratio;
      yield return null;
    }
    sound.audioSource.volume = FinalVol;
  }
  public void PauseResumeSound(string name, bool pause) {
    Sound sound = FindSound(name);
    if (sound == null) return;
    if (pause) { sound.audioSource.Pause(); } else {
      sound.audioSource.UnPause();
    }
  }

  #region oneShotSound
  static AudioSource oneShotSoundPlayer;
  void SetupOneShotSoundPlayer() {
    oneShotSoundPlayer = gameObject.AddComponent<AudioSource>();
  }
  public static AudioSource GetOneShotPlayer() {
    return oneShotSoundPlayer;
  }
  public static void SetOneshotClip(AudioClip clip) {
    oneShotSoundPlayer.clip = clip;
  }
  public void PlayOneShot(AudioClip clip) {
    PlayOneShot_Static(clip);
  }
  public void PlayOneShot(AudioClip clip, float volume) {
    PlayOneShot_Static(clip, volume);
  }

  public static void PlayOneShot_Static(AudioClip clip, float volume = 1f) {
    if (oneShotSoundPlayer == null) return;
    oneShotSoundPlayer.PlayOneShot(clip, volume);
  }

  #endregion
}
