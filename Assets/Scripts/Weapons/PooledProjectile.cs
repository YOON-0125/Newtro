using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    public float Speed { get; set; }
    public int Pierce { get; set; }

    private Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplySpeedMultiplier(float mul)
    {
        Speed *= mul;
        if (rb != null)
        {
            rb.linearVelocity *= mul;
        }
    }
}
