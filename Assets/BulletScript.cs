using UnityEngine;

public class BulletScript : OnDestroyScript
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name == "Destroyed") return;
        other.gameObject.name = "Destroyed";
        Destroy(other.gameObject);
        Destroy(gameObject);
    }
}