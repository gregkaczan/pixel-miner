using UnityEngine;
using System.Collections.Generic;

public class MoveController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveForce = 10f;
    public float maxSpeed = 5f;
    public float torqueForce = 10f;
    public float maxAngularSpeed = 200f;

    [Header("Digging Settings")]
    public float destructionRadius = 0.5f; // Radius to destroy ground pixels
    public float resistancePerPixel = 0.01f; // Force applied per pixel destroyed

    public float baseForcePerPixel = 0.01f;

    private float maxYPosition = 1.33f;

    private Rigidbody2D rb;
    private bool isMoving = false;

    private PerlinTerrainGenerator groundScript;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Find the ground GameObject and get the TerrainGeneratorAndDigging script
        GameObject ground = GameObject.FindWithTag("Ground");
        if (ground != null)
        {
            groundScript = ground.GetComponent<PerlinTerrainGenerator>();
        }
        else
        {
            Debug.LogError("Ground object with tag 'Ground' not found.");
        }
    }

    void Update()
    {
        // Detect if the right mouse button is pressed or released
        if (Input.GetMouseButtonDown(1))
        {
            isMoving = true;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isMoving = false;
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            // Movement code
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Calculate direction to the mouse
            Vector2 direction = (mouseWorldPos - rb.position).normalized;

            // Apply force towards the mouse
            rb.AddForce(direction * moveForce);

            // Limit the capsule's linear speed
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);

            // Rotate towards the mouse position using torque
            RotateTowardsMousePhysics(mouseWorldPos);

            // Check for collision with terrain and handle digging
            HandleDigging();
        }
        else
        {
            // Gradually reduce velocity and angular velocity when not moving
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 5f * Time.fixedDeltaTime);
            rb.angularVelocity = Mathf.Lerp(rb.angularVelocity, 0f, 5f * Time.fixedDeltaTime);
        }

        CapYPosition();
    }

    void CapYPosition()
    {
        // Get the current position
        Vector2 currentPosition = rb.position;

        if (currentPosition.y > maxYPosition)
        {
            // Set the Y position to the maximum allowed value
            currentPosition.y = maxYPosition;

            // Update the Rigidbody's position
            rb.position = currentPosition; 

            // If the capsule is moving upwards, prevent it
            if (rb.linearVelocity.y > 0)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.y = 0;
                rb.linearVelocity = velocity;
            }
        }
    }

    void RotateTowardsMousePhysics(Vector2 target)
    {
        // Rotation code
        Vector2 direction = target - rb.position;
        float desiredAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;

        // Calculate the shortest angle difference
        float angleDifference = Mathf.DeltaAngle(rb.rotation, desiredAngle);

        // Calculate the torque to apply
        float torque = angleDifference * torqueForce;

        // Apply torque to rotate the capsule
        rb.AddTorque(torque);

        // Limit the capsule's angular speed
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -maxAngularSpeed, maxAngularSpeed);
    }

    void HandleDigging()
    {
        if (groundScript != null)
        {
            // Get capsule's position
            Vector2 capsulePos = transform.position;

            // Destroy ground around the capsule and get the list of destroyed pixel data
            List<(Vector2 position, float hardness)> destroyedPixelData = groundScript.DestroyGroundAtPosition(
                capsulePos, destructionRadius);

            // Apply resistance force individually for each destroyed pixel
            foreach (var pixelData in destroyedPixelData)
            {
                Vector2 pixelPos = pixelData.position;
                float hardness = pixelData.hardness;

                // Calculate force vector from pixel to capsule center
                Vector2 forceDirection = capsulePos - pixelPos;
                float distance = forceDirection.magnitude;
                if (distance > 0f)
                {
                    forceDirection.Normalize();
                    // Scale force by base force and pixel hardness
                    float forceMagnitude = baseForcePerPixel * (1f + hardness);
                    Vector2 force = forceDirection * forceMagnitude;
                    rb.AddForce(force);
                }
            }
        }
    }
}