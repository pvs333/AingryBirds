using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using Assets.Scripts;

namespace AngryBirdsML
{
    /// <summary>
    /// Steps 7-8: Training Manager
    /// Manages the overall training loop and coordination
    /// Handles episode management, model checkpointing, and metrics
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        [Header("Training Settings")]
        public bool trainingEnabled = true;
        public int maxEpisodes = 10000;
        public int currentEpisode = 0;
        public bool autoRestartEpisodes = true;
        public float episodeRestartDelay = 2f;

        [Header("Performance Tracking")]
        public int successfulEpisodes = 0;
        public float averageReward = 0f;
        public float bestReward = float.MinValue;
        public int totalSteps = 0;

        [Header("Checkpointing")]
        public bool saveCheckpoints = true;
        public int checkpointFrequency = 100; // Save every N episodes
        public string checkpointDirectory = "../ML/models/checkpoints";

        [Header("Metrics")]
        public List<float> episodeRewards = new List<float>();
        public List<int> episodeLengths = new List<int>();
        public List<float> successRates = new List<float>();

        [Header("References")]
        public MLAgentBrain agentBrain;
        public GameManager gameManager;
        public string levelSceneName;

        // Timing
        private float episodeStartTime;
        private bool episodeInProgress = false;

        // Logging
        private StreamWriter logWriter;
        private string logFilePath;

        private static TrainingManager instance;

        private void Awake()
        {
            // Singleton pattern - only one TrainingManager should exist
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            // Make this object persist across scene reloads
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeTraining();
        }

        private void Update()
        {
            if (!trainingEnabled) return;

            // Check if episode should end
            if (episodeInProgress && ShouldEndEpisode())
            {
                EndEpisode();

                // Reload scene after episode ends for next episode
                if (autoRestartEpisodes && currentEpisode < maxEpisodes)
                {
                    Invoke(nameof(ReloadSceneForNextEpisode), episodeRestartDelay);
                }
            }
        }

        /// <summary>
        /// Reload scene and start next episode
        /// </summary>
        private void ReloadSceneForNextEpisode()
        {
            Debug.Log($"🔄 Reloading scene for Episode {currentEpisode + 1}/{maxEpisodes}...");

            // Reload the level scene
            if (!string.IsNullOrEmpty(levelSceneName))
            {
                SceneManager.LoadScene(levelSceneName);
            }
            else
            {
                // Reload current scene
                string sceneName = SceneManager.GetActiveScene().name;
                Debug.Log($"Reloading scene: {sceneName}");
                SceneManager.LoadScene(sceneName);
            }
        }

        /// <summary>
        /// Initialize training system
        /// </summary>
        public void InitializeTraining()
        {
            // Get or create references
            if (agentBrain == null)
                agentBrain = FindFirstObjectByType<MLAgentBrain>();

            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            // Setup logging (only on first init)
            if (logWriter == null)
            {
                SetupLogging();
            }

            Debug.Log("Training Manager initialized");
            Debug.Log($"Target episodes: {maxEpisodes}");

            // Start first episode ONLY if this is the very first initialization
            if (trainingEnabled && autoRestartEpisodes && currentEpisode == 0)
            {
                StartNewEpisode();
            }
            else if (currentEpisode > 0)
            {
                // Scene was reloaded, continue with next episode
                episodeInProgress = true;
                episodeStartTime = Time.time;
                Debug.Log($"Continuing Episode {currentEpisode}/{maxEpisodes}");
            }
        }

        /// <summary>
        /// Start a new training episode
        /// </summary>
        public void StartNewEpisode()
        {
            currentEpisode++;
            episodeInProgress = true;
            episodeStartTime = Time.time;

            Debug.Log($"\n========== Episode {currentEpisode}/{maxEpisodes} Started ==========");

            // Reset environment
            ResetEnvironment();

            // Reset agent components
            if (agentBrain != null)
            {
                agentBrain.GetComponent<GameStateCollector>()?.ResetEpisode();
                agentBrain.GetComponent<RewardCalculator>()?.ResetEpisode();
            }

            LogEvent($"Episode {currentEpisode} started");
        }

