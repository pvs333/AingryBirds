using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;

namespace AngryBirdsML
{
    /// <summary>
    /// Step 2: Data Preprocessing
    /// Processes raw gameplay data into structured features
    /// Extracts distances, angles, and obstacle density
    /// Normalizes values for machine learning
    /// </summary>
    public class DataPreprocessor : MonoBehaviour
    {
        [Header("Normalization Parameters")]
        public float maxDistance = 50f;
        public float maxAngle = 180f;
        public float maxForce = 100f;
        public int maxObstacles = 30;

        /// <summary>
        /// Extract features from game state (Step 2)
        /// </summary>
        public FeatureSet ExtractFeatures(GameStateData state)
        {
            FeatureSet features = new FeatureSet();

            // 1. Distance features
            features.distanceToNearestTarget = CalculateDistanceToNearestTarget(state);
            features.distanceToFarthestTarget = CalculateDistanceToFarthestTarget(state);
            features.averageTargetDistance = CalculateAverageTargetDistance(state);

            // 2. Angle features
            features.angleToNearestTarget = CalculateAngleToTarget(state, true);
            features.angleToFarthestTarget = CalculateAngleToTarget(state, false);

            // 3. Obstacle features
            features.obstacleDensity = CalculateObstacleDensity(state);
            features.obstaclesInPath = CountObstaclesInPath(state);
            features.weakestObstacleHealth = GetWeakestObstacleHealth(state);
            features.strongestObstacleHealth = GetStrongestObstacleHealth(state);

            // 4. Target features
            features.targetCount = state.pigPositions.Count;
            features.targetClustering = CalculateTargetClustering(state);
            features.highestTargetY = GetHighestTargetY(state);
            features.lowestTargetY = GetLowestTargetY(state);

            // 5. Bird features
            features.birdType = state.birdType;
            features.birdsRemaining = state.birdsRemaining;
            features.birdPosX = state.birdPosition.x;
            features.birdPosY = state.birdPosition.y;

            // 6. Previous attempts analysis
            features.previousAttemptCount = state.previousAttempts.Count;
            features.previousSuccessRate = CalculatePreviousSuccessRate(state);
            features.averagePreviousDamage = CalculateAveragePreviousDamage(state);

            // 7. Physics features
            features.gravityStrength = state.gravity.magnitude;

            return features;
        }

        /// <summary>
        /// Normalize features to [0, 1] or [-1, 1] range (Step 2)
        /// </summary>
        public NormalizedFeatures NormalizeFeatures(FeatureSet features)
        {
            NormalizedFeatures normalized = new NormalizedFeatures();

            // Normalize distances (0 to maxDistance -> 0 to 1)
            normalized.distanceToNearestTarget = Mathf.Clamp01(features.distanceToNearestTarget / maxDistance);
            normalized.distanceToFarthestTarget = Mathf.Clamp01(features.distanceToFarthestTarget / maxDistance);
            normalized.averageTargetDistance = Mathf.Clamp01(features.averageTargetDistance / maxDistance);

            // Normalize angles (-180 to 180 -> -1 to 1)
            normalized.angleToNearestTarget = Mathf.Clamp(features.angleToNearestTarget / maxAngle, -1f, 1f);
            normalized.angleToFarthestTarget = Mathf.Clamp(features.angleToFarthestTarget / maxAngle, -1f, 1f);

            // Normalize obstacle features (0 to 1)
            normalized.obstacleDensity = Mathf.Clamp01(features.obstacleDensity);
            normalized.obstaclesInPath = Mathf.Clamp01(features.obstaclesInPath / 10f);
            normalized.weakestObstacleHealth = Mathf.Clamp01(features.weakestObstacleHealth / 100f);
            normalized.strongestObstacleHealth = Mathf.Clamp01(features.strongestObstacleHealth / 100f);

            // Normalize target features
            normalized.targetCount = Mathf.Clamp01(features.targetCount / 10f);
            normalized.targetClustering = Mathf.Clamp01(features.targetClustering);
            normalized.highestTargetY = Mathf.Clamp(features.highestTargetY / 20f, -1f, 1f);
            normalized.lowestTargetY = Mathf.Clamp(features.lowestTargetY / 20f, -1f, 1f);

            // Normalize bird features
            normalized.birdType = features.birdType / 5f; // 0-5 bird types
            normalized.birdsRemaining = Mathf.Clamp01(features.birdsRemaining / 5f);
            normalized.birdPosX = Mathf.Clamp(features.birdPosX / 30f, -1f, 1f);
            normalized.birdPosY = Mathf.Clamp(features.birdPosY / 30f, -1f, 1f);

            // Normalize history features
            normalized.previousAttemptCount = Mathf.Clamp01(features.previousAttemptCount / 5f);
            normalized.previousSuccessRate = features.previousSuccessRate; // Already 0-1
            normalized.averagePreviousDamage = Mathf.Clamp01(features.averagePreviousDamage / 1000f);

            // Normalize physics
            normalized.gravityStrength = features.gravityStrength / 9.81f;

            return normalized;
        }

