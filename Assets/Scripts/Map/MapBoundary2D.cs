using UnityEngine;

[DisallowMultipleComponent]
public class MapBoundary2D : MonoBehaviour
{
    public static MapBoundary2D Instance { get; private set; }

    [Header("Visual Bounds")]
    [SerializeField] private Vector2 _visualMin;
    [SerializeField] private Vector2 _visualMax;

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Configure(Vector2 visualMin, Vector2 visualMax)
    {
        _visualMin = visualMin;
        _visualMax = visualMax;
    }

    public Vector3 ClampCameraPosition(Vector3 targetPosition, Camera camera)
    {
        if (camera == null || !camera.orthographic)
            return targetPosition;

        float verticalExtent = camera.orthographicSize;
        float horizontalExtent = verticalExtent * camera.aspect;

        targetPosition.x = ClampAxis(
            targetPosition.x,
            _visualMin.x + horizontalExtent,
            _visualMax.x - horizontalExtent);
        targetPosition.y = ClampAxis(
            targetPosition.y,
            _visualMin.y + verticalExtent,
            _visualMax.y - verticalExtent);
        return targetPosition;
    }

    private static float ClampAxis(float value, float min, float max)
    {
        return min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
    }
}
