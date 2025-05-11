using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using JetBrains.Annotations;

public class MoveToFinishAgent : Agent
{
    PlayerMovement PlayerMovement;
    private Rigidbody2D body;
    private Animator anim;
    private bool grounded;
    public int maxSteps = 1000;
    public float movespeed = 9.8f;
    private int directionX = 0;
    private int stepCount = 0;

    private float successRate = 0f;
    private int successCount = 0;
    private int lastEpisodes = 0;
    private float previousDistanceToGoal;

    [SerializeField] public Transform finish;

    [Header("Map Gen")]
    private int successfulEpisodes = 0;
    public float initialMapLength = 5f;
    public float maxMapLength = 170f;
    private float baseTrapInterval = 20f;
    public float currentMapLength;

    [Header("Traps")]
    public GameObject[] trapPrefabs;

    [Header("WallTrap")]
    public GameObject wallTrap;

    [Header("SpikeTrap")]
    public GameObject spikeTrap;

    [Header("FireTrap")]
    public GameObject fireTrap;

    [Header("SawPlatform")]
    public GameObject sawPlatformTrap;

    [Header("SawTrap")]
    public GameObject sawTrap;

    [Header("ArrowTrap")]
    public GameObject arrowTrap;

    public override void Initialize()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentMapLength = initialMapLength;
    }

    public void Update()
    {
        anim.SetBool("run", directionX != 0);
        anim.SetBool("grounded", grounded);
    }

    public void SetupTraps()
    {
        trapPrefabs = new GameObject[]
        {
            wallTrap,
            spikeTrap,
            fireTrap,
            sawPlatformTrap,
            sawTrap,
            arrowTrap
        };
        float currentX = 40f;
        while (currentX < currentMapLength)
        {
            int index = UnityEngine.Random.Range(0, trapPrefabs.Length);
            GameObject trapPrefab = trapPrefabs[index];
            Instantiate(trapPrefab, new Vector3(currentX, trapPrefabs[index].transform.localPosition.y, trapPrefabs[index].transform.localPosition.z), Quaternion.identity);
            currentX += baseTrapInterval;
        }
    }

    public void DestroyTraps()
    {
        GameObject[] traps = GameObject.FindGameObjectsWithTag("Trap");
        foreach (var trap in traps)
        {
            Object.Destroy(trap);
        }
    }

    public override void OnEpisodeBegin()
    {
        stepCount = 0;
        body.velocity = Vector3.zero;
        transform.localPosition = new Vector3(-7, -0.5f, 0);

        finish.localPosition = new Vector3(currentMapLength, finish.localPosition.y, finish.localPosition.z);
        previousDistanceToGoal = Vector2.Distance(transform.localPosition, finish.localPosition);
        SetupTraps();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x / currentMapLength);
        sensor.AddObservation(transform.localPosition.y / 10f);
        sensor.AddObservation(body.velocity.x / 20f);
        sensor.AddObservation(body.velocity.y / 20f);

        sensor.AddObservation((finish.localPosition.x - transform.localPosition.x) / currentMapLength);
        sensor.AddObservation(finish.localPosition.x / currentMapLength);
        GameObject nearestTrap = FindNearestTrap();
        if (nearestTrap != null)
        {
            Vector3 relativePos = nearestTrap.transform.localPosition - transform.localPosition;
            sensor.AddObservation(relativePos.x / currentMapLength);
            sensor.AddObservation(relativePos.y / 10f);

            int trapType = GetTrapType(nearestTrap);
            for (int i = 0; i < trapPrefabs.Length; i++)
            {
                sensor.AddObservation(i == trapType ? 1.0f : 0.0f);
            }
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            for (int i = 0; i < trapPrefabs.Length; i++)
            {
                sensor.AddObservation(0.0f);
            }
        }

        sensor.AddObservation(body.velocity.y > 0 ? 0.5f : (grounded ? 1.0f : 0.0f));
    }

    public int GetTrapType(GameObject trap)
    {
        if (trap.CompareTag("Trap"))
        {
            if (trap.name.Contains("Wall"))
                return 0;
            else if (trap.name.Contains("Spike"))
                return 1;
            else if (trap.name.Contains("Fire"))
                return 2;
            else if (trap.name.Contains("Platform"))
                return 3;
            else if (trap.name.Contains("Saw"))
                return 4;
            else if (trap.name.Contains("Arrow"))
                return 5;
        }
        return -1;
    }

    public GameObject FindNearestTrap()
    {
        GameObject[] traps = GameObject.FindGameObjectsWithTag("Trap");
        GameObject nearestTrap = null;
        float minDistance = Mathf.Infinity;

        foreach (var trap in traps)
        {
            float distance = Vector3.Distance(transform.localPosition, trap.transform.localPosition);
            if (distance < minDistance && trap.transform.localPosition.x > transform.localPosition.x)
            {
                minDistance = distance;
                nearestTrap = trap;
            }
        }
        return nearestTrap;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        switch (Mathf.RoundToInt(Input.GetAxisRaw("Horizontal")))
        {
            case +1: discreteActions[0] = 2; break;
            case 0: discreteActions[0] = 0; break;
            case -1: discreteActions[0] = 1; break;
        }
        discreteActions[1] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        stepCount++;
        AddReward(-0.001f);

        if (stepCount >= maxSteps)
        {
            AddReward(-5.0f);
            DestroyTraps();
            EndEpisode();
            return;
        }

        float currentDistanceToGoal = Vector2.Distance(transform.localPosition, finish.localPosition);
        float progressReward = previousDistanceToGoal - currentDistanceToGoal;
        AddReward(progressReward * 0.2f);
        previousDistanceToGoal = currentDistanceToGoal;

        int moveX = actions.DiscreteActions[0];
        int jump = actions.DiscreteActions[1];

        if (moveX == 2) // move right
        {
            directionX = 1;
            transform.localScale = new Vector3(5, 5, 5);
            body.velocity = new Vector2(directionX * movespeed, body.velocity.y);
            AddReward(0.01f);
        }
        else if (moveX == 1) // move left
        {
            directionX = -1;
            transform.localScale = new Vector3(-5, 5, 5);
            body.velocity = new Vector2(directionX * movespeed, body.velocity.y);
            AddReward(-0.01f);
        }
        else if (moveX == 0) // dont move
        {
            directionX = 0;
            body.velocity = new Vector2(directionX * movespeed, body.velocity.y);
            AddReward(-0.002f);
        }

        if (jump == 1 && grounded) // jump logic
        {
            body.velocity = new Vector2(body.velocity.x, (movespeed * 1.5f));
            anim.SetTrigger("jump");
            grounded = false;
            AddReward(-0.005f);
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            grounded = true;
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.gameObject.tag == "Finish")
        {
            successCount++;
            lastEpisodes++;
            successRate = successCount / (float)lastEpisodes;

            // Large reward for finishing
            AddReward(100f);

            // Adjust map difficulty based on performance
            if (lastEpisodes >= 20)
            {
                if (successRate > 0.8f) // 80% success rate - increase difficulty
                {
                    currentMapLength = Mathf.Min(currentMapLength + 20, maxMapLength);
                    Debug.Log($"Increasing difficulty to {currentMapLength}m " +
                             $"(success rate: {successRate:P0})");
                }
                else if (successRate < 0.3f) // 30% success rate - decrease difficulty
                {
                    currentMapLength = Mathf.Max(currentMapLength - 20, initialMapLength);
                    Debug.Log($"Decreasing difficulty to {currentMapLength}m " +
                             $"(success rate: {successRate:P0})");
                }

                // Reset counters
                successCount = 0;
                lastEpisodes = 0;
            }


            DestroyTraps();
            EndEpisode();
        }
        else if (collision.gameObject.tag == "Enemy" || collision.gameObject.layer == 9)
        {
            lastEpisodes++;
            successRate = successCount / (float)lastEpisodes;

            AddReward(-10f);
            Debug.Log($"Failed episode! Success rate: {successRate:P0} ({successCount}/{lastEpisodes})");
            DestroyTraps();
            EndEpisode();
        }
        else if (collision.gameObject.tag == "TrapCheckpoint")
        {
            float progress = transform.position.x / currentMapLength;
            AddReward(5f + (10f * progress));
        }
        else if (collision.gameObject.tag == "Coin")
        {
            AddReward(5f);
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!grounded)
        {
            actionMask.SetActionEnabled(1, 1, false); // Disable jump action when not grounded
        }
    }

    private void OnDrawGizmos()
    {
        // Draw line to finish
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, finish.position);

        // Draw line to nearest trap
        GameObject nearest = FindNearestTrap();
        if (nearest != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, nearest.transform.position);
        }

        // Visualize decision values
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.black;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
            $"Reward: {GetCumulativeReward():F2}\nSteps: {stepCount}", style);
    }
}
