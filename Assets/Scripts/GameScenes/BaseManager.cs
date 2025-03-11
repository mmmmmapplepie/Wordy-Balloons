using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BaseManager : NetworkBehaviour {

	const int DefaultMaxHP = 5;
	public static NetworkVariable<int> team1BaseHP = new NetworkVariable<int>(DefaultMaxHP);
	public static NetworkVariable<int> team2BaseHP = new NetworkVariable<int>
	(DefaultMaxHP);
	public static NetworkVariable<int> team1MaxHP = new NetworkVariable<int>(DefaultMaxHP);
	public static NetworkVariable<int> team2MaxHP = new NetworkVariable<int>
	(DefaultMaxHP);

	void Awake() {
		GameStateManager.GameResultSetEvent += ResultChanged;

		Balloon.BalloonCreated += BallonCreated;
	}
	public override void OnDestroy() {
		GameStateManager.GameResultSetEvent -= ResultChanged;

		Balloon.BalloonCreated += BallonCreated;
	}

	public static event Action BaseHPSet;
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		if (NetworkManager.Singleton.IsServer) SetBaseHP(DefaultMaxHP, DefaultMaxHP);
		team1BaseHP.OnValueChanged += BaseHP1Changed;
		team2BaseHP.OnValueChanged += BaseHP2Changed;
	}
	public override void OnNetworkDespawn() {
		base.OnNetworkDespawn();
		team1BaseHP.OnValueChanged += BaseHP1Changed;
		team2BaseHP.OnValueChanged += BaseHP2Changed;
	}
	public static void SetBaseHP(int team1HP, int team2HP) {
		team1BaseHP.Value = team1HP;
		team2BaseHP.Value = team2HP;
		team1MaxHP.Value = team1HP;
		team2MaxHP.Value = team2HP;
		BaseHPSet?.Invoke();
	}
	public static event Action<Team> TeamLose;
	public static event Action<Team, float> BaseTakenDamage;

	[ServerRpc(RequireOwnership = false)]
	public void DamageBaseServerRpc(Team team, int dmg) {
		DamageBase(team, dmg);
	}
	public static void DamageBase(Team teamBaseToDamage, int dmg) {
		NetworkVariable<int> target;
		NetworkVariable<int> max;
		if (teamBaseToDamage == Team.t1) {
			target = team1BaseHP;
			max = team1MaxHP;
		} else {
			target = team2BaseHP;
			max = team2MaxHP;
		}
		target.Value -= dmg;
		BaseTakenDamage?.Invoke(teamBaseToDamage, (float)target.Value / (float)max.Value);
		if (target.Value <= 0) {
			TeamLose?.Invoke(teamBaseToDamage);
		}
	}

	void BaseHP1Changed(int i, int curr) {
		Transform target = homeBase;
		if (BalloonManager.team != Team.t1) {
			target = awayBase;
		}
		DamagedBaseEffect(target);
	}
	void BaseHP2Changed(int i, int curr) {
		Transform target = homeBase;
		if (BalloonManager.team != Team.t2) {
			target = awayBase;
		}
		DamagedBaseEffect(target);
	}
	void DamagedBaseEffect(Transform tr) {
		// tr.GetComponent<Animator>().Play("Blink");
	}



	public Transform homeBase, awayBase;
	public GameObject splashEffect, finalEffect;
	public const float BaseDestroyAnimationTime = 7f;
	public Sprite destroyedBaseSprite;
	public AudioClip baseDestroySound, popSound;

	void BallonCreated(Team t) {
		Transform target = awayBase;
		if (t == BalloonManager.team) {
			target = homeBase;
		}
		target.GetComponent<Animator>().Play("CannonFire");
	}









	void ResultChanged(GameStateManager.GameResult result) {
		if (result == GameStateManager.GameResult.Undecided || result == GameStateManager.GameResult.Draw) return;
		Transform losingBase = homeBase;
		if (result == GameStateManager.GameResult.Team1Win && BalloonManager.team == Team.t1) losingBase = awayBase;
		StartCoroutine(BaseDestroyAnimation(losingBase));
	}

	IEnumerator BaseDestroyAnimation(Transform targetBase) {
		//disable damaged base overlay objects.
		InvokeRepeating(nameof(PlaySound), 1f, 1f);
		SpriteRenderer sr = targetBase.GetComponent<SpriteRenderer>();
		Bounds targetBounds = sr.bounds;
		Vector2 xRange = new Vector2(targetBounds.min.x, targetBounds.max.x);
		Vector2 yRange = new Vector2(targetBounds.min.y, targetBounds.max.y);
		int targetEffects = 80;
		int created = 0;
		while (created < targetEffects) {
			GameObject obj = Instantiate(splashEffect, new Vector3(UnityEngine.Random.Range(xRange.x, xRange.y), UnityEngine.Random.Range(yRange.x, yRange.y), targetBase.position.z), Quaternion.identity);
			obj.transform.localScale = Vector3.one * UnityEngine.Random.Range(1f, 3f);
			created++;
			if (created < 10) AudioPlayer.PlayOneShot_Static(popSound, (10 - created) / 10f);
			if (created > 30) CancelInvoke();
			yield return new WaitForSeconds(1.006f / created);//summed up gives about 5 seconds total
		}
		Instantiate(finalEffect, targetBase.position, Quaternion.identity);
		//maybe a booming sound?

		// sr.sprite = destroyedBaseSprite;
	}
	void PlaySound() {
		AudioPlayer.PlayOneShot_Static(baseDestroySound);
	}

}
