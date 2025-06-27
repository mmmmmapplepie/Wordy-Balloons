using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BaseManager : NetworkBehaviour {

	const int DefaultMaxHP = 50, TutorialMaxHP = 15;
	public static NetworkVariable<int> team1HP = new NetworkVariable<int>(DefaultMaxHP);
	public static NetworkVariable<int> team2HP = new NetworkVariable<int>
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

		Balloon.BalloonCreated -= BallonCreated;
		base.OnDestroy();
	}

	public static event Action BaseHPSet;
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		if (NetworkManager.Singleton.IsServer) {
			int targetHP = GameData.PlayMode == PlayModeEnum.Tutorial ? TutorialMaxHP : DefaultMaxHP;
			SetBaseHP(targetHP, targetHP);
		}
		BaseHPSet?.Invoke();

		team1HP.OnValueChanged += BaseHP1Changed;
		team2HP.OnValueChanged += BaseHP2Changed;
	}
	public override void OnNetworkDespawn() {
		team1HP.OnValueChanged -= BaseHP1Changed;
		team2HP.OnValueChanged -= BaseHP2Changed;
		base.OnNetworkDespawn();
	}
	public static void SetBaseHP(int t1HP, int t2HP) {
		team1MaxHP.Value = t1HP;
		team2MaxHP.Value = t2HP;
		team1HP.Value = t1HP;
		team2HP.Value = t2HP;
	}
	public static event Action<Team> TeamLose;
	public static event Action<Team, int> BaseTakenDamage;

	[ServerRpc(RequireOwnership = false)]
	public void DamageBaseServerRpc(Team team, int dmg) {
		DamageBase(team, dmg);
	}
	public static void DamageBase(Team teamBaseToDamage, int dmg) {
		NetworkVariable<int> target;
		if (teamBaseToDamage == Team.t1) {
			target = team1HP;
		} else {
			target = team2HP;
		}
		target.Value -= dmg;
		BaseTakenDamage?.Invoke(teamBaseToDamage, target.Value);
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

	void BallonCreated(Team t, Balloon balloon) {
		Transform target = awayBase;
		if (t == BalloonManager.team) {
			target = homeBase;
		}
		target.GetComponent<Animator>().Play("CannonFire");
	}








	public GameObject splashEffect, finalEffect;
	public const float BaseDestroyAnimationTime = 9f;
	public Sprite baseMain_Destroyed, basePipe_Destroyed, baseCannon_Destroyed;
	public AudioClip baseDestroySound, popSound, finalDestroySound;

	void ResultChanged(GameStateManager.GameResult result) {
		if (result == GameStateManager.GameResult.Undecided || result == GameStateManager.GameResult.Draw) return;
		Transform losingBase = homeBase;
		if (result == GameStateManager.GameResult.Team1Win && BalloonManager.team == Team.t1) losingBase = awayBase;
		if (result == GameStateManager.GameResult.Team2Win && BalloonManager.team == Team.t2) losingBase = awayBase;
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
			if (created < 10) AudioPlayer.PlayOneShot_Static(popSound, VolumeControl.GetEffectVol() * (10 - created) / 10f);
			if (created > 30) CancelInvoke();
			yield return new WaitForSeconds(1.006f / created);//summed up gives about 5 seconds total
		}
		GameObject g = Instantiate(finalEffect, targetBase.position, Quaternion.identity);
		g.transform.localScale = 2f * g.transform.localScale;
		AudioPlayer.PlayOneShot_Static(finalDestroySound, VolumeControl.GetEffectVol());
		Destroy(targetBase.GetComponent<Animator>());

		sr.sprite = baseMain_Destroyed;
		targetBase.GetChild(0).GetComponent<SpriteRenderer>().sprite = basePipe_Destroyed;
		targetBase.GetChild(1).GetComponent<SpriteRenderer>().sprite = baseCannon_Destroyed;
	}
	void PlaySound() {
		AudioPlayer.PlayOneShot_Static(baseDestroySound, VolumeControl.GetEffectVol());
	}

}
