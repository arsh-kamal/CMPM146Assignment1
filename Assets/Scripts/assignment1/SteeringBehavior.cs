using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SteeringBehavior : MonoBehaviour
{
    public Vector3 target;
    public KinematicBehavior kinematic;
    public List<Vector3> path;
    public TextMeshProUGUI label;

    private int currentWaypointIndex = 0;
    private Vector3 currentTarget;

    private float maxSpeed = 10f;
    private float arrivalRadius = 1f;
    private float rotationSpeed = 180f;
    private float waypointThreshold = 0.2f;

    private bool reachedFinalTarget = false;

    void Start()
    {
        kinematic = GetComponent<KinematicBehavior>();
        path = null;
        currentTarget = transform.position;
        EventBus.OnSetMap += SetMap;
        EventBus.OnPath += SetPath;
        EventBus.OnTarget += SetTarget;
    }

    void Update()
    {
        if (reachedFinalTarget)
        {
            kinematic.SetDesiredSpeed(0f);
            kinematic.SetDesiredRotationalVelocity(0f);
            if (label != null) label.text = "Target reached!";
            return;
        }

        if (path != null && path.Count > 0)
        {
            FollowPath();
        }
    }

    private void FollowPath()
    {
        if (currentWaypointIndex >= path.Count)
        {
            Vector3 toFinalTarget = path[path.Count - 1] - transform.position;
            toFinalTarget.y = 0;
            float distanceToFinal = toFinalTarget.magnitude;
            Debug.Log($"Reached final waypoint, distance to final target: {distanceToFinal}, position: {transform.position}, target: {path[path.Count - 1]}");

            if (distanceToFinal < waypointThreshold)
            {
                reachedFinalTarget = true;
                kinematic.SetDesiredSpeed(0f);
                kinematic.SetDesiredRotationalVelocity(0f);
                Vector3 finalPos = path[path.Count - 1];
                transform.position = new Vector3(finalPos.x, transform.position.y, finalPos.z);
                Debug.Log($"Car snapped to final target at {transform.position}");
            }
            else
            {
                currentTarget = path[path.Count - 1];
                MoveToTarget();
            }
            return;
        }

        currentTarget = path[currentWaypointIndex];
        MoveToTarget();
    }

    private void MoveToTarget()
    {
        Vector3 toTarget = currentTarget - transform.position;
        toTarget.y = 0;
        float distance = toTarget.magnitude;
        Debug.Log($"Moving to target at {currentTarget}, distance: {distance}, position: {transform.position}");

        if (distance < waypointThreshold)
        {
            transform.position = new Vector3(currentTarget.x, transform.position.y, currentTarget.z);
            kinematic.SetDesiredSpeed(0f);
            kinematic.SetDesiredRotationalVelocity(0f);
            Debug.Log($"Snapped to target at {transform.position}");

            if (currentWaypointIndex < path.Count)
            {
                currentWaypointIndex++;
                Debug.Log($"Reached waypoint {currentWaypointIndex - 1}, moving to next");
            }
            return;
        }

        Vector3 direction = toTarget.normalized;
        float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);

        bool shouldReverse = Mathf.Abs(angle) > 135f;
        Vector3 desiredDirection = shouldReverse ? -direction : direction;

        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        float newAngle = Vector3.Angle(transform.forward, direction);
        Debug.Log($"Angle to target: {newAngle}, shouldReverse: {shouldReverse}");

        float speed;
        if (newAngle < 10f)
        {
            speed = maxSpeed;
        }
        else if (shouldReverse && newAngle > 170f)
        {
            speed = -maxSpeed * 0.2f;
        }
        else
        {
            speed = 0f;
        }

        if (distance < arrivalRadius * 2)
        {
            float t = distance / (arrivalRadius * 2);
            if (shouldReverse)
            {
                speed = -maxSpeed * 0.2f * t;
            }
            else
            {
                speed = maxSpeed * t;
            }
            Debug.Log($"Slowing down, t: {t}, adjusted speed: {speed}");
        }

        kinematic.SetDesiredSpeed(speed);

        toTarget = currentTarget - transform.position;
        toTarget.y = 0;
        distance = toTarget.magnitude;
        if (distance < waypointThreshold)
        {
            transform.position = new Vector3(currentTarget.x, transform.position.y, currentTarget.z);
            kinematic.SetDesiredSpeed(0f);
            kinematic.SetDesiredRotationalVelocity(0f);
            Debug.Log($"Corrected overshoot, snapped to target at {transform.position}");
        }
    }

    public void SetPath(List<Vector3> path)
    {
        this.path = path;
        this.currentWaypointIndex = 0;
        this.reachedFinalTarget = false;

        if (path != null && path.Count > 0)
        {
            this.currentTarget = path[0];
            Debug.Log("✅ Strict path set with " + path.Count + " points: " + string.Join(", ", path));
        }
        else
        {
            Debug.Log("❌ Path is empty or null");
        }
    }

    public void SetTarget(Vector3 target)
    {
        Vector3 correctedTarget = new Vector3(target.x, transform.position.y, target.z);
        this.target = correctedTarget;
        this.path = null;
        this.currentWaypointIndex = 0;
        this.currentTarget = correctedTarget;
        this.reachedFinalTarget = false;

        Debug.Log($"SteeringBehavior received target at {correctedTarget}");
        EventBus.ShowTarget(correctedTarget);
    }

    public void SetMap(List<Wall> outline)
    {
        path = null;
        currentWaypointIndex = 0;
        target = transform.position;
        currentTarget = target;
        reachedFinalTarget = false;
        Debug.Log("SteeringBehavior reset for new map");
    }
}
