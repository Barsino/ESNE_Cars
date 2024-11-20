using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSelector : MonoBehaviour
{
    public GameObject[] cars;
    int index = 0;
    private int rotationSpeed = 20;

    [HideInInspector]
    public GameObject currentCar;

    public bool rotate = true;

    private void Start()
    {
        cars[index].SetActive(true);
    }

    private void Update()
    {
        if(rotate)
        {
            transform.rotation = transform.rotation * Quaternion.Euler(0, rotationSpeed * Time.deltaTime, 0);
            currentCar = cars[index];
        }
    }

    public void Next(int dir)
    {
        cars[index].SetActive(false);

        index += dir;

        if(index < 0) { index = cars.Length - 1; }
        if(index > cars.Length - 1) { index = 0; }

        cars[index].SetActive(true);
    }

    public void IsMenu(bool isMenu)
    {
        rotate = isMenu;
    }
}
