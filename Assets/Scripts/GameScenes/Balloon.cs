using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Balloon : NetworkBehaviour {
	float flyTime = 5f;
	[HideInInspector] public NetworkVariable<float> flyProgress = new NetworkVariable<float>(0f);
	[HideInInspector] public NetworkVariable<int> power;
	[HideInInspector] public NetworkVariable<Team> balloonTeam;
	[HideInInspector] public NetworkVariable<Color> balloonColor;

	float flightWidth = 10f;
	float flightHeight = 4f;
	NetworkVariable<float> realFlightHeight = new NetworkVariable<float>(4f);
	void Awake() {
		startPos = Vector3.right * -flightWidth / 2f;
		endPos = -startPos;
		if (NetworkManager.Singleton.IsServer) realFlightHeight.Value = Random.Range(0.7f, 1.2f) * flightHeight;
	}

	TextMeshPro powerTxt;
	public BalloonAnimation anim;

	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		transform.GetComponentInChildren<SpriteRenderer>().color = balloonColor.Value;

		powerTxt = transform.GetComponentInChildren<TextMeshPro>();

		flyProgress.OnValueChanged += progressChanged;
		power.OnValueChanged += powerChanged;

		powerTxt.text = power.Value.ToString();

		anim.InitilizeAnimations(balloonColor.Value);

		UpdateFlyProgress();
		UpdateScale();
	}
	public override void OnNetworkDespawn() {
		base.OnNetworkDespawn();
		flyProgress.OnValueChanged -= progressChanged;
		power.OnValueChanged -= powerChanged;
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
	float minScale = 0.7f, maxScale = 2f;
	void UpdateScale() {
		transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, power.Value / 15f);
	}
	Vector3 startPos, endPos;
	void progressChanged(float previous, float current) {
		if (current >= 1) {
			HitBase();
			return;
		}
		float realProgress = current;
		if (balloonTeam.Value != BalloonManager.team) realProgress = 1 - current;
		float p = ProgressBehaviour(realProgress);
		transform.position = GetProgressPosition(startPos, endPos, p);
	}
	void powerChanged(int prev, int curr) {
		powerTxt.text = curr.ToString();
		UpdateScale();
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
		p.y = start.y + realFlightHeight.Value * (Mathf.Sin(progress * Mathf.PI));
		return p;
	}



	void OnTriggerEnter2D(Collider2D other) {
		// if (!NetworkManager.Singleton.IsServer) return;
		if (power.Value <= 0) return;
		if (other.gameObject.TryGetComponent<Balloon>(out Balloon s)) {
			if (s.power.Value <= 0 || balloonTeam.Value == s.balloonTeam.Value) return;
			TakeDamage(s.SendAndReceiveDamage(power.Value));
		}
	}

	public int SendAndReceiveDamage(int dmg) {
		int initialHP = power.Value;
		TakeDamage(dmg);
		return initialHP;
	}

	void TakeDamage(int dmg) {
		if (NetworkManager.Singleton.IsServer) {
			power.Value = power.Value - dmg <= 0 ? 0 : power.Value - dmg;
			powerTxt.text = power.Value.ToString();
		}
		//take dmaage splash
		if (power.Value <= 0) DestroyBalloon();
	}
	void DestroyBalloon(bool onBase = false) {
		if (onBase) {
			//make effects
			if (NetworkManager.Singleton.IsServer) Destroy(gameObject);
		} else {
			//some other effects
			if (NetworkManager.Singleton.IsServer) Destroy(gameObject);
		}
	}

	void HitBase() {
		if (NetworkManager.Singleton.IsServer) {
			BaseManager.DamageBase(balloonTeam.Value == Team.t1 ? Team.t2 : Team.t1, power.Value);
			power.Value = 0;
		}
		DestroyBalloon(true);
	}



}