        // Helper methods for feature extraction

        private float CalculateDistanceToNearestTarget(GameStateData state)
        {
            if (state.targetPositions.Count == 0) return maxDistance;

            float minDist = float.MaxValue;
            foreach (Vector3 target in state.targetPositions)
            {
                float dist = Vector3.Distance(state.birdPosition, target);
                if (dist < minDist) minDist = dist;
            }
            return minDist;
        }

        private float CalculateDistanceToFarthestTarget(GameStateData state)
        {
            if (state.targetPositions.Count == 0) return 0f;

            float maxDist = 0f;
            foreach (Vector3 target in state.targetPositions)
            {
                float dist = Vector3.Distance(state.birdPosition, target);
                if (dist > maxDist) maxDist = dist;
            }
            return maxDist;
        }

        private float CalculateAverageTargetDistance(GameStateData state)
        {
            if (state.targetPositions.Count == 0) return 0f;

            float totalDist = 0f;
            foreach (Vector3 target in state.targetPositions)
            {
                totalDist += Vector3.Distance(state.birdPosition, target);
            }
            return totalDist / state.targetPositions.Count;
        }

        private float CalculateAngleToTarget(GameStateData state, bool nearest)
        {
            if (state.targetPositions.Count == 0) return 0f;

            Vector3 targetPos = nearest ?
                GetNearestTarget(state) : GetFarthestTarget(state);

            Vector3 direction = targetPos - state.birdPosition;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return angle;
        }

        private Vector3 GetNearestTarget(GameStateData state)
        {
            Vector3 nearest = Vector3.zero;
            float minDist = float.MaxValue;

            foreach (Vector3 target in state.targetPositions)
            {
                float dist = Vector3.Distance(state.birdPosition, target);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = target;
                }
            }
            return nearest;
        }

