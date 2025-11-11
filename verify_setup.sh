#!/bin/bash

# Verification script for Angry Birds AI/ML setup
# Run this to check if everything is configured correctly

echo "============================================"
echo "Angry Birds AI/ML Setup Verification"
echo "============================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

ERRORS=0
WARNINGS=0

# Function to check if file exists
check_file() {
    if [ -f "$1" ]; then
        echo -e "${GREEN}✓${NC} Found: $1"
        return 0
    else
        echo -e "${RED}✗${NC} Missing: $1"
        ERRORS=$((ERRORS + 1))
        return 1
    fi
}

# Function to check if directory exists
check_dir() {
    if [ -d "$1" ]; then
        echo -e "${GREEN}✓${NC} Found: $1"
        return 0
    else
        echo -e "${RED}✗${NC} Missing: $1"
        ERRORS=$((ERRORS + 1))
        return 1
    fi
}

echo "1. Checking Project Structure..."
echo "--------------------------------"

# Check Unity scripts
check_file "Assets/Scripts/ML/GameStateCollector.cs"
check_file "Assets/Scripts/ML/DataPreprocessor.cs"
check_file "Assets/Scripts/ML/StateVectorConverter.cs"
check_file "Assets/Scripts/ML/MLAgentBrain.cs"
check_file "Assets/Scripts/ML/RewardCalculator.cs"
check_file "Assets/Scripts/ML/TrainingManager.cs"
check_file "Assets/Scripts/ML/PerformanceMonitor.cs"

echo ""
echo "2. Checking Python Backend..."
echo "--------------------------------"

# Check ML directory
check_dir "ML"
check_file "ML/requirements.txt"
check_file "ML/setup_environment.sh"
check_dir "ML/training"
check_file "ML/training/train_agent.py"
check_file "ML/training/policy_network.py"
check_file "ML/training/unity_environment.py"
check_file "ML/training/replay_buffer.py"
check_file "ML/training/config.yaml"

echo ""
echo "3. Checking Documentation..."
echo "--------------------------------"

check_file "README.md"
check_file "QUICKSTART.md"
check_file "AIML_SETUP_GUIDE.md"
check_file "ARCHITECTURE.md"
check_file "IMPLEMENTATION_SUMMARY.md"
check_file "ML/README.md"

echo ""
echo "4. Checking Python Environment..."
echo "--------------------------------"

if [ -d "ML/angrybirds_ml_env" ]; then
    echo -e "${GREEN}✓${NC} Python virtual environment found"
    
    # Check if we can activate it
    if [ -f "ML/angrybirds_ml_env/bin/activate" ]; then
        echo -e "${GREEN}✓${NC} Virtual environment is valid"
    else
        echo -e "${RED}✗${NC} Virtual environment activation script missing"
        ERRORS=$((ERRORS + 1))
    fi
else
    echo -e "${YELLOW}⚠${NC} Python virtual environment not found"
    echo "  Run: cd ML && ./setup_environment.sh"
    WARNINGS=$((WARNINGS + 1))
fi

echo ""
echo "5. Checking Python Packages (if env exists)..."
echo "--------------------------------"

if [ -d "ML/angrybirds_ml_env" ]; then
    # Try to check installed packages
    if command -v python3 &> /dev/null; then
        if [ -f "ML/angrybirds_ml_env/bin/python" ]; then
            ML/angrybirds_ml_env/bin/python -c "import torch; print('✓ PyTorch installed')" 2>/dev/null || echo -e "${YELLOW}⚠${NC} PyTorch not installed"
            ML/angrybirds_ml_env/bin/python -c "import numpy; print('✓ NumPy installed')" 2>/dev/null || echo -e "${YELLOW}⚠${NC} NumPy not installed"
            ML/angrybirds_ml_env/bin/python -c "import yaml; print('✓ PyYAML installed')" 2>/dev/null || echo -e "${YELLOW}⚠${NC} PyYAML not installed"
        fi
    fi
else
    echo -e "${YELLOW}⚠${NC} Skipping (no virtual environment)"
fi

echo ""
echo "6. Checking Unity Original Scripts..."
echo "--------------------------------"

check_file "Assets/Scripts/Bird.cs"
check_file "Assets/Scripts/SlingShot.cs"
check_file "Assets/Scripts/GameManager.cs"
check_file "Assets/Scripts/Pig.cs"

echo ""
echo "============================================"
echo "Verification Summary"
echo "============================================"

if [ $ERRORS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
    echo -e "${GREEN}✓ All checks passed!${NC}"
    echo ""
    echo "Your setup is complete and ready to use!"
    echo ""
    echo "Next steps:"
    echo "1. Open the project in Unity"
    echo "2. Add ML components to a GameObject (see QUICKSTART.md)"
    echo "3. Start training: cd ML/training && python train_agent.py"
elif [ $ERRORS -eq 0 ]; then
    echo -e "${YELLOW}⚠ Setup is mostly complete with $WARNINGS warning(s)${NC}"
    echo ""
    echo "You may need to:"
    echo "- Run: cd ML && ./setup_environment.sh"
else
    echo -e "${RED}✗ Setup incomplete: $ERRORS error(s), $WARNINGS warning(s)${NC}"
    echo ""
    echo "Please check the missing files above."
    echo "You may need to re-run the setup process."
fi

echo ""
echo "For help, see:"
echo "- QUICKSTART.md for quick setup"
echo "- AIML_SETUP_GUIDE.md for detailed instructions"
echo "============================================"

# Exit with appropriate code
if [ $ERRORS -gt 0 ]; then
    exit 1
else
    exit 0
fi
