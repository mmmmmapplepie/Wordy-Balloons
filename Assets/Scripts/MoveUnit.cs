using Unity.Netcode;
using UnityEngine;

public class MoveUnit : NetworkBehaviour {
	float speed = 10f;
	public Material transparent;
	static NetworkVariable<int> v = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	void ShowValue(int input, int output) {
		Debug.LogWarning(NetworkObjectId + ";" + output);
	}
	public override void OnNetworkSpawn() {
		if (!IsOwner) GetComponent<MeshRenderer>().material = transparent;
		v.OnValueChanged += ShowValue;

	}
	void Update() {
		if (!IsOwner) return;
		Vector3 dir = Vector3.zero;
		dir.x = Input.GetAxis("Horizontal");
		dir.z = Input.GetAxis("Vertical");
		transform.position += speed * dir * Time.deltaTime;
		if (!IsServer) return;
		if (Input.GetKeyDown(KeyCode.C)) v.Value += 1;

	}
}
