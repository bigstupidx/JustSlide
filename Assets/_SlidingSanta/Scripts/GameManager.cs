using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using System;

namespace SgLib
{
    public enum GameState
    {
        Prepare,
        Playing,
        Paused,
        PreGameOver,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static event System.Action<GameState, GameState> GameStateChanged = delegate { };

        private static bool isRestart;

        public GameState GameState
        {
            get
            {
                return _gameState;
            }
            private set
            {
                if (value != _gameState)
                {
                    GameState oldState = _gameState;
                    _gameState = value;

                    GameStateChanged(_gameState, oldState);
                }
            }
        }

        public static int GameCount
        { 
            get { return _gameCount; } 
            private set { _gameCount = value; } 
        }

        private static int _gameCount = 0;

        [Header("Current game state (for debugging purposes)")]
        [SerializeField]
        private GameState _gameState = GameState.Prepare;

        [Header("Set the target frame rate for this game")]
        [Tooltip("Use 60 for games requiring smooth quick motion, set -1 to use platform default frame rate")]
        public int targetFrameRate = 60;

        [Header("Gameplay Config")]
        public int wallNumber;
        //How many wall is generated when the start
        public float maxBirdFlyingSpeed;
        //Max bird flying speed
        public float minBirdFlyingSpeed;
        //Min bird flying speed
        public float minWallDistance = 2;
        //The min space between 2 wall
        public float maxWallDistance = 3;
        //The max space between 2 wall
        [Range(0f, 1f)]
        public float pathDifficulty;
        //The larger value will generate the path harder
        [Range(0f, 1f)]
        public float obstacleFrequency;
        //Probability to create obstacle
        [Range(0f, 1f)]
        public float giftFrequency;
        //Probability to create gold
        [Range(0f, 1f)]
        public float birdFrequency;
        //Probability to create bird

        [Header("Gameplay References")]
        public UIManager uiManager;
        public PlayerController playerController;
        public EnvironmentManager envManager;
        public GameObject pineManager;
        public GameObject obstaclesManager;
        public GameObject birdManager;
        public GameObject giftPrefab;
        public GameObject wallBlockPrefab;
        public GameObject landPrefab;
        public GameObject firstLeftWall;
        public GameObject firstRightWall;
        public GameObject firstLand;
        public Material planeMaterial;
        public GameObject mainHouse;

        [Header("Game Center")]
        private string ios_Leaderboard = "christmasdefense_normalmode";
        private string android_Leaderboard = "CgkItYWW9sgKEAIQAA";
        private string leaderboard;
        
        private List<GameObject> listLeftWall = new List<GameObject>();
        //Stored the left twall
        private List<GameObject> listRightWall = new List<GameObject>();
        //Stored the right wall
        private List<GameObject> listLand = new List<GameObject>();
        //Stored the land

