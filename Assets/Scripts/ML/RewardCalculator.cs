using UnityEngine;
using Assets.Scripts;

namespace AngryBirdsML
{
    /// <summary>
    /// Step 6: Reward Assignment
    /// Evaluates outcome of actions and assigns rewards
    /// Positive rewards for hits/damage, penalties for misses
    /// </summary>
    public class RewardCalculator : MonoBehaviour
    {
        [Header("Reward Values")]
        public float hitTargetReward = 100f;
        public float missTargetPenalty = -10f;
        public float structuralDamageReward = 0.5f; // Per unit damage
        public float pigDestroyedReward = 200f;
        public float levelCompleteReward = 1000f;
        public float timePenalty = -0.1f; // Per second
        public float efficiencyBonus = 50f; // Per bird saved

        [Header("Tracking")]
        private float episodeStartTime;
        private int initialPigCount;
        private int initialBrickCount;
        private float totalDamageDealt;
        private int pigsDestroyed;
        private int bricksDestroyed;

        private void Start()
        {
            ResetEpisode();
        }

        /// <summary>
        /// Calculate reward for an action outcome (Step 6)
        /// </summary>
        public float CalculateReward(AttemptOutcome outcome)
        {
            float reward = 0f;

            // Hit/Miss reward
            if (outcome.hitTarget)
            {
                reward += hitTargetReward;
                Debug.Log($"Hit Target! Reward: +{hitTargetReward}");
            }
            else
            {
                reward += missTargetPenalty;
                Debug.Log($"Missed Target! Penalty: {missTargetPenalty}");
            }

            // Structural damage reward
            float damageReward = outcome.damageDealt * structuralDamageReward;
            reward += damageReward;
            totalDamageDealt += outcome.damageDealt;
            Debug.Log($"Damage Dealt: {outcome.damageDealt}, Reward: +{damageReward}");

            // Pig destroyed reward
            if (outcome.pigsKilled > 0)
            {
                float pigReward = outcome.pigsKilled * pigDestroyedReward;
                reward += pigReward;
                pigsDestroyed += outcome.pigsKilled;
                Debug.Log($"Pigs Destroyed: {outcome.pigsKilled}, Reward: +{pigReward}");
            }

            // Block destroyed reward
            if (outcome.blocksDestroyed > 0)
            {
                bricksDestroyed += outcome.blocksDestroyed;
            }

            // Time penalty
            float timeTaken = Time.time - episodeStartTime;
            float timeReward = timeTaken * timePenalty;
            reward += timeReward;

            return reward;
        }

        /// <summary>
        /// Calculate final episode reward (for level completion)
        /// </summary>
        public float CalculateEpisodeReward(bool levelComplete, int birdsUsed)
        {
            float reward = 0f;

            // Level completion bonus
            if (levelComplete)
            {
                reward += levelCompleteReward;
                Debug.Log($"Level Complete! Reward: +{levelCompleteReward}");

                // Efficiency bonus for unused birds
                int birdsRemaining = GameManager.birdsNumber;
                if (birdsRemaining > 0)
                {
                    float efficiencyReward = birdsRemaining * efficiencyBonus;
                    reward += efficiencyReward;
                    Debug.Log($"Birds Remaining: {birdsRemaining}, Efficiency Bonus: +{efficiencyReward}");
                }
            }

            // Summary
            Debug.Log($"=== Episode Complete ===");
            Debug.Log($"Total Damage: {totalDamageDealt}");
            Debug.Log($"Pigs Destroyed: {pigsDestroyed}/{initialPigCount}");
            Debug.Log($"Blocks Destroyed: {bricksDestroyed}/{initialBrickCount}");
            Debug.Log($"Final Reward: {reward}");

            return reward;
        }

        /// <summary>
        /// Calculate shaped reward (intermediate rewards for better learning)
        /// </summary>
        public float CalculateShapedReward(GameStateData currentState, GameStateData previousState)
        {
            float shapedReward = 0f;

            // Reward for getting closer to targets
            float currentMinDist = GetMinDistanceToTarget(currentState);
            float previousMinDist = GetMinDistanceToTarget(previousState);

            if (currentMinDist < previousMinDist)
            {
                shapedReward += 10f; // Reward for approaching target
            }

            // Reward for destroying obstacles
            int obstacleChange = previousState.obstacleCount - currentState.obstacleCount;
            if (obstacleChange > 0)
            {
                shapedReward += obstacleChange * 20f;
            }

            // Reward for destroying pigs
            int pigChange = previousState.pigsRemaining - currentState.pigsRemaining;
            if (pigChange > 0)
            {
                shapedReward += pigChange * pigDestroyedReward;
            }

            return shapedReward;
        }

        /// <summary>
        /// Reset for new episode
        /// </summary>
        public void ResetEpisode()
        {
            episodeStartTime = Time.time;
            initialPigCount = GameManager.Pigs != null ? GameManager.Pigs.Count : 0;
            initialBrickCount = GameManager.Bricks != null ? GameManager.Bricks.Count : 0;
            totalDamageDealt = 0f;
            pigsDestroyed = 0;
            bricksDestroyed = 0;

            Debug.Log($"Episode Reset - Pigs: {initialPigCount}, Bricks: {initialBrickCount}");
        }

        /// <summary>
        /// Create attempt outcome from current game state
        /// </summary>
        public AttemptOutcome CreateOutcome(bool hitTarget, float damage, int pigsKilled, int blocksDestroyed, Vector3 impactPoint)
        {
            return new AttemptOutcome
            {
                hitTarget = hitTarget,
                damageDealt = damage,
                pigsKilled = pigsKilled,
                blocksDestroyed = blocksDestroyed,
                impactPoint = impactPoint,
                timeToImpact = Time.time - episodeStartTime
            };
        }

        // Helper methods

        private float GetMinDistanceToTarget(GameStateData state)
        {
            if (state.pigPositions == null || state.pigPositions.Count == 0)
                return float.MaxValue;

            float minDist = float.MaxValue;
            foreach (Vector3 pigPos in state.pigPositions)
            {
                float dist = Vector3.Distance(state.birdPosition, pigPos);
                if (dist < minDist)
                    minDist = dist;
            }
            return minDist;
        }

        /// <summary>
        /// Get total reward accumulated this episode
        /// </summary>
        public float GetEpisodeReward()
        {
            return (totalDamageDealt * structuralDamageReward) +
                   (pigsDestroyed * pigDestroyedReward) +
                   (bricksDestroyed * structuralDamageReward * 10f);
        }
    }
}
