using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SInglePlayerManager : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;

    [Header("Player Car")]
    [SerializeField] private CarSelector carSelector;
    private GameObject playerCar;

    [Header("Camera params")]
    [SerializeField] private float cameraFollowSpeed;
    [SerializeField] private float cameraRotationSpeed;
    [SerializeField] private Vector3 cameraOffset;

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Slider lapsSlider;
    [SerializeField] private Slider oponentsSlider;
    private int totalLaps;
    [SerializeField] private TextMeshProUGUI laps;
    [SerializeField] private TextMeshProUGUI countDown;
    [SerializeField] private TextMeshProUGUI velocity;
    [SerializeField] private Transform platform;

    [Header("Grid position")]
    [SerializeField] private GameObject grid;
    private List<Transform> gridPositions = new List<Transform>();
    private int oponents;
    private List<GameObject> selectedCars = new List<GameObject>();

    [Header("Laps and Race Positions")]
    public List<GameObject> carsInRace = new List<GameObject>();
    public List<GameObject> lapsColliders = new List<GameObject>();
    [SerializeField] private TextMeshProUGUI positions;

    [Header("Race End Event")]
    [SerializeField] private UnityEvent onRaceEnd;




    // Start is called before the first frame update
    void Start()
    {
        // Get grid positions
        for(int i = 0; i < grid.transform.childCount; i++)
        {
            Transform child = grid.transform.GetChild(i);
            gridPositions.Add(child.transform);
        }
    }

    // Update is called once per frame
    void Update()
    {      
        if(playerCar != null)
        {
            // Update laps in canvas
            laps.text = "Laps  " + playerCar.GetComponent<CarController>().lap.ToString() + " / " + totalLaps;

            // Update velocity in canvas
            velocity.text = Math.Floor(playerCar.GetComponent<CarController>().rigidbody.velocity.magnitude * 3.6f) + " Km/h";

            if (playerCar.GetComponent<CarController>().lap >= totalLaps)
            {
                EndRace();
            }
        }

        if (selectedCars.Count != 0)
        {
            // Update positions
            // Sorts the list according to lapsFactor
            carsInRace.Sort((x, y) => y.GetComponent<CarController>().posFactor.CompareTo(x.GetComponent<CarController>().posFactor));

            for(int i = 0; i < carsInRace.Count; i++)
            {
                if (carsInRace[i] == playerCar)
                {
                    positions.text = "Pos  " + (i + 1) + " / " + (oponents + 1);
                }

                if (carsInRace[i].GetComponent<CarController>().lap >= totalLaps)
                {
                    EndRace();
                    return;
                }
            }
        }
    }

    public void StartGame()
    {
        // Set PlayerCar
        playerCar = carSelector.currentCar;
        playerCar.AddComponent<PlayerController>();

        // Change canvas to Overlay
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Look to the player with the camera.
        cameraController.ChangeTarget(playerCar.transform, cameraFollowSpeed, cameraRotationSpeed, cameraOffset);

        // Set laps
        totalLaps = (int)lapsSlider.value;
        laps.text = "Laps  " + playerCar.GetComponent<CarController>().lap.ToString() + " / " + totalLaps;

        // Set oponents
        oponents = (int)oponentsSlider.value;
        List<GameObject> availableCars = new List<GameObject>(carSelector.cars);
        availableCars.Remove(playerCar);

        for(int i = 0; i < oponents; i++)
        {
            int randomCar = UnityEngine.Random.Range(0, availableCars.Count);
            GameObject selectedCar = availableCars[randomCar];
            selectedCar.AddComponent<AIController>();
            selectedCars.Add(selectedCar);
            availableCars.Remove(selectedCar);
        }

        // Set grid positions

        // Player
        playerCar.transform.position = new Vector3(gridPositions[oponents].transform.position.x, 
                                                   gridPositions[oponents].transform.position.y, 
                                                   gridPositions[oponents].transform.position.z - 1.7f);

        playerCar.transform.forward = gridPositions[oponents].transform.forward;
        positions.text = "Pos  " + (oponents + 1) + " / " + (oponents + 1);

        // Oponents
        for(int i = 0; i < oponents; i++)
        {
            selectedCars[i].SetActive(true);
            selectedCars[i].transform.position = new Vector3(gridPositions[i].transform.position.x,
                                                             gridPositions[i].transform.position.y,
                                                             gridPositions[i].transform.position.z - 1.7f);

            selectedCars[i].transform.forward = gridPositions[i].transform.forward;
        }

        // Add all cars avaible (player and AI)
        foreach(GameObject car in selectedCars) { carsInRace.Add(car); }
        carsInRace.Add(playerCar);

        // Start count
        StartCoroutine(CountDownStart(carsInRace));
    }

    void EndRace()
    {
        // Detener los coches
        foreach (GameObject car in carsInRace)
        {
            if (car.GetComponent<AIController>() != null)
            {
                car.GetComponent<AIController>().start = false;
                Destroy(car.GetComponent<AIController>());
            }
            if (car.GetComponent<PlayerController>() != null)
            {
                car.GetComponent<PlayerController>().start = false;
                Destroy(car.GetComponent<PlayerController>());
            }

            car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            car.transform.position = platform.position;
            car.transform.rotation = platform.rotation;
            car.SetActive(false);
        }

        playerCar = null;
        carsInRace.Clear();

        // Change canvas to Overlay
        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        cameraController.CanvasCameraTr();

        carSelector.rotate = true;

        onRaceEnd.Invoke();
    }

    IEnumerator CountDownStart(List<GameObject> carsInRace)
    {
        yield return new WaitForSeconds(1f);
        countDown.text = "3";
        countDown.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);
        countDown.text = "2";

        yield return new WaitForSeconds(1f);
        countDown.text = "1";

        yield return new WaitForSeconds(1f);
        countDown.gameObject.SetActive(false);

        // Activate AI control an Player control
        for(int i = 0; i < carsInRace.Count; i++)
        {
            if (carsInRace[i] == playerCar)
            {
                carsInRace[i].GetComponent<PlayerController>().start = true;
            }
            else
            {
                carsInRace[i].GetComponent<AIController>().start = true;
            }
        }       
    }
}
