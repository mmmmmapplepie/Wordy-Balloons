using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Balloon : NetworkBehaviour {
	float flyTime = 5f;
	[HideInInspector] public int tempPower;
	[HideInInspector] public Team tempTeam;
	[HideInInspector] public Color tempColor;
	[HideInInspector] public NetworkVariable<float> flyProgress = new NetworkVariable<float>(0f);
	[HideInInspector] public NetworkVariable<int> power;
	[HideInInspector] public NetworkVariable<Team> balloonTeam;
	[HideInInspector] public NetworkVariable<Color> balloonColor;

	float flightHeight = 4f;
	NetworkVariable<float> realFlightHeight = new NetworkVariable<float>(4f);
	void Awake() {
		powerTxt = transform.GetComponentInChildren<TextMeshPro>();
	}

	TextMeshPro powerTxt;
	public BalloonAnimation anim;
	public static event System.Action<Team, Balloon> BalloonCreated;
	public static event System.Action<bool, Balloon> BalloonDestroyed;
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		power.OnValueChanged += PowerChanged;
		flyProgress.OnValueChanged += ProgressChanged;
		if (NetworkManager.Singleton.IsServer) {
			realFlightHeight.Value = Random.Range(0.7f, 1.2f) * flightHeight;
			power.Value = tempPower;
			balloonTeam.Value = tempTeam;
			balloonColor.Value = tempColor;
			startP.Value = startPos;
			endP.Value = endPos;
		}
		PowerChanged(0, power.Value);
		ProgressChanged(0f, 0f);
		SetPosition(0);
		latestProgressUpdateTime = Time.time - smoothingTime;
		UpdateScale();
		BalloonCreated?.Invoke(balloonTeam.Value, this);
		anim.InitilizeAnimations(balloonColor.Value);
	}
	public override void OnNetworkDespawn() {
		flyProgress.OnValueChanged -= ProgressChanged;
		power.OnValueChanged -= PowerChanged;
		base.OnNetworkDespawn();
	}

	void Update() {
		UpdateFlyProgress();
	}
	float localProgress = 0f;
	float latestProgressUpdateTime = -100f;
	float smoothingTime = 1f;
	float maxError = 0.05f;
	float lerpSpeed = 1.2f;
	float currSetProgress = 0f;

	void UpdateFlyProgress() {
		if (NetworkManager.Singleton.IsServer) {
			flyProgress.Value = Mathf.Clamp01(flyProgress.Value + Time.deltaTime / flyTime);
			SetPosition(flyProgress.Value);
		} else {
			print(localProgress);
			localProgress = Mathf.Clamp01(localProgress + Time.deltaTime / flyTime);
			if (interpolation != null) return;
			SetPosition(localProgress);
		}
	}
	float minScale = 0.7f, maxScale = 2f;
	void UpdateScale() {
		transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, power.Value / 15f);
	}
	NetworkVariable<Vector3> startP = new NetworkVariable<Vector3>(), endP = new NetworkVariable<Vector3>();
	[HideInInspector] public Vector3 startPos, endPos;
	void ProgressChanged(float previous, float current) {
		if (current >= 1) {
			HitBase();
			return;
		}

		Debug.LogError($"Progress Set{current}");
		if (NetworkManager.Singleton.IsServer) return;
		if (Mathf.Abs(currSetProgress - current) > maxError) {
			if (interpolation != null) StopCoroutine(interpolation);
			Debug.LogError("Snap===============================");
			SetPosition(current);
			interpolation = StartCoroutine(InterpolatePosition(currSetProgress, current));
		} else {
			if (interpolation != null) StopCoroutine(interpolation);
			interpolation = StartCoroutine(InterpolatePosition(currSetProgress, current));
		}
	}
	Coroutine interpolation = null;
	IEnumerator InterpolatePosition(float start, float target) {
		float t = 0;
		float period = Mathf.Abs(target - start) * flyTime / lerpSpeed;
		while (t < period) {
			t += Time.deltaTime;
			SetPosition(Mathf.Lerp(start, target, t / period));
			Debug.LogWarning($"going ot target : {currSetProgress}");
			yield return null;
		}

		while (true) {
			float newProgress = currSetProgress + Mathf.Sign(localProgress - currSetProgress) * Time.deltaTime * lerpSpeed / flyTime;
			Debug.LogWarning($"to local: {newProgress}");
			if (Mathf.Abs(localProgress - newProgress) <= Time.deltaTime * lerpSpeed / flyTime) { Debug.LogWarning("breaking"); break; }
			SetPosition(newProgress);
			yield return null;
		}
		SetPosition(localProgress);
		interpolation = null;
	}
	void SetPosition(float progress) {
		Debug.LogAssertion($"currSET: {progress}");
		currSetProgress = progress;
		float realProgress = progress;
		if (balloonTeam.Value != BalloonManager.team) realProgress = 1 - progress;
		float p = ProgressBehaviour(realProgress);
		transform.position = GetProgressPosition(startP.Value, endP.Value, p);
	}
	void PowerChanged(int prev, int curr) {
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
		if (power.Value - dmg <= 0) DestroyBalloon();
		if (NetworkManager.Singleton.IsServer) {
			power.Value = power.Value - dmg <= 0 ? 0 : power.Value - dmg;
			powerTxt.text = power.Value.ToString();
		}
	}
	void DestroyBalloon(bool onBase = false) {
		DestroyEffectClientRpc(onBase);
		if (NetworkManager.Singleton.IsServer) Destroy(gameObject);
	}

	[ClientRpc]
	void DestroyEffectClientRpc(bool onBase = false) {
		if (onBase) anim.BaseCollisionEffect();
		else anim.CollisionEffect();
		BalloonDestroyed?.Invoke(onBase, this);
	}

	void HitBase() {
		DestroyBalloon(true);
		if (NetworkManager.Singleton.IsServer) {
			BaseManager.DamageBase(balloonTeam.Value == Team.t1 ? Team.t2 : Team.t1, power.Value);
			power.Value = 0;
		}
	}



}
