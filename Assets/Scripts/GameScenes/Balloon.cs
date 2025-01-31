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

	float flightWidth = 10f;
	float flightHeight = 4f;
	void Start() {
		startPos = Vector3.right * -flightWidth / 2f;
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
			float newVal = flyProgress.Value + Time.deltaTime / flyTime;
			if (newVal >= 1) newVal = 1f;
			flyProgress.Value = newVal;
		}
	}
	Vector3 startPos, endPos;
	void progressChanged(float previous, float current) {
		float realProgress = current;
		if (!BalloonManager.teamIDs.Contains(balloonTeamID.Value)) realProgress = 1 - current;
		float p = ProgressBehaviour(realProgress);
		transform.position = GetProgressPosition(startPos, endPos, p);
	}

	float ProgressBehaviour(float progress) {
		if (progress <= 0.5) {
			return Mathf.Sin(progress * Mathf.PI) / 2f;
		} else {
			return (2f - Mathf.Sin(progress * Mathf.PI)) / 2f;
		}
	}

	Vector3 GetProgressPosition(Vector3 start, Vector3 end, float progress) {
		Vector3 p = start;
		p.x = Mathf.Lerp(start.x, end.x, progress);
		p.y = start.y + flightHeight * (Mathf.Sin(progress * Mathf.PI));
		return p;
	}







}
