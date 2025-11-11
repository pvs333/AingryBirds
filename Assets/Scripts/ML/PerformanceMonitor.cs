using UnityEngine;
using System.Collections.Generic;

namespace AngryBirdsML
{
    /// <summary>
    /// Step 10: Performance Monitor
    /// Continuously monitors agent performance and provides feedback
    /// Tracks metrics and identifies when retraining is needed
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Monitoring Settings")]
        public bool monitoringEnabled = true;
        public float performanceCheckInterval = 10f; // Check every N episodes
        public float performanceThreshold = 0.6f; // Success rate threshold

        [Header("Performance Metrics")]
        public float currentSuccessRate = 0f;
        public float recentAverageReward = 0f;
        public float performanceTrend = 0f; // Positive = improving, Negative = declining

        [Header("Alerts")]
        public bool performanceDropDetected = false;
        public bool retrainingRecommended = false;

        // Rolling window for metrics
        private Queue<bool> recentSuccesses = new Queue<bool>();
        private Queue<float> recentRewards = new Queue<float>();
        private int windowSize = 100;

        // Trend analysis
        private List<float> successRateHistory = new List<float>();
        private int trendWindowSize = 10;

        private float lastCheckTime = 0f;

        private void Start()
        {
            InitializeMonitor();
        }

        private void Update()
        {
            if (!monitoringEnabled) return;

            // Periodic performance check
            if (Time.time - lastCheckTime >= performanceCheckInterval)
            {
                PerformPerformanceCheck();
                lastCheckTime = Time.time;
            }
        }

        /// <summary>
        /// Initialize performance monitor
        /// </summary>
        public void InitializeMonitor()
        {
            recentSuccesses.Clear();
            recentRewards.Clear();
            successRateHistory.Clear();
            performanceDropDetected = false;
            retrainingRecommended = false;

            Debug.Log("Performance Monitor initialized");
        }

        /// <summary>
        /// Record episode outcome
        /// </summary>
        public void RecordEpisode(bool success, float reward)
        {
            // Add to rolling windows
            recentSuccesses.Enqueue(success);
            recentRewards.Enqueue(reward);

            // Maintain window size
            if (recentSuccesses.Count > windowSize)
            {
                recentSuccesses.Dequeue();
                recentRewards.Dequeue();
            }

            // Calculate current metrics
            UpdateMetrics();
        }

        /// <summary>
        /// Update performance metrics
        /// </summary>
        private void UpdateMetrics()
        {
            // Calculate success rate
            if (recentSuccesses.Count > 0)
            {
                int successCount = 0;
                foreach (bool success in recentSuccesses)
                {
                    if (success) successCount++;
                }
                currentSuccessRate = (float)successCount / recentSuccesses.Count;
            }

            // Calculate average reward
            if (recentRewards.Count > 0)
            {
                float totalReward = 0f;
                foreach (float reward in recentRewards)
                {
                    totalReward += reward;
                }
                recentAverageReward = totalReward / recentRewards.Count;
            }

            // Track success rate history for trend
            successRateHistory.Add(currentSuccessRate);
            if (successRateHistory.Count > trendWindowSize)
            {
                successRateHistory.RemoveAt(0);
            }

            // Calculate trend
            CalculatePerformanceTrend();
        }

        /// <summary>
        /// Perform comprehensive performance check
        /// </summary>
        private void PerformPerformanceCheck()
        {
            Debug.Log($"\n=== Performance Check ===");
            Debug.Log($"Success Rate: {currentSuccessRate * 100f:F1}%");
            Debug.Log($"Average Reward: {recentAverageReward:F2}");
            Debug.Log($"Performance Trend: {(performanceTrend > 0 ? "Improving" : performanceTrend < 0 ? "Declining" : "Stable")}");

            // Check for performance drop
            CheckPerformanceDrop();

            // Recommend retraining if needed
            CheckRetrainingNeeded();

            Debug.Log($"=========================\n");
        }

