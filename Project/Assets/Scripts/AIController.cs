using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityStandardAssets.Utility;

[RequireComponent(typeof(CarController))]
public class AIController : MonoBehaviour
{
    public WaypointCircuit circuit;
    public CarController car;

    [SerializeField] private float targetOffset = 10f;
    [SerializeField] private float targetFactor = 0.1f;
    [SerializeField] private float brakeTargetOffset = 35f;
    [SerializeField] private float brakeTargetFactor = 0.1f;

    private float SpeedOffset = 50;
    private float speedFactor = 0.2f;

    public WaypointCircuit.RoutePoint targetPoint;
    public WaypointCircuit.RoutePoint speedPoint;
    public WaypointCircuit.RoutePoint progressPoint;

    public Transform target;
    public Transform brakeTarget;

    [HideInInspector]
    public float progressDistance;
    private int progressNum;
    private Vector3 lastPosition;
    private float speed;
    private float brake = 0f;
    private float torque = 1f;
    private float steer = 0f;

    private Ray[] rays;
    private RaycastHit hit;
    private float rayDistance = 10f;

    [SerializeField] Transform raysSpawn;

    [SerializeField] LayerMask obstacleLayer;
    [SerializeField] LayerMask carsLayer;

    public bool start = false;

    private void Awake()
    {
        if(target == null) { target = transform.Find("Target"); }

        if(brakeTarget == null) { brakeTarget = transform.Find("BrakeTarget"); }

        if(car == null) { car = GetComponent<CarController>(); }
        
        if(circuit == null) { circuit = GameObject.Find("WaypointCircuit").GetComponent<WaypointCircuit>(); }

        if(raysSpawn == null) { raysSpawn = transform.Find("RaySpawn"); }

        obstacleLayer = LayerMask.GetMask("Obstacles");
        carsLayer = LayerMask.GetMask("Cars");

        rays = new Ray[5];
    }
    private void Update()
    {
        speed = car.actualSpeed;

        target.position = circuit.GetRoutePoint(progressDistance + targetOffset + targetFactor * speed).position;
        target.rotation = Quaternion.LookRotation(circuit.GetRoutePoint(progressDistance + SpeedOffset + speedFactor * speed).direction);

        progressPoint = circuit.GetRoutePoint(progressDistance);
        Vector3 progressDelta = progressPoint.position - transform.position;
        if(Vector3.Dot(progressDelta, progressPoint.direction) < 0)
        {
            progressDistance += progressDelta.magnitude * 0.5f;
        }

        brakeTarget.position = circuit.GetRoutePoint(progressDistance + brakeTargetOffset + brakeTargetFactor * speed).position;
        brakeTarget.rotation = Quaternion.LookRotation(circuit.GetRoutePoint(progressDistance + SpeedOffset + speedFactor * speed).direction);

        lastPosition = transform.position;
    }


    private void FixedUpdate()
    {
        // Crear rayos en diferentes direcciones para una mejor detección de obstáculos y coches
        rays[0] = new Ray(raysSpawn.position, transform.forward);
        rays[1] = new Ray(raysSpawn.position, Quaternion.Euler(0, 15, 0) * transform.forward);
        rays[2] = new Ray(raysSpawn.position, Quaternion.Euler(0, -15, 0) * transform.forward);
        rays[3] = new Ray(raysSpawn.position, Quaternion.Euler(0, 30, 0) * transform.forward);
        rays[4] = new Ray(raysSpawn.position, Quaternion.Euler(0, -30, 0) * transform.forward);

        bool obstacleDetected = false;
        bool carDetected = false;

        foreach (Ray ray in rays)
        {
            if (Physics.Raycast(ray, out hit, rayDistance, obstacleLayer))
            {
                obstacleDetected = true;
                AdjustSteeringAndSpeedForObstacle();
                break;
            }
            else if (Physics.Raycast(ray, out hit, rayDistance, carsLayer))
            {
                carDetected = true;
                AdjustSteeringAndSpeedForCar();
                break;
            }
        }

        if (!obstacleDetected && !carDetected)
        {
            AdjustSteeringAndSpeedForPath();
        }

        if (start)
        {
            car.ApplyTorque(torque);
            car.ApplyBrake(brake);
            car.ApplySteering(steer);
        }
    }

    private void AdjustSteeringAndSpeedForObstacle()
    {
        torque = -1f + Time.deltaTime;
        steer = hit.point.x < 0 ? 0.5f : -0.5f;
    }

    private void AdjustSteeringAndSpeedForCar()
    {
        float hitSpeed = hit.collider.GetComponentInParent<CarController>().actualSpeed;
        if (hitSpeed < car.actualSpeed - 15)
        {
            torque = -0.5f + Time.deltaTime;
            steer = hit.point.x < 0 ? 0.5f : -0.5f;
        }
    }

    private void AdjustSteeringAndSpeedForPath()
    {
        Vector3 targetPoint = transform.InverseTransformPoint(target.position);
        float targetAngle = Mathf.Atan2(targetPoint.x, targetPoint.z) * Mathf.Rad2Deg;
        steer = Mathf.Clamp(targetAngle * 0.01f, -1f, 1f) * Mathf.Sign(car.actualSpeed);

        Vector3 brakeTargetPoint = transform.InverseTransformPoint(brakeTarget.position);
        float brakeAngle = Mathf.Atan2(brakeTargetPoint.x, brakeTargetPoint.z) * Mathf.Rad2Deg;

        if (Mathf.Abs(brakeAngle) > 20 && car.actualSpeed > 50)
        {
            torque = -1f + Time.deltaTime;
        }
        else if (Mathf.Abs(brakeAngle) > 25 && car.actualSpeed > 50)
        {
            torque = -0.4f + Time.deltaTime;
        }
        else
        {
            brake = 0f;
            torque = 1f;
        }
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(target.position, 1);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, brakeTarget.position);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(brakeTarget.position, 1);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(circuit.GetRoutePosition(progressDistance), 0.2f);
            Gizmos.DrawLine(transform.position, circuit.GetRoutePosition(progressDistance));
            Gizmos.DrawLine(target.position, target.position + target.forward);

            // Dibujar rayos de colisión con obstáculos en negro
            Gizmos.color = Color.black;
            foreach (Ray ray in rays)
            {
                Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * rayDistance);
            }
        }
    }

}
