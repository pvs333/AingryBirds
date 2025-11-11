#!/bin/bash

# Setup script for Angry Birds AI/ML Environment
# Run this script to set up the Python environment for ML training

echo "Setting up Angry Birds AI/ML Environment..."

# Create virtual environment
python3.10 -m venv angrybirds_ml_env

# Activate virtual environment
source angrybirds_ml_env/bin/activate

# Upgrade pip
pip install --upgrade pip

# Install required packages
pip install -r requirements.txt

echo "Environment setup complete!"
echo "To activate the environment, run: source angrybirds_ml_env/bin/activate"
