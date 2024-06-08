using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(NetworkManager))]
public class NetworkManagerSingleton : MonoBehaviour {
	void Awake() {
		if (NetworkManager.Singleton != null && NetworkManager.Singleton != GetComponent<NetworkManager>()) Destroy(gameObject);
	}
}
