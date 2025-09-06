using System.Collections.Generic;
using UnityEngine;

public class ButtonCollisionEffect : MonoBehaviour {
  public GameObject effect;
  void Start() {
    PhysicsBtn.CollisionEffect += Collision;
  }
  void OnDestroy() {
    PhysicsBtn.CollisionEffect -= Collision;
  }

  void Collision(Vector3 pos) {
    float scale = Random.Range(0.5f, 1.5f);
    GameObject o = Instantiate(effect, pos, Quaternion.identity, transform);
    o.transform.localScale = Vector3.one * scale;
  }

}
