# Troubleshooting Guide - Connection Issues

## Issue 1: "Failed to connect to Python server: Connection refused"

### ✅ FIXED - Solution Applied

The error occurred because `MLAgentBrain` was trying to connect to a Python server that wasn't running. I've updated the code to support **two training modes**:

### Mode 1: Unity-Only Training (No Python Required) ⭐ RECOMMENDED FOR GETTING STARTED

**This is now the default and will work immediately!**

#### In Unity Inspector:

1. Select the GameObject with `MLAgentBrain` component
2. Configure these settings:

   ```
   Mode Settings:
   ✓ Use Random Actions: TRUE (checked)
   ✓ Use Python Server: FALSE (unchecked)

   Mode: Training
   ```

3. Click Play - it will work immediately!

**How it works:**

- The agent will play randomly and collect data
- No Python server needed
- Perfect for testing and learning the system
- You'll see episodes running in the console

### Mode 2: Python-Connected Training (For Advanced Training)

Use this when you want the full neural network training with Python.

#### Setup Steps:

1. **Start Python Server First:**

   ```bash
   cd ML/training
   source ../angrybirds_ml_env/bin/activate
   python train_agent.py
   ```

2. **Then in Unity Inspector:**

   ```
   Mode Settings:
   ✓ Use Random Actions: FALSE (for policy network)
   ✓ Use Python Server: TRUE (checked)

   Mode: Training
   ```

3. **Then Click Play in Unity**

---

## Issue 2: "Failed to generate training report: Could not find path"

### ✅ FIXED - Solution Applied

The error occurred because the checkpoint directory didn't exist when trying to save training reports.

**What was fixed:**

- Added directory creation check in `GenerateTrainingReport()` method
- Created `ML/models/checkpoints` and `ML/models/logs` directories
- Training reports will now save successfully

**No action needed - it's already fixed!**

---

## Issue 3: Game Playing on Its Own (Auto-Play)

### ✅ FIXED - Solution Applied

### Problem
The AI was automatically playing the game even when you wanted manual control.

### Solution
Added `Enable Auto Play` flag to `MLAgentBrain` component.

**To Play Manually (Default):**
```
MLAgentBrain Inspector:
✗ Enable Auto Play: FALSE (unchecked)
```
Now you can drag and shoot the bird yourself!

**To Let AI Play Automatically:**
```
MLAgentBrain Inspector:
✓ Enable Auto Play: TRUE (checked)
✓ Use Random Actions: TRUE (for random training)
```
The AI will automatically shoot when ready.

**Current Default: Manual Control** - Auto-play is OFF by default.

---

## Issue 4: DOTween Warnings

### Problem

```
DOTWEEN ► Tween startup failed (NULL target/property)
```

This happens when DOTween tries to animate objects that have been destroyed or are null.

### Solutions

#### Option 1: Quick Fix (Ignore)

These warnings don't break functionality. The game will work fine. You can ignore them.

#### Option 2: Install DOTween (Recommended)

1. In Unity, go to `Window > Package Manager`
2. Click `+` (top-left) → `Add package from git URL`
3. Enter: `https://github.com/Demigiant/dotween.git`
4. Or download from Unity Asset Store: "DOTween (HOTween v2)"

#### Option 3: Add Null Checks

Edit the scripts to add null checks before animations:

```csharp
// Example in Bird.cs
if (gameObject != null && !Thrown)
{
    gameObject.transform.DOJump(...);
}
```

---

## Current Status After Fixes

### ✅ What's Working Now:

- ML Agent initializes successfully
- Training mode activates
- Episodes start and reset properly
- GameStateCollector works
- TrainingManager is active
- No more connection errors (with Unity-only mode)
- Training reports save correctly to `ML/models/checkpoints`
- Checkpoint directories created automatically
- **Manual control is now the default** (auto-play disabled)
- Infinite loop fixed

### ⚠️ Minor Warnings (Safe to Ignore):

- DOTween warnings (don't affect ML training)
- Python connection warning if not using Python server (informational only)

---

## Quick Start Commands

### Manual Play (Default):

1. In Unity Inspector (MLAgentBrain):
   - `Enable Auto Play`: ✗ FALSE
   - `Use Random Actions`: ✓ TRUE
   - `Use Python Server`: ✗ FALSE
2. Click Play
3. **Drag and shoot the bird yourself!**

### Auto-Play Training:

1. In Unity Inspector (MLAgentBrain):
   - `Enable Auto Play`: ✓ TRUE
   - `Use Random Actions`: ✓ TRUE
   - `Use Python Server`: ✗ FALSE
2. Click Play
3. Watch AI play automatically!

### Python-Connected Training (Advanced):

```bash
# Terminal 1 - Start Python
cd ML/training
source ../angrybirds_ml_env/bin/activate
python train_agent.py

# Terminal 2 - Not needed, just play in Unity
# In Unity: Use Python Server = TRUE, then Play
```

---

## Expected Console Output (Working Correctly)

You should see:

```
ML Agent initialized in Training mode
GameStateCollector initialized
Performance Monitor initialized
Training Manager initialized
Target episodes: 10000
========== Episode 1/10000 Started ==========
Episode Reset - Pigs: 1, Bricks: 14
```

If you see Python connection warnings but episodes still start, **that's fine!** The system is working in Unity-only mode.

---

## Verification Checklist

- [x] ML Agent initializes
- [x] Episode starts
- [x] GameStateCollector works
- [x] Training Manager works
- [x] No more blocking errors
- [ ] (Optional) Python server connected

---

## Next Steps

### For Learning/Testing:

1. ✅ Keep current settings (Unity-only mode)
2. ✅ Let it run for 10-20 episodes
3. ✅ Watch the console output
4. ✅ See if it starts hitting targets

### For Real Training:

1. Setup Python virtual environment (if not done)
2. Start Python server
3. Enable "Use Python Server" in Unity
4. Run for hundreds/thousands of episodes

---

## Need More Help?

### Check These:

1. **Unity Console** - Look for red errors (warnings are OK)
2. **Inspector Settings** - Verify MLAgentBrain settings
3. **Scene Setup** - Make sure GameObject has all components

### Common Questions:

**Q: Why do I see "Failed to connect" warnings?**
A: That's expected if `Use Python Server` is FALSE. It's informational only.

**Q: Is the AI learning without Python?**
A: In Unity-only mode, it's collecting data and trying random actions. For real learning, you need Python mode.

**Q: When should I use Python mode?**
A: After you've verified everything works in Unity-only mode and want serious training.

**Q: How long to train?**
A: Start with 100 episodes in Unity-only mode (~30 min), then 1000+ episodes in Python mode (hours/days).

---

## Current Configuration (Applied)

```csharp
// MLAgentBrain settings
useRandomActions = true;      // ✓ Random exploration
usePythonServer = false;      // ✗ No Python needed
mode = Training;              // ✓ Training active
```

**Status: ✅ READY TO USE**

Just click Play in Unity and watch it train!
