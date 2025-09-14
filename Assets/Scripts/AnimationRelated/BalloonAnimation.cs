using System.Collections.Generic;
using UnityEngine;

public class BalloonAnimation : MonoBehaviour {
  public Material wiggleMat;
  public Transform bodyHolder, outlineHolder;
  public SpriteRenderer highlight, shadow, body, outline;


  float distortionMax, rotationRate, wiggleRate;

  Material outlineMat;
  Material hsMat;
  Material bodyMat;

  public void InitilizeAnimations(Color c) {
    foreach (Transform t in transform) {
      t.gameObject.SetActive(true);
    }

    Material wiggleMatCopy = new Material(wiggleMat);

    int bumpsCount = Random.Range(2, 10);
    wiggleMatCopy.SetFloat("_Bumps", bumpsCount);

    outlineMat = new Material(wiggleMatCopy);
    hsMat = new Material(wiggleMatCopy); // highlight and shadow mat.
    bodyMat = new Material(wiggleMatCopy);
    bodyMat.SetColor("_Color", c);

    highlight.material = hsMat;
    shadow.material = hsMat;
    body.material = bodyMat;
    outline.material = outlineMat;

    distortionMax = Random.Range(0.5f, 1f);
    rotationRate = Random.Range(0, 90f);
    rotationRate *= Mathf.Sign(Random.Range(-1f, 1f));
    wiggleRate = Random.Range(2f, 8f);

    GameObject newObj = Instantiate(fireEffect, transform.position, Quaternion.identity);
    newObj.transform.localScale = transform.localScale;
    AudioPlayer.PlayOneShot_Static(GetRandomClipFromList(fireSounds), Random.Range(0.4f, 0.5f) * VolumeControl.GetEffectVol(), Random.Range(0.9f, 1.2f));
  }

  float wiggleProgress = 0;
  void Update() {
    bodyHolder.localRotation *= Quaternion.Euler(0, 0, Time.deltaTime * rotationRate);
    outlineHolder.localRotation = bodyHolder.localRotation;
    hsMat.SetFloat("_Rotation", bodyHolder.localEulerAngles.z);


    wiggleProgress += wiggleRate * Time.deltaTime;
    wiggleProgress %= 2f * Mathf.PI;
    float inputVal = Mathf.Sin(wiggleProgress) * distortionMax;

    outlineMat.SetFloat("_MaxDistortion", inputVal);
    hsMat.SetFloat("_MaxDistortion", inputVal);
    bodyMat.SetFloat("_MaxDistortion", inputVal);
  }


  public GameObject collisionEffect, baseCollisionEffect, fireEffect;
  public List<AudioClip> collisionSounds, fireSounds, collisionOnBaseSounds;
  public void DestroyEffect(bool onBase) {
    AudioPlayer.PlayOneShot_Static(GetRandomClipFromList(onBase ? collisionOnBaseSounds : collisionSounds), (onBase ? 1f : 0.5f) * VolumeControl.GetEffectVol() * Random.Range(0.8f, 1f), Random.Range(0.8f, 1.3f));
    int ops = collisionEffect.transform.childCount;
    GameObject newObj = Instantiate(collisionEffect, transform.position, Quaternion.identity);
    newObj.transform.localScale = transform.localScale;
    for (int i = 0; i < 5; i++) {
      int target = Random.Range(0, ops);
      newObj.transform.GetChild(target).gameObject.SetActive(true);
    }
  }
  public void DestroyOnBaseEffect() {
    DestroyEffect(true);
    GameObject newObj = Instantiate(baseCollisionEffect, transform.position, Quaternion.identity);
    newObj.transform.localScale = transform.localScale;
  }

  AudioClip GetRandomClipFromList(List<AudioClip> list) {
    int len = list.Count;
    if (len == 0) return null;
    return list[Random.Range(0, len)];
  }



}
