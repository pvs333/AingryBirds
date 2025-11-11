# Angry Birds AI/ML Project

This directory contains the AI/ML components for transforming the Angry Birds Unity game into an intelligent agent using Reinforcement Learning.

## Overview

The system implements a complete reinforcement learning pipeline:

1. **Data Collection**: Captures gameplay states from Unity
2. **Data Preprocessing**: Extracts features (distances, angles, obstacles)
3. **State Representation**: Converts features to numerical vectors
4. **Action Selection**: Policy network for angle/force selection
5. **Environment Interaction**: Executes actions in Unity physics
6. **Reward Assignment**: Evaluates outcomes (hits, misses, damage)
7. **Policy Update**: Neural network training via backpropagation
8. **Training Loop**: Iterative improvement over episodes
9. **Model Deployment**: Real-time AI gameplay
10. **Continuous Monitoring**: Performance tracking and updates

## Setup

### Prerequisites

- Python 3.8 or higher
- Unity 2020.3 or higher
- CUDA (optional, for GPU acceleration)

### Installation

1. Run the setup script:

```bash
cd ML
chmod +x setup_environment.sh
./setup_environment.sh
```

2. Activate the environment:

```bash
source angrybirds_ml_env/bin/activate
```

## Project Structure

```
ML/
├── requirements.txt          # Python dependencies
├── setup_environment.sh      # Environment setup script
├── training/                 # Training scripts
│   ├── train_agent.py       # Main training script
│   ├── policy_network.py    # Neural network architecture
│   └── config.yaml          # Training configuration
├── models/                   # Saved model checkpoints
├── data/                     # Training data and logs
└── utils/                    # Utility functions
```

## Usage

### Training

```bash
python training/train_agent.py --config training/config.yaml
```

### Testing

```bash
python training/test_agent.py --model models/trained_model.pth
```

## Unity Integration

The C# scripts in `Assets/Scripts/ML/` provide the Unity interface:

- `GameStateCollector.cs`: Collects gameplay data
- `MLAgentBrain.cs`: Agent controller
- `DataPreprocessor.cs`: Feature extraction
- `PolicyNetwork.cs`: Neural network inference
- `RewardCalculator.cs`: Reward computation
- `TrainingManager.cs`: Training orchestration

## Algorithm Steps

Each step of the algorithm is implemented across both Python and C# components for seamless Unity integration.
