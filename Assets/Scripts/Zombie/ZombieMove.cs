using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieMove : MonoBehaviour
{

    public float speed = 20f;
	// Use this for initialization
    [HideInInspector]
    public int row;
	
	// Update is called once per frame
	void Update ()
	{
		transform.Translate(-speed*Time.deltaTime,0,0);
	}
}