        private Vector3 GetFarthestTarget(GameStateData state)
        {
            Vector3 farthest = Vector3.zero;
            float maxDist = 0f;

            foreach (Vector3 target in state.targetPositions)
            {
                float dist = Vector3.Distance(state.birdPosition, target);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    farthest = target;
                }
            }
            return farthest;
        }

        private float CalculateObstacleDensity(GameStateData state)
        {
            if (state.obstacleLayout.Count == 0) return 0f;

            // Calculate average distance between obstacles
            float totalDist = 0f;
            int count = 0;

            for (int i = 0; i < state.obstacleLayout.Count; i++)
            {
                for (int j = i + 1; j < state.obstacleLayout.Count; j++)
                {
                    totalDist += Vector3.Distance(
                        state.obstacleLayout[i].position,
                        state.obstacleLayout[j].position
                    );
                    count++;
                }
            }

            if (count == 0) return 0f;

            float avgDist = totalDist / count;
            // Convert to density (inverse of distance)
            return Mathf.Clamp01(1f / (avgDist + 0.1f));
        }

        private int CountObstaclesInPath(GameStateData state)
        {
            if (state.targetPositions.Count == 0) return 0;

            Vector3 targetPos = GetNearestTarget(state);
            int count = 0;

            foreach (ObstacleData obstacle in state.obstacleLayout)
            {
                // Check if obstacle is roughly between bird and target
                if (IsInPath(state.birdPosition, targetPos, obstacle.position))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsInPath(Vector3 start, Vector3 end, Vector3 point)
        {
            // Simple check: point is in bounding box with some tolerance
            float minX = Mathf.Min(start.x, end.x) - 2f;
            float maxX = Mathf.Max(start.x, end.x) + 2f;
            float minY = Mathf.Min(start.y, end.y) - 2f;
            float maxY = Mathf.Max(start.y, end.y) + 2f;

            return point.x >= minX && point.x <= maxX &&
                   point.y >= minY && point.y <= maxY;
        }

        private float GetWeakestObstacleHealth(GameStateData state)
        {
            if (state.obstacleLayout.Count == 0) return 0f;

            float minHealth = float.MaxValue;
            foreach (ObstacleData obstacle in state.obstacleLayout)
            {
                if (obstacle.health < minHealth)
                    minHealth = obstacle.health;
            }
            return minHealth;
        }

        private float GetStrongestObstacleHealth(GameStateData state)
        {
            if (state.obstacleLayout.Count == 0) return 0f;

            float maxHealth = 0f;
            foreach (ObstacleData obstacle in state.obstacleLayout)
            {
                if (obstacle.health > maxHealth)
                    maxHealth = obstacle.health;
            }
            return maxHealth;
        }

        private float CalculateTargetClustering(GameStateData state)
        {
            if (state.targetPositions.Count <= 1) return 0f;

            // Calculate average distance between targets
            float totalDist = 0f;
            int count = 0;

            for (int i = 0; i < state.targetPositions.Count; i++)
            {
                for (int j = i + 1; j < state.targetPositions.Count; j++)
                {
                    totalDist += Vector3.Distance(
                        state.targetPositions[i],
                        state.targetPositions[j]
                    );
                    count++;
                }
            }

            float avgDist = totalDist / count;
            // Convert to clustering score (inverse of distance)
            return Mathf.Clamp01(1f / (avgDist + 0.1f));
        }

        private float GetHighestTargetY(GameStateData state)
        {
            if (state.targetPositions.Count == 0) return 0f;
            return state.targetPositions.Max(t => t.y);
        }

        private float GetLowestTargetY(GameStateData state)
        {
            if (state.targetPositions.Count == 0) return 0f;
            return state.targetPositions.Min(t => t.y);
        }

        private float CalculatePreviousSuccessRate(GameStateData state)
        {
            if (state.previousAttempts.Count == 0) return 0f;

            int successes = state.previousAttempts.Count(a => a.hitTarget);
            return (float)successes / state.previousAttempts.Count;
        }

        private float CalculateAveragePreviousDamage(GameStateData state)
        {
            if (state.previousAttempts.Count == 0) return 0f;

            float totalDamage = state.previousAttempts.Sum(a => a.damageDealt);
            return totalDamage / state.previousAttempts.Count;
        }
    }

    // Feature data structures

    [System.Serializable]
    public class FeatureSet
    {
        // Distance features
        public float distanceToNearestTarget;
        public float distanceToFarthestTarget;
        public float averageTargetDistance;

        // Angle features
        public float angleToNearestTarget;
        public float angleToFarthestTarget;

        // Obstacle features
        public float obstacleDensity;
        public int obstaclesInPath;
        public float weakestObstacleHealth;
        public float strongestObstacleHealth;

        // Target features
        public int targetCount;
        public float targetClustering;
        public float highestTargetY;
        public float lowestTargetY;

        // Bird features
        public int birdType;
        public int birdsRemaining;
        public float birdPosX;
        public float birdPosY;

        // History features
        public int previousAttemptCount;
        public float previousSuccessRate;
        public float averagePreviousDamage;

        // Physics features
        public float gravityStrength;
    }

    [System.Serializable]
    public class NormalizedFeatures
    {
        // All values normalized to [-1, 1] or [0, 1]
        public float distanceToNearestTarget;
        public float distanceToFarthestTarget;
        public float averageTargetDistance;
        public float angleToNearestTarget;
        public float angleToFarthestTarget;
        public float obstacleDensity;
        public float obstaclesInPath;
        public float weakestObstacleHealth;
        public float strongestObstacleHealth;
        public float targetCount;
        public float targetClustering;
        public float highestTargetY;
        public float lowestTargetY;
        public float birdType;
        public float birdsRemaining;
        public float birdPosX;
        public float birdPosY;
        public float previousAttemptCount;
        public float previousSuccessRate;
        public float averagePreviousDamage;
        public float gravityStrength;
    }
}
