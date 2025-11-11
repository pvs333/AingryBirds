"""
Example training script showing how to use the Angry Birds AI system
Run this after setting up the Unity project
"""

import sys
import os

# Add parent directory to path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from train_agent import AngryBirdsTrainer
import argparse


def main():
    """
    Example training workflow
    """
    print("=" * 60)
    print("Angry Birds AI Training - Example Script")
    print("=" * 60)
    
    parser = argparse.ArgumentParser(description='Train Angry Birds AI')
    parser.add_argument('--config', type=str, default='config.yaml',
                       help='Configuration file path')
    parser.add_argument('--episodes', type=int, default=None,
                       help='Override max episodes')
    parser.add_argument('--fast', action='store_true',
                       help='Fast training mode (fewer episodes for testing)')
    parser.add_argument('--resume', type=str, default=None,
                       help='Resume from checkpoint')
    
    args = parser.parse_args()
    
    # Create trainer
    print("\n[1/4] Initializing trainer...")
    trainer = AngryBirdsTrainer(config_path=args.config)
    
    # Override episodes if specified
    if args.episodes:
        trainer.config['training']['max_episodes'] = args.episodes
        print(f"Max episodes set to: {args.episodes}")
    
    # Fast mode
    if args.fast:
        print("Fast mode enabled - reducing training parameters")
        trainer.config['training']['max_episodes'] = 100
        trainer.config['training']['save_frequency'] = 20
        trainer.config['logging']['print_frequency'] = 5
    
    # Resume from checkpoint
    if args.resume:
        print(f"\n[2/4] Loading checkpoint from: {args.resume}")
        trainer.load_model(args.resume)
    else:
        print("\n[2/4] Starting fresh training...")
    
    # Display configuration
    print("\n[3/4] Training Configuration:")
    print(f"  Max Episodes: {trainer.config['training']['max_episodes']}")
    print(f"  Learning Rate: {trainer.config['training']['learning_rate']}")
    print(f"  Exploration: {trainer.config['training']['exploration_rate_start']} → {trainer.config['training']['exploration_rate_end']}")
    print(f"  Network: {trainer.config['network']['model_type']}")
    print(f"  State Dim: {trainer.config['network']['state_dim']}")
    print(f"  Action Dim: {trainer.config['network']['action_dim']}")
    
    print("\n[4/4] Starting training...")
    print("=" * 60)
    print("\n⚠️  IMPORTANT: Make sure Unity is running and playing the game!\n")
    
    input("Press Enter to start training (Ctrl+C to cancel)...")
    
    # Start training
    try:
        trainer.train()
    except KeyboardInterrupt:
        print("\n\nTraining interrupted by user")
        print("Saving current progress...")
        trainer.save_model('interrupted')
        print("Progress saved!")
    except Exception as e:
        print(f"\n\nError during training: {e}")
        import traceback
        traceback.print_exc()
    
    print("\n" + "=" * 60)
    print("Training session complete!")
    print("=" * 60)


if __name__ == "__main__":
    main()
