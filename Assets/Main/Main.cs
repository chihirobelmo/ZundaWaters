using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour {
    [SerializeField] public GameObject player;
    static public GameObject clientPlayer;

	// Use this for initialization
	void Start ()
    {
        clientPlayer = Instantiate(player);
	}
	
	// Update is called once per frame
	void Update () {
	}
}
