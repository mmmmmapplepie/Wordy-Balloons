using System;
using System.Collections.Generic;
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

	public static event Action BaseHPSet;
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		if (NetworkManager.Singleton.IsServer) SetBaseHP(DefaultMaxHP, DefaultMaxHP);
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
}
