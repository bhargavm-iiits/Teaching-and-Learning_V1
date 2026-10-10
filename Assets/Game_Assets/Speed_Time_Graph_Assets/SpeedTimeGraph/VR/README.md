# First-person VR speed–time challenge

Open `Assets/Scenes/Speed_Time_Graph.unity` and enter Play with an active OpenXR headset runtime. The scene initializes OpenXR itself; project-wide automatic XR startup stays disabled. The existing XR Interaction Toolkit Starter Assets supply tracked hands, trigger selection, grip grabbing, snap turning and teleport input.

- Aim a controller at the console and pull the trigger to select buttons or drag graph handles. The first speed point is locked: bike **10 m/s**, car **20 m/s**, train **30 m/s**.
- Select CAR / TRAIN / BIKE to edit that vehicle's speed scale. One shared normalized graph drives all three. Targets, gates and slow zones scale by bike ×1, car ×2, train ×3; the displayed units are actual metres and m/s.
- Run, pause/resume, reset/retry, restore the graph, change questions, and review the timeline with the physical console.
- Grip the console frame to reposition it; RECENTER CONSOLE restores its station position.
- Use the teleport input to aim at a viewing platform, or select START / MID / FINISH on the console. The console follows station changes.
- Vehicles are selectable with a controller. Live labels and graph links show motion and stopping predictions.

There are three challenges: graph shape, area/distance, and gate/slow-zone constraints. Vehicles travel in world metres at their actual graph speed. Roads extend beyond the full editable motion range.

Without a headset, the scene offers a first-person mouse preview: right mouse rotates the view; left mouse operates the console. Physical headset tracking, controller bindings and comfort need checking on the intended headset before distribution. This is a Unity scene, not a packaged APK or executable.

All VR scripts are in this separate `VR` folder. Existing project scripts and the earlier desktop lesson scripts are preserved. Scene and XR settings backups are under `Backups/SpeedTimeGraph/VR`.

Editor tools: `Tools > Speed Time Graph > VR > Build First Person VR Scene` regenerates this scene. Automated isolated-project validation checks graph integration and controller ray interaction, playback and teleport behavior.
