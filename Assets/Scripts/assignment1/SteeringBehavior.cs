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
    private float arrivalRadius = 0.2f;
    private float rotationSpeed = 180f; // degrees/second
    private float waypointThreshold = 0.2f;

    private bool reachedFinalTarget = false;

    void Start()
    {
        kinematic = GetComponent<KinematicBehavior>();
        path = null;
        currentTarget = transform.position;
        EventBus.OnSetMap += SetMap;
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
            reachedFinalTarget = true;
            kinematic.SetDesiredSpeed(0f);
            kinematic.SetDesiredRotationalVelocity(0f);
            return;
        }

        currentTarget = path[currentWaypointIndex];
        Vector3 toTarget = currentTarget - transform.position;
        toTarget.y = 0;
        float distance = toTarget.magnitude;

        // Direction to face
        Vector3 direction = toTarget.normalized;
        float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);

        // Reverse if angle is behind
        bool shouldReverse = Mathf.Abs(angle) > 135f;
        Vector3 desiredDirection = shouldReverse ? -direction : direction;

        // Rotate car smoothly toward target direction
        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Recalculate angle after rotating
        float newAngle = Vector3.Angle(transform.forward, direction);

        // Only move if facing close enough
        if (newAngle < 10f)
        {
            float speed = shouldReverse ? -maxSpeed * 0.4f : maxSpeed;
            // Slow down near destination
            if (distance < arrivalRadius * 2)
            {
                float t = distance / (arrivalRadius * 2);
                speed *= t;
            }

            kinematic.SetDesiredSpeed(speed);
        }
        else
        {
            kinematic.SetDesiredSpeed(0f);
        }

        // Stop at waypoint
        if (distance < waypointThreshold)
        {
            currentWaypointIndex++;
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
            Debug.Log("✅ Strict path set with " + path.Count + " points.");
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

        EventBus.ShowTarget(correctedTarget);
    }

    public void SetMap(List<Wall> outline)
    {
        path = null;
        currentWaypointIndex = 0;
        target = transform.position;
        currentTarget = target;
        reachedFinalTarget = false;
    }
}

