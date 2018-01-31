using UnityEngine;
using System.Collections;

namespace SgLib
{
    public class PlayerController : MonoBehaviour
    {
        public static event System.Action PlayerDied;
        public static event System.Action StartRun;
        public static event System.Action PlayerHeadingAnEdge;

        [Header("Gameplay Config")]
        public float maxSpeed;
        public float maxSpeedFactor;
        public float initialCollisionForce;
        public float maxCollisionForce;
        public float increaseSpeedFactor;
        public float increaseCollisionForceFactor;


        [Header("Gameplay References")]
        public CameraController cameraController;
        public MouseOverDetector selectCharacterButtonMOD;
        public GameObject main;
        public GameObject vehicle;
        public GameObject point_1;
        public GameObject point_2;

        [HideInInspector]
        public float turnRightSpeed;
        [HideInInspector]
        public bool startRun;
        [HideInInspector]
        public bool isAtScreenEdge;

        private Rigidbody mainRigid;
        private Rigidbody vehicleRigid;
        private MeshCollider mainCollider;
        private MeshCollider vehicleCollider;
        private Vector3 rightvector;
        private Vector3 leftVector;
        private Vector3 upVector;
        private Vector3 totalVector;
        private Vector3 collisionDirection;
        private float currentSpeedFactor;
        private float currentCollisionForce;
        private float turnLeftSpeed;
        private bool stop;
        bool isDead;

        void OnEnable()
        {
            GameManager.GameStateChanged += OnGameStateChanged;
        }

        void OnDisable()
        {
            GameManager.GameStateChanged -= OnGameStateChanged;
        }

