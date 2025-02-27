using System.Collections.Generic;
using UnityEngine;

public class PlayAnimationAtTrigger : MonoBehaviour {

	public AnimationClip clip;

	Animator animator;
	void Awake() {
		animator = GetComponent<Animator>();
	}
	public bool playOnEnable, playOnStart;
	void OnEnable() {
		if (playOnEnable) PlayAnimation();
	}
	void Start() {
		if (playOnStart) PlayAnimation();
	}

	public void PlayAnimation() {
		if (animator == null || clip == null) return;
		animator.Play(clip.name);
	}
}
