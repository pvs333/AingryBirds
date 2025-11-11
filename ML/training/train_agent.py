"""
Main Training Script for Angry Birds AI Agent
Implements the complete reinforcement learning pipeline (Steps 1-8)
"""

import torch
import torch.nn as nn
import torch.optim as optim
import numpy as np
import yaml
import os
import logging
from datetime import datetime
from collections import deque
import matplotlib.pyplot as plt

from policy_network import create_model, ActorCriticNetwork
from unity_environment import UnityEnvironmentWrapper
from replay_buffer import ReplayBuffer
from torch.utils.tensorboard import SummaryWriter


class AngryBirdsTrainer:
    """
    Main trainer class implementing the 10-step algorithm
    """
    
    def __init__(self, config_path='config.yaml'):
        """Initialize trainer with configuration"""
        # Get the directory where this script is located
        script_dir = os.path.dirname(os.path.abspath(__file__))
        
        # Make config path relative to script directory if it's just a filename
        if not os.path.isabs(config_path) and not os.path.exists(config_path):
            config_path = os.path.join(script_dir, config_path)
        
        # Load configuration
        with open(config_path, 'r') as f:
            self.config = yaml.safe_load(f)
        
        # Setup logging
        self.setup_logging()
        
        # Initialize components
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.logger.info(f"Using device: {self.device}")
        
        # Create network (Step 4, 7)
        self.policy_network = create_model(
            model_type=self.config['network']['model_type'],
            state_dim=self.config['network']['state_dim'],
            action_dim=self.config['network']['action_dim'],
            hidden_dims=self.config['network']['hidden_dims']
        ).to(self.device)
        
        # Setup optimizer (Step 7)
        self.optimizer = optim.Adam(
            self.policy_network.parameters(),
            lr=self.config['training']['learning_rate'],
            weight_decay=self.config['optimization']['weight_decay']
        )
        
        # Replay buffer for experience replay
        self.replay_buffer = ReplayBuffer(
            capacity=self.config['replay_buffer']['capacity']
        )
        
        # Training metrics
        self.episode_rewards = []
        self.episode_lengths = []
        self.success_rates = []
        self.exploration_rate = self.config['training']['exploration_rate_start']
        
        # Tensorboard
        if self.config['logging']['tensorboard']:
            self.writer = SummaryWriter(self.config['training']['log_dir'])
        
        self.logger.info("Trainer initialized successfully")
    
    def setup_logging(self):
        """Setup logging configuration"""
        log_dir = self.config['training']['log_dir']
        os.makedirs(log_dir, exist_ok=True)
        
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler(f"{log_dir}/training_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log"),
                logging.StreamHandler()
            ]
        )
        self.logger = logging.getLogger(__name__)
    
    def collect_data(self, env, num_steps):
        """
        Step 1: Data Collection
        Collect gameplay states from Unity environment
        """
        game_states = []
        
        state = env.reset()
        for step in range(num_steps):
            # Select action (Step 4)
            action = self.select_action(state)
            
            # Execute in environment (Step 5)
            next_state, reward, done, info = env.step(action)
            
            # Store experience
            game_states.append({
                'state': state,
                'action': action,
                'reward': reward,
                'next_state': next_state,
                'done': done,
                'info': info
            })
            
            if done:
                state = env.reset()
            else:
                state = next_state
        
        return game_states
    
    def select_action(self, state, training=True):
        """
        Step 4: Action Selection
        Select action using exploration/exploitation strategy
        """
        # Exploration vs Exploitation
        if training and np.random.random() < self.exploration_rate:
            # Random action (exploration)
            angle = np.random.uniform(
                self.config['environment']['angle_range'][0],
                self.config['environment']['angle_range'][1]
            )
            force = np.random.uniform(
                self.config['environment']['force_range'][0],
                self.config['environment']['force_range'][1]
            )
            action = np.array([angle, force])
        else:
            # Policy network action (exploitation)
            state_tensor = torch.FloatTensor(state).unsqueeze(0).to(self.device)
            
            if isinstance(self.policy_network, ActorCriticNetwork):
                action, _, _ = self.policy_network.sample_action(state_tensor)
                action = action.squeeze(0).cpu().numpy()
            else:
                action = self.policy_network.predict(state)
        
        return action
    
    def compute_reward(self, info):
        """
        Step 6: Reward Assignment
        Calculate reward based on outcome
        """
        reward = 0.0
        rewards_config = self.config['rewards']
        
        # Hit/miss rewards
        if info.get('hit_target', False):
            reward += rewards_config['hit_target']
        else:
            reward += rewards_config['miss_penalty']
        
        # Structural damage
        damage = info.get('damage', 0)
        reward += damage * rewards_config['structural_damage']
        
        # Pig destroyed
        pigs_destroyed = info.get('pigs_destroyed', 0)
        reward += pigs_destroyed * rewards_config['pig_destroyed']
        
        # Level complete
        if info.get('level_complete', False):
            reward += rewards_config['level_complete']
            
            # Efficiency bonus
            birds_used = info.get('birds_used', 0)
            if birds_used < 3:
                reward += rewards_config['efficiency_bonus'] * (3 - birds_used)
        
        # Time penalty
        time_taken = info.get('time_taken', 0)
        reward += time_taken * rewards_config['time_penalty']
        
        return reward
    
    def update_policy(self, batch):
        """
        Step 7: Policy Update
        Update neural network using backpropagation
        """
        # Extract batch data
        states = torch.FloatTensor(np.array([exp['state'] for exp in batch])).to(self.device)
        actions = torch.FloatTensor(np.array([exp['action'] for exp in batch])).to(self.device)
        rewards = torch.FloatTensor(np.array([exp['reward'] for exp in batch])).to(self.device)
        next_states = torch.FloatTensor(np.array([exp['next_state'] for exp in batch])).to(self.device)
        dones = torch.FloatTensor(np.array([exp['done'] for exp in batch])).to(self.device)
        
        # Compute loss based on network type
        if isinstance(self.policy_network, ActorCriticNetwork):
            loss = self.compute_actor_critic_loss(states, actions, rewards, next_states, dones)
        else:
            loss = self.compute_policy_loss(states, actions, rewards, next_states, dones)
        
        # Backpropagation (Step 7)
        self.optimizer.zero_grad()
        loss.backward()
        
        # Gradient clipping
        torch.nn.utils.clip_grad_norm_(
            self.policy_network.parameters(),
            self.config['optimization']['gradient_clip']
        )
        
        self.optimizer.step()
        
        return loss.item()
    
    def compute_actor_critic_loss(self, states, actions, rewards, next_states, dones):
        """Compute Actor-Critic loss"""
        # Get current predictions
        action_mean, action_log_std, values = self.policy_network(states)
        action_std = action_log_std.exp()
        
        # Compute advantages
        with torch.no_grad():
            _, _, next_values = self.policy_network(next_states)
            target_values = rewards.unsqueeze(1) + \
                          self.config['training']['gamma'] * next_values * (1 - dones.unsqueeze(1))
            advantages = target_values - values
        
        # Policy loss (actor)
        dist = torch.distributions.Normal(action_mean, action_std)
        log_probs = dist.log_prob(actions).sum(dim=-1, keepdim=True)
        policy_loss = -(log_probs * advantages.detach()).mean()
        
        # Value loss (critic)
        value_loss = nn.MSELoss()(values, target_values.detach())
        
        # Entropy bonus for exploration
        entropy = dist.entropy().sum(dim=-1).mean()
        
        # Total loss
        total_loss = policy_loss + \
                    self.config['optimization']['value_loss_coefficient'] * value_loss - \
                    self.config['optimization']['entropy_coefficient'] * entropy
        
        return total_loss
    
    def compute_policy_loss(self, states, actions, rewards, next_states, dones):
        """Compute basic policy gradient loss"""
        predicted_actions, predicted_values = self.policy_network(states)
        
        # Compute returns
        with torch.no_grad():
            _, next_values = self.policy_network(next_states)
            returns = rewards.unsqueeze(1) + \
                     self.config['training']['gamma'] * next_values * (1 - dones.unsqueeze(1))
        
        # Policy loss
        action_loss = nn.MSELoss()(predicted_actions, actions)
        value_loss = nn.MSELoss()(predicted_values, returns)
        
        total_loss = action_loss + 0.5 * value_loss
        return total_loss
    
    def train(self):
        """
        Step 8: Training Loop
        Main training loop iterating through episodes
        """
        self.logger.info("Starting training...")
        
        # Create Unity environment wrapper (Step 1)
        # Note: Requires Unity game to be running with ML-Agents
        self.logger.info("=" * 60)
        self.logger.info("WAITING FOR UNITY CONNECTION")
        self.logger.info("=" * 60)
        self.logger.info("Please start Unity and click Play now!")
        self.logger.info("The Python server is listening on port 5004...")
        self.logger.info("")
        
        try:
            env = UnityEnvironmentWrapper(self.config)
            self.logger.info("✅ Successfully connected to Unity!")
        except Exception as e:
            self.logger.error(f"❌ Failed to connect to Unity environment: {e}")
            self.logger.error("Make sure:")
            self.logger.error("  1. Unity is running")
            self.logger.error("  2. MLAgentBrain 'Use Python Server' = TRUE")
            self.logger.error("  3. Unity is in Play mode")
            raise ConnectionError("Cannot connect to Unity. Please start Unity and try again.")
        
        max_episodes = self.config['training']['max_episodes']
        
        for episode in range(max_episodes):
            # Reset environment
            state = env.reset()
            episode_reward = 0
            episode_length = 0
            episode_data = []
            
            # Run episode
            done = False
            while not done:
                # Select action (Step 4)
                action = self.select_action(state, training=True)
                
                # Execute action (Step 5)
                next_state, _, done, info = env.step(action)
                
                # Compute reward (Step 6)
                reward = self.compute_reward(info)
                
                # Store experience
                experience = {
                    'state': state,
                    'action': action,
                    'reward': reward,
                    'next_state': next_state,
                    'done': done,
                    'info': info
                }
                episode_data.append(experience)
                self.replay_buffer.add(experience)
                
                episode_reward += reward
                episode_length += 1
                state = next_state
                
                # Update policy (Step 7)
                if len(self.replay_buffer) >= self.config['replay_buffer']['min_size']:
                    if episode_length % self.config['training']['update_frequency'] == 0:
                        batch = self.replay_buffer.sample(self.config['training']['batch_size'])
                        loss = self.update_policy(batch)
            
            # Update exploration rate
            self.exploration_rate = max(
                self.config['training']['exploration_rate_end'],
                self.exploration_rate * self.config['training']['exploration_decay']
            )
            
            # Log metrics
            self.episode_rewards.append(episode_reward)
            self.episode_lengths.append(episode_length)
            
            # Print progress
            if episode % self.config['logging']['print_frequency'] == 0:
                avg_reward = np.mean(self.episode_rewards[-100:])
                self.logger.info(
                    f"Episode {episode}/{max_episodes} | "
                    f"Reward: {episode_reward:.2f} | "
                    f"Avg Reward (100): {avg_reward:.2f} | "
                    f"Exploration: {self.exploration_rate:.4f}"
                )
                
                # Tensorboard logging
                if self.config['logging']['tensorboard']:
                    self.writer.add_scalar('Reward/Episode', episode_reward, episode)
                    self.writer.add_scalar('Reward/Average', avg_reward, episode)
                    self.writer.add_scalar('Exploration Rate', self.exploration_rate, episode)
            
            # Save model checkpoint
            if episode % self.config['training']['save_frequency'] == 0:
                self.save_model(episode)
        
        # Final save
        self.save_model('final')
        self.logger.info("Training completed!")
        env.close()
    
    def save_model(self, episode):
        """Save model checkpoint"""
        checkpoint_dir = self.config['training']['checkpoint_dir']
        os.makedirs(checkpoint_dir, exist_ok=True)
        
        checkpoint_path = os.path.join(checkpoint_dir, f'model_episode_{episode}.pth')
        torch.save({
            'episode': episode,
            'model_state_dict': self.policy_network.state_dict(),
            'optimizer_state_dict': self.optimizer.state_dict(),
            'exploration_rate': self.exploration_rate,
            'episode_rewards': self.episode_rewards,
        }, checkpoint_path)
        
        self.logger.info(f"Model saved to {checkpoint_path}")
    
    def load_model(self, checkpoint_path):
        """Load model checkpoint"""
        checkpoint = torch.load(checkpoint_path, map_location=self.device)
        self.policy_network.load_state_dict(checkpoint['model_state_dict'])
        self.optimizer.load_state_dict(checkpoint['optimizer_state_dict'])
        self.exploration_rate = checkpoint['exploration_rate']
        self.episode_rewards = checkpoint['episode_rewards']
        
        self.logger.info(f"Model loaded from {checkpoint_path}")


if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(description='Train Angry Birds AI Agent')
    parser.add_argument('--config', type=str, default='config.yaml',
                       help='Path to configuration file')
    parser.add_argument('--resume', type=str, default=None,
                       help='Path to checkpoint to resume training')
    
    args = parser.parse_args()
    
    # Create trainer
    trainer = AngryBirdsTrainer(config_path=args.config)
    
    # Resume from checkpoint if specified
    if args.resume:
        trainer.load_model(args.resume)
    
    # Start training
    trainer.train()
