using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace AngryBirdsML
{
    /// <summary>
    /// Step 4 & 9: ML Agent Brain
    /// Controls the AI agent behavior, action selection, and deployment
    /// Interfaces with Python training code via sockets
    /// </summary>
    public class MLAgentBrain : MonoBehaviour
    {
        [Header("Mode Settings")]
        public AgentMode mode = AgentMode.Training;
        public bool useRandomActions = true; // Set to true for Unity-only training
        public bool usePythonServer = false; // Set to true only if Python server is running
        public bool usePerfectShots = false; // Set to true to generate perfect training data!

        [Header("Network Settings")]
        public string pythonServerHost = "localhost";
        public int pythonServerPort = 5004;
        public float connectionTimeout = 5f;

        [Header("Exploration")]
        public float explorationRate = 0.1f;
        public float explorationDecay = 0.995f;
        public float minExplorationRate = 0.01f;

        [Header("References")]
        public GameStateCollector stateCollector;
        public DataPreprocessor preprocessor;
        public StateVectorConverter stateVectorConverter;
        public RewardCalculator rewardCalculator;
        public SlingShot slingshot;

        [Header("Model Path (for Deployment)")]
        public string modelPath = "../ML/models/trained_model.onnx";

        // Communication
        private TcpClient client;
        private NetworkStream stream;
        private bool connected = false;

        // State tracking
        private GameStateData currentState;
        private GameStateData previousState;
        private float[] currentStateVector;

        // Action tracking
        private Vector2 lastAction; // [angle, force]
        private bool actionExecuted = false;

        // Episode tracking
        private int episodeCount = 0;
        private float episodeReward = 0f;
        private int stepCount = 0;

        public enum AgentMode
        {
            Training,
            Deployment,
            Testing
        }

        private void Start()
        {
            InitializeAgent();
        }

        private void Update()
        {
            if (mode == AgentMode.Training || mode == AgentMode.Deployment)
            {
                // Check if we should take an action
                if (ShouldTakeAction())
                {
                    TakeAction();
                }
            }
        }

        /// <summary>
        /// Initialize the agent
        /// </summary>
        public void InitializeAgent()
        {
            // Get or add required components
            if (stateCollector == null)
                stateCollector = GetComponent<GameStateCollector>() ?? gameObject.AddComponent<GameStateCollector>();

            if (preprocessor == null)
                preprocessor = GetComponent<DataPreprocessor>() ?? gameObject.AddComponent<DataPreprocessor>();

            if (stateVectorConverter == null)
                stateVectorConverter = GetComponent<StateVectorConverter>() ?? gameObject.AddComponent<StateVectorConverter>();

            if (rewardCalculator == null)
                rewardCalculator = GetComponent<RewardCalculator>() ?? gameObject.AddComponent<RewardCalculator>();

            // Find scene references (slingshot)
            RefreshSceneReferences();

            // Connect to Python server only if enabled and in training mode
            if (mode == AgentMode.Training && usePythonServer)
            {
                ConnectToPythonServer();
            }
            else if (mode == AgentMode.Training)
            {
                Debug.Log("Python server connection disabled - using Unity-only training mode");
            }

            Debug.Log($"ML Agent initialized in {mode} mode");
        }

        /// <summary>
        /// Refresh scene references (called after scene reload)
        /// </summary>
        private void RefreshSceneReferences()
        {
            // Re-find slingshot if it's null or destroyed
            if (slingshot == null)
            {
                slingshot = FindFirstObjectByType<SlingShot>();
                if (slingshot != null)
                {
                    Debug.Log("Slingshot reference refreshed");
                }
            }
        }

        /// <summary>
        /// Check if agent should take an action
        /// </summary>
        private bool ShouldTakeAction()
        {
            // Refresh references in case scene was reloaded
            RefreshSceneReferences();

            // Only act during playing state
            if (GameManager.CurrentGameState != Assets.Scripts.GameState.Playing)
                return false;

            // Check if slingshot exists and is idle
            if (slingshot == null)
                return false;

            // Only act when slingshot is idle and we haven't acted yet
            if (slingshot.slingshotState == Assets.Scripts.SlingshotState.Idle && !actionExecuted)
                return true;

            return false;
        }

        /// <summary>
        /// Step 4: Action Selection
        /// Select action using policy network or exploration
        /// </summary>
        public void TakeAction()
        {
            // Collect current state
            currentState = stateCollector.CollectCurrentState();
            currentStateVector = stateVectorConverter.CreateStateVector(currentState);

            // Select action
            Vector2 action;

            if (usePerfectShots)
            {
                // Perfect shot (for generating training data)
                action = CalculatePerfectShot();
                Debug.Log($"🎯 Perfect Shot: angle={action.x:F1}°, force={action.y:F2}");
            }
            else if (useRandomActions || UnityEngine.Random.value < explorationRate)
            {
                // Random action (exploration)
                action = SelectRandomAction();
                Debug.Log($"Exploring: Random action selected");
            }
            else
            {
                // Policy network action (exploitation)
                action = SelectPolicyAction(currentStateVector);
                Debug.Log($"Exploiting: Policy action selected");
            }

            // Execute action
            ExecuteAction(action);

            actionExecuted = true;
            stepCount++;
        }

        /// <summary>
        /// Select random action for exploration
        /// </summary>
        private Vector2 SelectRandomAction()
        {
            float angle = UnityEngine.Random.Range(-90f, 90f);
            float force = UnityEngine.Random.Range(0.3f, 1.0f);
            return new Vector2(angle, force);
        }

        /// <summary>
        /// Calculate perfect shot using physics trajectory
        /// </summary>
        private Vector2 CalculatePerfectShot()
        {
            // Get target position (nearest pig or structure)
            Vector3 targetPos = GetNearestTarget();
            Vector3 birdPos = slingshot.BirdWaitPosition.position;

            Debug.Log($"Bird position: {birdPos}, Target position: {targetPos}");

            // Calculate direction to target
            Vector3 toTarget = targetPos - birdPos;
            float horizontalDist = toTarget.x;
            float verticalDist = toTarget.y;

            // Calculate angle to target (in degrees)
            float targetAngle = Mathf.Atan2(verticalDist, horizontalDist) * Mathf.Rad2Deg;

            // Adjust for optimal trajectory (aim a bit higher for arc)
            targetAngle += 15f; // Add arc to the shot

            // Clamp angle to reasonable range
            targetAngle = Mathf.Clamp(targetAngle, 10f, 70f);

            // Add slight randomness for realism (±3 degrees)
            float angle = targetAngle + UnityEngine.Random.Range(-3f, 3f);

            // Calculate distance to target
            float distance = Vector2.Distance(new Vector2(birdPos.x, birdPos.y),
                                            new Vector2(targetPos.x, targetPos.y));

            // Calculate force based on distance
            // Closer targets need less force, farther targets need more
            float baseForce = Mathf.Clamp(distance / 10f, 0.5f, 0.95f);

            // Add slight randomness to force (±0.05)
            float force = baseForce + UnityEngine.Random.Range(-0.05f, 0.05f);
            force = Mathf.Clamp(force, 0.4f, 1.0f);

            Debug.Log($"Perfect shot calculated: angle={angle:F1}°, force={force:F2}, distance={distance:F1}");

            return new Vector2(angle, force);
        }

        /// <summary>
        /// Get nearest target position (pig or structure)
        /// </summary>
        private Vector3 GetNearestTarget()
        {
            Vector3 birdPos = slingshot.transform.position;
            float minDistance = float.MaxValue;
            Vector3 nearestTarget = birdPos + new Vector3(5f, 2f, 0f); // Default target

            // Check pigs first (priority targets)
            if (GameManager.Pigs != null && GameManager.Pigs.Count > 0)
            {
                foreach (GameObject pig in GameManager.Pigs)
                {
                    if (pig != null)
                    {
                        float dist = Vector3.Distance(birdPos, pig.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearestTarget = pig.transform.position;
                        }
                    }
                }
            }

            // If no pigs, aim for structures
            if (minDistance == float.MaxValue && GameManager.Bricks != null && GameManager.Bricks.Count > 0)
            {
                foreach (GameObject brick in GameManager.Bricks)
                {
                    if (brick != null)
                    {
                        float dist = Vector3.Distance(birdPos, brick.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearestTarget = brick.transform.position;
                        }
                    }
                }
            }

            return nearestTarget;
        }

        /// <summary>
        /// Select action using policy network
        /// </summary>
        private Vector2 SelectPolicyAction(float[] stateVector)
        {
            if (mode == AgentMode.Training && usePythonServer && connected)
            {
                // Request action from Python server
                return RequestActionFromPython(stateVector);
            }
            else if (mode == AgentMode.Deployment)
            {
                // Use loaded model (ONNX runtime)
                return PredictActionFromModel(stateVector);
            }
            else
            {
                // Fallback to random (Unity-only training)
                return SelectRandomAction();
            }
        }

        /// <summary>
        /// Step 5: Execute action in Unity physics
        /// </summary>
        private void ExecuteAction(Vector2 action)
        {
            lastAction = action;

            float angle = action.x;
            float force = action.y;

            Debug.Log($"Executing action: Angle={angle:F2}°, Force={force:F2}");

            // Convert to slingshot pull position
            Vector3 pullPosition = CalculatePullPosition(angle, force);

            // Simulate slingshot pull and release
            StartCoroutine(SimulateSlingshot(pullPosition));
        }

        /// <summary>
        /// Calculate slingshot pull position from angle and force
        /// </summary>
        private Vector3 CalculatePullPosition(float angleDegrees, float force)
        {
            // Convert angle to radians
            float angleRad = angleDegrees * Mathf.Deg2Rad;

            // Calculate pull distance (force determines how far to pull)
            float maxPullDistance = 1.5f;
            float pullDistance = force * maxPullDistance;

            // Calculate position relative to slingshot center
            Vector3 slingshotCenter = slingshot.BirdWaitPosition.position;

            // Pull is in opposite direction of angle
            float pullX = -Mathf.Cos(angleRad) * pullDistance;
            float pullY = -Mathf.Sin(angleRad) * pullDistance;

            Vector3 pullPosition = slingshotCenter + new Vector3(pullX, pullY, 0f);

            return pullPosition;
        }

        /// <summary>
        /// Simulate slingshot pull and release
        /// </summary>
        private System.Collections.IEnumerator SimulateSlingshot(Vector3 pullPosition)
        {
            GameObject bird = GameManager.Birds[GameManager.currentBirdIndex];

            // Pull back
            float pullTime = 0.5f;
            float elapsed = 0f;
            Vector3 startPos = bird.transform.position;

            while (elapsed < pullTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pullTime;
                bird.transform.position = Vector3.Lerp(startPos, pullPosition, t);
                yield return null;
            }

            bird.transform.position = pullPosition;

            // Small delay
            yield return new WaitForSeconds(0.1f);

            // Release
            slingshot.slingshotState = Assets.Scripts.SlingshotState.BirdFlying;

            // Calculate velocity from slingshot middle point (like original code does)
            Vector3 slingshotMiddle = new Vector3(
                (slingshot.LeftSlingshotOrigin.position.x + slingshot.RightSlingshotOrigin.position.x) / 2,
                (slingshot.LeftSlingshotOrigin.position.y + slingshot.RightSlingshotOrigin.position.y) / 2,
                0f
            );

            Vector3 velocity = slingshotMiddle - pullPosition;
            float distance = Vector3.Distance(slingshotMiddle, pullPosition);

            bird.GetComponent<Bird>().OnThrow();
            bird.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(velocity.x, velocity.y) * slingshot.ThrowSpeed * distance;

            // Wait for outcome
            yield return StartCoroutine(WaitForOutcome());
        }

        /// <summary>
        /// Wait for action outcome and calculate reward
        /// </summary>
        private System.Collections.IEnumerator WaitForOutcome()
        {
            // Wait for physics to settle
            yield return new WaitForSeconds(0.5f);

            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                // Check if bird has stopped or hit something
                GameObject bird = GameManager.Birds[GameManager.currentBirdIndex];
                if (bird == null || bird.GetComponent<Bird>().Collided)
                {
                    break;
                }

                // Check if everything has stopped moving
                if (GameManager.BricksBirdsPigsStoppedMoving())
                {
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Calculate outcome and reward
            ProcessOutcome();
        }

        /// <summary>
        /// Process action outcome and calculate reward (Step 6)
        /// </summary>
        private void ProcessOutcome()
        {
            // Collect new state
            GameStateData newState = stateCollector.CollectCurrentState();

            // Determine outcome
            bool hitTarget = DetermineIfHitTarget();
            float damage = CalculateDamage(previousState, newState);
            int pigsKilled = (previousState?.pigsRemaining ?? 0) - newState.pigsRemaining;
            int blocksDestroyed = (previousState?.obstacleCount ?? 0) - newState.obstacleCount;

            // Create outcome
            AttemptOutcome outcome = rewardCalculator.CreateOutcome(
                hitTarget, damage, pigsKilled, blocksDestroyed, Vector3.zero
            );

            // Calculate reward
            float reward = rewardCalculator.CalculateReward(outcome);
            episodeReward += reward;

            // Record attempt
            stateCollector.RecordAttempt(lastAction, 1f, outcome);

            // Send experience to Python if training with Python server
            if (mode == AgentMode.Training && usePythonServer && connected)
            {
                SendExperienceToPython(currentStateVector, lastAction, reward, newState);
            }

            // Check if episode is done
            bool done = CheckEpisodeDone();

            if (done)
            {
                HandleEpisodeEnd();
            }

            previousState = currentState;
            actionExecuted = false;
        }

        /// <summary>
        /// Check if episode is complete
        /// </summary>
        private bool CheckEpisodeDone()
        {
            return GameManager.CurrentGameState == Assets.Scripts.GameState.Won ||
                   GameManager.CurrentGameState == Assets.Scripts.GameState.Lost;
        }

        /// <summary>
        /// Handle episode end
        /// </summary>
        private void HandleEpisodeEnd()
        {
            bool levelComplete = GameManager.CurrentGameState == Assets.Scripts.GameState.Won;
            float finalReward = rewardCalculator.CalculateEpisodeReward(levelComplete, GameManager.currentBirdIndex + 1);

            episodeReward += finalReward;

            Debug.Log($"=== Episode {episodeCount} Complete ===");
            Debug.Log($"Total Reward: {episodeReward}");
            Debug.Log($"Steps: {stepCount}");
            Debug.Log($"Level Complete: {levelComplete}");

            // Update exploration rate
            explorationRate = Mathf.Max(minExplorationRate, explorationRate * explorationDecay);

            // Reset for next episode
            episodeCount++;
            episodeReward = 0f;
            stepCount = 0;
            actionExecuted = false;

            stateCollector.ResetEpisode();
            rewardCalculator.ResetEpisode();
        }

        // Helper methods

        private bool DetermineIfHitTarget()
        {
            // Check if bird hit something meaningful (not just ground)
            // A "target hit" means hitting pigs, structures, or causing damage

            // Check if any pigs were destroyed
            if (currentState != null && previousState != null)
            {
                bool pigsDestroyed = currentState.pigsRemaining < previousState.pigsRemaining;
                bool structuresDestroyed = currentState.obstacleCount < previousState.obstacleCount;

                // Return true only if we destroyed something
                return pigsDestroyed || structuresDestroyed;
            }

            return false;
        }

        private float CalculateDamage(GameStateData prev, GameStateData current)
        {
            if (prev == null) return 0f;

            // Estimate damage from destroyed objects
            int objectsDestroyed = (prev.obstacleCount - current.obstacleCount) +
                                  (prev.pigsRemaining - current.pigsRemaining);
            return objectsDestroyed * 50f; // Rough estimate
        }

        // Python communication methods

        private void ConnectToPythonServer()
        {
            try
            {
                client = new TcpClient();
                client.Connect(pythonServerHost, pythonServerPort);
                stream = client.GetStream();
                connected = true;
                Debug.Log($"✓ Connected to Python server at {pythonServerHost}:{pythonServerPort}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠ Failed to connect to Python server: {e.Message}");
                Debug.LogWarning("Continuing with Unity-only training mode (random actions)");
                Debug.LogWarning("To use Python training: 1) Run 'python ML/training/train_agent.py' 2) Enable 'Use Python Server'");
                connected = false;
            }
        }

        private Vector2 RequestActionFromPython(float[] stateVector)
        {
            // Implementation would send state to Python and receive action
            // For now, return random action as fallback
            return SelectRandomAction();
        }

        private void SendExperienceToPython(float[] state, Vector2 action, float reward, GameStateData nextState)
        {
            // Implementation would send experience to Python for training
            // Format: {state, action, reward, next_state, done}
        }

        private Vector2 PredictActionFromModel(float[] stateVector)
        {
            // Would use ONNX Runtime or similar to run inference
            // For now, return random action
            return SelectRandomAction();
        }

        private void OnDestroy()
        {
            if (connected)
            {
                stream?.Close();
                client?.Close();
            }
        }
    }
}
