using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCountDown : MonoBehaviour {

    private GameManagerScript GMS;
	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void setCountDown()
    {
        GMS = GameObject.Find("GameManager").GetComponent<GameManagerScript>();
        GMS.CountdownDone = true;
    }
}
