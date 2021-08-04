using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
	public Transform target;

	void Start()
	{
		target = GameObject.FindGameObjectWithTag ("MainCamera").transform;
	}

	void Update()
	{
		Vector3 targetPosition = new Vector3 (target.position.x, this.transform.position.y, target.position.z);
		this.transform.LookAt (targetPosition);
	}
}