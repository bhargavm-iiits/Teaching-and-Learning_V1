# Teaching and Learning V1 — Interactive VR Kinematics & Physics Suite

An immersive, room-scale Virtual Reality (VR) educational suite designed for conceptual understanding of **Kinematics and Physics**. Built with **Unity 6**, **Universal Render Pipeline (URP)**, and **XR Interaction Toolkit (XRI 3.5.1)** using **OpenXR**, this project bridges classical mechanics theory and intuitive physical simulation.

---

## 🎬 Video Demonstrations

Recorded in-engine gameplay and interactive VR walkthroughs showcasing core kinematics principles:

### 1. Speed–Time Graph Walkthrough
> Dynamic acceleration, deceleration, and distance derivation through area under the speed-time curve.
- 📹 **File:** [`Recordings/Speed time graph.mp4`](Recordings/Speed%20time%20graph.mp4)
- **Direct Video Preview:**
<video src="Recordings/Speed%20time%20graph.mp4" controls="controls" width="100%"></video>

---

### 2. Velocity–Time Graph Walkthrough
> Vector velocity visualization, directional sign changes, slope as instantaneous acceleration, and area calculation.
- 📹 **File:** [`Recordings/Velocity time graph.mp4`](Recordings/Velocity%20time%20graph.mp4)
- **Direct Video Preview:**
<video src="Recordings/Velocity%20time%20graph.mp4" controls="controls" width="100%"></video>

---

### 3. Direction and Distance Walkthrough
> 1D coordinate reference frames, origin points, path length vs. directional displacement vectors.
- 📹 **File:** [`Recordings/Direction and Distance.mp4`](Recordings/Direction%20and%20Distance.mp4)
- **Direct Video Preview:**
<video src="Recordings/Direction%20and%20Distance.mp4" controls="controls" width="100%"></video>

---

### 4. Rest and Motion Walkthrough
> Relative frames of reference inside a rising elevator cabin; observer rest relative to cabin vs. motion relative to outside city landmarks.
- 📹 **File:** [`Recordings/Motion.mp4`](Recordings/Motion.mp4)
- **Direct Video Preview:**
<video src="Recordings/Motion.mp4" controls="controls" width="100%"></video>

---

## 📚 Learning Modules & Interactive Scenes

| # | Scene File | Concept Covered | Pedagogical Objective |
|---|---|---|---|
| **1** | [`Direction_Origin.unity`](Assets/Scenes/Direction_Origin.unity) | Direction & Coordinate Origin | Understanding reference points $(x=0)$, positive/negative orientation, and position measurement. |
| **2** | [`Distance_And_Displacement.unity`](Assets/Scenes/Distance_And_Displacement.unity) | Distance vs. Displacement | Scalar total path length vs. vector shortest path between initial and final coordinates. |
| **3** | [`Rest_and_Motion.unity`](Assets/Scenes/Rest_and_Motion.unity) | Reference Frames (Rest & Motion) | Elevator ride simulation demonstrating that state of motion depends entirely on the chosen observer reference frame. |
| **4** | [`Speed.unity`](Assets/Scenes/Speed.unity) | Speed (Scalar Rate) | Rate of change of distance, instantaneous vs. average speed, and digital/analog speedometer readouts. |
| **5** | [`Velocity.unity`](Assets/Scenes/Velocity.unity) | Velocity (Vector Rate) | Rate of change of displacement with explicit directional heading and signed vector indicators. |
| **6** | [`Distance_Time_Graph.unity`](Assets/Scenes/Distance_Time_Graph.unity) | Distance–Time Graphs | Real-time plotting where slope represents speed; linear vs curved slopes for uniform vs non-uniform motion. |
| **7** | [`Velocity_Time_Graph.unity`](Assets/Scenes/Velocity_Time_Graph.unity) | Velocity–Time Graphs | Interactive graph plotting where slope $\frac{\Delta v}{\Delta t}$ equals acceleration, and area equals displacement. |
| **8** | [`Unfirom_Motion.unity`](Assets/Scenes/Unfirom_Motion.unity) | Uniform Motion | Motion covering equal distances in equal intervals of time with constant velocity and zero acceleration. |
| **9** | [`Non_Unifrom_Motion.unity`](Assets/Scenes/Non_Unifrom_Motion.unity) | Non-Uniform Motion | Variable speeds and accelerations along curved circuits and elevation changes. |
| **10** | [`Acceleration_Retardation.unity`](Assets/Scenes/Acceleration_Retardation.unity) | Acceleration & Retardation | Positive acceleration (speeding up) vs negative acceleration / retardation / deceleration (braking). |
| **11** | [`Equations_of_Motion.unity`](Assets/Scenes/Equations_of_Motion.unity) | Equations of Motion | Numerical validation of Newton's kinematic formulas ($v = u + at$, $s = ut + \frac{1}{2}at^2$, $v^2 = u^2 + 2as$). |

