using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    public Transform target;
    public Vector3 offset;
    public float followSpeed = 0f;
    public float rotationSpeed = 0f;

    void LateUpdate()
    {
        // follow update
        FollowUpdate(target);

        // look update
        LookUpdate(target);
    }

    public void LookUpdate(Transform target)
    {
        Vector3 lookDirection = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }

    public void FollowUpdate(Transform target)
    {
        Vector3 targetPosition = target.position +
                         target.forward * offset.z +
                         target.right * offset.x +
                         ((target.up.y >= 1f) ? target.up * offset.y : Vector3.up * offset.y);
        transform.position = Vector3.Slerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    public void ChangeTarget(Transform newTarget, float newFollowSpeed, float newRotationSpeed, Vector3 newOffset)
    {
        target = newTarget;
        followSpeed = newFollowSpeed;
        rotationSpeed = newRotationSpeed;
        offset = newOffset;
    }

    public void CanvasCameraTr()
    {
        this.transform.position = new Vector3(0f, 4.5f, -10f);
        this.transform.rotation = Quaternion.Euler(new Vector3(16f, 0f, 0f));

        followSpeed = 0f;
        rotationSpeed = 0f;
    }
}
