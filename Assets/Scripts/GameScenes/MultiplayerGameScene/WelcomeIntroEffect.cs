using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WelcomeIntroEffect : MonoBehaviour {
	[HideInInspector] public bool AnimationFinished = false;



	void OnEnable() {
		StartCoroutine(StartOpeningAnimation());
	}


	public Image blackoutImg;
	public GameObject welcomeBox, title;

	IEnumerator StartOpeningAnimation() {
		//blackout
		float blackoutTime = 2f;
		float t = 0f;
		while (t < blackoutTime) {
			t += Time.unscaledDeltaTime;
			blackoutImg.color = Color.Lerp(Color.clear, Color.black, t / blackoutTime);
			//quiet down bgm as well
			yield return null;
		}
		blackoutImg.color = Color.black;



		//welcome     to -- fade in the words -- shift it up and also reduce size a lil (target, pos 100 y, size 80) ---(start at y = 0, size 120)



		//wordyballoons -- drop in from left parabolic flight.


		//then fireworks and wordyballoons expands with sound effects and :shake:




		//return to normal -- fireworks smaller ones in background? --> words bobbing left and right



		//blackout fades
		t = 0f;
		while (t < blackoutTime) {
			t += Time.unscaledDeltaTime;
			blackoutImg.color = Color.Lerp(Color.black, Color.clear, t / blackoutTime);
			//return bgm as well
			yield return null;
		}
		blackoutImg.color = Color.clear;



		AnimationFinished = true;
	}




}
