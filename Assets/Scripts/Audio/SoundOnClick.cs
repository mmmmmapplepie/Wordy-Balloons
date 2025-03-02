using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoundOnClick : OneshotSound, IPointerClickHandler {
	public void OnPointerClick(PointerEventData eventData) {
		PlayOneshotClip();
	}
}
