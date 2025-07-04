using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractableBtnCheck : MonoBehaviour {
	public Button b;
	public GameObject g;
	void Update() {
		g.SetActive(!b.interactable);
	}
}
