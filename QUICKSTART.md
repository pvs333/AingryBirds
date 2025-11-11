# Quick Start Guide - Angry Birds AI/ML

## 5-Minute Setup

### 1. Install Python Dependencies (2 minutes)

```bash
cd ML
chmod +x setup_environment.sh
./setup_environment.sh
source angrybirds_ml_env/bin/activate
```

### 2. Add ML Components to Unity (2 minutes)

1. Open Unity project
2. Create empty GameObject: `GameObject > Create Empty`
3. Rename to "ML_System"
4. Add these scripts (in order):

   - `GameStateCollector`
   - `DataPreprocessor`
   - `StateVectorConverter`
   - `RewardCalculator`
   - `MLAgentBrain`
   - `TrainingManager`
   - `PerformanceMonitor`

5. Configure MLAgentBrain:
   - Find your `SlingShot` GameObject in hierarchy
   - Drag it to the `Slingshot` field in MLAgentBrain
   - Set `Mode` to `Training`
   - Check `Use Random Actions` initially

### 3. Start Training (1 minute)

**Simple Mode (No Python):**

- Just click Play in Unity!
- Agent will start learning randomly
- Watch console for episode results

**Advanced Mode (With Python):**

Terminal 1:

```bash
cd ML/training
python train_agent.py
```

Terminal 2:

```bash
# In Unity, click Play
```

## Testing the AI

After some training episodes:

1. Set `MLAgentBrain.mode = Deployment`
2. Uncheck `Use Random Actions`
3. Click Play
4. Watch AI play!

## Quick Configuration

### Make Training Faster

```
TrainingManager:
- Max Episodes: 100 (for quick test)
- Episode Restart Delay: 0.5
```

### Make Training Better

```
TrainingManager:
- Max Episodes: 10000
- Checkpoint Frequency: 100

MLAgentBrain:
- Exploration Rate: 0.3
- Exploration Decay: 0.995
```

### Increase Rewards

```csharp
RewardCalculator:
- Hit Target Reward: 200
- Pig Destroyed Reward: 500
- Level Complete Reward: 2000
```

## Monitor Progress

Watch Unity Console for:

```
========== Episode 1/10000 Started ==========
Exploring: Random action selected
Executing action: Angle=45.23°, Force=0.78
Hit Target! Reward: +100
Damage Dealt: 250, Reward: +125
Pigs Destroyed: 1, Reward: +200
========== Episode 1 Ended ==========
Success: False | Reward: 425.00 | Steps: 3 | Time: 5.23s
```

## Troubleshooting

**Nothing happens?**

- Check ML_System GameObject is active
- Verify SlingShot reference is set
- Make sure scene has pigs and birds

**No rewards?**

- Hit 'M' key to reset progress
- Check pigs have Pig.cs component
- Verify GameManager is in scene

**Training too slow?**

- Reduce Max Episodes to 100
- Increase Episode Restart Delay to 0
- Set Time Scale to 2: `Edit > Project Settings > Time`

## Next Steps

1. **Train for 100+ episodes** to see improvement
2. **Adjust rewards** if behavior is not optimal
3. **Monitor performance** with PerformanceMonitor
4. **Save checkpoints** every 100 episodes
5. **Deploy** trained model to play autonomously

## Example Results

After 100 episodes, you should see:

- Success Rate: ~10-20%
- Average Reward: 100-300
- Some successful level completions

After 1000 episodes:

- Success Rate: ~40-60%
- Average Reward: 500-800
- Consistent accuracy

## Advanced: Custom Levels

1. Create new scene
2. Add ML_System GameObject (copy from existing scene)
3. Set up level with pigs and obstacles
4. Update `TrainingManager.levelSceneName`
5. Train!

---

**Need Help?** Check `AIML_SETUP_GUIDE.md` for detailed documentation.
