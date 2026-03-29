using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDeathController deathController = other.GetComponent<PlayerDeathController>();

        if (deathController != null)
        {
            deathController.Die();
        }
    }
}
