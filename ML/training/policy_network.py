"""
Policy Network Architecture for Angry Birds AI
Implements the neural network for learning optimal launch strategies
"""

import torch
import torch.nn as nn
import torch.nn.functional as F
import numpy as np


class PolicyNetwork(nn.Module):
    """
    Deep Neural Network for predicting launch angle and force
    
    Input: State vector (target positions, obstacle layout, bird position)
    Output: Action (angle, force)
    """
    
    def __init__(self, state_dim=64, action_dim=2, hidden_dims=[256, 256, 128]):
        """
        Args:
            state_dim: Dimension of state vector
            action_dim: Dimension of action (angle, force)
            hidden_dims: List of hidden layer dimensions
        """
        super(PolicyNetwork, self).__init__()
        
        self.state_dim = state_dim
        self.action_dim = action_dim
        
        # Build network layers
        layers = []
        input_dim = state_dim
        
        for hidden_dim in hidden_dims:
            layers.append(nn.Linear(input_dim, hidden_dim))
            layers.append(nn.ReLU())
            layers.append(nn.Dropout(0.2))
            input_dim = hidden_dim
        
        self.feature_extractor = nn.Sequential(*layers)
        
        # Action head (angle and force)
        self.action_head = nn.Linear(hidden_dims[-1], action_dim)
        
        # Value head for critic (used in Actor-Critic)
        self.value_head = nn.Linear(hidden_dims[-1], 1)
        
    def forward(self, state):
        """
        Forward pass through network
        
        Args:
            state: State tensor [batch_size, state_dim]
            
        Returns:
            action: Predicted action [batch_size, action_dim]
            value: State value estimate [batch_size, 1]
        """
        features = self.feature_extractor(state)
        
        # Predict action (angle in [-90, 90], force in [0, 1])
        action = self.action_head(features)
        action[:, 0] = torch.tanh(action[:, 0]) * 90  # Angle
        action[:, 1] = torch.sigmoid(action[:, 1])     # Force (0-1)
        
        # Predict value
        value = self.value_head(features)
        
        return action, value
    
    def predict(self, state):
        """
        Predict action for a single state (inference mode)
        
        Args:
            state: State array or tensor
            
        Returns:
            action: Predicted action [angle, force]
        """
        self.eval()
        with torch.no_grad():
            if not isinstance(state, torch.Tensor):
                state = torch.FloatTensor(state).unsqueeze(0)
            
            action, _ = self.forward(state)
            return action.squeeze(0).cpu().numpy()


class DQNNetwork(nn.Module):
    """
    Deep Q-Network for Q-learning based approach
    Alternative to policy gradient methods
    """
    
    def __init__(self, state_dim=64, num_actions=100):
        """
        Args:
            state_dim: Dimension of state vector
            num_actions: Number of discrete actions
        """
        super(DQNNetwork, self).__init__()
        
        self.fc1 = nn.Linear(state_dim, 256)
        self.fc2 = nn.Linear(256, 256)
        self.fc3 = nn.Linear(256, 128)
        self.fc4 = nn.Linear(128, num_actions)
        
    def forward(self, state):
        """
        Forward pass to estimate Q-values
        
        Args:
            state: State tensor [batch_size, state_dim]
            
        Returns:
            q_values: Q-value estimates [batch_size, num_actions]
        """
        x = F.relu(self.fc1(state))
        x = F.relu(self.fc2(x))
        x = F.relu(self.fc3(x))
        q_values = self.fc4(x)
        return q_values


class ActorCriticNetwork(nn.Module):
    """
    Actor-Critic Network for advantage-based learning (A2C/A3C)
    """
    
    def __init__(self, state_dim=64, action_dim=2, hidden_dims=None):
        super(ActorCriticNetwork, self).__init__()
        
        # Default hidden dimensions if not provided
        if hidden_dims is None:
            hidden_dims = [256, 256]
        
        # Shared feature extractor
        layers = []
        prev_dim = state_dim
        for hidden_dim in hidden_dims:
            layers.append(nn.Linear(prev_dim, hidden_dim))
            layers.append(nn.ReLU())
            prev_dim = hidden_dim
        
        self.shared = nn.Sequential(*layers)
        final_hidden = hidden_dims[-1] if hidden_dims else state_dim
        
        # Actor: outputs mean and std for continuous actions
        self.actor_mean = nn.Linear(final_hidden, action_dim)
        self.actor_log_std = nn.Linear(final_hidden, action_dim)
        
        # Critic: outputs state value
        self.critic = nn.Linear(final_hidden, 1)
        
    def forward(self, state):
        shared_features = self.shared(state)
        
        # Actor outputs
        action_mean = self.actor_mean(shared_features)
        action_mean[:, 0] = torch.tanh(action_mean[:, 0]) * 90  # Angle
        action_mean[:, 1] = torch.sigmoid(action_mean[:, 1])     # Force
        
        action_log_std = self.actor_log_std(shared_features)
        action_log_std = torch.clamp(action_log_std, -20, 2)
        
        # Critic output
        value = self.critic(shared_features)
        
        return action_mean, action_log_std, value
    
    def sample_action(self, state):
        """Sample action from policy distribution"""
        action_mean, action_log_std, value = self.forward(state)
        action_std = action_log_std.exp()
        
        # Sample from normal distribution
        normal = torch.distributions.Normal(action_mean, action_std)
        action = normal.sample()
        log_prob = normal.log_prob(action).sum(dim=-1, keepdim=True)
        
        return action, log_prob, value


def create_model(model_type='policy', **kwargs):
    """
    Factory function to create appropriate model
    
    Args:
        model_type: 'policy', 'dqn', or 'actor_critic'
        **kwargs: Additional arguments for model
        
    Returns:
        model: Neural network model
    """
    if model_type == 'policy':
        return PolicyNetwork(**kwargs)
    elif model_type == 'dqn':
        return DQNNetwork(**kwargs)
    elif model_type == 'actor_critic':
        return ActorCriticNetwork(**kwargs)
    else:
        raise ValueError(f"Unknown model type: {model_type}")


if __name__ == "__main__":
    # Test the networks
    print("Testing Policy Network...")
    policy_net = PolicyNetwork(state_dim=64, action_dim=2)
    test_state = torch.randn(4, 64)  # Batch of 4 states
    action, value = policy_net(test_state)
    print(f"Input shape: {test_state.shape}")
    print(f"Action shape: {action.shape}, Value shape: {value.shape}")
    print(f"Sample action: angle={action[0, 0]:.2f}°, force={action[0, 1]:.2f}")
    
    print("\nTesting Actor-Critic Network...")
    ac_net = ActorCriticNetwork(state_dim=64, action_dim=2)
    action, log_prob, value = ac_net.sample_action(test_state)
    print(f"Action shape: {action.shape}")
    print(f"Log prob shape: {log_prob.shape}")
    print(f"Value shape: {value.shape}")
