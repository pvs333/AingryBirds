# Angry Birds AI/ML Project - Complete Setup and Usage Guide

## Overview

This project transforms the Unity Angry Birds game into an AI-powered system using **Reinforcement Learning**. The agent learns to play the game optimally by training a neural network to predict the best launch angle and force for each shot.

## Architecture

The system implements a complete 10-step reinforcement learning pipeline:

### Steps 1-3: Data Collection & Processing

- **GameStateCollector.cs**: Collects gameplay states (targets, obstacles, bird position)
- **DataPreprocessor.cs**: Extracts features (distances, angles, obstacle density)
- **StateVectorConverter.cs**: Converts features into numerical state vectors

### Steps 4-6: Action & Reward

- **MLAgentBrain.cs**: Selects actions using policy network (angle/force)
- **RewardCalculator.cs**: Assigns rewards based on hits, damage, and outcomes

### Steps 7-8: Training

- **TrainingManager.cs**: Orchestrates training loop and episode management
- **Python Scripts**: Neural network training with PyTorch

### Steps 9-10: Deployment & Monitoring

- **MLAgentBrain.cs**: Deploys trained model for real-time play
- **PerformanceMonitor.cs**: Monitors performance and recommends retraining

## Installation

### Prerequisites

1. **Unity 2020.3 or higher**
2. **Python 3.8+**
3. **Git** (for cloning)

### Step 1: Setup Python Environment

```bash
cd ML
chmod +x setup_environment.sh
./setup_environment.sh
```

This installs:

- PyTorch (deep learning)
- Unity ML-Agents
- NumPy, SciPy (numerical computing)
- TensorBoard (visualization)

### Step 2: Activate Python Environment

```bash
source ML/angrybirds_ml_env/bin/activate
```

### Step 3: Configure Unity

1. Open the project in Unity
2. Navigate to `Assets/Scripts/ML/`
3. Add `MLAgentBrain` component to an empty GameObject in your scene
4. Configure references:
   - Assign `SlingShot` reference
   - Set `GameStateCollector` reference
   - Set mode to `Training` or `Deployment`

### Step 4: Setup Training Scene

1. Create or select a level scene
2. Add these components to a GameObject:

   - `MLAgentBrain`
   - `TrainingManager`
   - `GameStateCollector`
   - `DataPreprocessor`
   - `StateVectorConverter`
   - `RewardCalculator`
   - `PerformanceMonitor`

3. Configure TrainingManager:
   - Set `maxEpisodes` (e.g., 10000)
   - Enable `autoRestartEpisodes`
   - Set `levelSceneName` to your level

## Usage

### Training Mode

#### Option 1: Unity-Only Training (Simplified)

1. Set `MLAgentBrain.mode = Training`
2. Enable `useRandomActions = true` initially
3. Click Play in Unity
4. The agent will play randomly and collect data
5. After collecting data, switch to policy network

#### Option 2: Full Python Training (Recommended)

1. **Start Python Training Server:**

```bash
cd ML/training
python train_agent.py --config config.yaml
```

2. **Start Unity:**

   - Set `MLAgentBrain.mode = Training`
   - Click Play
   - Unity connects to Python server
   - Training begins automatically

3. **Monitor Training:**

```bash
tensorboard --logdir=../data/logs
```

Open http://localhost:6006 in browser to view:

- Episode rewards
- Success rates
- Loss curves
- Exploration rate

### Testing Mode

Test a trained model:

```bash
cd ML/training
python test_agent.py --model ../models/trained_model.pth --episodes 10
```

### Deployment Mode

Use trained AI to play:

1. Set `MLAgentBrain.mode = Deployment`
2. Set `modelPath` to trained model file
3. Disable `useRandomActions`
4. Click Play
5. Watch the AI play!

## Configuration

### Training Parameters (`ML/training/config.yaml`)

```yaml
training:
  max_episodes: 10000 # Total training episodes
  learning_rate: 0.0003 # Neural network learning rate
  gamma: 0.99 # Discount factor
  batch_size: 64 # Training batch size

  # Exploration
  exploration_rate_start: 1.0 # Start with 100% random
  exploration_rate_end: 0.01 # End with 1% random
  exploration_decay: 0.995 # Decay per episode
```

### Reward Structure (`ML/training/config.yaml`)

```yaml
rewards:
  hit_target: 100.0 # Bonus for hitting
  miss_penalty: -10.0 # Penalty for missing
  structural_damage: 50.0 # Per destroyed object
  pig_destroyed: 200.0 # Per pig killed
  level_complete: 1000.0 # Completing level
  efficiency_bonus: 50.0 # Per unused bird
```

### Network Architecture

- **Input**: 64-dimensional state vector

  - Bird position/type
  - Target positions (up to 10)
  - Obstacle layout (up to 8)
  - Previous attempt history
  - Physics parameters

- **Hidden Layers**: [256, 256, 128] neurons

  - ReLU activation
  - Dropout (0.2) for regularization

- **Output**: 2-dimensional action
  - Angle: [-90, 90] degrees
  - Force: [0, 1] normalized

## Project Structure

