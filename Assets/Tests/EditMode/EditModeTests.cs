using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.MLAgents.Actuators;
using Unity.MLAgents;
using System.Linq;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;

[TestFixture]
public class EditModeTests
{
    private GameObject testObj;
    private MoveToFinishAgent agent;

    [SetUp]
    public void Setup()
    {
        testObj = new GameObject("TestAgent");
        testObj.tag = "Player";
        testObj.AddComponent<Rigidbody2D>();
        testObj.AddComponent<BoxCollider2D>();
        testObj.AddComponent<Animator>();
        agent = testObj.AddComponent<MoveToFinishAgent>();
        agent.wallTrap = new GameObject("WallTrap") { tag = "Trap" };
        agent.spikeTrap = new GameObject("SpikeTrap") { tag = "Trap" };
        agent.fireTrap = new GameObject("FireTrap") { tag = "Trap" };
        agent.sawPlatformTrap = new GameObject("SawPlatform") { tag = "Trap" };
        agent.sawTrap = new GameObject("Saw") { tag = "Trap" };
        agent.arrowTrap = new GameObject("ArrowTrap") { tag = "Trap" };
        agent.finish = new GameObject("Finish").transform;
        agent.finish.tag = "Finish";
        agent.Initialize();
        agent.SetupTraps();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(testObj);
        foreach (var trap in GameObject.FindGameObjectsWithTag("Trap"))
            Object.DestroyImmediate(trap);
    }

    [Test]
    public void SetupTrapsInstantiatesTraps()
    {
        var traps = GameObject.FindGameObjectsWithTag("Trap");
        Assert.IsTrue(traps.Length > 0, "Traps should be instantiated during SetupTraps.");
    }

    [Test]
    public void AgentInitializationTest()
    {
        Assert.IsNotNull(agent.GetComponent<Rigidbody2D>(), "Rigidbody2D should be added to the agent.");
        Assert.IsNotNull(agent.GetComponent<BoxCollider2D>(), "BoxCollider2D should be added to the agent.");
        Assert.IsNotNull(agent.GetComponent<Animator>(), "Animator should be added to the agent.");
    }

    [Test]
    public void OnActionReceivedMoveRight()
    {
        var initialX = agent.GetComponent<Rigidbody2D>().velocity.x;
        agent.OnActionReceived(CreateActionBuffers(2, 0));
        Assert.Greater(agent.GetComponent<Rigidbody2D>().velocity.x, initialX);
    }

    [Test]
    public void GetTrapTypeReturnsCorrectIndexForEachTrap()
    {
        Assert.AreEqual(0, agent.GetTrapType(agent.wallTrap), "WallTrap index should be 0.");
        Assert.AreEqual(1, agent.GetTrapType(agent.spikeTrap), "SpikeTrap index should be 1.");
        Assert.AreEqual(2, agent.GetTrapType(agent.fireTrap), "FireTrap index should be 2.");
        Assert.AreEqual(3, agent.GetTrapType(agent.sawPlatformTrap), "SawPlatformTrap index should be 3.");
        Assert.AreEqual(4, agent.GetTrapType(agent.sawTrap), "SawTrap index should be 4.");
        Assert.AreEqual(5, agent.GetTrapType(agent.arrowTrap), "ArrowTrap index should be 5.");
    }

    [Test]
    public void OnActionReceivedJumpOnlyWhenGrounded()
    {
        agent.GetType().GetField("grounded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(agent, true);

        agent.OnActionReceived(CreateActionBuffers(0, 1));

        Assert.Greater(agent.GetComponent<Rigidbody2D>().velocity.y, 0);
    }

    [Test]
    public void FindNearestTrapReturnsCorrectTrap()
    {
        var trap1 = new GameObject("Trap1");
        var trap2 = new GameObject("Trap2");

        trap1.tag = "Trap";
        trap2.tag = "Trap";

        trap1.transform.position = new Vector3(5, 0, 0);
        trap2.transform.position = new Vector3(10, 0, 0);
        agent.transform.position = Vector3.zero;

        var result = agent.FindNearestTrap();
        Assert.AreEqual(trap1, result);
    }

    // --------- Helpers ---------
    private class TestActionMask : IDiscreteActionMask
    {
        private readonly Dictionary<(int, int), bool> mask = new();

        public void SetActionEnabled(int branch, int action, bool isEnabled)
        {
            mask[(branch, action)] = isEnabled;
        }

        public bool IsEnabled(int branch, int action)
        {
            return mask.TryGetValue((branch, action), out bool val) ? val : true;
        }
    }

    private ActionBuffers CreateActionBuffers(int move, int jump)
    {
        var discrete = new int[2];
        discrete[0] = move;
        discrete[1] = jump;
        return new ActionBuffers(
            ActionSegment<float>.Empty,
            new ActionSegment<int>(discrete)
        );
    }
}
