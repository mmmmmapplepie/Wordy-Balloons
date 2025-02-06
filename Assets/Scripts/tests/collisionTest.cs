using System.Collections.Generic;
using UnityEngine;

public class collisionTest : MonoBehaviour {
	public int hp = 5;
	public int team = 1;
	void OnTriggerEnter2D(Collider2D other) {
		if (hp <= 0) return;
		if (other.gameObject.TryGetComponent<collisionTest>(out collisionTest s)) {
			if (s.hp <= 0 || s.team == team) return;
			hp -= s.SendAndReceiveDamage(hp);
		}
	}

	public int SendAndReceiveDamage(int dmg) {
		print("dmg");
		int initialHP = hp;
		hp -= dmg;
		return initialHP;
	}
}
