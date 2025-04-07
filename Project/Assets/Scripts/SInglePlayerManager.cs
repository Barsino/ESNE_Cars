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
        // Cambiar canvas a Overlay
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Establecer vueltas
        totalLaps = (int)lapsSlider.value;

        // Establecer oponentes
        oponents = (int)oponentsSlider.value;
        List<GameObject> availableCars = new List<GameObject>(carSelector.cars);
        availableCars.Remove(carSelector.currentCar); // evitar duplicar coche del jugador

        // Instanciar coche del jugador en la última posición de la parrilla
        Vector3 playerPos = gridPositions[oponents].position + new Vector3(0f, 0f, -1.7f);
        Quaternion playerRot = Quaternion.LookRotation(gridPositions[oponents].forward);
        playerCar = Instantiate(carSelector.currentCar, playerPos, playerRot);
        playerCar.SetActive(true);
        playerCar.AddComponent<PlayerController>();

        // Posicionar cámara en el coche del jugador
        cameraController.ChangeTarget(playerCar.transform, cameraFollowSpeed, cameraRotationSpeed, cameraOffset);

        // Mostrar vueltas iniciales
        laps.text = "Laps  " + playerCar.GetComponent<CarController>().lap.ToString() + " / " + totalLaps;

        // Instanciar oponentes
        for (int i = 0; i < oponents; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableCars.Count);
            GameObject selectedCarPrefab = availableCars[randomIndex];

            Vector3 opponentPos = gridPositions[i].position + new Vector3(0f, 0f, -1.7f);
            Quaternion opponentRot = Quaternion.LookRotation(gridPositions[i].forward);

            GameObject opponentCar = Instantiate(selectedCarPrefab, opponentPos, opponentRot);
            opponentCar.SetActive(true);
            opponentCar.AddComponent<AIController>();

            selectedCars.Add(opponentCar);
            availableCars.RemoveAt(randomIndex);
        }

        // Mostrar posición inicial del jugador
        positions.text = "Pos  " + (oponents + 1) + " / " + (oponents + 1);

        // Agregar todos los coches a la lista de la carrera
        foreach (GameObject car in selectedCars)
            carsInRace.Add(car);
        carsInRace.Add(playerCar);

        // Iniciar cuenta regresiva
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
