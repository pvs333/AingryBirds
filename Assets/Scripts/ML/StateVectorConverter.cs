using UnityEngine;
using System.Collections.Generic;

namespace AngryBirdsML
{
    /// <summary>
    /// Step 3: State Representation
    /// Converts preprocessed features into numerical state vector
    /// Provides complete input for the learning agent
    /// </summary>
    public class StateVectorConverter : MonoBehaviour
    {
        [Header("State Vector Settings")]
        public int stateVectorSize = 64;
        public bool includeObstacleDetails = true;
        public int maxObstaclesInVector = 20;

        /// <summary>
        /// Convert normalized features to state vector (Step 3)
        /// </summary>
        public float[] ConvertToStateVector(NormalizedFeatures features, GameStateData rawState)
        {
            List<float> vectorList = new List<float>();

            // Core features (20 values)
            vectorList.Add(features.distanceToNearestTarget);
            vectorList.Add(features.distanceToFarthestTarget);
            vectorList.Add(features.averageTargetDistance);
            vectorList.Add(features.angleToNearestTarget);
            vectorList.Add(features.angleToFarthestTarget);
            vectorList.Add(features.obstacleDensity);
            vectorList.Add(features.obstaclesInPath);
            vectorList.Add(features.weakestObstacleHealth);
            vectorList.Add(features.strongestObstacleHealth);
            vectorList.Add(features.targetCount);
            vectorList.Add(features.targetClustering);
            vectorList.Add(features.highestTargetY);
            vectorList.Add(features.lowestTargetY);
            vectorList.Add(features.birdType);
            vectorList.Add(features.birdsRemaining);
            vectorList.Add(features.birdPosX);
            vectorList.Add(features.birdPosY);
            vectorList.Add(features.previousAttemptCount);
            vectorList.Add(features.previousSuccessRate);
            vectorList.Add(features.averagePreviousDamage);

            // Add target positions (up to 10 targets, 2 values each = 20 values)
            int targetCount = Mathf.Min(rawState.targetPositions.Count, 10);
            for (int i = 0; i < targetCount; i++)
            {
                Vector3 target = rawState.targetPositions[i];
                vectorList.Add(NormalizePosition(target.x, -30f, 30f));
                vectorList.Add(NormalizePosition(target.y, -10f, 20f));
            }
            // Pad with zeros if fewer than 10 targets
            for (int i = targetCount; i < 10; i++)
            {
                vectorList.Add(0f);
                vectorList.Add(0f);
            }

            // Add obstacle information if enabled (24 values)
            if (includeObstacleDetails)
            {
                int obstacleCount = Mathf.Min(rawState.obstacleLayout.Count, maxObstaclesInVector);

                // Simple obstacle encoding: position and health (8 obstacles * 3 values = 24)
                for (int i = 0; i < Mathf.Min(obstacleCount, 8); i++)
                {
                    ObstacleData obstacle = rawState.obstacleLayout[i];
                    vectorList.Add(NormalizePosition(obstacle.position.x, -30f, 30f));
                    vectorList.Add(NormalizePosition(obstacle.position.y, -10f, 20f));
                    vectorList.Add(obstacle.health / 100f); // Normalized health
                }
                // Pad remaining
                for (int i = obstacleCount; i < 8; i++)
                {
                    vectorList.Add(0f);
                    vectorList.Add(0f);
                    vectorList.Add(0f);
                }
            }

            // Ensure exact size by padding or truncating
            while (vectorList.Count < stateVectorSize)
            {
                vectorList.Add(0f);
            }
            if (vectorList.Count > stateVectorSize)
            {
                vectorList = vectorList.GetRange(0, stateVectorSize);
            }

            return vectorList.ToArray();
        }

        /// <summary>
        /// Create state vector from raw game state
        /// </summary>
        public float[] CreateStateVector(GameStateData gameState)
        {
            // Get preprocessor
            DataPreprocessor preprocessor = GetComponent<DataPreprocessor>();
            if (preprocessor == null)
            {
                preprocessor = gameObject.AddComponent<DataPreprocessor>();
            }

            // Extract and normalize features
            FeatureSet features = preprocessor.ExtractFeatures(gameState);
            NormalizedFeatures normalized = preprocessor.NormalizeFeatures(features);

            // Convert to vector
            return ConvertToStateVector(normalized, gameState);
        }

        /// <summary>
        /// Normalize position value to [-1, 1]
        /// </summary>
        private float NormalizePosition(float value, float min, float max)
        {
            return Mathf.Clamp((value - min) / (max - min) * 2f - 1f, -1f, 1f);
        }

        /// <summary>
        /// Get state vector size
        /// </summary>
        public int GetStateVectorSize()
        {
            return stateVectorSize;
        }
    }
}
