"""
Test a trained Angry Birds AI model
"""

import torch
import numpy as np
import argparse
from policy_network import create_model


def test_model(model_path, num_episodes=10):
    """
    Test a trained model
    
    Args:
        model_path: Path to saved model checkpoint
        num_episodes: Number of test episodes
    """
    print(f"Loading model from: {model_path}")
    
    # Load checkpoint
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    checkpoint = torch.load(model_path, map_location=device)
    
    # Create model
    model = create_model(
        model_type='actor_critic',
        state_dim=64,
        action_dim=2
    )
    model.load_state_dict(checkpoint['model_state_dict'])
    model.eval()
    
    print(f"Model loaded successfully!")
    print(f"Trained for {checkpoint.get('episode', 'unknown')} episodes")
    
    # Test with random states
    print(f"\nTesting model with {num_episodes} random states...")
    
    for i in range(num_episodes):
        # Generate random state
        state = torch.randn(1, 64).to(device)
        
        # Predict action
        with torch.no_grad():
            action, log_prob, value = model.sample_action(state)
        
        angle = action[0, 0].item()
        force = action[0, 1].item()
        
        print(f"Test {i+1}: Angle={angle:6.2f}°, Force={force:.3f}, Value={value.item():.2f}")
    
    print("\n✓ Model test complete!")
    print("\nTo use this model in Unity:")
    print("1. Set MLAgentBrain.mode = Deployment")
    print(f"2. Set MLAgentBrain.modelPath = '{model_path}'")
    print("3. Uncheck 'Use Random Actions'")
    print("4. Click Play in Unity")


def test_network_architecture():
    """Test that network architecture is working"""
    print("Testing network architecture...")
    
    from policy_network import PolicyNetwork, ActorCriticNetwork, DQNNetwork
    
    # Test PolicyNetwork
    print("\n1. Testing PolicyNetwork...")
    policy_net = PolicyNetwork(state_dim=64, action_dim=2)
    test_input = torch.randn(4, 64)
    action, value = policy_net(test_input)
    print(f"   Input shape: {test_input.shape}")
    print(f"   Action shape: {action.shape}")
    print(f"   Value shape: {value.shape}")
    print("   ✓ PolicyNetwork OK")
    
    # Test ActorCriticNetwork
    print("\n2. Testing ActorCriticNetwork...")
    ac_net = ActorCriticNetwork(state_dim=64, action_dim=2)
    action, log_prob, value = ac_net.sample_action(test_input)
    print(f"   Action shape: {action.shape}")
    print(f"   Log prob shape: {log_prob.shape}")
    print(f"   Value shape: {value.shape}")
    print("   ✓ ActorCriticNetwork OK")
    
    # Test DQNNetwork
    print("\n3. Testing DQNNetwork...")
    dqn_net = DQNNetwork(state_dim=64, num_actions=100)
    q_values = dqn_net(test_input)
    print(f"   Q-values shape: {q_values.shape}")
    print("   ✓ DQNNetwork OK")
    
    print("\n✓ All network architectures working correctly!")


def main():
    parser = argparse.ArgumentParser(description='Test Angry Birds AI Model')
    parser.add_argument('--model', type=str, default=None,
                       help='Path to model checkpoint')
    parser.add_argument('--episodes', type=int, default=10,
                       help='Number of test episodes')
    parser.add_argument('--test-arch', action='store_true',
                       help='Test network architecture only')
    
    args = parser.parse_args()
    
    if args.test_arch:
        test_network_architecture()
    elif args.model:
        test_model(args.model, args.episodes)
    else:
        print("Error: Must specify --model or --test-arch")
        print("\nExamples:")
        print("  python test_agent.py --model ../models/trained_model.pth")
        print("  python test_agent.py --test-arch")


if __name__ == "__main__":
    main()
