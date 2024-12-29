using UnityEngine;
using DG.Tweening;
using Cinemachine;

public class CarMovement : MonoBehaviour
{
    public float dragSpeed = 5f;
    private bool isDragging = false;
    public bool isBumping = false;
    private Vector2 dragStartPosition;
    private Rigidbody carRigidbody;
    private CinemachineDollyCart _cart;
    private LevelManager manager;
    private ParticleSystem particle;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    public float maxSpeed = 5f;
    public float acceleration = 2f;
    public float deceleration = 4f;
   
    public LayerMask carMask;
    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        _cart = GetComponent<CinemachineDollyCart>();
        _cart.enabled = false;
        manager = FindObjectOfType<LevelManager>();
        particle = GetComponentInChildren<ParticleSystem>();
        carRigidbody.isKinematic = true;
        initialPosition = transform.position;
        initialRotation = transform.rotation;   
    }

    void Update()
    {
        HandleInput();
        CheckBump();
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

        float minDragDistance = 25f;

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
    void CheckBump()
    {
        if (_cart.m_Speed < maxSpeed)
        {
            _cart.m_Speed += acceleration * Time.fixedDeltaTime;
        }

        Vector3 boxCenter = transform.position + transform.forward * 2f; // Adjust the offset value
        Vector3 boxHalfExtents = new(.8f, 0.25f, .5f);

        RaycastHit[] hits = new RaycastHit[1];
        int hitCount = Physics.BoxCastNonAlloc(boxCenter, boxHalfExtents, transform.forward, hits, transform.rotation, .5f, carMask);

        if (hitCount > 0)
        {
            if (_cart.m_Speed > 0)
            {
                _cart.m_Speed -= deceleration * Time.fixedDeltaTime;
            }
            isBumping = true;
        }
        else
        {
            isBumping = false;
        }

        if (_cart.m_Speed < 0)
        {
            _cart.m_Speed = 0;
        }
    }

    void StopDragging()
    {
        isDragging = false;
       
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Exit")&&!_cart.enabled)
        {
            if (!isBumping)
            {
                particle.Play();

                if (other.gameObject.TryGetComponent<FinishPathTrigger>(out var finish))
                {
                    var path = finish._path;
                    _cart.m_Path = path;
                    var closestPoint = path.FindClosestPoint(transform.position, 0, -1, 10);
                    closestPoint = path.FromPathNativeUnits(closestPoint, _cart.m_PositionUnits);
                    var startPosition = path.EvaluatePositionAtUnit(closestPoint, _cart.m_PositionUnits);
                    var startRotation = path.EvaluateOrientationAtUnit(closestPoint, _cart.m_PositionUnits);

                    transform.DOMove(startPosition, 0.2f);

                    transform.DORotateQuaternion(startRotation, 0.2f)
                        .OnComplete(() =>
                        {
                            _cart.m_Position = closestPoint;
                            _cart.enabled = true;

                            DOVirtual.Float(_cart.m_Speed / 3f, _cart.m_Speed, 0.2f, value => _cart.m_Speed = value);
                        });
                    return;
                }
            }
            else
            {
               GoBack();
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
            this.gameObject.SetActive(false);
        }
       
    }
    
    void StopCarAndShake(Collider collision)
    {
        if (!_cart.enabled)
        {
            // Stop the car's movement
            StopDragging();
            DOTween.Kill(transform, true);
            var rotation = transform.InverseTransformDirection(collision.transform.position);
            (rotation.x, rotation.z) = (-rotation.z, rotation.x);
            //collision.transform.DOPunchRotation(rotation * 3f, 0.2f, 1).SetId(transform);
            transform.DOPunchRotation(rotation * 3f, 0.2f, 1).SetId(transform)
                .OnComplete(() =>
                {
                    ResetCarPosition();
                });
            //Camera.main.DOShakeRotation(.2f, rotation * .2f);
        }

    }
    void ResetCarPosition()
    {       
        // Tween the car back to its initial position
        transform.DOMove(initialPosition, 0.5f).OnComplete(() =>
        {
            // After reaching the initial position, reset rotation and make it kinematic
            transform.rotation = initialRotation;
            carRigidbody.isKinematic = true;
            
        });      

    }
    void GoBack()
    {
        _cart.enabled = false;
        StopDragging();
        DOTween.Kill(transform, true);
        transform.DOMove(initialPosition, .5f).OnComplete(() =>
        {
            transform.rotation = initialRotation;
            carRigidbody.isKinematic = true;

        });
       
    }
   
}