        /// <summary>
        /// End current episode and record metrics
        /// </summary>
        public void EndEpisode()
        {
            episodeInProgress = false;
            float episodeTime = Time.time - episodeStartTime;

            // Get episode metrics
            bool success = GameManager.CurrentGameState == GameState.Won;
            float episodeReward = agentBrain.GetComponent<RewardCalculator>()?.GetEpisodeReward() ?? 0f;
            int episodeSteps = agentBrain.GetComponent<GameStateCollector>()?.GetCollectedStates().Count ?? 0;

            // Update statistics
            UpdateStatistics(success, episodeReward, episodeSteps);

            // Log episode results
            LogEpisodeResults(success, episodeReward, episodeSteps, episodeTime);

            // Save checkpoint if needed
            if (saveCheckpoints && currentEpisode % checkpointFrequency == 0)
            {
                SaveCheckpoint();
            }

            Debug.Log($"========== Episode {currentEpisode} Ended ==========");
            Debug.Log($"Success: {success} | Reward: {episodeReward:F2} | Steps: {episodeSteps} | Time: {episodeTime:F2}s\n");

            // Check if training is complete
            if (currentEpisode >= maxEpisodes)
            {
                CompleteTraining();
            }
        }

        /// <summary>
        /// Update training statistics
        /// </summary>
        private void UpdateStatistics(bool success, float reward, int steps)
        {
            if (success)
                successfulEpisodes++;

            episodeRewards.Add(reward);
            episodeLengths.Add(steps);
            totalSteps += steps;

            // Calculate rolling averages
            int windowSize = Mathf.Min(100, episodeRewards.Count);
            float recentRewardSum = 0f;
            for (int i = episodeRewards.Count - windowSize; i < episodeRewards.Count; i++)
            {
                recentRewardSum += episodeRewards[i];
            }
            averageReward = recentRewardSum / windowSize;

            // Calculate success rate
            int recentSuccesses = 0;
            for (int i = Mathf.Max(0, currentEpisode - windowSize); i < currentEpisode; i++)
            {
                if (i < episodeRewards.Count && episodeRewards[i] > 500f) // Threshold for success
                    recentSuccesses++;
            }
            float successRate = (float)recentSuccesses / windowSize;
            successRates.Add(successRate);

            // Track best reward
            if (reward > bestReward)
            {
                bestReward = reward;
                Debug.Log($"*** New Best Reward: {bestReward:F2} ***");
            }
        }

        /// <summary>
        /// Check if episode should end
        /// </summary>
        private bool ShouldEndEpisode()
        {
            // Episode ends when game state is Won or Lost
            bool shouldEnd = GameManager.CurrentGameState == GameState.Won ||
                             GameManager.CurrentGameState == GameState.Lost;

            if (shouldEnd)
            {
                Debug.Log($"Episode ending detected - Game State: {GameManager.CurrentGameState}");
            }

            return shouldEnd;
        }

        /// <summary>
        /// Reset the game environment for new episode
        /// </summary>
        private void ResetEnvironment()
        {
            // Don't reload scene immediately - let the episode actually play first!
            // Scene will be reloaded after episode ends (in Update method)

            // Reset game state
            if (gameManager != null)
            {
                GameManager.CurrentGameState = GameState.Start;
            }

            Debug.Log($"Episode Reset - Pigs: {GameObject.FindGameObjectsWithTag("Pig")?.Length ?? 0}, Bricks: {GameObject.FindGameObjectsWithTag("Brick")?.Length ?? 0}");
        }

        /// <summary>
        /// Save training checkpoint
        /// </summary>
        private void SaveCheckpoint()
        {
            try
            {
                if (!Directory.Exists(checkpointDirectory))
                {
                    Directory.CreateDirectory(checkpointDirectory);
                }

                string checkpointPath = Path.Combine(checkpointDirectory, $"checkpoint_episode_{currentEpisode}.json");

                CheckpointData checkpoint = new CheckpointData
                {
                    episode = currentEpisode,
                    successfulEpisodes = successfulEpisodes,
                    averageReward = averageReward,
                    bestReward = bestReward,
                    totalSteps = totalSteps,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonUtility.ToJson(checkpoint, true);
                File.WriteAllText(checkpointPath, json);

                Debug.Log($"Checkpoint saved: {checkpointPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save checkpoint: {e.Message}");
            }
        }

        /// <summary>
        /// Complete training and generate report
        /// </summary>
        private void CompleteTraining()
        {
            trainingEnabled = false;

            Debug.Log("\n========================================");
            Debug.Log("TRAINING COMPLETE");
            Debug.Log("========================================");
            Debug.Log($"Total Episodes: {currentEpisode}");
            Debug.Log($"Successful Episodes: {successfulEpisodes} ({(float)successfulEpisodes / currentEpisode * 100f:F1}%)");
            Debug.Log($"Average Reward: {averageReward:F2}");
            Debug.Log($"Best Reward: {bestReward:F2}");
            Debug.Log($"Total Steps: {totalSteps}");
            Debug.Log("========================================\n");

            // Generate final report
            GenerateTrainingReport();

            // Close log file
            logWriter?.Close();
        }

        /// <summary>
        /// Generate comprehensive training report
        /// </summary>
        private void GenerateTrainingReport()
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(checkpointDirectory))
                {
                    Directory.CreateDirectory(checkpointDirectory);
                }

