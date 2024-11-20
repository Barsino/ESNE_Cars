using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class CanvasCarController : MonoBehaviour
{
    public TMP_Text velocimeter;
    public CarController car;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        velocimeter.text = Math.Floor(car.rigidbody.velocity.magnitude * 3.6f) + " Km/h";
    }
}