        /// <summary>
        /// Calculate performance trend
        /// </summary>
        private void CalculatePerformanceTrend()
        {
            if (successRateHistory.Count < 2)
            {
                performanceTrend = 0f;
                return;
            }

            // Calculate linear regression slope
            float n = successRateHistory.Count;
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;

            for (int i = 0; i < successRateHistory.Count; i++)
            {
                float x = i;
                float y = successRateHistory[i];
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            // Slope of regression line
            performanceTrend = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        }

        /// <summary>
        /// Check for performance drop
        /// </summary>
        private void CheckPerformanceDrop()
        {
            // Performance drop if below threshold and declining trend
            if (currentSuccessRate < performanceThreshold && performanceTrend < -0.01f)
            {
                if (!performanceDropDetected)
                {
                    performanceDropDetected = true;
                    Debug.LogWarning("⚠️ Performance Drop Detected!");
                    Debug.LogWarning($"Success rate ({currentSuccessRate * 100f:F1}%) is below threshold ({performanceThreshold * 100f:F1}%)");
                    Debug.LogWarning("Consider fine-tuning the model or adjusting reward structure");
                }
            }
            else if (currentSuccessRate >= performanceThreshold && performanceTrend > 0.01f)
            {
                if (performanceDropDetected)
                {
                    performanceDropDetected = false;
                    Debug.Log("✓ Performance recovered");
                }
            }
        }

        /// <summary>
        /// Check if retraining is recommended
        /// </summary>
        private void CheckRetrainingNeeded()
        {
            // Recommend retraining if:
            // 1. Success rate is very low
            // 2. Strong declining trend
            // 3. Average reward is poor

            bool lowSuccessRate = currentSuccessRate < (performanceThreshold * 0.5f);
            bool strongDecline = performanceTrend < -0.02f;
            bool poorReward = recentAverageReward < 100f;

            if ((lowSuccessRate && strongDecline) || (lowSuccessRate && poorReward))
            {
                if (!retrainingRecommended)
                {
                    retrainingRecommended = true;
                    Debug.LogWarning("🔄 RETRAINING RECOMMENDED");
                    Debug.LogWarning("Agent performance has significantly degraded");
                    Debug.LogWarning("Consider retraining with updated data or adjusting hyperparameters");
                }
            }
            else if (currentSuccessRate > performanceThreshold && performanceTrend > 0f)
            {
                retrainingRecommended = false;
            }
        }

        /// <summary>
        /// Get performance report
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            return new PerformanceReport
            {
                successRate = currentSuccessRate,
                averageReward = recentAverageReward,
                trend = performanceTrend,
                windowSize = recentSuccesses.Count,
                performanceDrop = performanceDropDetected,
                retrainingNeeded = retrainingRecommended
            };
        }

        /// <summary>
        /// Reset performance monitoring
        /// </summary>
        public void ResetMonitoring()
        {
            InitializeMonitor();
        }

        /// <summary>
        /// Export performance data
        /// </summary>
        public void ExportPerformanceData(string filepath)
        {
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filepath))
                {
                    writer.WriteLine("Episode,SuccessRate,AverageReward,Trend");

                    for (int i = 0; i < successRateHistory.Count; i++)
                    {
                        writer.WriteLine($"{i},{successRateHistory[i]},{recentAverageReward},{performanceTrend}");
                    }
                }

                Debug.Log($"Performance data exported to {filepath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export performance data: {e.Message}");
            }
        }
    }

    [System.Serializable]
    public class PerformanceReport
    {
        public float successRate;
        public float averageReward;
        public float trend;
        public int windowSize;
        public bool performanceDrop;
        public bool retrainingNeeded;

        public override string ToString()
        {
            return $"Performance Report:\n" +
                   $"  Success Rate: {successRate * 100f:F1}%\n" +
                   $"  Average Reward: {averageReward:F2}\n" +
                   $"  Trend: {(trend > 0 ? "Improving" : trend < 0 ? "Declining" : "Stable")} ({trend:F4})\n" +
                   $"  Sample Size: {windowSize}\n" +
                   $"  Performance Drop: {performanceDrop}\n" +
                   $"  Retraining Needed: {retrainingNeeded}";
        }
    }
}
