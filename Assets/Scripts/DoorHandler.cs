using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DoorHandler : NetworkBehaviour {
	NetworkVariable<bool> doorState = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public List<DoorTrigger> doorTriggers = new List<DoorTrigger>();
	public override void OnNetworkSpawn() {
		doorState.OnValueChanged += CheckDoor;
		CheckDoor(false, doorState.Value);
	}
	void CheckDoor(bool initial, bool curr) {
		door.SetActive(!curr);
	}
	void Update() {
		if (!Input.GetKeyDown(KeyCode.F)) return;
		OpenDoorServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	void OpenDoorServerRpc() {
		doorState.Value = !doorState.Value;
	}

	public GameObject door;


}