```
AingryBirds/
├── Assets/
│   └── Scripts/
│       ├── ML/                              # ML System Scripts
│       │   ├── GameStateCollector.cs        # Step 1: Data Collection
│       │   ├── DataPreprocessor.cs          # Step 2: Preprocessing
│       │   ├── StateVectorConverter.cs      # Step 3: State Representation
│       │   ├── MLAgentBrain.cs              # Steps 4,9: Action & Deployment
│       │   ├── RewardCalculator.cs          # Step 6: Rewards
│       │   ├── TrainingManager.cs           # Steps 7-8: Training
│       │   └── PerformanceMonitor.cs        # Step 10: Monitoring
│       ├── Bird.cs                          # Original game scripts
│       ├── SlingShot.cs
│       ├── GameManager.cs
│       └── ...
├── ML/
│   ├── requirements.txt                     # Python dependencies
│   ├── setup_environment.sh                 # Setup script
│   ├── README.md                            # ML documentation
│   ├── training/
│   │   ├── train_agent.py                   # Main training script
│   │   ├── policy_network.py                # Neural network models
│   │   ├── unity_environment.py             # Unity communication
│   │   ├── replay_buffer.py                 # Experience replay
│   │   └── config.yaml                      # Training configuration
│   ├── models/                              # Saved models
│   │   └── checkpoints/                     # Training checkpoints
│   └── data/
│       └── logs/                            # Training logs
└── README.md                                # This file
```

## Monitoring & Debugging

### Check Training Progress

```python
# In Python console during training
print(f"Episode: {trainer.episode_count}")
print(f"Avg Reward: {trainer.average_reward}")
print(f"Exploration: {trainer.exploration_rate}")
```

### Unity Console Output

Monitor for:

- Episode starts/ends
- Reward values
- Hit/miss detection
- Performance warnings

### Common Issues

**1. Python can't connect to Unity**

- Ensure Unity is running
- Check firewall settings
- Verify port 5004 is free

**2. Training is slow**

- Reduce `max_episodes`
- Increase `batch_size`
- Enable GPU acceleration

**3. Poor performance**

- Adjust reward values
- Increase exploration initially
- Check state vector includes relevant features

**4. Model not learning**

- Verify rewards are being calculated
- Check learning rate (try 0.0001-0.001)
- Ensure sufficient exploration

## Performance Monitoring

The `PerformanceMonitor` tracks:

- **Success Rate**: % of successful episodes
- **Average Reward**: Rolling average of rewards
- **Performance Trend**: Improving/declining indicator
- **Alerts**: Automatic detection of performance drops

### Interpreting Metrics

- **Success Rate > 60%**: Agent is performing well
- **Positive Trend**: Agent is improving
- **Performance Drop Alert**: Consider retraining
- **Retraining Recommended**: Performance critically low

## Advanced Features

### Custom Bird Types

The system handles all bird types:

- Red (0): Standard
- Chuck (1): Speed boost
- Blues (2): Split into three
- Bomb (3): Explosion
- Matilda (4): Egg drop
- Terence (5): Heavy

### Adaptive Learning

The agent automatically adapts to:

- Different level layouts
- Varying obstacle configurations
- Different bird types
- Physics variations

### Transfer Learning

Train on one level, deploy on others:

1. Train on simple levels
2. Save checkpoint
3. Load checkpoint for complex levels
4. Fine-tune with fewer episodes

## Results & Metrics

Expected performance after training:

| Episodes | Success Rate | Avg Reward | Notes                  |
| -------- | ------------ | ---------- | ---------------------- |
| 0-100    | 5-10%        | -50 to 50  | Random exploration     |
| 100-500  | 15-30%       | 50-200     | Learning basic physics |
| 500-2000 | 30-50%       | 200-500    | Improving accuracy     |
| 2000+    | 50-70%+      | 500-1000+  | Expert performance     |

## Contributing

To extend the system:

1. **Add new features**: Modify `DataPreprocessor.cs`
2. **Change rewards**: Edit `RewardCalculator.cs`
3. **New architectures**: Update `policy_network.py`
4. **Custom actions**: Modify `MLAgentBrain.cs`

## Troubleshooting

### Training Tips

1. **Start simple**: Train on easy levels first
2. **Monitor early**: Watch first 100 episodes closely
3. **Adjust rewards**: If not learning, tweak reward values
4. **Use checkpoints**: Save frequently to resume training

### Debugging

Enable detailed logging:

```csharp
// In MLAgentBrain.cs
Debug.Log($"State Vector: {string.Join(", ", stateVector)}");
Debug.Log($"Action: Angle={angle}, Force={force}");
Debug.Log($"Reward: {reward}");
```

## References

- **Reinforcement Learning**: Sutton & Barto, "Reinforcement Learning: An Introduction"
- **Deep Q-Learning**: Mnih et al., "Playing Atari with Deep Reinforcement Learning"
- **Actor-Critic**: Konda & Tsitsiklis, "Actor-Critic Algorithms"
- **Unity ML-Agents**: https://github.com/Unity-Technologies/ml-agents

## License

See LICENSE.md for details.

## Support

For issues or questions:

1. Check Unity console for errors
2. Review Python training logs
3. Verify configuration in `config.yaml`
4. Check GitHub Issues for similar problems

---

**Happy Training! 🎮🤖🎯**
