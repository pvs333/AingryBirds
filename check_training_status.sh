#!/bin/bash

# Quick Status Check Script
# Shows your current training progress

echo "======================================"
echo "   AI TRAINING STATUS CHECK"
echo "======================================"
echo ""

# Check if checkpoint files exist
if [ -d "ML/models/checkpoints" ]; then
    checkpoint_count=$(ls -1 ML/models/checkpoints/checkpoint_episode_*.json 2>/dev/null | wc -l)
    echo "📊 Checkpoints found: $checkpoint_count"
    
    if [ $checkpoint_count -gt 0 ]; then
        latest_checkpoint=$(ls -t ML/models/checkpoints/checkpoint_episode_*.json 2>/dev/null | head -1)
        if [ -f "$latest_checkpoint" ]; then
            echo "📁 Latest checkpoint: $(basename $latest_checkpoint)"
            echo ""
            echo "--- Checkpoint Data ---"
            cat "$latest_checkpoint"
            echo ""
        fi
    else
        echo "⚠️  No checkpoints yet (need to reach episode 100)"
    fi
else
    echo "⚠️  Checkpoint directory not found"
fi

echo ""
echo "======================================"
echo "   CURRENT CONFIGURATION"
echo "======================================"
echo ""

# Check Python server status
if lsof -Pi :5004 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
    echo "🐍 Python server: ✅ RUNNING (port 5004)"
    echo "   Mode: LEARNING MODE (Neural network active)"
else
    echo "🐍 Python server: ❌ NOT RUNNING"
    echo "   Mode: RANDOM MODE (Data collection only)"
fi

echo ""
echo "======================================"
echo "   TO START LEARNING"
echo "======================================"
echo ""
echo "1. Open a new terminal"
echo "2. Run these commands:"
echo "   cd ML/training"
echo "   source ../angrybirds_ml_env/bin/activate"
echo "   python train_agent.py"
echo ""
echo "3. In Unity Inspector (MLAgentBrain):"
echo "   - Set 'Use Python Server' = TRUE"
echo "   - Set 'Use Random Actions' = FALSE"
echo ""
echo "4. Click Play in Unity"
echo ""
echo "======================================"
echo "   PROGRESS ESTIMATE"
echo "======================================"
echo ""

if [ -f "ML/models/logs/training_log_"*.txt ]; then
    latest_log=$(ls -t ML/models/logs/training_log_*.txt 2>/dev/null | head -1)
    if [ -f "$latest_log" ]; then
        episode_count=$(grep -c "Episode" "$latest_log" 2>/dev/null || echo "0")
        success_count=$(grep -c "SUCCESS" "$latest_log" 2>/dev/null || echo "0")
        
        if [ "$episode_count" -gt 0 ]; then
            success_rate=$(echo "scale=1; $success_count * 100 / $episode_count" | bc)
            echo "📈 Episodes completed: ~$episode_count"
            echo "🎯 Successful attempts: $success_count"
            echo "📊 Success rate: ${success_rate}%"
            echo ""
            
            if [ "$episode_count" -lt 100 ]; then
                echo "Status: 🔴 Just started (collecting initial data)"
            elif [ "$episode_count" -lt 500 ]; then
                echo "Status: 🟡 Early training (still mostly random)"
            elif [ "$episode_count" -lt 2000 ]; then
                echo "Status: 🟠 Learning phase (should see improvement)"
            elif [ "$episode_count" -lt 5000 ]; then
                echo "Status: 🟢 Advanced training (getting good)"
            else
                echo "Status: 🌟 Expert level (high accuracy)"
            fi
        else
            echo "⚠️  No episode data found yet"
        fi
    fi
else
    echo "⚠️  No training logs found yet"
    echo "   Training hasn't started or logs not saved"
fi

echo ""
echo "======================================"
