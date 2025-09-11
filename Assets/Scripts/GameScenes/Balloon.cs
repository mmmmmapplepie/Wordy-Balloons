using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Balloon : NetworkBehaviour {
  [HideInInspector] public int tempPower;
  [HideInInspector] public Team tempTeam;
  [HideInInspector] public Color tempColor;
  [HideInInspector] public NetworkVariable<float> flyProgress = new NetworkVariable<float>(0f);
  [HideInInspector] public NetworkVariable<int> power;
  [HideInInspector] public NetworkVariable<Team> balloonTeam;
  [HideInInspector] public NetworkVariable<Color> balloonColor;
  Rigidbody2D rb;

  NetworkVariable<float> realFlightHeight = new NetworkVariable<float>(4f);
  NetworkVariable<float> flyTime = new NetworkVariable<float>(5f);
  void Awake() {
    rb = GetComponent<Rigidbody2D>();
    powerTxt = transform.GetComponentInChildren<TextMeshPro>();
  }

  TextMeshPro powerTxt;
  public BalloonAnimation anim;
  public static event System.Action<Team, Balloon> BalloonCreated;
  public static event System.Action<bool, Balloon> BalloonDestroyed;
  public override void OnNetworkSpawn() {
    base.OnNetworkSpawn();
    power.OnValueChanged += PowerChanged;
    GameStateManager.GameResultSetEvent += GameSet;
    if (NetworkManager.Singleton.IsServer) {
      realFlightHeight.Value = Random.Range(BalloonManager.FlightHeightMin, BalloonManager.FlightHeightMax);
      power.Value = tempPower;
      balloonTeam.Value = tempTeam;
      balloonColor.Value = tempColor;
      startP.Value = startPos;
      endP.Value = endPos;
    }
    PowerChanged(0, power.Value);
    SetPosition(0);
    UpdateScale();
    BalloonCreated?.Invoke(balloonTeam.Value, this);
    anim.InitilizeAnimations(balloonColor.Value);
  }
  public override void OnNetworkDespawn() {
    power.OnValueChanged -= PowerChanged;
    GameStateManager.GameResultSetEvent -= GameSet;
    base.OnNetworkDespawn();
  }

  void Update() {
    UpdateFlyProgress();
  }
  float localProgress = 0f;
  float smoothingSpeedMultiplier = 2f;
  float maxError = 0.05f;
  float currSetProgress = 0f;

  void UpdateFlyProgress() {
    if (NetworkManager.Singleton.IsServer) {
      ServerPosUpdate();
    } else {
      ClientPosUpdate();
    }
  }
  float minScale = 0.7f, maxScale = 2f;
  void UpdateScale() {
    transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, power.Value / 15f);
  }
  NetworkVariable<Vector3> startP = new NetworkVariable<Vector3>(), endP = new NetworkVariable<Vector3>();
  [HideInInspector] public Vector3 startPos, endPos;
  void ServerPosUpdate() {
    flyTime.Value = BalloonManager.Flytime;
    flyProgress.Value = Mathf.Clamp01(flyProgress.Value + Time.deltaTime / flyTime.Value);
    SetPosition(flyProgress.Value);
    if (flyProgress.Value >= 1f) {
      HitBase();
    }
  }
  void ClientPosUpdate() {
    float deltaDiff = Time.deltaTime / flyTime.Value;
    if (Mathf.Abs(localProgress - flyProgress.Value) > maxError) {
      if (flyProgress.Value > localProgress) {
        localProgress = flyProgress.Value;
      } else {
        deltaDiff /= (smoothingSpeedMultiplier * 1.25f);
      }
    } else {
      localProgress += deltaDiff;
    }
    float currLocDiff = Mathf.Abs(currSetProgress - localProgress);
    if (currLocDiff > maxError) {
      deltaDiff *= smoothingSpeedMultiplier;
    }

    float diffMin = Mathf.Min(deltaDiff, currLocDiff);
    SetPosition(currSetProgress + Mathf.Sign(localProgress - currSetProgress) * diffMin);
  }

  void SetPosition(float progress) {
    progress = Mathf.Clamp01(progress);
    currSetProgress = progress;
    float realProgress = progress;
    if (balloonTeam.Value != BalloonManager.team) realProgress = 1 - progress;
    float p = ProgressBehaviour(realProgress);
    if (progress == 0) {
      transform.position = GetProgressPosition(startP.Value, endP.Value, p);
      return;
    }
    rb.MovePosition(GetProgressPosition(startP.Value, endP.Value, p));
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
    if (NetworkManager.Singleton.IsServer) GetComponent<NetworkObject>().Despawn(true);
  }

  [ClientRpc]
  void DestroyEffectClientRpc(bool onBase) {
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


  void GameSet(GameState r) {
    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
    DestroyBalloon();
  }



}
