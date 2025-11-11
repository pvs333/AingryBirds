"""
Replay Buffer for Experience Replay
Stores and samples past experiences for training
"""

import numpy as np
from collections import deque
import random


class ReplayBuffer:
    """
    Experience replay buffer for storing and sampling past experiences
    """
    
    def __init__(self, capacity=100000):
        """
        Args:
            capacity: Maximum number of experiences to store
        """
        self.buffer = deque(maxlen=capacity)
        self.capacity = capacity
    
    def add(self, experience):
        """
        Add experience to buffer
        
        Args:
            experience: Dict with keys: state, action, reward, next_state, done, info
        """
        self.buffer.append(experience)
    
    def sample(self, batch_size):
        """
        Sample random batch of experiences
        
        Args:
            batch_size: Number of experiences to sample
            
        Returns:
            batch: List of sampled experiences
        """
        return random.sample(self.buffer, min(batch_size, len(self.buffer)))
    
    def __len__(self):
        """Return current size of buffer"""
        return len(self.buffer)
    
    def clear(self):
        """Clear the buffer"""
        self.buffer.clear()


class PrioritizedReplayBuffer(ReplayBuffer):
    """
    Prioritized experience replay buffer
    Samples important experiences more frequently
    """
    
    def __init__(self, capacity=100000, alpha=0.6, beta=0.4):
        """
        Args:
            capacity: Maximum buffer size
            alpha: Prioritization exponent
            beta: Importance sampling exponent
        """
        super().__init__(capacity)
        self.priorities = deque(maxlen=capacity)
        self.alpha = alpha
        self.beta = beta
        self.epsilon = 1e-6  # Small constant to avoid zero priority
    
    def add(self, experience, priority=None):
        """Add experience with priority"""
        if priority is None:
            priority = max(self.priorities) if self.priorities else 1.0
        
        self.buffer.append(experience)
        self.priorities.append(priority)
    
    def sample(self, batch_size):
        """Sample batch based on priorities"""
        if len(self.buffer) == 0:
            return []
        
        # Calculate sampling probabilities
        priorities = np.array(self.priorities)
        probabilities = priorities ** self.alpha
        probabilities /= probabilities.sum()
        
        # Sample indices
        indices = np.random.choice(
            len(self.buffer),
            size=min(batch_size, len(self.buffer)),
            replace=False,
            p=probabilities
        )
        
        # Get experiences
        batch = [self.buffer[idx] for idx in indices]
        
        # Calculate importance sampling weights
        weights = (len(self.buffer) * probabilities[indices]) ** (-self.beta)
        weights /= weights.max()
        
        # Add weights to experiences
        for i, exp in enumerate(batch):
            exp['weight'] = weights[i]
        
        return batch
    
    def update_priorities(self, indices, priorities):
        """Update priorities for sampled experiences"""
        for idx, priority in zip(indices, priorities):
            self.priorities[idx] = priority + self.epsilon


if __name__ == "__main__":
    # Test replay buffer
    print("Testing Replay Buffer...")
    buffer = ReplayBuffer(capacity=100)
    
    # Add some experiences
    for i in range(50):
        exp = {
            'state': np.random.randn(64),
            'action': np.random.randn(2),
            'reward': np.random.randn(),
            'next_state': np.random.randn(64),
            'done': False,
            'info': {}
        }
        buffer.add(exp)
    
    print(f"Buffer size: {len(buffer)}")
    
    # Sample batch
    batch = buffer.sample(32)
    print(f"Sampled batch size: {len(batch)}")
    
    # Test prioritized buffer
    print("\nTesting Prioritized Replay Buffer...")
    pri_buffer = PrioritizedReplayBuffer(capacity=100)
    
    for i in range(50):
        exp = {
            'state': np.random.randn(64),
            'action': np.random.randn(2),
            'reward': np.random.randn(),
            'next_state': np.random.randn(64),
            'done': False,
            'info': {}
        }
        pri_buffer.add(exp, priority=np.random.rand())
    
    print(f"Prioritized buffer size: {len(pri_buffer)}")
    batch = pri_buffer.sample(32)
    print(f"Sampled batch size: {len(batch)}")
    print(f"Sample has weights: {'weight' in batch[0]}")
