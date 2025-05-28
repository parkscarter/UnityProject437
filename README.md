
# **Checker Lock**

This game was developed for **Comsci 437** at **Iowa State University**. It’s a mix of terrain exploration and strategic decision-making, where you’ll face AI bots in both a 3D terrain and a Checkers game. The terrain bots use decision trees and NavMesh for pathfinding, and the exit challenge includes a Minimax AI with variable difficulty.

---

## **Table of Contents**

1. [Gameplay](#gameplay)
2. [Features](#features)
3. [AI Mechanics](#ai-mechanics)
4. [Controls](#controls)
5. [Installation](#installation)
---

## **Gameplay**

- **Terrain Scene**: You’ll explore a large terrain where enemies will try to hunt you down. The enemies are pretty clever (thanks to a state machine). They roam the map until you get too close, if you're spotted the enemies chase you and deal damage.
  
- **Checkers Challenge**: Once you make it to one of the exit gates, it’s time for a Checkers showdown! Each gate has a different difficulty:
    - **Green Trim**: Easy AI
    - **Yellow Trim**: Medium AI
    - **Red Trim**: Hard AI

Win the Checkers game, and you win the entire game!

---

## **AI Mechanics**

### **Enemy Behavior**
- Each enemy has different characteristics such as thier vision and speed.
    - **Roaming**: When the player is out of range, they roam around the map searching.
    - **Chasing**: Get too close, and they’ll chase you down relentlessly.
    - **NavMesh Pathfinding**: Enemies use **Unity’s NavMesh** to navigate the terrain, making their movement realistic and intelligent.

### **Checkers AI (Minimax Algorithm)**
The AI in the Checkers game doesn’t just make random moves. It uses the **Minimax algorithm** to evaluate potential moves and pick the best one. It’s a great way to make sure players of all skill levels are challenged, with different difficulty levels based on the exit gate you choose.

---

## **Controls**

- **W, A, S, D**: Move around the terrain (forward, left, backward, right).
- **C**: Sprint to move faster (but be careful—stamina drains quickly!).
- **Health & Stamina**: You’ll see these bars in the top left. If your health runs out, it’s game over.

---

## **Installation**

To run this game on your machine, follow these steps:

0. **Pre-Reqs**: Download Unity Hub (free)

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/parkscarter/UnityProject437.git

2. **Open Project**: Using Unity hub, open the cloned repo in the Unity editor

3. **Open Terrain Scene**: Then hit play from the editor
    

