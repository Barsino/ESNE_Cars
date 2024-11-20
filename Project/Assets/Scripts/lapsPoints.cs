using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lapsPoints : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Car")
        {
            other.gameObject.GetComponentInParent<CarController>().lapColliderName = gameObject.name;
        }
    }
}
