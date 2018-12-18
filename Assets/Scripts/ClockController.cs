using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClockController : MonoBehaviour {
	
	public Text clock;
	void Update () {
		clock.text = DateTime.Now.ToString("hh:mm tt");
	}
}
