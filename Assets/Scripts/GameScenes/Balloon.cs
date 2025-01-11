using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Balloon : NetworkBehaviour {
	float flyTime = 5f;
	[HideInInspector] public NetworkVariable<float> flyProgress;
	[HideInInspector] public NetworkVariable<int> power;
	[HideInInspector] public NetworkVariable<ulong> balloonTeamID;
	[HideInInspector] public NetworkVariable<Color> balloonColor;

	void Start() {
		startPos = Vector3.right * -5f;
		endPos = -startPos;
	}

	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		flyProgress.OnValueChanged += progressChanged;
		//Fix balloon size and position

		GetComponent<SpriteRenderer>().color = balloonColor.Value;


		transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = power.Value.ToString();
	}
	public override void OnNetworkDespawn() {
		base.OnNetworkDespawn();
		flyProgress.OnValueChanged -= progressChanged;
	}

	void Update() {
		UpdateFlyProgress();
	}

	void UpdateFlyProgress() {
		if (NetworkManager.Singleton.IsServer) {
			flyProgress.Value += Time.deltaTime / flyTime;
		}
	}
	Vector3 startPos, endPos;
	void progressChanged(float previous, float current) {
		float realProgress = current;
		if (!BalloonManager.teamIDs.Contains(balloonTeamID.Value)) realProgress = 1 - current;


		transform.position = Vector3.Lerp(startPos, endPos, realProgress);
	}







}
