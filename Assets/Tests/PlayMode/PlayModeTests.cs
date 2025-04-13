using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayModeTests
{
    private GameObject testObj;
    private MoveToFinishAgent agent;
    private GameObject finish;
    private GameObject trap;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        testObj = new GameObject("TestAgent");
        testObj.tag = "Player";

        // Required components
        var rb = testObj.AddComponent<Rigidbody2D>();
        var col = testObj.AddComponent<BoxCollider2D>();

        // Assign a dummy AnimatorController to prevent null errors
        var animator = testObj.AddComponent<Animator>();
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Tests/PlayMode/EmptyController"); // You'll need to create this

        agent = testObj.AddComponent<MoveToFinishAgent>();

        // Trap placeholders with default positions
        agent.wallTrap = new GameObject("WallTrap") { tag = "Trap" };
        agent.wallTrap.transform.localPosition = new Vector3(0, 0, 0);

        agent.spikeTrap = new GameObject("SpikeTrap") { tag = "Trap" };
        agent.spikeTrap.transform.localPosition = new Vector3(0, 0, 0);

        agent.fireTrap = new GameObject("FireTrap") { tag = "Trap" };
        agent.fireTrap.transform.localPosition = new Vector3(0, 0, 0);

        agent.sawPlatformTrap = new GameObject("SawPlatform") { tag = "Trap" };
        agent.sawPlatformTrap.transform.localPosition = new Vector3(0, 0, 0);

        agent.sawTrap = new GameObject("SawTrap") { tag = "Trap" };
        agent.sawTrap.transform.localPosition = new Vector3(0, 0, 0);

        agent.arrowTrap = new GameObject("ArrowTrap") { tag = "Trap" };
        agent.arrowTrap.transform.localPosition = new Vector3(0, 0, 0);

        // Finish setup
        finish = new GameObject("Finish");
        finish.tag = "Finish";
        finish.transform.position = new Vector3(50f, 0f, 0f);
        agent.finish = finish.transform;

        agent.Initialize();

        yield return null;
    }

    [UnityTest]
    public IEnumerator AgentEndsEpisodeOnFinish()
    {
        agent.Initialize();
        agent.OnEpisodeBegin();

        var finishCollider = agent.finish.gameObject.AddComponent<BoxCollider2D>();
        finishCollider.isTrigger = true;

        
        agent.GetComponent<BoxCollider2D>().isTrigger = true;

        agent.OnTriggerEnter2D(finishCollider);
        Assert.LessOrEqual(agent.GetCumulativeReward(), 100f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AgentEndsEpisodeOnHittingEnemy()
    {
        agent.Initialize();
        agent.OnEpisodeBegin();

        var trap = agent.fireTrap.gameObject.AddComponent<BoxCollider2D>();
        trap.isTrigger = true;


        agent.GetComponent<BoxCollider2D>().isTrigger = true;

        agent.OnTriggerEnter2D(trap);
        Assert.LessOrEqual(agent.GetCumulativeReward(), -10f);
        yield return null;
    }


    [UnityTest]
    public IEnumerator Agent_Resets_OnEpisodeBegin()
    {
        agent.OnEpisodeBegin();
        yield return null;

        Assert.AreEqual(new Vector3(-7f, -0.5f, 0), agent.transform.localPosition);
        Assert.AreEqual(0f, agent.GetCumulativeReward(), "Reward should be reset");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(agent);
        if (finish != null) Object.Destroy(finish);
        if (trap != null) Object.Destroy(trap);
        yield return null;
    }
}
