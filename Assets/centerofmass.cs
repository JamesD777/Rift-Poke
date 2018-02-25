using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class centerofmass : MonoBehaviour {

	public Vector3 com;
	public Rigidbody rb;
	void Start() {
		rb = GetComponent<Rigidbody>();
		com.Set (0.0f, 0.0f, 0.49f);
		rb.centerOfMass = com;
	}
}
