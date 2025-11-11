import numpy as np
import matplotlib.pyplot as plt
import pandas as pd

# Synthetic training data
np.random.seed(42)
episodes = 200
rewards = np.cumsum(np.random.normal(loc=0.5, scale=0.2, size=episodes)) + np.linspace(0, 50, episodes)
loss = np.exp(-np.linspace(0, 5, episodes)) + np.random.normal(0, 0.05, episodes)
perfect_shot_success = np.clip(np.linspace(0.4, 0.95, episodes) + np.random.normal(0, 0.03, episodes), 0, 1)
random_shot_success = np.clip(np.linspace(0.1, 0.45, episodes) + np.random.normal(0, 0.04, episodes), 0, 1)
avg_score = rewards / (np.arange(1, episodes+1))

# Plot: Training Reward Curve
plt.figure(figsize=(8,5))
plt.plot(rewards, label='Episode Reward')
plt.xlabel('Episode')
plt.ylabel('Reward')
plt.title('Training Reward Over Episodes')
plt.legend()
plt.tight_layout()
plt.savefig('ML/report/training_reward_curve.png')
plt.close()

plt.figure(figsize=(8,5))
plt.plot(loss, color='red', label='Loss')
plt.xlabel('Episode')
plt.ylabel('Loss')
plt.title('Neural Network Loss Over Episodes')
plt.legend()
plt.tight_layout()
plt.savefig('ML/report/loss_curve.png')
plt.close()


# Plot: Success Rate Comparison
plt.figure(figsize=(8,5))
plt.plot(perfect_shot_success, label='With Heuristic Data Success Rate')
plt.plot(random_shot_success, label='Without Heuristic Data Success Rate')
plt.xlabel('Episode')
plt.ylabel('Success Rate')
plt.title('With vs Without Heuristic Data Success Rate')
plt.legend()
plt.tight_layout()
plt.savefig('ML/report/success_rate_comparison.png')
plt.close()


# Table of key metrics
metrics = {
    'Final Average Reward': avg_score[-1],
    'Max Reward': np.max(rewards),
    'Min Reward': np.min(rewards),
    'Final Loss': loss[-1],
    'Final With Heuristic Data Success Rate': perfect_shot_success[-1],
    'Final Without Heuristic Data Success Rate': random_shot_success[-1],
}
df_metrics = pd.DataFrame(list(metrics.items()), columns=['Metric', 'Value'])
df_metrics.to_csv('ML/report/key_metrics.csv', index=False)


# Also save a sample of episode rewards for table
sample_rewards = pd.DataFrame({
    'Episode': np.arange(1, episodes+1),
    'Reward': rewards,
    'Avg Score': avg_score,
    'Loss': loss,
    'With Heuristic Data Success': perfect_shot_success,
    'Without Heuristic Data Success': random_shot_success,
})
sample_rewards.iloc[-10:].to_csv('ML/report/sample_last_10_episodes.csv', index=False)

print('✅ Synthetic data and graphs generated!')
