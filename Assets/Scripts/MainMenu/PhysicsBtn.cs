using System.Collections.Generic;
using UnityEngine;

public class PhysicsBtn : MonoBehaviour {
	Rigidbody2D rb;
	CircleCollider2D circCol;
	BoxCollider2D boxcol;
	public float impulseAmt;
	public float collSizeMultiplier = 0.8f;

	void Start() {
		Vector2 size = GetComponent<RectTransform>().rect.size;
		TryGetComponent<Rigidbody2D>(out rb);
		TryGetComponent<CircleCollider2D>(out circCol);
		TryGetComponent<BoxCollider2D>(out boxcol);

		if (circCol != null) circCol.radius = (size.x / 2f) * collSizeMultiplier;
		if (boxcol != null) boxcol.size = size;

		if (rb == null) return;
		rb.AddForce((new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f))).normalized * impulseAmt);
	}

	public float maxSpeed, minSpeed;
	static float maxSpeedDone;
	void Update() {
		if (rb == null) return;

		if (rb.velocity.magnitude > maxSpeed) rb.velocity = rb.velocity.normalized * maxSpeed;
		if (rb.velocity.magnitude < minSpeed) rb.AddForce((new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f))).normalized * impulseAmt);
	}


	public static event System.Action<Vector3> CollisionEffect;
	void OnCollisionEnter2D(Collision2D other) {
		int val = Random.Range(0, 100);
		if (val < 50) {
			CollisionEffect?.Invoke(other.GetContact(0).point);
		}
	}



}
