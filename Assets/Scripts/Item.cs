using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour {
	public GameObject itemPrefab;
	GameObject spawnedItem;
	void Update() {
		if (Input.GetKey(KeyCode.S) && NetworkManager.Singleton.IsConnectedClient) {
			spawnedItem = Instantiate(itemPrefab);
			spawnedItem.GetComponent<NetworkObject>().Spawn();
		}
	}
}
