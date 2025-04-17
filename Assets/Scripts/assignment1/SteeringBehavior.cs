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

    private float maxSpeed = 3f;
    private float maxRotationSpeed = 40f;
    private float arrivalRadius = 0.8f;        // Increased to prevent early slowdown
    private float rotationFactor = 0.7f;
    private float waypointThreshold = 0.8f;    // Increased to prevent early stopping

    void Start()
    {
        kinematic = GetComponent<KinematicBehavior>();
        target = transform.position;
        path = null;
        EventBus.OnSetMap += SetMap;
        currentTarget = target;
        Debug.Log($"Car starting position: {transform.position}");
    }

    void Update()
    {
        if (kinematic == null)
        {
            if (label != null) label.text = "Error: Missing KinematicBehavior!";
            return;
        }

        if (path != null && path.Count > 0)
        {
            FollowPath();
        }
        else
        {
            currentTarget = target;
            SeekSingleTarget();
        }
    }

    private void SeekSingleTarget()
    {
        Vector3 toTarget = currentTarget - transform.position;
        toTarget.y = 0;
        float distance = toTarget.magnitude;

        Debug.Log($"Seeking target. Distance: {distance:F2}, Target: {currentTarget}, Position: {transform.position}");

        if (distance < waypointThreshold)
        {
            transform.position = new Vector3(currentTarget.x, transform.position.y, currentTarget.z);
            kinematic.SetDesiredSpeed(0f);
            kinematic.SetDesiredRotationalVelocity(0f);
            if (label != null) label.text = $"Target reached! Distance: {distance:F2}";
            Debug.Log($"Target reached at position: {transform.position}");
            return;
        }

        Vector3 forward = transform.forward;
        forward.y = 0;
        forward = forward.normalized;
        float angle = Vector3.SignedAngle(forward, toTarget.normalized, Vector3.up);

        if (label != null)
        {
            label.text = $"Seeking target\nDistance: {distance:F2}\nAngle: {angle:F2}";
        }

        float desiredRotation = angle * rotationFactor;
        float angleAbs = Mathf.Abs(angle);
        float rotationScale = Mathf.Clamp01(angleAbs / 45f);
        desiredRotation *= rotationScale;

        if (angleAbs < 2f)
        {
            desiredRotation = 0f;
        }
        else if (angleAbs < 10f)
        {
            desiredRotation *= 0.3f;
        }

        desiredRotation = Mathf.Clamp(desiredRotation, -maxRotationSpeed, maxRotationSpeed);
        kinematic.SetDesiredRotationalVelocity(desiredRotation);

        float speed = maxSpeed * Mathf.Sign(Vector3.Dot(forward, toTarget.normalized));
        if (angleAbs > 90f)
        {
            speed *= 0.3f;
        }
        else if (angleAbs > 45f)
        {
            speed *= 0.4f;
        }
        else if (angleAbs > 10f)
        {
            speed *= 0.6f;
        }

        if (distance < arrivalRadius)
        {
            speed *= (distance / arrivalRadius);
            if (speed < 0) speed = Mathf.Max(speed, -maxSpeed * 0.3f);
        }

        kinematic.SetDesiredSpeed(speed);
        Debug.Log($"Set speed: {speed:F2}, Rotation: {desiredRotation:F2}");
    }

    private void FollowPath()
    {
        if (path == null || path.Count == 0 || currentWaypointIndex >= path.Count)
        {
            Debug.Log("Path is null, empty, or completed.");
            path = null;
            currentWaypointIndex = 0;
            transform.position = new Vector3(currentTarget.x, transform.position.y, currentTarget.z);
            kinematic.SetDesiredSpeed(0f);
            kinematic.SetDesiredRotationalVelocity(0f);
            if (label != null) label.text = "Path completed!";
            Debug.Log($"Path following completed at position: {transform.position}");
            return;
        }

        currentTarget = path[currentWaypointIndex];
        currentTarget.y = transform.position.y;

        Vector3 toTarget = currentTarget - transform.position;
        toTarget.y = 0;
        float distance = toTarget.magnitude;

        Debug.Log($"Following waypoint {currentWaypointIndex + 1}/{path.Count}. Distance: {distance:F2}, Angle: {Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up):F2}, Target: {currentTarget}, Position: {transform.position}");

        if (distance < waypointThreshold)
        {
            Debug.Log($"Reached waypoint {currentWaypointIndex + 1}/{path.Count}. Distance: {distance:F2}");
            currentWaypointIndex++;

            if (currentWaypointIndex >= path.Count)
            {
                Debug.Log("Reached end of path.");
                path = null;
                currentWaypointIndex = 0;
                transform.position = new Vector3(currentTarget.x, transform.position.y, currentTarget.z);
                kinematic.SetDesiredSpeed(0f);
                kinematic.SetDesiredRotationalVelocity(0f);
                if (label != null) label.text = "Path completed!";
                Debug.Log($"Path following completed at position: {transform.position}");
                return;
            }

            currentTarget = path[currentWaypointIndex];
            currentTarget.y = transform.position.y;
            Debug.Log($"Moving to waypoint {currentWaypointIndex + 1}/{path.Count}: {currentTarget}");
        }

        Vector3 forward = transform.forward;
        forward.y = 0;
        forward = forward.normalized;
        float angle = Vector3.SignedAngle(forward, toTarget.normalized, Vector3.up);

        if (label != null)
        {
            label.text = $"Following path\nWaypoint: {currentWaypointIndex + 1}/{path.Count}\nDistance: {distance:F2}\nAngle: {angle:F2}";
        }

        float desiredRotation = angle * rotationFactor;
        float angleAbs = Mathf.Abs(angle);
        float rotationScale = Mathf.Clamp01(angleAbs / 45f);
        desiredRotation *= rotationScale;

        if (angleAbs < 2f)
        {
            desiredRotation = 0f;
        }
        else if (angleAbs < 10f)
        {
            desiredRotation *= 0.3f;
        }

        desiredRotation = Mathf.Clamp(desiredRotation, -maxRotationSpeed, maxRotationSpeed);
        kinematic.SetDesiredRotationalVelocity(desiredRotation);

        float speed = maxSpeed * Mathf.Sign(Vector3.Dot(forward, toTarget.normalized));
        if (angleAbs > 90f)
        {
            speed *= 0.8f; // Increased further to ensure movement
        }
        else if (angleAbs > 45f)
        {
            speed *= 0.9f; // Increased to keep moving
        }
        else if (angleAbs > 10f)
        {
            speed *= 1.0f; // Full speed for small angles
        }

        if (distance < arrivalRadius)
        {
            float speedScale = distance / arrivalRadius;
            speed *= speedScale;
            Debug.Log($"Slowing down near target. Speed scale: {speedScale:F2}, New speed: {speed:F2}");
        }

        kinematic.SetDesiredSpeed(speed);
        Debug.Log($"Set speed: {speed:F2}, Rotation: {desiredRotation:F2}");
    }

    public void SetTarget(Vector3 target)
    {
        this.target = target;
        EventBus.ShowTarget(target);
        path = null;
        currentWaypointIndex = 0;
        currentTarget = target;
        Debug.Log($"SetTarget called. New target: {target}");
    }

    public void SetPath(List<Vector3> path)
    {
        this.path = path;
        currentWaypointIndex = 0;
        if (path != null && path.Count > 0)
        {
            while (currentWaypointIndex < path.Count)
            {
                currentTarget = path[currentWaypointIndex];
                currentTarget.y = transform.position.y;
                float distance = (currentTarget - transform.position).magnitude;
                if (distance < waypointThreshold)
                {
                    Debug.Log($"Skipping waypoint {currentWaypointIndex + 1} at {currentTarget}. Distance: {distance:F2}");
                    currentWaypointIndex++;
                }
                else
                {
                    break;
                }
            }

            if (currentWaypointIndex >= path.Count)
            {
                Debug.Log("All waypoints are too close to the car's position. Path cleared.");
                this.path = null;
                return;
            }

            Debug.Log($"SetPath called with {path.Count} waypoints: {string.Join(", ", path)}");
            Debug.Log($"Starting at waypoint {currentWaypointIndex + 1}: {currentTarget}");
        }
        else
        {
            Debug.Log("SetPath called with null or empty path.");
            this.path = null;
        }
    }

    public void SetMap(List<Wall> outline)
    {
        if (path != null && path.Count > 0)
        {
            Debug.Log("SetMap called, but preserving path because car is following it.");
            return;
        }
        this.path = null;
        currentWaypointIndex = 0;
        this.target = transform.position;
        currentTarget = target;
    }
}
