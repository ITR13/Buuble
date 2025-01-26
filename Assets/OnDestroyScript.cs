using UnityEngine;

public class OnDestroyScript : MonoBehaviour
{
    private void OnDestroy()
    {
        FindAnyObjectByType<AudioPlayer>().PlayAudio();

        var onDestroy = transform.Find("OnDestroy");
        if (onDestroy == null) return;

        var velocity = GetComponent<Rigidbody2D>().linearVelocity;

        for (var i = onDestroy.childCount - 1; i >= 0; i--)
        {
            var child = onDestroy.GetChild(i);
            var position = transform.TransformPoint(Random.onUnitSphere / 10);
            var scale = child.localScale;

            child.transform.parent = transform.parent;

            child.transform.position = position;
            child.transform.localScale = scale;
            child.gameObject.SetActive(true);
            child.GetComponent<Rigidbody2D>().linearVelocity = velocity;
        }
    }
}