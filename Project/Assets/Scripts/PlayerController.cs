using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class PlayerController : MonoBehaviour
{
    private CarController car;

    //[HideInInspector]
    public bool start = false;

    private void Awake()
    {
        car = GetComponent<CarController>();
    }

    private void FixedUpdate()
    {
        float torqueInput = Input.GetAxis("Vertical");
        float brakeInput = Input.GetAxis("Jump");
        float steerInput = Input.GetAxis("Horizontal");

        if(start)
        {
            // torque
            car.ApplyTorque(torqueInput);


            // brake
            car.ApplyBrake(brakeInput);

            // steering
            car.ApplySteering(steerInput);
        }
    }

}
