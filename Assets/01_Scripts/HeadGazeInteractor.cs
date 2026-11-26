using UnityEngine;

public class HeadGazeInteractor : MonoBehaviour
{
    [Header("Gaze Settings")]
    [SerializeField] private LayerMask gazeLayer;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float dwellTime = 1.2f;

    private GazeInfoTarget currentTarget;
    private float gazeTimer = 0f;
    private bool infoShown = false;

    public LayerMask GazeLayer
    {
        get => gazeLayer;
        set => gazeLayer = value;
    }

    public float MaxDistance
    {
        get => maxDistance;
        set => maxDistance = value;
    }

    public float DwellTime
    {
        get => dwellTime;
        set => dwellTime = value;
    }

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        GazeInfoTarget target = null;

        if (Physics.Raycast(ray, out hit, maxDistance, gazeLayer))
        {
            target = hit.collider.GetComponentInParent<GazeInfoTarget>();
        }

        if (target != currentTarget)
        {
            ResetGaze();
            currentTarget = target;
        }

        if (currentTarget != null)
        {
            gazeTimer += Time.deltaTime;
            if (gazeTimer >= dwellTime && !infoShown)
            {
                currentTarget.ShowInfo();
                infoShown = true;
            }
        }
    }

    private void ResetGaze()
    {
        if (currentTarget != null)
        {
            currentTarget.HideInfo();
        }

        currentTarget = null;
        gazeTimer = 0f;
        infoShown = false;
    }
}

