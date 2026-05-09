using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WorkbenchSurface : MonoBehaviour
{
    public static WorkbenchSurface Instance { get; private set; }

    [Header("������� �����")]
    [SerializeField] private BoxCollider tableBounds;
    [SerializeField] private float boundsPadding = 0.05f;

    [Header("������� ���������")]
    [SerializeField] private float rescueBelowSurfaceOffset = -1.0f;
    [SerializeField] private float rescueBoundsExtra = 0.35f;

    public float SurfaceY => tableBounds != null ? tableBounds.bounds.max.y : transform.position.y;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("На сцене больше одного WorkbenchSurface. Будет использоваться последний Awake().");
        }

        Instance = this;

        if (tableBounds == null)
            tableBounds = GetComponent<BoxCollider>();

        if (tableBounds != null)
        {
            Debug.Log($"[WorkbenchSurface.Awake] Initialized on {name}: SurfaceY={SurfaceY}, bounds.center={tableBounds.bounds.center}, bounds.size={tableBounds.bounds.size}");
        }
        else
        {
            Debug.LogError($"[WorkbenchSurface.Awake] tableBounds is NULL on {name}!");
        }
    }

    public bool TryGetMousePoint(Camera cam, out Vector3 point)
    {
        point = Vector3.zero;

        if (cam == null)
            return false;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, SurfaceY, 0f));
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!plane.Raycast(ray, out float enter))
            return false;

        point = ray.GetPoint(enter);
        return true;
    }

    public bool IsInsideWorkingArea(Vector3 worldPosition)
    {
        Bounds b = tableBounds.bounds;
        b.Expand(new Vector3(boundsPadding * 2f, 0f, boundsPadding * 2f));
        return b.Contains(worldPosition);
    }

    public bool IsInsideRecoveryArea(Vector3 worldPosition)
    {
        Bounds b = tableBounds.bounds;
        b.Expand(new Vector3(rescueBoundsExtra * 2f, 100f, rescueBoundsExtra * 2f));
        return b.Contains(worldPosition);
    }

    public bool IsOutOfBounds(Vector3 worldPosition)
    {
        if (worldPosition.y < SurfaceY + rescueBelowSurfaceOffset)
            return true;

        return !IsInsideRecoveryArea(worldPosition);
    }
}