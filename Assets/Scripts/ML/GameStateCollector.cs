using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;

namespace AngryBirdsML
{
    /// <summary>
    /// Step 1: Data Collection
    /// Collects gameplay states from the Unity environment
    /// </summary>
    public class GameStateCollector : MonoBehaviour
    {
        [Header("References")]
        public SlingShot slingshot;
        public Transform targetArea;

        [Header("Collection Settings")]
        public bool collectingData = true;
        public int maxStatesPerEpisode = 1000;

        // Collected data storage
        private List<GameStateData> gameStates = new List<GameStateData>();
        private GameStateData currentState;

        // Track previous attempts
        private List<AttemptData> previousAttempts = new List<AttemptData>();

        private void Start()
        {
            InitializeCollection();
        }

        private void Update()
        {
            if (collectingData)
            {
                CollectCurrentState();
            }
        }

        /// <summary>
        /// Initialize data collection system
        /// </summary>
        public void InitializeCollection()
        {
            gameStates.Clear();
            previousAttempts.Clear();

            // Find slingshot if not assigned
            if (slingshot == null)
            {
                slingshot = FindFirstObjectByType<SlingShot>();
            }

            Debug.Log("GameStateCollector initialized");
        }

        /// <summary>
        /// Collect current game state (Step 1)
        /// </summary>
        public GameStateData CollectCurrentState()
        {
            // Refresh slingshot reference if needed
            if (slingshot == null)
            {
                slingshot = FindFirstObjectByType<SlingShot>();
            }

            // Safety check
            if (slingshot == null)
            {
                Debug.LogWarning("Slingshot reference is null, cannot collect state");
                return new GameStateData { timestamp = Time.time };
            }

            currentState = new GameStateData
            {
                timestamp = Time.time,

                // Bird information
                birdPosition = GetCurrentBirdPosition(),
                birdType = GetCurrentBirdType(),
                birdsRemaining = GameManager.birdsNumber,

                // Target information
                targetPositions = GetTargetPositions(),
                pigPositions = GetPigPositions(),

                // Obstacle layout
                obstacleLayout = GetObstacleLayout(),
                obstacleCount = GameManager.Bricks.Count,

                // Slingshot state
                slingshotPosition = slingshot.BirdWaitPosition.position,
                slingshotPulled = slingshot.slingshotState == SlingshotState.UserPulling,

                // Previous attempts data
                previousAttempts = new List<AttemptData>(this.previousAttempts),

                // Game state
                gameState = GameManager.CurrentGameState,
                pigsRemaining = GameManager.Pigs.Count(p => p != null),

                // Physics data
                gravity = Physics2D.gravity,
                timeScale = Time.timeScale
            };

            if (gameStates.Count < maxStatesPerEpisode)
            {
                gameStates.Add(currentState);
            }

            return currentState;
        }

        /// <summary>
        /// Record an attempt outcome
        /// </summary>
        public void RecordAttempt(Vector2 launchAngle, float launchForce, AttemptOutcome outcome)
        {
            AttemptData attempt = new AttemptData
            {
                angle = launchAngle,
                force = launchForce,
                outcome = outcome,
                damageDealt = outcome.damageDealt,
                pigsKilled = outcome.pigsKilled,
                blocksDestroyed = outcome.blocksDestroyed,
                hitTarget = outcome.hitTarget,
                timestamp = Time.time
            };

            previousAttempts.Add(attempt);
        }

        /// <summary>
        /// Get list of all collected states
        /// </summary>
        public List<GameStateData> GetCollectedStates()
        {
            return new List<GameStateData>(gameStates);
        }

        /// <summary>
        /// Clear collected data (called when episode ends)
        /// </summary>
        public void ClearData()
        {
            gameStates.Clear();
        }

        /// <summary>
        /// Reset for new episode
        /// </summary>
        public void ResetEpisode()
        {
            previousAttempts.Clear();
            gameStates.Clear();
        }

        // Helper methods for data collection

        private Vector3 GetCurrentBirdPosition()
        {
            if (GameManager.currentBirdIndex < GameManager.Birds.Count)
            {
                GameObject bird = GameManager.Birds[GameManager.currentBirdIndex];
                return bird != null ? bird.transform.position : Vector3.zero;
            }
            return Vector3.zero;
        }

        private int GetCurrentBirdType()
        {
            if (GameManager.currentBirdIndex < GameManager.Birds.Count)
            {
                GameObject bird = GameManager.Birds[GameManager.currentBirdIndex];
                if (bird != null)
                {
                    Bird birdComponent = bird.GetComponent<Bird>();
                    return birdComponent != null ? birdComponent.birdType : 0;
                }
            }
            return 0;
        }

        private List<Vector3> GetTargetPositions()
        {
            List<Vector3> targets = new List<Vector3>();

            // Get pig positions as primary targets
            foreach (GameObject pig in GameManager.Pigs)
            {
                if (pig != null)
                {
                    targets.Add(pig.transform.position);
                }
            }

            return targets;
        }

        private List<Vector3> GetPigPositions()
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (GameObject pig in GameManager.Pigs)
            {
                if (pig != null)
                {
                    positions.Add(pig.transform.position);
                }
            }
            return positions;
        }

        private List<ObstacleData> GetObstacleLayout()
        {
            List<ObstacleData> obstacles = new List<ObstacleData>();

            foreach (GameObject brick in GameManager.Bricks)
            {
                if (brick != null)
                {
                    Brick brickComponent = brick.GetComponent<Brick>();
                    ObstacleData obstacle = new ObstacleData
                    {
                        position = brick.transform.position,
                        rotation = brick.transform.rotation.eulerAngles,
                        scale = brick.transform.localScale,
                        health = brickComponent != null ? brickComponent.currentHealth : 0,
                        maxHealth = brickComponent != null ? brickComponent.health : 0,
                        obstacleType = GetObstacleType(brick)
                    };
                    obstacles.Add(obstacle);
                }
            }

            return obstacles;
        }

        private int GetObstacleType(GameObject brick)
        {
            // Determine obstacle type from name or components
            string name = brick.name.ToLower();
            if (name.Contains("wood")) return 0;
            if (name.Contains("glass")) return 1;
            if (name.Contains("stone")) return 2;
            return 0; // Default
        }
    }

    // Data structures for collected information

    [System.Serializable]
    public class GameStateData
    {
        public float timestamp;

        // Bird data
        public Vector3 birdPosition;
        public int birdType;
        public int birdsRemaining;

        // Targets
        public List<Vector3> targetPositions;
        public List<Vector3> pigPositions;

        // Obstacles
        public List<ObstacleData> obstacleLayout;
        public int obstacleCount;

        // Slingshot
        public Vector3 slingshotPosition;
        public bool slingshotPulled;

        // History
        public List<AttemptData> previousAttempts;

        // Game state
        public GameState gameState;
        public int pigsRemaining;

        // Physics
        public Vector2 gravity;
        public float timeScale;
    }

    [System.Serializable]
    public class ObstacleData
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public float health;
        public float maxHealth;
        public int obstacleType; // 0: wood, 1: glass, 2: stone
    }

    [System.Serializable]
    public class AttemptData
    {
        public Vector2 angle;
        public float force;
        public AttemptOutcome outcome;
        public float damageDealt;
        public int pigsKilled;
        public int blocksDestroyed;
        public bool hitTarget;
        public float timestamp;
    }

    [System.Serializable]
    public class AttemptOutcome
    {
        public bool hitTarget;
        public float damageDealt;
        public int pigsKilled;
        public int blocksDestroyed;
        public Vector3 impactPoint;
        public float timeToImpact;
    }
}
