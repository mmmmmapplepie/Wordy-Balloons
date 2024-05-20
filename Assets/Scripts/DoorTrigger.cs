using System;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour {
	public int index = 0;
	public event Action<int, int> Triggered;
	List<GameObject> enteredPlayers = new List<GameObject>();
	void OnTriggerEnter(Collider col) {
		enteredPlayers.Add(col.gameObject);
		Triggered?.Invoke(index, enteredPlayers.Count > 0 ? 1 : 0);
	}
	void OnTriggerExit(Collider col) {
		enteredPlayers.Remove(col.gameObject);
		Triggered?.Invoke(index, enteredPlayers.Count > 0 ? 1 : 0);
	}
}
