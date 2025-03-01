using System.Collections.Generic;
using UnityEngine;

public class BalloonAnimation : MonoBehaviour {
	public Material wiggleMat;
	public Transform bodyHolder;
	public SpriteRenderer highlight, shadow, body, outline;


	float distortionMax, rotationRate, wiggleRate;

	Material outlineMat;
	Material hsMat; // highlight and shadow mat.
	Material bodyMat;
	public void InitilizeAnimations(Color c) {
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
	}

	float wiggleProgress = 0;
	void Update() {
		bodyHolder.localRotation *= Quaternion.Euler(0, 0, Time.deltaTime * rotationRate);
		hsMat.SetFloat("_Rotation", bodyHolder.localEulerAngles.z);


		wiggleProgress += wiggleRate * Time.deltaTime;
		wiggleProgress %= 2f * Mathf.PI;
		float inputVal = Mathf.Sin(wiggleProgress) * distortionMax;

		outlineMat.SetFloat("_MaxDistortion", inputVal);
		hsMat.SetFloat("_MaxDistortion", inputVal);
		bodyMat.SetFloat("_MaxDistortion", inputVal);
	}


	public GameObject collisionEffect, baseCollisionEffect;

	public void CollisionEffect() {
		int ops = collisionEffect.transform.childCount;
		GameObject newObj = Instantiate(collisionEffect, transform.position, Quaternion.identity);
		newObj.transform.localScale = transform.localScale;
		for (int i = 0; i < 5; i++) {
			int target = Random.Range(0, ops);
			newObj.transform.GetChild(target).gameObject.SetActive(true);
		}
	}
	public void BaseCollisionEffect() {
		CollisionEffect();
		GameObject newObj = Instantiate(baseCollisionEffect, transform.position, Quaternion.identity);
		newObj.transform.localScale = transform.localScale;
	}



}