        // Listens to changes in game state
        void OnGameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Playing && oldState == GameState.Prepare)
            {
                startRun = true;

                // Fire event
                if (StartRun != null)
                    StartRun();
            }
        }

        // Use this for initialization
        void Start()
        {
            //Change the character to the selected one
            GameObject currentVehicle = VehicleManager.Instance.vehicles[VehicleManager.Instance.CurrentVehicleIndex];
            Mesh vehicleMesh = currentVehicle.GetComponent<MeshFilter>().sharedMesh;
            Material vehicleMaterial = currentVehicle.GetComponent<Renderer>().sharedMaterial;
            vehicle.GetComponent<MeshFilter>().mesh = vehicleMesh;
            vehicle.GetComponent<MeshRenderer>().material = vehicleMaterial;
            vehicle.GetComponent<MeshCollider>().sharedMesh = vehicleMesh;

            GameObject currentHuman = HumanManager.Instance.humans[HumanManager.Instance.CurrentHumanIndex];
            Mesh humanMesh = currentHuman.GetComponent<MeshFilter>().sharedMesh;
            Material humanMaterial = currentHuman.GetComponent<Renderer>().sharedMaterial;
            main.GetComponent<MeshFilter>().mesh = humanMesh;
            main.GetComponent<MeshRenderer>().material = humanMaterial;
            main.GetComponent<MeshCollider>().sharedMesh = humanMesh;

            mainRigid = main.GetComponent<Rigidbody>();
            mainCollider = main.GetComponent<MeshCollider>();
            vehicleRigid = vehicle.GetComponent<Rigidbody>();
            vehicleCollider = vehicle.GetComponent<MeshCollider>();
            collisionDirection = (point_2.transform.position - point_1.transform.position).normalized;

            turnLeftSpeed = 0;
            turnRightSpeed = 0;
            currentSpeedFactor = 0;
            currentCollisionForce = initialCollisionForce;
            StartCoroutine(IncreaseSpeed());
            StartCoroutine(IncreaseCollisionForce());
        }

        // Update is called once per frame
        void Update()
        {
            if (startRun && GameManager.Instance.GameState != GameState.GameOver)
            {
                if (Input.GetMouseButton(0))
                {
                    if (turnLeftSpeed < maxSpeed)
                    {
                        turnLeftSpeed += currentSpeedFactor * Time.deltaTime;
                    }
                    else
                    {
                        turnLeftSpeed = maxSpeed;
                    }
                }
                else
                {
                    if (turnLeftSpeed > 0)
                    {
                        turnLeftSpeed -= currentSpeedFactor * Time.deltaTime;
                    }
                    else
                    {
                        turnLeftSpeed = 0;
                    }
                }
            }

            if (!stop)
            {
                rightvector = (Vector3.back + Vector3.right) * turnRightSpeed;
                leftVector = (Vector3.back + Vector3.left) * turnLeftSpeed;
                upVector = (Vector3.forward + Vector3.left) * turnLeftSpeed;    // this one is counter of rightVector
                totalVector = rightvector + leftVector + upVector; //Moving direction
                transform.position += totalVector * Time.deltaTime;
                float rotateAngle = Vector3.Angle(rightvector, totalVector);
                if (turnLeftSpeed == maxSpeed)
                {
                    rotateAngle = 90;
                }
                else if (turnLeftSpeed == 0)
                {
                    rotateAngle = 0;
                }
                transform.rotation = Quaternion.Euler(0, 45 + rotateAngle, 0);
            }

            float viewportX = Camera.main.WorldToViewportPoint(transform.position).x;

            bool isHeadingRightEdge = viewportX >= 0.95f && turnLeftSpeed == 0;
            bool isHeadingLeftEdge = viewportX <= 0.05f && turnLeftSpeed > 0;

            if (isHeadingLeftEdge || isHeadingRightEdge)
            {
                isAtScreenEdge = true;

                if (PlayerHeadingAnEdge != null)
                    PlayerHeadingAnEdge();
            }
            else
            {
                isAtScreenEdge = false;
            }

            if (!stop && (GameManager.Instance.GameState != GameState.GameOver && (viewportX > 1.15f || viewportX < -0.15f)))
            {
                stop = true;
                cameraController.ShakeCamera();
                SoundManager.Instance.PlaySound(SoundManager.Instance.crash, true);
                Die();
            }
        }

        // Calls this when the player dies and game over
        public void Die()
        {
            if (!isDead)
            {
                isDead = true;

                // Fire event
                if (PlayerDied != null)
                    PlayerDied();
            }

        }

        public bool IsGoingOutOfScreen()
        {
            float viewportX = Camera.main.WorldToViewportPoint(transform.position).x;

            bool isHeadingRightEdge = viewportX >= 0.95f && turnLeftSpeed == 0;
            bool isHeadingLeftEdge = viewportX <= 0.05f && turnLeftSpeed > 0;

            return isHeadingRightEdge || isHeadingLeftEdge;
        }

        bool IsHittingObstacle(Collider other)
        {
            if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
            {
                return true;
            }

            return false;
        }

        IEnumerator IncreaseSpeed()
        {
            while (GameManager.Instance.GameState != GameState.GameOver)
            {
                if (startRun)
                {
                    if (turnRightSpeed < maxSpeed)
                    {
                        turnRightSpeed += increaseSpeedFactor * Time.deltaTime;
                    }

                    if (currentSpeedFactor < maxSpeedFactor)
                    {
                        currentSpeedFactor += increaseSpeedFactor * Time.deltaTime;
                    }
                }

                if (turnRightSpeed > maxSpeed)
                {
                    turnRightSpeed = maxSpeed;
                }
                if (currentSpeedFactor > maxSpeedFactor)
                {
                    currentSpeedFactor = maxSpeedFactor;
                }

                if (turnRightSpeed == maxSpeed && currentSpeedFactor == maxSpeedFactor)
                {
                    yield break;
                }

                yield return null;
            }
        }

        IEnumerator IncreaseCollisionForce()
        {
            while (GameManager.Instance.GameState != GameState.GameOver)
            {
                if (startRun)
                {
                    if (currentCollisionForce < maxCollisionForce)
                    {
                        currentCollisionForce += increaseCollisionForceFactor * Time.deltaTime;
                    }
                    else
                    {
                        currentCollisionForce = maxCollisionForce;
                        yield break;
                    }
                }
                yield return null;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (GameManager.Instance.GameState != GameState.GameOver)
            {
                if (other.CompareTag("Wall") || other.CompareTag("Obstacle")) //Hit wall or obstacle
                {
                    stop = true;
                    GetComponent<BoxCollider>().enabled = false;
                    mainCollider.isTrigger = false;
                    vehicleCollider.isTrigger = false;
                    AttackPhysics();
                    cameraController.ShakeCamera();
                    SoundManager.Instance.PlaySound(SoundManager.Instance.crash, true);
                    Die();
                }
                else if (other.CompareTag("Gold"))//Hit gold
                {
                    other.enabled = false;  // Disable the item's collider to prevent OnTriggerEnter being called again when it's going up
                    other.GetComponent<GoldController>().GoUp();
                    SoundManager.Instance.PlaySound(SoundManager.Instance.coin);
                    ScoreManager.Instance.AddScore(1);  // Count gifts collected this round
                    CoinManager.Instance.AddCoins(1);   // Total gifts having
                }
            }
        }

        void AttackPhysics()
        {
            StartCoroutine(AddCollisionForce(collisionDirection));
        }

        IEnumerator AddCollisionForce(Vector3 dir)
        {
            yield return new WaitForFixedUpdate();
            mainRigid.isKinematic = false;
            vehicleRigid.isKinematic = false;
            yield return null;
            vehicleRigid.AddForce(dir * currentCollisionForce);
            vehicleRigid.AddTorque(dir * currentCollisionForce);
        }
    }
}
