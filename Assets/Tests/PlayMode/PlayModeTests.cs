using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PlayModeTests
{
    private GameObject agentObj;
    private MoveToFinishAgent agent;
    private Rigidbody2D rb;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Load the empty test scene
        yield return SceneManager.LoadSceneAsync("Test", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.1f); // Let Unity load the scene

        // Create and configure agent
        agentObj = new GameObject("TestAgentScene");
        agentObj.tag = "Player";
        rb = agentObj.AddComponent<Rigidbody2D>();
        agentObj.AddComponent<BoxCollider2D>();
        agentObj.AddComponent<Animator>();
        agent = agentObj.AddComponent<MoveToFinishAgent>();

        agent.wallTrap = CreateTrap("WallTrap");
        agent.spikeTrap = CreateTrap("SpikeTrap");
        agent.fireTrap = CreateTrap("FireTrap");
        agent.sawPlatformTrap = CreateTrap("SawPlatformTrap");
        agent.sawTrap = CreateTrap("SawTrap");
        agent.arrowTrap = CreateTrap("ArrowTrap");

        GameObject finish = new GameObject("Finish") { tag = "Finish" };
        finish.transform.position = new Vector3(20, 0, 0);
        finish.AddComponent<BoxCollider2D>().isTrigger = true;
        agent.finish = finish.transform;

        agent.Initialize();
        agent.SetupTraps();
        yield return null;
    }

    [UnityTest]
    public IEnumerator AgentReachesFinishRewards()
    {
        var finishGO = agent.finish.gameObject;
        finishGO.tag = "Finish";

        var finishCollider = finishGO.AddComponent<BoxCollider2D>();
        finishCollider.isTrigger = true;

        var agentRB = agent.GetComponent<Rigidbody2D>();
        agentRB.gravityScale = 0f;
        var agentCollider = agent.GetComponent<BoxCollider2D>();
        agentCollider.isTrigger = false;

        agent.transform.position = agent.finish.position + new Vector3(-1f, 0, 0);
        agentRB.velocity = new Vector2(5f, 0);

        float timeout = 2f;
        float startTime = Time.time;

        while (Time.time - startTime < timeout && agent.GetCumulativeReward() < 100f)
        {
            yield return null;
        }

        float reward = agent.GetCumulativeReward();

        Assert.GreaterOrEqual(reward, 100f, "Agent should be rewarded for reaching the finish.");
        var stepField = typeof(MoveToFinishAgent).GetField("stepCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int stepsAfter = (int)(stepField?.GetValue(agent) ?? -1);
        Assert.AreEqual(0, stepsAfter, "Agent should have reset steps after reaching finish.");
    }

    [UnityTest]
    public IEnumerator AgentNegativeRewardForJumping()
    {
        float baseReward = agent.GetCumulativeReward();

        agent.OnActionReceived(CreateActionBuffers(0, 1));
        yield return new WaitForFixedUpdate();
        float jumpReward = agent.GetCumulativeReward();

        Assert.Less(jumpReward, baseReward, "Moving left should give penalty.");
    }


    [UnityTest]
    public IEnumerator AgentGetsCorrectMovementRewards()
    {
        float baseReward = agent.GetCumulativeReward();

        agent.OnActionReceived(CreateActionBuffers(2, 0));
        yield return new WaitForFixedUpdate();
        float rightReward = agent.GetCumulativeReward();

        agent.OnActionReceived(CreateActionBuffers(1, 0));
        yield return new WaitForFixedUpdate();
        float leftReward = agent.GetCumulativeReward();

        agent.OnActionReceived(CreateActionBuffers(0, 0));
        yield return new WaitForFixedUpdate();
        float idleReward = agent.GetCumulativeReward();

        Assert.Greater(rightReward, baseReward, "Moving right should give positive reward.");
        Assert.Less(leftReward, rightReward, "Moving left should give penalty.");
        Assert.Less(idleReward, rightReward, "Standing still should be less optimal than moving right.");
    }

    [UnityTest]
    public IEnumerator AgentCannotJumpWhileAirborne()
    {
        SetPrivate(agent, "grounded", false);

        float initialY = agent.transform.position.y;

        agent.OnActionReceived(CreateActionBuffers(0, 1));
        yield return new WaitForFixedUpdate();

        Assert.LessOrEqual(agent.GetComponent<Rigidbody2D>().velocity.y, 0.01f, "Agent should not jump when not grounded.");
    }

    [UnityTest]
    public IEnumerator AgentGetsEnemyReward()
    {
        var trap = new GameObject("Enemy") { tag = "Enemy", layer = 9 };
        trap.transform.position = agent.transform.position + Vector3.right * 0.5f;

        var trapCollider = trap.AddComponent<BoxCollider2D>();
        trapCollider.isTrigger = true;

        var trapRb = trap.AddComponent<Rigidbody2D>();
        trapRb.bodyType = RigidbodyType2D.Kinematic;

        agent.transform.position = trap.transform.position;

        float initialReward = agent.GetCumulativeReward();

        agent.OnTriggerEnter2D(trapCollider);

        yield return new WaitForFixedUpdate();
        yield return null;

        float rewardAfter = agent.GetCumulativeReward();

        Assert.Less(rewardAfter, initialReward, "Agent should receive a negative reward upon colliding with enemy.");
    }

    [UnityTest]
    public IEnumerator AgentGetsCheckPointReward()
    {
        var ck = new GameObject("TrapCheckpoint") { tag = "TrapCheckpoint" };
        ck.transform.position = agent.transform.position + Vector3.right * 0.5f;

        var ckCollider = ck.AddComponent<BoxCollider2D>();
        ckCollider.isTrigger = true;

        var ckRb = ck.AddComponent<Rigidbody2D>();
        ckRb.bodyType = RigidbodyType2D.Kinematic;

        agent.transform.position = ck.transform.position;

        float initialReward = agent.GetCumulativeReward();

        agent.OnTriggerEnter2D(ckCollider);

        yield return new WaitForFixedUpdate();
        yield return null;

        float rewardAfter = agent.GetCumulativeReward();

        Assert.NotZero(rewardAfter, "Agent should receive a positive reward for passing a checkpoint.");
    }


    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(agentObj);
        foreach (var obj in GameObject.FindGameObjectsWithTag("Trap"))
            Object.Destroy(obj);

        var finish = GameObject.FindWithTag("Finish");
        if (finish != null) Object.Destroy(finish);
        yield return null;
    }
    

    //----------Helpers----------------------------------
    private GameObject CreateTrap(string name)
    {
        var trap = new GameObject(name) { tag = "Trap" };
        trap.AddComponent<BoxCollider2D>();
        trap.transform.position = new Vector3(10, 0, 0);
        return trap;
    }

    private void SetPrivate(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }

    private ActionBuffers CreateActionBuffers(int move, int jump)
    {
        return new ActionBuffers(
            ActionSegment<float>.Empty,
            new ActionSegment<int>(new[] { move, jump })
        );
    }
}
