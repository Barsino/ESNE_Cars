using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public class Client_02 : MonoBehaviour
{
    [Header("UI params")]
    [SerializeField] private TMP_InputField playerName;
    [SerializeField] private TMP_InputField serverAddresInput;
    [SerializeField] private TMP_InputField serverPortInput;
    private string serverAddres;
    private int serverPort;

    [Header("Server params")]
    private TcpClient client;
    private NetworkStream stream;
    private const string updateKey = "UPDATE_CAR";
    private StringBuilder messageBuffer = new StringBuilder();

    [Header("Camera params")]
    [SerializeField] private CarSelector carSelector;
    private GameObject playerCar;
    public Transform startpos;
    private string carId;
    private string carTrString;
    private string carRotationString;
    private string prefabName;

    [SerializeField] Canvas canvas;

    [Header("Camera params")]
    [SerializeField] CameraController cameraController;
    [SerializeField] private float cameraFollowSpeed;
    [SerializeField] private float cameraRotationSpeed;
    [SerializeField] private Vector3 cameraOffset;

    Dictionary<string, GameObject> carsOnLobby = new Dictionary<string, GameObject>();

    [Header("Pause Event")]
    [SerializeField] private UnityEvent onPause;

    [SerializeField] private GameObject spawnPoint;

    void Start()
    {
        serverAddresInput.text = "127.0.0.1";
        serverPortInput.text = "666";
    }

    void Update()
    {
        // Actualizar la IP y el puerto
        serverAddres = serverAddresInput.text;

        if (int.TryParse(serverPortInput.text, out int result))
        {
            serverPort = result;
        }

        // Leer los mensajes del servidor
        if (stream != null && stream.DataAvailable)
        {
            byte[] data = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(data, 0, client.ReceiveBufferSize);
            string message = Encoding.ASCII.GetString(data, 0, bytesRead);

            messageBuffer.Append(message);

            string bufferString = messageBuffer.ToString();
            int delimiterIndex;

            while((delimiterIndex = bufferString.IndexOf("\n")) != -1)
            {
                string completeMessage = bufferString.Substring(0, delimiterIndex).Trim();
                bufferString = bufferString.Substring(delimiterIndex + 1);
                ProcessMessage(completeMessage);
            }

            messageBuffer.Clear();
            messageBuffer.Append(bufferString);
        }

        if(playerCar != null)
        {
            SendMessageToServer(UpdateCarMessage());
        }

        Pause();
    }

    public void StartConexion()
    {
        // Conectar al servidor cuando se inicie el juego
        client = new TcpClient(serverAddres, serverPort);
        stream = client.GetStream();
    }
    void ProcessMessage(string message)
    {
        // Dividir el mensaje en comando y datos
        string[] parts = message.Split(' ');
        string command = parts[0];
        string[] args = parts.Skip(1).ToArray();

        // Procesar el comando
        switch (command)
        {
            case updateKey:
                //// Actualizar la posición y rotación del coche
                string carId = args[0];
                string carName = args[1];

                if(carsOnLobby.ContainsKey(carId))
                {
                    Vector3 position = new Vector3(float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4]));
                    Quaternion rotation = new Quaternion(float.Parse(args[5]), float.Parse(args[6]), float.Parse(args[7]), float.Parse(args[8]));
                    UpdateCar(carsOnLobby[carId], carId, position, rotation);
                }
                else
                {
                    for(int i = 0; i < carSelector.cars.Length; i++)
                    {
                        if(carName == carSelector.cars[i].name || carName == carSelector.cars[i].name + "(Clone)")
                        {
                            GameObject newCar = Instantiate(carSelector.cars[i]);
                            newCar.GetComponent<CarController>().ID = carId;
                            carsOnLobby.Add(carId, newCar);
                            newCar.SetActive(true);
                            break;
                        }
                    }
                }


                //Debug.Log("Updating Car: " + carId + ", " + position + ", " + rotation);
                break;
                // Aquí puedes agregar más comandos si es necesario
        }
    }

    void UpdateCar(GameObject car, string carId, Vector3 position, Quaternion rotation)
    {
        car.transform.position = position;
        car.transform.rotation = rotation;
    }

    public void SendMessageToServer(string message)
    {
        // Enviar un mensaje al servidor
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    public void OnApplicationQuit()
    {
        // Cerrar la conexión cuando se cierre el juego
        stream = null;
        playerCar = null;
        client.Close();
    }

    public void StartPlaying()
    {
        playerCar = Instantiate(carSelector.currentCar);
        carsOnLobby.Add(playerCar.GetComponent<CarController>().ID, playerCar);
        carSelector.currentCar.SetActive(false);
        playerCar.AddComponent<PlayerController>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        cameraController.ChangeTarget(playerCar.transform, cameraFollowSpeed, cameraRotationSpeed, cameraOffset);

        playerCar.transform.position = spawnPoint.transform.position;

        playerCar.GetComponent<PlayerController>().start = true;
    }

    string UpdateCarMessage()
    {
        carId = playerCar.GetComponent<CarController>().ID;
        prefabName = playerCar.name;
        carTrString = playerCar.transform.position.ToString("F2").Replace(",", "").Trim(new char[] { '(', ')' });
        carRotationString = playerCar.transform.rotation.ToString("F2").Replace(",", "").Trim(new char[] { '(', ')' });

        return updateKey + " " + carId + " " + prefabName + " " + carTrString + " " + carRotationString + "\n";
    }

    void Pause()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            onPause.Invoke();
        }
    }

    public void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }
}
