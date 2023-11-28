using UnityEngine;
using DG.Tweening;
using Cinemachine;

public class CarMovement : MonoBehaviour
{
    public float dragSpeed = 5f;
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private Rigidbody carRigidbody;
    private CinemachineDollyCart _cart;
    private LevelManager manager;
    private ParticleSystem particle;
    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        _cart = GetComponent<CinemachineDollyCart>();
        _cart.enabled = false;
        manager = FindObjectOfType<LevelManager>();
        particle = GetComponentInChildren<ParticleSystem>();
        carRigidbody.isKinematic = true;
    }

    void Update()
    {
        HandleInput();     
    }

    void HandleInput()
    { 
        if (Input.touches.Length > 0)//Handling touch input seperately even unity allows mouse input to be used to handle touch input
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (IsTouchOnCar(touch.position))
                    {
                        StartDragging(touch.position);
                    }
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        UpdateCarPosition(touch.position);
                    }
                    break;

                case TouchPhase.Ended:
                    StopDragging();
                    break;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOnCar(Input.mousePosition))
            {
                StartDragging(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (isDragging)
            {
                UpdateCarPosition(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopDragging();
        }
    }

    bool IsTouchOnCar(Vector2 touchPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        return GetComponent<Collider>().Raycast(ray, out hit, Mathf.Infinity);
    }

    bool IsMouseOnCar(Vector2 mousePosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        return GetComponent<Collider>().Raycast(ray, out hit, Mathf.Infinity);
    }

    void StartDragging(Vector2 startPosition)
    {
        isDragging = true;
        dragStartPosition = startPosition;
    }

    private void UpdateCarPosition(Vector2 currentPosition)
    {
        Vector2 dragDelta = currentPosition - dragStartPosition;

        float minDragDistance = 30f;

        // Clamp the dragDelta to be within the specified range
        dragDelta = Vector2.ClampMagnitude(dragDelta, minDragDistance);

        if (dragDelta.magnitude < minDragDistance)
        {
            return;
        }

        Vector3 forwardDirection = transform.forward;

        // Calculate the movement based on the clamped dragDelta
        Vector3 movement = dragSpeed * Time.deltaTime * new Vector3(dragDelta.x, 0f, dragDelta.y);

        float projectedMovement = Vector3.Dot(movement, forwardDirection);
        Vector3 finalMovement = forwardDirection * projectedMovement;
        carRigidbody.isKinematic = false;
        carRigidbody.AddForce(finalMovement, ForceMode.VelocityChange);
    }

    void StopDragging()
    {
        isDragging = false;
       
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Exit"))
        {
            particle.Play();

            if (other.gameObject.TryGetComponent<FinishPathTrigger>(out var finish))
            {
                var path = finish._path;
                _cart.m_Path = path;
                var closestPoint = path.FindClosestPoint( transform.position,0, -1, 10);
                closestPoint = path.FromPathNativeUnits(closestPoint, _cart.m_PositionUnits);

                _cart.m_Position = closestPoint;
                _cart.enabled = true;

            }
        }
        else if (other.CompareTag("Movable"))
        {
            StopCarAndShake(other);
            //Debug.Log("Crashed!");
        }
        else if (other.CompareTag("Wall"))
        {
            StopCarAndShake(other);
        }
        else if (other.CompareTag("LevelEnd"))
        {
            manager.CarFinished(this.gameObject);
        }
    }

    void StopCarAndShake(Collider collision)
    {
        // Stop the car's movement
        StopDragging();

        carRigidbody.velocity = Vector3.zero;
        DOTween.Kill(transform, true);
        var rotation = transform.InverseTransformDirection(collision.transform.position);
        (rotation.x, rotation.z) = (-rotation.z, rotation.x);
        transform.DOPunchRotation(rotation * 3f, 0.2f, 1).SetId(transform);
          
    }

}