                string reportPath = Path.Combine(checkpointDirectory, $"training_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                using (StreamWriter writer = new StreamWriter(reportPath))
                {
                    writer.WriteLine("===========================================");
                    writer.WriteLine("ANGRY BIRDS AI TRAINING REPORT");
                    writer.WriteLine("===========================================\n");

                    writer.WriteLine("SUMMARY:");
                    writer.WriteLine($"  Total Episodes: {currentEpisode}");
                    writer.WriteLine($"  Successful Episodes: {successfulEpisodes} ({(float)successfulEpisodes / currentEpisode * 100f:F1}%)");
                    writer.WriteLine($"  Average Reward: {averageReward:F2}");
                    writer.WriteLine($"  Best Reward: {bestReward:F2}");
                    writer.WriteLine($"  Total Steps: {totalSteps}");
                    writer.WriteLine($"  Average Steps per Episode: {(float)totalSteps / currentEpisode:F1}\n");

                    writer.WriteLine("PERFORMANCE TRENDS:");
                    if (episodeRewards.Count >= 100)
                    {
                        writer.WriteLine($"  First 100 episodes avg reward: {GetAverageReward(0, 100):F2}");
                        writer.WriteLine($"  Last 100 episodes avg reward: {GetAverageReward(episodeRewards.Count - 100, episodeRewards.Count):F2}");
                        writer.WriteLine($"  Improvement: {(GetAverageReward(episodeRewards.Count - 100, episodeRewards.Count) - GetAverageReward(0, 100)):F2}\n");
                    }

                    writer.WriteLine("CONFIGURATION:");
                    writer.WriteLine($"  Max Episodes: {maxEpisodes}");
                    writer.WriteLine($"  Checkpoint Frequency: {checkpointFrequency}");
                    writer.WriteLine($"  Training Start: {DateTime.Now.AddSeconds(-(currentEpisode * 30)):yyyy-MM-dd HH:mm:ss}"); // Rough estimate
                    writer.WriteLine($"  Training End: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

                    writer.WriteLine("===========================================");
                }

                Debug.Log($"Training report generated: {reportPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate training report: {e.Message}");
            }
        }

        /// <summary>
        /// Setup logging system
        /// </summary>
        private void SetupLogging()
        {
            try
            {
                string logDir = Path.Combine(checkpointDirectory, "..", "logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                logFilePath = Path.Combine(logDir, $"training_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                logWriter = new StreamWriter(logFilePath, true);
                logWriter.AutoFlush = true;

                LogEvent("Training session started");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to setup logging: {e.Message}");
            }
        }

        /// <summary>
        /// Log an event
        /// </summary>
        private void LogEvent(string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            logWriter?.WriteLine(logMessage);
        }

        /// <summary>
        /// Log episode results
        /// </summary>
        private void LogEpisodeResults(bool success, float reward, int steps, float time)
        {
            string result = success ? "SUCCESS" : "FAILED";
            LogEvent($"Episode {currentEpisode} - {result} | Reward: {reward:F2} | Steps: {steps} | Time: {time:F2}s");
        }

        /// <summary>
        /// Get average reward for a range of episodes
        /// </summary>
        private float GetAverageReward(int start, int end)
        {
            float sum = 0f;
            int count = 0;
            for (int i = start; i < end && i < episodeRewards.Count; i++)
            {
                sum += episodeRewards[i];
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }

        private void OnDestroy()
        {
            logWriter?.Close();
        }

        private void OnApplicationQuit()
        {
            if (trainingEnabled && currentEpisode > 0)
            {
                CompleteTraining();
            }
        }
    }

    [Serializable]
    public class CheckpointData
    {
        public int episode;
        public int successfulEpisodes;
        public float averageReward;
        public float bestReward;
        public int totalSteps;
        public string timestamp;
    }
}
