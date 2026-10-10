# Speed-Time Graph lesson

Open `Assets/Scenes/Speed_Time_Graph.unity` and press Play. The reference composition is designed for a 16:9 Game view.

All lesson code is isolated in this folder. Existing project scripts, source GLBs, other scenes, package configuration and build settings are not changed by this implementation.

## Controls

- Drag a glowing graph point to change speed. Questions 2 and 3 also allow time edits.
- PLAY starts all three vehicles on the same integrated speed curve.
- RESET / RETRY restores their starting positions.
- Q1, Q2 and Q3 select the JSON-driven challenges; NEXT advances to the next question.
- After playback, drag the timeline to review the synchronized positions.

The initial graph predicts 35 m. Question 1 targets 30 m, question 2 targets 60 m, and question 3 combines the 15 m speed gate, 30–38 m slow zone, and 50 m stopping target. Question-specific world scaling fits each challenge into the same camera composition; the scale is identical across all three lanes.

## Assets and implementation

- `Models/` contains OBJ conversions of the supplied Lamborghini, motorcycle, train and railway GLBs, including embedded textures. The standard-library converter lives in `Work/SpeedTimeGraph/convert_models.py`.
- `Generated/` contains new URP materials, combined ghost meshes, environmental meshes and rounded UI art.
- `Scripts/SpeedTimeChallenge.cs` supplies one analytic piecewise-linear speed function and trapezoidal distance integral for all vehicles, ghosts and review positions.
- `Scripts/SpeedTimeGraphPlot.cs` draws the graph, area fill and shared time cursor.
- `Editor/SpeedTimeSceneBuilder.cs` can rebuild the scene with **Tools > Speed Time Graph > Build Reference Scene**. It backs up the destination scene before rebuilding.
- `Editor/SpeedTimePlayChecks.cs` contains the isolated Play Mode interaction checks.

The supplied vehicle meshes are used with new scene-specific colors. Their actual shapes differ from the illustrated hatchback, passenger train and motorcycle in the reference; the scene reconstructs the reference layout rather than reproducing every illustrated pixel.

## Verification

The scene was compiled and rendered with Unity 6000.5.5f1 in an isolated project under `Work/SpeedTimeGraph/BuildProject`, using the project's installed package versions. Reports and the rendered preview are in `Work/SpeedTimeGraph/`.

Checks cover analytic distance and speed, valid solutions to all three questions, slow-zone rejection, synchronized vehicle endpoints, initialization in Play Mode, Input System graph dragging, live ghost updates, Play/Reset/question buttons, and timeline scrubbing.
