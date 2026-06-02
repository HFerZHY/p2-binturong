using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FadeWhenPlayerInsideSpriteBounds : MonoBehaviour
{
    // [Header("Player")]
    // [SerializeField]
    private static readonly Vector3 playerOffset = new Vector3(0, -0.85f, 0);

    [Header("Fade")]
    [SerializeField]
    [Range(0f, 1f)]
    private float fadedAlpha = 0.5f;

    [SerializeField]
    private float fadeSpeed = 4f;

    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }

        Vector3 playerPos = mainCamera.transform.position + playerOffset;

        Bounds bounds = spriteRenderer.bounds;

        bool playerInside =
            playerPos.x >= bounds.min.x &&
            playerPos.x <= bounds.max.x &&
            playerPos.y >= bounds.min.y &&
            playerPos.y <= bounds.max.y;

        float targetAlpha = playerInside ? fadedAlpha : 1f;

        Color c = spriteRenderer.color;
        c.a = Mathf.MoveTowards(
            c.a,
            targetAlpha,
            fadeSpeed * Time.deltaTime);

        spriteRenderer.color = c;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(sr.bounds.center, sr.bounds.size);

        if (Camera.main != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(
                Camera.main.transform.position + playerOffset,
                0.1f);
        }
    }
#endif
}