---

## 🥽 VR Interaction & Controls

### Hardware Support
- **XR Runtime:** OpenXR
- **Target Devices:** Meta Quest 2 / 3 / Pro (Link / AirLink), HTC Vive, Valve Index, Windows Mixed Reality
- **Desktop Simulation:** XR Device Simulator enabled for development without headset hardware

### Controls Overview
| Action | VR Controllers | XR Device Simulator (Desktop) |
|---|---|---|
| **Look / Aim** | Head Tracking (HMD) | Mouse Right-Click + Drag |
| **Interact / Select** | Trigger / Grip / Poke Interactor | Space / Left-Click |
| **Locomotion** | Primary Thumbstick / Teleport Ray | WASD Keys |
| **Turn** | Secondary Thumbstick (Snap / Smooth) | Q / E Keys |
| **Reset / Menu** | Menu Button (Left Controller) | Tab / Esc |

---

## 🗂️ Project Repository Structure

```text
Teaching_And_Learning/
├── Assets/
│   ├── Game_Assets/              # Modular 3D models, textures, and assets grouped by topic
│   │   ├── Acceleration_Retardation/
│   │   ├── Distance and Displacement/
│   │   ├── DistanceTimeGraph/
│   │   ├── Equations_of_Motion/
│   │   ├── Non Uniform Motion/
│   │   ├── Rest_and_Motion/
│   │   ├── Speed/
│   │   ├── Speed_Time_Graph_Assets/
│   │   ├── Uniform_Motion/
│   │   └── Velocity/
│   ├── Scenes/                   # All 11 kinematic learning scenes
│   ├── Scripts/                  # Gameplay, tracking, and physics controller scripts
│   ├── Settings/                 # URP graphic profiles and pipeline settings
│   └── XR/                       # OpenXR, interaction profiles, and simulation settings
├── Docs/                         # Detailed lesson plans, reference diagrams, and specs
├── Packages/                     # Unity package manifest and dependency lockfiles
├── ProjectSettings/              # Project, build, input, and physics settings
├── Recordings/                   # 4 demonstration MP4 videos
├── .gitattributes                # Git LFS configurations for models, assets, and videos
├── .gitignore                    # Unity build and cache exclusion rules
└── README.md                     # Project documentation and video guides
```

---

## 🚀 Getting Started

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/bhargavm-iiits/Teaching-and-Learning_V1.git
   cd Teaching-and-Learning_V1
   ```
2. **Pull Large Files (Git LFS):**
   ```bash
   git lfs install
   git lfs pull
   ```
3. **Open with Unity:**
   - Unity Version: **Unity 6 (6000.x)** or compatible LTS with **URP** and **OpenXR** support.
   - Select `Teaching_And_Learning` folder in Unity Hub.
4. **Select a Scene:**
   - Navigate to `Assets/Scenes/` and double-click any topic scene to load and test in Play Mode.

---

## 👤 Author & Maintainer
- **Author:** Bhargav
- **Email:** `bhargav.m23@iiits.in`
- **Institution:** Indian Institute of Information Technology, Sri City (IIIT Sri City)
