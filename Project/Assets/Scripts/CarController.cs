using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [HideInInspector]
    public WheelController[] frontWheels = new WheelController[2];
    [HideInInspector]
    public WheelController[] rearWheels = new WheelController[2];
    [HideInInspector]
    public WheelController[] wheels = new WheelController[4];

    public new Rigidbody rigidbody;
    private Transform centerOfMass;


    public float goTorque = 1000f;
    public float brakeTorque = 2000f;
    public float maxSteerAngle = 30f;

    public enum CarType
    {
        FWD,
        RWD,
        AWD
    }
    public CarType carType = CarType.AWD;

    public float antiRoll = 5000f;

    private float[] skidValues = new float[4];
    public AudioSource skidSound;
    public float skidThreshold = 0.1f;

    public AudioSource engineSound;
    public float gearLength = 3f;
    public float currentGearSpeed { get { return rigidbody.velocity.magnitude * gearLength; } }
    public float maxGearSpeed = 300f;
    public int numGears = 5;
    private float gearProp = 1f;
    private float rpm;
    private int currentGear = 1;
    private float currentGearProp;
    public float engineMinSoundPitch = 1f;
    public float engineMaxSoundPitch = 6f;

    public float maxSpeed = 100f;

    public float actualSpeed
    {
        get { return Mathf.Floor(rigidbody.velocity.magnitude * 3.6f); }
    }

    public ParticleSystem smokePrefab;
    private ParticleSystem[] skidSmokes = new ParticleSystem[4];

    public Transform skidTrailPrefab;
    private Transform[] skidTrails = new Transform[4];

    public GameObject[] breakLights;

    [HideInInspector]
    public int lapCount = 0;
    [HideInInspector]
    public int lap = 1;
    private List<GameObject> lapsColliders = new List<GameObject>();
    [HideInInspector]
    public string lapColliderName;
    [HideInInspector]
    public float posFactor;

    public string ID;

    public float stabilityFactor = 0.3f;
    public float tractionControlFactor = 0.5f;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();

        centerOfMass = transform.Find("CenterOfMass");
        if (centerOfMass)
            rigidbody.centerOfMass = centerOfMass.localPosition;

        // get the wheels references
        frontWheels[0] = transform.Find("FrontLeftWheel").GetComponent<WheelController>();
        frontWheels[1] = transform.Find("FrontRightWheel").GetComponent<WheelController>();
        rearWheels[0] = transform.Find("RearLeftWheel").GetComponent<WheelController>();
        rearWheels[1] = transform.Find("RearRightWheel").GetComponent<WheelController>();

        wheels[0] = frontWheels[0];
        wheels[1] = frontWheels[1];
        wheels[2] = rearWheels[0];
        wheels[3] = rearWheels[1];

        if(GameObject.Find("GameManager"))
        {
            lapsColliders = new List<GameObject>(GameObject.Find("GameManager").GetComponent<SInglePlayerManager>().lapsColliders);
        }

        ID = Guid.NewGuid().ToString();
    }

    private void Start()
    {
        for(int i = 0; i < skidSmokes.Length; i++)
        {
            skidSmokes[i] = Instantiate(smokePrefab);
            skidSmokes[i].Stop();
        }

        engineSound.Play();
    }

    void FixedUpdate()
    {
        GroundWheels(frontWheels[0].wheelCollider, frontWheels[1].wheelCollider);
        GroundWheels(rearWheels[0].wheelCollider, rearWheels[1].wheelCollider);

        CheckSkid();
        CalculateEngineSound();

        CalculateLaps();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R)) { ResetCar(); }
    }

    private void CalculateLaps()
    {
        if(lapsColliders.Count > 0)
        {
            if (lapColliderName == lapsColliders[lapCount].name)
            {
                lapCount++;
                posFactor++;
            }

            if (lapCount >= lapsColliders.Count)
            {
                lap++;
                lapCount = 0;
            }
        }
    }

    public void ApplyTorque(float torqueInput)
    {
        float torque = goTorque * torqueInput;

        if (actualSpeed < maxSpeed)
        {
            switch (carType)
            {
                case CarType.FWD:
                    foreach (WheelController wheel in frontWheels)
                    {
                        wheel.wheelCollider.motorTorque = torque;                       
                    }
                    break;
                case CarType.RWD:
                    foreach (WheelController wheel in rearWheels)
                    {
                        wheel.wheelCollider.motorTorque = torque;
                    }
                    break;
                case CarType.AWD:
                    foreach (WheelController wheel in wheels)
                    {
                        wheel.wheelCollider.motorTorque = torque;
                    }
                    break;
            }
        }

        else
        {
            foreach(WheelController wheel in wheels)
            {
                wheel.wheelCollider.motorTorque = 0f;
            }
        }
    }

    public void ApplyBrake(float brakeInput)        
    {
        float brake = brakeTorque * brakeInput;
        foreach (WheelController wheel in wheels)
        {
            wheel.wheelCollider.brakeTorque = brake;
        }

        if(brakeInput > float.Epsilon)
        {
            foreach(GameObject breakLight in breakLights) { breakLight.SetActive(true); }
        }
        else
        {
            foreach(GameObject breakLight in breakLights) { breakLight.SetActive(false); }
        }
    }

    public void ApplySteering(float steerInput)
    {
        float steer = maxSteerAngle * steerInput;
        foreach (WheelController wheel in frontWheels)
        {
            wheel.wheelCollider.steerAngle = steer;
        }
    }

    public void ResetCar()
    {
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        foreach (WheelController wheel in frontWheels)
            wheel.wheelCollider.brakeTorque = Mathf.Infinity;

        transform.position += Vector3.up * 2f;
        transform.rotation = Quaternion.LookRotation(transform.forward);
    }

    private void GroundWheels(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        WheelHit hit;
        float leftTravel = 1f, rightTravel = 1f;

        // calculate the proportions of how grounded each wheel is
        bool leftGrounded = leftWheel.GetGroundHit(out hit);
        if (leftGrounded)
            leftTravel = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;

        bool rightGrounded = rightWheel.GetGroundHit(out hit);
        if (rightGrounded)
            rightTravel = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;

        float antiRollForce = (leftTravel - rightTravel) * antiRoll;

        if (leftGrounded)
            rigidbody.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);

        if (rightGrounded)
            rigidbody.AddForceAtPosition(rightWheel.transform.up * -antiRollForce, rightWheel.transform.position);
    }

    private void CheckSkid()
    {
        int wheelsSkidding = 0; // number of wheels skidding

        for (int i = 0; i < wheels.Length; i++)
        {
            WheelHit wheelHit;
            wheels[i].wheelCollider.GetGroundHit(out wheelHit);

            float forwardSlip = Mathf.Abs(wheelHit.forwardSlip);
            float sidewaysSlip = Mathf.Abs(wheelHit.sidewaysSlip);

            if (forwardSlip >= skidThreshold || sidewaysSlip >= skidThreshold)
            {
                wheelsSkidding++;
                skidValues[i] = forwardSlip + sidewaysSlip;

                // Smoke
                skidSmokes[i].transform.position = wheels[i].wheelCollider.transform.position - wheels[i].wheelCollider.transform.up * wheels[i].wheelCollider.radius;
                skidSmokes[i].Emit(1);

                // Trail
                StartSkidTrail(i);

            }
            else
            {
                skidValues[i] = 0f;
                EndSkidTrail(i);
            }
        }

        // skidding sound
        if (wheelsSkidding == 0 && skidSound.isPlaying)
        {
            skidSound.Stop();
            
        }
        else if (wheelsSkidding > 0)
        {
            // update the drifting volume
            skidSound.volume = (float)wheelsSkidding / wheels.Length;

            skidSound.panStereo = -skidValues[0] + skidValues[1] - skidValues[2] + skidValues[3];

            if (!skidSound.isPlaying && skidSound.isActiveAndEnabled)
                skidSound.Play();
        }
    }

    private void StartSkidTrail(int i)
    {
        if (skidTrails[i] == null)
        {
            skidTrails[i] = Instantiate(skidTrailPrefab);
        }

        skidTrails[i].parent = wheels[i].transform;
        skidTrails[i].localRotation = Quaternion.Euler(90f, 0f, 0f);
        skidTrails[i].localPosition = -Vector3.up * wheels[i].wheelCollider.radius;
    }

    private void EndSkidTrail(int i)
    {
        if (skidTrails[i] == null)
        {
            return;
        }

        Transform skidTrail = skidTrails[i];
        skidTrails[i] = null;
        skidTrail.parent = null;
        skidTrail.rotation = Quaternion.Euler(90f, 0f, 0f);
        Destroy(skidTrail.gameObject, 30);
    }

    private void CalculateEngineSound()
    {
        float speedProp = currentGearSpeed / maxGearSpeed;
        float targetFactor = Mathf.InverseLerp(gearProp * currentGear, gearProp * (currentGear + 1), speedProp);
        currentGearProp = Mathf.Lerp(currentGearProp, targetFactor, Time.deltaTime * 5f);

        float gearNumFactor = currentGear / (float)numGears;
        rpm = Mathf.Lerp(gearNumFactor, 1, currentGearProp);

        float upperGearMax = gearProp * (currentGear + 1);
        float downGearMax = gearProp * currentGear;

        if(currentGear > 0 && speedProp < downGearMax) { currentGear--; }

        if(currentGear < (numGears - 1) && speedProp > upperGearMax) { currentGear++; }

        engineSound.pitch = Mathf.Lerp(engineMinSoundPitch, engineMaxSoundPitch, rpm) * 0.25f;
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < skidValues.Length; i++)
        {
            if (wheels[i])
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(wheels[i].transform.position, skidValues[i]);
            }
        }
    }

}
