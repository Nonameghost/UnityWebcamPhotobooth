using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotation : MonoBehaviour {

	public Vector3 axis;

	public float angle;

	public bool randomize = false;

	// Use this for initialization
	void Start () {
		if (randomize)
		{
			angle = Random.Range (10.0f, 180.0f);
		}
	}
	
	// Update is called once per frame
	void Update () {
			transform.RotateAround(transform.position, axis, angle * Time.deltaTime);
	}
}