        private Vector3 nextLeftWallPosition;
        //Position to generate next left wall
        private Vector3 nextRightWallPosition;
        //Position to generate next right wall
        private Vector3 nextLandPosition;
        //Position to generate next land
        private float landNumber;
        //Howmany land is generated when the start
        private int obstacleNumber;
        //How many obstacle is generated when the start
        private float yLandSize;
        private float wallSize = 22f;
        //The space between left wall and next left wall (keep this value to generate nice path)
        private float totalPineNumber;
        //How many pine will be created
        [HideInInspector]
        public float pineNumberInOneWall = 7f;
        //How many pine is created in one wall
        private float pineDistance = 1.6f;
        private int goldNumber;
        //How many gold is generated when the start
        private int birdNumber;
        //How many bird is generated when the start
        private int listLeftIndex = 0;
        private int listRightIndex = 0;
        private int listLandIndex = 0;
        private bool gameStarted;
        private bool isPreGameOver;
        private EnvironmentManager.Environment currentEnv;
        private GameState tmpState;
        private int tempHighScore;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(Instance.gameObject);
                Instance = this;
            }
            //GooglePlayGames.PlayGamesPlatform.Activate();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            StopAllCoroutines();
        }

        void OnEnable()
        {
            PlayerController.PlayerDied += PlayerController_PlayerDied;
            PlayerController.PlayerHeadingAnEdge += PlayerController_PlayerHeadingAnEdge;
            PreCrashDetector.PreCrashing += OnPreCrashing;
        }

        void OnDisable()
        {
            PlayerController.PlayerDied -= PlayerController_PlayerDied;
            PlayerController.PlayerHeadingAnEdge -= PlayerController_PlayerHeadingAnEdge;
            PreCrashDetector.PreCrashing -= OnPreCrashing;
        }

        // Use this for initialization
        void Start()
        {
            // Initial setup
            //GooglePlayGames.PlayGamesPlatform.Activate();
            Application.targetFrameRate = targetFrameRate;
            

            PrepareGame();
            tempHighScore = ScoreManager.Instance.HighScore;


        }


        //Show Unity Ads
        public void ShowAd(string zone)
        {
            UnityAds.instance.ShowAd(zone);
            
        }




        // Listens to the event when player dies and call GameOver
        void PlayerController_PlayerDied()
        {
            if (GameState != GameState.GameOver)
                GameOver();
        }

        void OnPreCrashing()
        {
            if (!isPreGameOver)
            {
                SetPreGameOver(0.5f);
            }
        }

        void PlayerController_PlayerHeadingAnEdge()
        {
            if (!isPreGameOver)
            {
                SetPreGameOver(0.5f);
            }
        }

        // Make initial setup and preparations before the game can be played
        public void PrepareGame()
        {
            GameState = GameState.Prepare;
            currentEnv = envManager.GetCurrentEvironment();
            StartCoroutine(WaitToGetGameObjectList());
            SoundManager.Instance.PlayMusic(SoundManager.Instance.menuMusic);
        }

        private IEnumerator WaitToGetGameObjectList()
        {
            yield return new WaitForEndOfFrame();
            DoPrepareGame();
            if (isRestart)
            {
                isRestart = false;
                StartGame();
            }
            StartCoroutine(CheckAndMoveWallAndLand());
            StartCoroutine(CheckAndDisableSantaHouse());
        }

        // A new game official starts
        public void StartGame()
        {
            if (_gameState.Equals(GameState.GameOver))
            {
                RestartGame();
            }
            else
            {
                GameState = GameState.Playing;

                if (currentEnv.useCustomBackgroundMusic && currentEnv.backgroundMusic.clip != null)
                {
                    SoundManager.Instance.PlayMusic(currentEnv.backgroundMusic);
                }
                else
                {
                    SoundManager.Instance.PlayMusic(SoundManager.Instance.ingameMusic);
                }
            }
        }

        // Called when the player died
        public void GameOver()
        {
            GameState = GameState.GameOver;
            GameCount++;
            if(GameCount % 5 == 0)
            {
                //UnityAds.instance.ShowAd();
            }
            GoogleManager.ReportScore();
            AppsFlyerMMP.Score(ScoreManager.Instance.Score);
            if(ScoreManager.Instance.HighScore > tempHighScore)
            {
                AppsFlyerMMP.HighScore();
            }

            
            StartCoroutine(CRStopMusic(1f));
        }

        IEnumerator CRStopMusic(float delay)
        {
            yield return new WaitForSeconds(delay);
            SoundManager.Instance.StopMusic();
            SoundManager.Instance.PlaySound(SoundManager.Instance.gameOver);
            yield return new WaitForSeconds(2);
            SoundManager.Instance.PlayMusic(SoundManager.Instance.menuMusic);
        }

        // Start a new game
        public void RestartGame(float delay = 0)
        {
            isRestart = true;
            StartCoroutine(CRRestartGame(delay));
        }

        IEnumerator CRRestartGame(float delay = 0)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void SetPreGameOver(float resetDelay)
        {
            isPreGameOver = true;

            // Change state and fire event
            tmpState = GameState;
            GameState = GameState.PreGameOver;

            // If the detection was right, game over should occur within a short time.
            // Otherwise, we need to reset to repeat the whole process in case the player survives.
            StartCoroutine(CRResetIsPreGameOver(resetDelay));
        }

        IEnumerator CRResetIsPreGameOver(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (GameManager.Instance.GameState != GameState.GameOver)
            {
                isPreGameOver = false;
                GameState = tmpState;
            }
        }

        void DoPrepareGame()
        {
            ScoreManager.Instance.Reset();
            yLandSize = landPrefab.transform.localScale.y; //Get y scale of land
            firstLand.transform.parent = transform; //Parrenting land
            firstLeftWall.transform.parent = transform; //Parrentting first left wall
            firstRightWall.transform.parent = transform;//Parrentting first right wall
            totalPineNumber = (pineNumberInOneWall * pineNumberInOneWall) * ((wallNumber + 1) * 2); //Caculate total pine number
            goldNumber = 10 * (wallNumber + 1);
            obstacleNumber = (wallNumber + 1) * 2;
            birdNumber = wallNumber + 1;
            landNumber = Mathf.Ceil(wallNumber / 2f);
            pineDistance = 7f * 1.6f / pineNumberInOneWall;

            nextLeftWallPosition = firstLeftWall.transform.position + Vector3.back * wallSize; //Set position of next left wall
            nextRightWallPosition = firstRightWall.transform.position + Vector3.back * wallSize;//Set position of next right wall
            nextLandPosition = firstLand.transform.position + Vector3.back * yLandSize;//Set position of next land

            listLand.Add(firstLand);
            listLeftWall.Add(firstLeftWall);
            listRightWall.Add(firstRightWall);

            // Instantiate main house
            if (mainHouse != null)
                Destroy(mainHouse);
            
            mainHouse = Instantiate(currentEnv.mainHouse);

            //Instantiate trees
            for (int i = 0; i < totalPineNumber; i++)
            {
                GameObject thePine = Instantiate(currentEnv.trees[UnityEngine.Random.Range(0, currentEnv.trees.Length)]);
                thePine.SetActive(false);
                thePine.transform.parent = pineManager.transform;
            }

            //Instantiate rocks
            for (int i = 0; i < obstacleNumber; i++)
            {
                GameObject obstacle = Instantiate(currentEnv.obstacles[UnityEngine.Random.Range(0, currentEnv.obstacles.Length)]);
                obstacle.SetActive(false);
                obstacle.transform.parent = obstaclesManager.transform;
            }

            // Create fences
            Material wallMat = null;
            Mesh wallMesh = null;

            if (currentEnv.enableFence && currentEnv.fencePrefab != null)
            {
                wallMat = currentEnv.fencePrefab.GetComponent<MeshRenderer>().sharedMaterial;
                wallMesh = currentEnv.fencePrefab.GetComponent<MeshFilter>().sharedMesh;
            }

            ReplaceWallMesh(firstLeftWall, wallMat, wallMesh);
            ReplaceWallMesh(firstRightWall, wallMat, wallMesh);

            //Instantiate left fences
            for (int i = 0; i < wallNumber; i++)
            {
                GameObject theLeftWall = Instantiate(wallBlockPrefab);
                ReplaceWallMesh(theLeftWall, wallMat, wallMesh);
                theLeftWall.SetActive(false);
                theLeftWall.transform.parent = transform;
                listLeftWall.Add(theLeftWall);
            }

            //Instantiate right fences
            for (int i = 0; i < wallNumber; i++)
            {
                GameObject theRightWall = Instantiate(wallBlockPrefab);
                ReplaceWallMesh(theRightWall, wallMat, wallMesh);
                theRightWall.SetActive(false);
                theRightWall.transform.parent = transform;
                listRightWall.Add(theRightWall);
            }

            // Instantiate land
            // Change land color
            planeMaterial.color = currentEnv.planeColor;
            for (int i = 0; i < landNumber; i++)
            {
                GameObject theLand = Instantiate(landPrefab);
                theLand.SetActive(false);
                theLand.transform.parent = transform;
                listLand.Add(theLand);
            }

            //Instantiate gifts
            for (int i = 0; i < goldNumber; i++)
            {
                GameObject theGift = Instantiate(giftPrefab);
                theGift.SetActive(false);
                theGift.transform.parent = CoinManager.Instance.transform;
            }

            //Instantiate birds
            if (currentEnv.enableBirds && currentEnv.birdPrefabs != null && currentEnv.birdPrefabs.Length > 0)
            {
                for (int i = 0; i < birdNumber; i++)
                {
                    GameObject theBird = Instantiate(currentEnv.birdPrefabs[UnityEngine.Random.Range(0, currentEnv.birdPrefabs.Length)]);
                    theBird.SetActive(false);
                    theBird.transform.parent = birdManager.transform;
                }
            }

            // Create particles
            if (currentEnv.enableFallingParticle && currentEnv.fallingParticle != null)
            {
                GameObject p = Instantiate(
                                   currentEnv.fallingParticle,  
                                   Camera.main.transform);

                p.transform.localPosition = currentEnv.fallingParticle.transform.position;
                p.transform.localRotation = currentEnv.fallingParticle.transform.rotation;
                p.GetComponent<ParticleSystem>().Play();
            }

            EnableLand();

            //Find the point of first left wall and create pines 
            for (int i = 0; i < firstLeftWall.transform.childCount; i++)
            {
                if (firstLeftWall.transform.GetChild(i).CompareTag("Point"))
                {
                    CreateTree(firstLeftWall.transform.GetChild(i).gameObject, firstLeftWall);
                }
            }

            //Find the point of first right wall and create pines 
            for (int i = 0; i < firstRightWall.transform.childCount; i++)
            {
                if (firstRightWall.transform.GetChild(i).CompareTag("Point"))
                {
                    CreateTree(firstRightWall.transform.GetChild(i).gameObject, firstRightWall);
                }
            }

            //Active all wall (use listLeftWall.Count cause the length of listLeftWall and listRightWall is the same) 
            for (int i = 1; i < listLeftWall.Count; i++)
            {
                float movingDistance = (UnityEngine.Random.value <= pathDifficulty)
                            ? (UnityEngine.Random.Range(minWallDistance, maxWallDistance))
                            : 0;
                MoveLeftWallToNextPosition(listLeftWall[i], movingDistance);
                listLeftWall[i].SetActive(true);

                MoveRightWallToNextPosition(listRightWall[i], -movingDistance);
                listRightWall[i].SetActive(true);
                CreateItem(listLeftWall[i], listRightWall[i]);
            }
        }

        private void ReplaceWallMesh(GameObject wall, Material mat, Mesh mesh)
        {
            MeshRenderer[] meshRendererList = wall.GetComponentsInChildren<MeshRenderer>();
            MeshFilter[] meshFilterList = wall.GetComponentsInChildren<MeshFilter>();

            if (currentEnv.enableFence && mat != null && mesh != null)
            {
                foreach (var item in meshRendererList)
                {
                    item.material = mat;
                }
                foreach (var item in meshFilterList)
                {
                    item.mesh = mesh;
                }
            }
            else
            {
                foreach (var item in meshRendererList)
                {
                    item.enabled = false;
                }
            }
        }

        void EnableLand()
        {
            for (int i = 1; i < listLand.Count; i++)
            {
                MoveLandToNextPosition(listLand[i]);
                listLand[i].SetActive(true);
            }
        }

        void MoveLandToNextPosition(GameObject land)
        {
            land.transform.position = nextLandPosition;
            nextLandPosition = land.transform.position + Vector3.back * yLandSize;
        }

        //Move left wall to next position and create pines for it
        void MoveLeftWallToNextPosition(GameObject wall, float distance)
        {
            wall.transform.position = nextLeftWallPosition;
            nextLeftWallPosition = wall.transform.position + Vector3.back * wallSize;
            wall.transform.position += new Vector3(distance, 0, 0);
            for (int i = 0; i < wall.transform.childCount; i++)
            {
                if (wall.transform.GetChild(i).CompareTag("Point"))
                {
                    CreateTree(wall.transform.GetChild(i).gameObject, wall);
                    break;
                }
            }
        }

        //Move right wall to next position and create pines for it
        void MoveRightWallToNextPosition(GameObject wall, float distance)
        {
            wall.transform.position = nextRightWallPosition;
            nextRightWallPosition = wall.transform.position + Vector3.back * wallSize;
            wall.transform.position += new Vector3(distance, 0, 0);

            for (int i = 0; i < wall.transform.childCount; i++)
            {
                if (wall.transform.GetChild(i).CompareTag("Point"))
                {
                    CreateTree(wall.transform.GetChild(i).gameObject, wall);
                    break;
                }
            }
        }

        //Create trees base on the point of the wall
        void CreateTree(GameObject point, GameObject obstacle)
        {
            Vector3 createDirection = Vector3.forward + Vector3.left;
            Vector3 position = point.transform.position;
            for (int i = 0; i < pineNumberInOneWall; i++)
            {
                GameObject currentPine = GetRandomPine();
                currentPine.transform.position = position;
                currentPine.transform.rotation = Quaternion.Euler(0, 45, 0);
                currentPine.transform.parent = obstacle.transform;
                currentPine.SetActive(true);
                FilledWall(currentPine, obstacle);
                position = currentPine.transform.position + createDirection * pineDistance;
            }
        }

        //Filled the wall by pines
        void FilledWall(GameObject pine, GameObject obstacle)
        {
            Vector3 position = pine.transform.position + (Vector3.back + Vector3.left) * pineDistance;
            for (int i = 0; i < pineNumberInOneWall - 1; i++)
            {
                GameObject currentPine = GetRandomPine();
                currentPine.transform.position = position;
                currentPine.transform.rotation = Quaternion.Euler(0, 45, 0);
                currentPine.transform.parent = obstacle.transform;
                currentPine.SetActive(true);
                position = currentPine.transform.position + (Vector3.back + Vector3.left) * pineDistance;
            }
        }

        //Create gold or obstacle base on left wall and right wall
        void CreateItem(GameObject leftWall, GameObject rightWall)
        {

            GameObject barrier_1 = leftWall.transform.Find("Barrier_1").gameObject;
            GameObject barrier_4 = rightWall.transform.Find("Barrier_4").gameObject;
            float barrierLength = barrier_1.GetComponent<Renderer>().bounds.size.x; 
            float maxDistance = (Mathf.Abs(leftWall.transform.position.x) + Mathf.Abs(rightWall.transform.position.x)) / 5f;
            float minDistance = 3f;
            for (float i = 0; i < barrierLength / 2f; i = i + 2)
            {
                if (UnityEngine.Random.value <= 0.5f)
                {
                    if (UnityEngine.Random.value <= giftFrequency)
                    {
                        GameObject gold = GetRandomGold();
                        gold.transform.position = barrier_1.transform.position +
                        (Vector3.forward + Vector3.right) * i +
                        (Vector3.right + Vector3.back) * UnityEngine.Random.Range(minDistance, maxDistance);
                        gold.transform.parent = leftWall.transform;
                        gold.SetActive(true);
                    }
                }
                else
                {
                    if (UnityEngine.Random.value <= obstacleFrequency)
                    {
                        GameObject obstacle = GetRandomObstacle();
                        obstacle.transform.position = barrier_1.transform.position +
                        (Vector3.forward + Vector3.right) * i +
                        (Vector3.right + Vector3.back) * UnityEngine.Random.Range(minDistance, maxDistance);
                        obstacle.transform.parent = leftWall.transform;
                        obstacle.SetActive(true);
                    }
                }
                      

                if (i == 2)
                {
                    if (UnityEngine.Random.value <= 0.5f)
                    {
                        if (UnityEngine.Random.value <= giftFrequency)
                        {
                            GameObject gold = GetRandomGold();
                            gold.transform.position = barrier_1.transform.position +
                            (Vector3.back + Vector3.left) * i +
                            (Vector3.right + Vector3.back) * UnityEngine.Random.Range(minDistance, maxDistance);
                            gold.transform.parent = leftWall.transform;
                            gold.SetActive(true);
                        }
                    }
                    else
                    {
                        if (UnityEngine.Random.value <= obstacleFrequency)
                        {
                            GameObject obstacle = GetRandomObstacle();
                            obstacle.transform.position = barrier_1.transform.position +
                            (Vector3.back + Vector3.left) * i +
                            (Vector3.right + Vector3.back) * UnityEngine.Random.Range(minDistance, maxDistance);
                            obstacle.transform.parent = leftWall.transform;
                            obstacle.SetActive(true);
                        }
                    }              
                }
            }

       
            for (float i = 0; i < barrierLength / 2f; i = i + 2)
            {
                if (UnityEngine.Random.value <= 0.5f)
                {
                    if (UnityEngine.Random.value <= giftFrequency)
                    {
                        GameObject gold = GetRandomGold();
                        gold.transform.position = barrier_4.transform.position +
                        (Vector3.forward + Vector3.left) * i +
                        (Vector3.left + Vector3.back) * UnityEngine.Random.Range(minDistance, maxDistance);
                        gold.transform.parent = rightWall.transform;
                        gold.SetActive(true);
                    }
                }
                else
                {
                    if (UnityEngine.Random.value <= obstacleFrequency)
                    {
                        GameObject obstacle = GetRandomObstacle();
                        obstacle.transform.position = barrier_4.transform.position +
                        (Vector3.forward + Vector3.left) * i +
                        (Vector3.left + Vector3.back) * UnityEngine.Random.Range(minDistance, maxDistance);
                        obstacle.transform.parent = rightWall.transform;
                        obstacle.SetActive(true);
                    }
                }

                if (i == 2)
                {
                    if (UnityEngine.Random.value <= 0.5f)
                    {
                        if (UnityEngine.Random.value <= giftFrequency)
                        {
                            GameObject gold = GetRandomGold();
                            gold.transform.position = barrier_4.transform.position +
                            (Vector3.back + Vector3.right) * i +
                            (Vector3.left + Vector3.back) * UnityEngine.Random.Range(minDistance, maxDistance);
                            gold.transform.parent = rightWall.transform;
                            gold.SetActive(true);
                        }
                    }
                    else
                    {
                        if (UnityEngine.Random.value <= obstacleFrequency)
                        {
                            GameObject obstacle = GetRandomObstacle();
                            obstacle.transform.position = barrier_4.transform.position +
                            (Vector3.back + Vector3.right) * i +
                            (Vector3.left + Vector3.back) * UnityEngine.Random.Range(minDistance, maxDistance);
                            obstacle.transform.parent = rightWall.transform;
                            obstacle.SetActive(true);
                        }
                    }
               
                }            
            }
        }


        /*Create bird base on viewport point of camera 
        (the bird will get by BirdManager(BirdManager is the place that stored the bird))*/
        void CreateBird()
        {
            Vector3 birdPos_1 = Camera.main.ViewportToWorldPoint(new Vector3(1.2f, -0.2f, 0f));
            Vector3 birdPos_2 = Camera.main.ViewportToWorldPoint(new Vector3(-0.2f, -0.2f, 0f));
            GameObject bird = GetRandomBird();
            bird.transform.parent = null;
            if (UnityEngine.Random.value <= 0.5f)
            {
                bird.transform.position = birdPos_1;
                bird.transform.rotation = Quaternion.Euler(0, 225, 0);
                StartCoroutine(MoveBird(bird, Vector3.forward + Vector3.left));
            }
            else
            {
                bird.transform.position = birdPos_2;
                bird.transform.rotation = Quaternion.Euler(0, 315, 0);
                StartCoroutine(MoveBird(bird, Vector3.forward + Vector3.right));
            }
            bird.SetActive(true);

            if (UnityEngine.Random.Range(0, 2) == 1)
            {
                Invoke("BirdChirp", UnityEngine.Random.Range(1f, 3f));
            }
        }

        void BirdChirp()
        {
            SoundManager.Instance.PlaySound(SoundManager.Instance.birdChirp);
        }

        //Move the bird
        IEnumerator MoveBird(GameObject bird, Vector3 movingDirection)
        {
            float speed = UnityEngine.Random.Range(minBirdFlyingSpeed, maxBirdFlyingSpeed);
            while (Camera.main.WorldToViewportPoint(bird.transform.position).y < 1f)
            {
                bird.transform.position += movingDirection * speed * Time.deltaTime;
                yield return null;
            }

            bird.SetActive(false);
            bird.transform.parent = birdManager.transform;
        }

        //Get random pine object base on pinesManager
        GameObject GetRandomPine()
        {
            return pineManager.transform.GetChild(UnityEngine.Random.Range(0, pineManager.transform.childCount)).gameObject;
        }

        //Get random obstacle base on obstaclesManager
        GameObject GetRandomObstacle()
        {
            return obstaclesManager.transform.GetChild(UnityEngine.Random.Range(0, obstaclesManager.transform.childCount)).gameObject;
        }

        //Get random gold
        GameObject GetRandomGold()
        {
            GameObject obj = CoinManager.Instance.transform.GetChild(UnityEngine.Random.Range(0, CoinManager.Instance.transform.childCount)).gameObject;
            obj.GetComponent<GoldController>().stopBounce = false;
            obj.GetComponent<MeshCollider>().enabled = true;

            return obj;
        }

        //Get random bird
        GameObject GetRandomBird()
        {
            return birdManager.transform.GetChild(UnityEngine.Random.Range(0, birdManager.transform.childCount)).gameObject;
        }

  
        /*Check if the wall and the land run out of camera -> move land to next position, 
        refesh the wall (disable all pine of the wall)
        , then move it to next position and create new pines*/
        IEnumerator CheckAndMoveWallAndLand()
        {
            while (GameManager.Instance.GameState != GameState.GameOver)
            {
                float yWall = Camera.main.WorldToViewportPoint(listRightWall[listRightIndex].transform.position).y;
                if (yWall > 2f)
                {
                    float distance = (UnityEngine.Random.value <= pathDifficulty)
                            ? (UnityEngine.Random.Range(minWallDistance, maxWallDistance))
                            : 0;

                    RefeshWall(listLeftWall[listLeftIndex]);
                    RefeshWall(listRightWall[listRightIndex]);

                    MoveLeftWallToNextPosition(listLeftWall[listLeftIndex], distance);
                    MoveRightWallToNextPosition(listRightWall[listRightIndex], -distance);

                    CreateItem(listLeftWall[listLeftIndex], listRightWall[listRightIndex]);
                
                    if (currentEnv.enableBirds && UnityEngine.Random.value <= birdFrequency)
                    {
                        CreateBird();
                    }

                    listLeftIndex = (listLeftIndex + 1 == listLeftWall.Count) ? (0) : (listLeftIndex + 1);
                    listRightIndex = (listRightIndex + 1 == listRightWall.Count) ? (0) : (listRightIndex + 1);
                }

                float yLand = Camera.main.WorldToViewportPoint(listLand[listLandIndex].transform.position).y;

                if (yLand > 2f)
                {
                    MoveLandToNextPosition(listLand[listLandIndex]);
                    listLandIndex = (listLandIndex + 1 == listLand.Count) ? (0) : (listLandIndex + 1);
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        //Find all pines or gold of the wall -> unactive and set parrent for it
        void RefeshWall(GameObject wall)
        {
            List<GameObject> listPine = ListPine(wall);
            for (int i = 0; i < listPine.Count; i++)
            {
                listPine[i].SetActive(false);
                listPine[i].transform.parent = pineManager.transform;
            }

            List<GameObject> listRock = ListObstacle(wall);
            for (int i = 0; i < listRock.Count; i++)
            {
                listRock[i].SetActive(false);
                listRock[i].transform.parent = obstaclesManager.transform;
            }

            List<GameObject> listGold = ListGold(wall);
            for (int i = 0; i < listGold.Count; i++)
            {
                listGold[i].SetActive(false);
                listGold[i].transform.parent = CoinManager.Instance.transform;
            }
        }

        List<GameObject> ListPine(GameObject wall)
        {
            List<GameObject> newList = new List<GameObject>();
            for (int i = 0; i < wall.transform.childCount; i++)
            {
                if (wall.transform.GetChild(i).CompareTag("Pine"))
                {
                    newList.Add(wall.transform.GetChild(i).gameObject);
                }
            }

            return newList;
        }

        List<GameObject> ListObstacle(GameObject wall)
        {
            List<GameObject> newList = new List<GameObject>();
            for (int i = 0; i < wall.transform.childCount; i++)
            {
                if (wall.transform.GetChild(i).CompareTag("Obstacle"))
                {
                    newList.Add(wall.transform.GetChild(i).gameObject);
                }
            }

            return newList;
        }

        List<GameObject> ListGold(GameObject wall)
        {
            List<GameObject> newList = new List<GameObject>();
            for (int i = 0; i < wall.transform.childCount; i++)
            {
                if (wall.transform.GetChild(i).CompareTag("Gold"))
                {
                    newList.Add(wall.transform.GetChild(i).gameObject);
                }
            }

            return newList;
        }

        IEnumerator CheckAndDisableSantaHouse()
        {
            while (GameManager.Instance.GameState != GameState.GameOver)
            {
                if (Camera.main.WorldToViewportPoint(mainHouse.transform.position).y > 1.5f)
                {
                    mainHouse.SetActive(false);
                    yield break;
                }

                yield return null;
            }
        }
    }
}
