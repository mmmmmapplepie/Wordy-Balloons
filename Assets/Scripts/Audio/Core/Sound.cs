using UnityEngine;
[System.Serializable]
public class Sound {
	[HideInInspector]
	public AudioSource audioSource;
	public string Name;
	public AudioClip clip;
	[Range(0, 255)]
	public int priority;
	[Range(0, 1)]
	public float volume = 1f;
	public float pitch = 1f;
	public bool loop;
	public bool playOnAwake;
	[Range(0, 1)]
	public float spatialBlend;
	public float minDistance = 1f;
	public float maxDistance = 500f;
}
