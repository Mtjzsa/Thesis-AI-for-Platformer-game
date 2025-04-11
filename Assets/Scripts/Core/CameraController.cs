using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    //Follow player
    [SerializeField] private Transform player;
    [SerializeField] private float aheadDistance;
    [SerializeField] private float cameraSpeed;
    private float lookAhead;
    private Health playerHealth;

    private void Start()
    {
        playerHealth = player.GetComponent<Health>();
    }

    private void Update()
    {

        //Follow player
        if (playerHealth == null || !playerHealth.IsDead())
        {
            transform.position = new Vector3(player.position.x + lookAhead, transform.position.y, transform.position.z);
            lookAhead = Mathf.Lerp(lookAhead, (aheadDistance * player.localScale.x), Time.deltaTime * cameraSpeed);
        }
    }

}
