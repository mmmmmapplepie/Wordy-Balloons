using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HarmonicOscillator : MonoBehaviour {
	public float damping, springConstant;
	public float driveMagnitude, driveFrequency;
	public float maxPos, minPos;
	public float elasticity;

	List<float> ImpulsesToAdd = new List<float>();
	public float velocity { get; private set; }
	public float position { get; private set; }
	public void AddImpulse(float i) {
		ImpulsesToAdd.Add(i);
	}
	public void SetDrivingFunction(Func<float> fn) {
		driveFunction = fn;
	}
	Func<float> driveFunction;
	Vector3 basePos;
	void Start() {
		basePos = transform.position;
	}
	void Update() {
		Oscillate();
	}
	void Oscillate() {
		AddForces();
		AddImpulses();
		AddDisplacement();
		CheckForWalls();
		SetObjectPos();
	}

	float DefaultDriveFunction() {
		return driveMagnitude * Mathf.Cos(Time.time * driveFrequency * 2f * Mathf.PI);
	}

	void AddForces() {
		if (driveFunction == null) driveFunction = DefaultDriveFunction;
		float accel = -springConstant * position + DefaultDriveFunction() - damping * velocity;
		velocity += accel * Time.deltaTime;

	}
	void AddImpulses() {
		velocity += ImpulsesToAdd.Sum();
		ImpulsesToAdd.Clear();
	}
	void AddDisplacement() {
		position += velocity * Time.deltaTime;
	}
	void CheckForWalls() {
		bool? maxDir = null;
		if (velocity > 0 && position > maxPos) {
			maxDir = true;
		} else if (velocity < 0 && position < minPos) {
			maxDir = false;
		}
		if (maxDir == null) return;
		(float excess, float remainderRatio) = GetExcessAndRemainder((bool)maxDir);
		int bounces = GetBounceFactor(excess);
		velocity *= Mathf.Pow(-1 * elasticity, bounces);
		SetBouncePosition(bounces, (bool)maxDir, remainderRatio);
	}

	(float, float) GetExcessAndRemainder(bool maxDir) {
		float excess = maxDir ? Mathf.Abs(position - maxPos) : Mathf.Abs(position - minPos);
		return (excess, excess % (maxPos - minPos));
	}
	int GetBounceFactor(float excess) {
		return Mathf.FloorToInt(excess / (maxPos - minPos)) + 1;
	}
	void SetBouncePosition(int bounces, bool maxDir, float remainder) {
		float realRemainder = (bounces % 2 != 0) == (bool)maxDir ? 1f - remainder : remainder;
		position = Mathf.Lerp(minPos, maxPos, realRemainder);
	}

	void SetObjectPos() {
		transform.position = basePos + Vector3.up * position;
	}


}
