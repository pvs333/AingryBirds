"""
Unity Environment Wrapper
Handles communication between Python training code and Unity game
"""

import numpy as np
import socket
import json
import struct
from typing import Tuple, Dict, Any


class UnityEnvironmentWrapper:
    """
    Wrapper for Unity environment communication
    Uses socket communication to send/receive data from Unity
    """
    
    def __init__(self, config):
        """
        Initialize Unity environment connection
        
        Args:
            config: Configuration dictionary
        """
        self.config = config
        self.port = config['unity']['port']
        self.timeout = config['unity']['timeout']
        
        self.state_dim = config['network']['state_dim']
        self.action_dim = config['network']['action_dim']
        
        # Connection
        self.socket = None
        self.connected = False
        
        # Connect to Unity
        self.connect()
    
    def connect(self):
        """Establish connection with Unity - Python acts as SERVER"""
        try:
            # Create server socket
            server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server_socket.bind(('localhost', self.port))
            server_socket.listen(1)
            
            print(f"🐍 Python server listening on port {self.port}...")
            print(f"⏳ Waiting for Unity to connect...")
            print(f"   (Start Unity and press Play)")
            
            # Wait for Unity to connect (with timeout)
            server_socket.settimeout(300)  # 5 minute timeout
            self.socket, address = server_socket.accept()
            server_socket.close()
            
            self.connected = True
            print(f"✅ Unity connected from {address}")
        except socket.timeout:
            print(f"❌ Connection timeout - Unity did not connect within 5 minutes")
            raise ConnectionError("Unity connection timeout")
        except Exception as e:
            print(f"❌ Failed to establish server: {e}")
            raise
    
    def reset(self) -> np.ndarray:
        """
        Reset the environment
        
        Returns:
            state: Initial state vector
        """
        # Send reset command
        command = {'type': 'reset'}
        self.send_data(command)
        
        # Receive initial state
        response = self.receive_data()
        state = np.array(response['state'], dtype=np.float32)
        
        return state
    
    def step(self, action: np.ndarray) -> Tuple[np.ndarray, float, bool, Dict[str, Any]]:
        """
        Execute action in environment
        
        Args:
            action: Action array [angle, force]
            
        Returns:
            next_state: Next state vector
            reward: Reward value (computed separately)
            done: Whether episode is finished
            info: Additional information
        """
        # Send action to Unity
        command = {
            'type': 'step',
            'action': {
                'angle': float(action[0]),
                'force': float(action[1])
            }
        }
        self.send_data(command)
        
        # Receive response
        response = self.receive_data()
        
        next_state = np.array(response['state'], dtype=np.float32)
        done = response['done']
        info = response['info']
        
        # Reward is computed by reward calculator
        reward = 0.0
        
        return next_state, reward, done, info
    
    def send_data(self, data: Dict):
        """Send data to Unity"""
        try:
            # Convert to JSON
            json_data = json.dumps(data)
            message = json_data.encode('utf-8')
            
            # Send message length first
            self.socket.sendall(struct.pack('!I', len(message)))
            
            # Send message
            self.socket.sendall(message)
        except Exception as e:
            print(f"Error sending data: {e}")
            self.connected = False
            raise
    
    def receive_data(self) -> Dict:
        """Receive data from Unity"""
        try:
            # Receive message length
            length_data = self.socket.recv(4)
            if not length_data:
                raise ConnectionError("Connection closed by Unity")
            
            message_length = struct.unpack('!I', length_data)[0]
            
            # Receive message
            chunks = []
            bytes_received = 0
            while bytes_received < message_length:
                chunk = self.socket.recv(min(message_length - bytes_received, 4096))
                if not chunk:
                    raise ConnectionError("Connection closed during message receive")
                chunks.append(chunk)
                bytes_received += len(chunk)
            
            message = b''.join(chunks)
            
            # Parse JSON
            data = json.loads(message.decode('utf-8'))
            return data
            
        except Exception as e:
            print(f"Error receiving data: {e}")
            self.connected = False
            raise
    
    def close(self):
        """Close connection"""
        if self.socket:
            try:
                command = {'type': 'close'}
                self.send_data(command)
            except:
                pass
            
            self.socket.close()
            self.connected = False
            print("Connection closed")
    
    def __del__(self):
        """Cleanup on deletion"""
        self.close()


class MockUnityEnvironment:
    """
    Mock environment for testing without Unity
    """
    
    def __init__(self, config):
        self.config = config
        self.state_dim = config['network']['state_dim']
        self.action_dim = config['network']['action_dim']
        
        self.current_state = None
        self.steps = 0
        self.max_steps = 100
    
    def reset(self):
        """Reset environment"""
        self.current_state = np.random.randn(self.state_dim).astype(np.float32)
        self.steps = 0
        return self.current_state
    
    def step(self, action):
        """Execute action"""
        # Simulate state transition
        self.current_state = np.random.randn(self.state_dim).astype(np.float32)
        self.steps += 1
        
        # Random outcome
        done = self.steps >= self.max_steps or np.random.random() < 0.1
        
        info = {
            'hit_target': np.random.random() < 0.3,
            'damage': np.random.randint(0, 100),
            'pigs_destroyed': np.random.randint(0, 3),
            'level_complete': done and np.random.random() < 0.2,
            'time_taken': self.steps * 0.1,
            'birds_used': 1
        }
        
        return self.current_state, 0.0, done, info
    
    def close(self):
        """Close environment"""
        pass


if __name__ == "__main__":
    # Test mock environment
    print("Testing Mock Unity Environment...")
    
    config = {
        'network': {'state_dim': 64, 'action_dim': 2},
        'unity': {'port': 5004, 'timeout': 60}
    }
    
    env = MockUnityEnvironment(config)
    
    state = env.reset()
    print(f"Initial state shape: {state.shape}")
    
    for i in range(5):
        action = np.array([np.random.uniform(-90, 90), np.random.uniform(0, 1)])
        next_state, reward, done, info = env.step(action)
        print(f"Step {i+1}: action={action}, done={done}, info={info}")
        
        if done:
            break
    
    env.close()
    print("Test completed!")
