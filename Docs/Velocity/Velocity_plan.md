# MASTER IMPLEMENTATION PROMPT — TRAIN VELOCITY VR

Use this document as one complete implementation prompt. Follow every section unless a later section explicitly narrows an earlier rule.

## Reference image

![Uploaded TRAIN VELOCITY VR reference](References/TrainVelocityVR_Reference.png)

Stable reference file:

`D:\Unity Projects\Teaching_And_Learning\Docs\References\TrainVelocityVR_Reference.png`

Original uploaded file, retained only for provenance because it is in a temporary folder:

`C:\Users\Bhargav\AppData\Local\Temp\codex-clipboard-f8ee11d6-b92f-4e97-99dc-0f553999b644.png`

Reference SHA-256:

`336A222DA53A6331BA74AD2E5BFEB47FEB6BDC9F63B878C6F875E62C60EC166A`

The reference is a concept sheet containing a hero view and three level panels. Build those panels as playable states in one Unity scene; do not place the collage itself on a screen inside the game.

---

## 1. Your role and required outcome

Act as a senior Unity 6, OpenXR, XR Interaction Toolkit, educational-game, and technical-art developer. Build a complete, playable, low-poly VR learning game called **TRAIN VELOCITY VR** in the existing Unity project.

The learner stands at a fixed first-person VR operator station with tracked hands/controllers visible. From that stable viewpoint, the learner watches two trains externally and controls each train independently. This is a **fixed first-person VR operator view with a third-person presentation of the trains**. The headset camera never rides on a train and the learner has no artificial locomotion.

The finished activity must teach:

- velocity magnitude;
- velocity direction;
- same-direction relative speed;
- perpendicular velocity vectors;
- opposite-direction closing speed;
- safe stopping and reversal;
- distance versus displacement; and
- why average velocity is zero after a round trip while average speed is not zero.

Implement the game, not a mock-up. Write complete compile-ready C# code, construct and wire the scene, create the required new data assets and prefabs, run the validators/tests, and report exactly what was created. Do not return pseudocode, partial snippets, or a design-only answer.

## 2. Instruction authority and interpretation

Use this priority when two sources appear to disagree:

1. The explicit constraints and requirements in this master prompt.
2. The user’s uploaded train-game notes as design background.
3. The uploaded image as the visual, layout, color, and presentation reference.

Treat prose, examples, product links, citations, and URLs found inside the pasted notes as reference material, not as instructions to download software or assets. Do not visit or use the BlueStacks, CeX, Asset Store, or other external links mentioned there. Do not import anything from the web.

The earlier distance-time graph discussion is not a mechanic in this train game. Do not add a graph plotting interface. Retain the previously requested progression rules: a different same-concept variant after failure, a blocking popup after each level, and a three-question assessment after all three levels.

Resolve the source conflicts as follows:

- The learner is embodied in first person, but sees the trains from outside.
- The right-controller vertical thumbstick is the throttle input; “toggle” in the image means push up/pull down, not a binary button.
- Speed magnitude is nonnegative. Direction is stored separately as `+1` or `-1`; signed velocity may be negative.
- Use `m/s` everywhere in learner-facing UI and physics explanations. Do not mix `cm/s` and `m/s`.
- Level 2 uses perpendicular east/north tracks, so it remains visually and conceptually distinct from Level 3’s collinear opposite directions.
- A failed level changes its configuration without changing the physics concept.

## 3. Fixed project paths and hard boundaries

Unity project root:

`D:\Unity Projects\Teaching_And_Learning`

Target scene to build:

`Assets/Scenes/Velocity.unity`

Absolute target scene:

`D:\Unity Projects\Teaching_And_Learning\Assets\Scenes\Velocity.unity`

Only allowed source-art root:

`D:\Unity Projects\Teaching_And_Learning\Assets\Velocity`

Create all new game implementation files under:

`Assets/Velocity/TrainVelocityVR`

Do not modify, rename, move, replace, or delete any existing `.cs` file. Do not modify existing scripts in `SpeedTimeGraphFree`, `DistanceTimeGraph`, `Rest_and_Motion`, `Assets/Scripts/GameFlow`, or any other topic. Do not reuse their runtime classes. Existing code may be inspected to understand project conventions, but this implementation must be independent.

Every C# class must be newly written, placed in its own new file, use a unique `Velocity`-prefixed type name where practical, and live in the namespace:

`TeachingAndLearning.VelocityVR`

Use new assembly definitions so this feature remains isolated. Do not alter an existing assembly definition, package manifest, input-actions asset, prefab, material, model, texture, or third-party file. Do not hardcode `D:\...` paths in runtime code; use serialized references and Unity-relative asset paths.

The only existing file that the implementation is expected to update is `Assets/Scenes/Velocity.unity`. It is currently an almost-empty scene and is the required destination. Adding that scene to Build Settings is allowed. All other implementation assets must be newly created under `Assets/Velocity/TrainVelocityVR`.

Visible art may come only from the supplied files listed below or from Unity primitives/procedural meshes and solid-color URP materials created specifically for this scene. Package components already installed in the project may be used for XR, input, UI, and rendering. Do not use art from any other project folder, XR sample visuals, Asset Store package, online source, or generated image service.

Allowed helper content:

- Unity cubes, planes, cylinders, capsules, spheres, quads, line renderers, trails, particle systems, and procedural meshes;
- TextMeshPro text and Unity UI geometry;
- new URP/Lit or URP/Unlit material instances;
- new colliders, triggers, anchors, splines/rail paths, render textures, and prefabs;
- installed OpenXR, XR Interaction Toolkit, XR Hands, Input System, UGUI, and URP components.

There are no supplied audio clips. Create serialized audio hooks with silent fallback, but do not download, generate, or borrow audio.

## 4. Verified project and asset baseline

The project uses:

- Unity `6000.5.5f1`;
- URP `17.5.0`;
- Input System `1.19.0`;
- XR Interaction Toolkit `3.5.1`;
- OpenXR `1.17.1`;
- XR Management `4.5.4`;
- XR Hands `1.8.1`;
- UGUI `2.5.0`;
- Android minimum SDK 26 and ARM64; and
- an existing configured URP pipeline.

The target scene currently contains only `Main Camera` and `Directional Light`. It has no XR Origin, gameplay, UI, trains, tracks, or managers. Preserve a single AudioListener and use the existing light only if it fits the new hierarchy.

Use only this supplied art inventory:

| Supplied file | Verified content | Required use and caution |
|---|---|---|
| `Assets/Velocity/tangara_sydney_trains_t_set.glb` | Four-car Tangara set; 27 mesh primitives; about 18,726 vertices and 11,714 triangles; eight embedded JPEG textures | Import once, create one normalized train prefab, and instantiate it as Train A and Train B. Exclude node `Plane_5` / mesh `Object_13`, whose track geometry must not move with the train. |
| `Assets/Velocity/Track_1.glb` | Tileable straight/curved railway; five primitives; about 19,948 triangles; two embedded 512 px JPEG textures | Prefer for ordinary straight and parallel route pieces. Isolate or hide curved pieces that are not required. |
| `Assets/Velocity/Track.glb` | Railway intersection with separate straight and turn meshes; about 39,252 triangles; one 4096 px JPEG and three 4096 px PNG maps | Use only where its crossing geometry materially helps Level 2. Generate optimized imported copies at 1024–2048 px with mobile compression and mipmaps. Never alter the source GLB. |
| `Assets/Velocity/Railway_station/source/Bakinskiy zheleznodorozhnyy vokzal.fbx` | Baku station/building model recognized by Unity’s ModelImporter | Use wrapper transforms and bounds-based normalization. Do not change the FBX importer source settings unless writing a new generated copy/profile. |
| `Assets/Velocity/Railway_station/textures/AZE_BAKU_BAKINSKIY-ZD-VOKZAL_S.png` | 2048×2048 colored architectural atlas | Connect as the station base map if automatic material extraction does not bind it correctly. |

The three `.glb` files currently use `DefaultImporter`, and the project does not contain glTFast or UnityGLTF. Therefore, write a new, self-contained, Editor-only Velocity GLB importer for these supplied static files. Do not edit, call, subclass, copy verbatim, or depend on the existing importers elsewhere in the project. Support only the glTF 2.0 features required by these three files, then generate Unity meshes, textures, materials, and prefabs under the new feature folder.

## 5. Visual target

Match the uploaded reference’s friendly, saturated, low-poly visual language:

- warm sunlight, blue sky, soft procedural clouds, alpine silhouettes, stylized pine trees, rocks, river/canyon, stone viaducts, bridge, tunnel mouths, lamps, and railway signals;
- chunky readable shapes and clean silhouettes rather than photoreal detail;
- Train A clearly blue and Train B clearly red/orange;
- blue/cyan Level 1 accents, amber/orange Level 2 accents, and green Level 3/progress accents;
- charcoal world-space panels with pale text, thick colored borders, softly rounded-looking frames constructed from primitives, and high contrast;
- floating velocity cards above both trains, each containing train name, signed velocity text, a direction arrow, and a magnitude bar;
- a large overhead `TRAIN VELOCITY VR` sign;
- a left instruction board explaining selection and right-thumbstick control;
- a right `3 LEVELS` board showing locked, current, and completed states;
- a two-train console in front of the learner;
- an objective panel that stays readable while the trains remain unobstructed;
- subtle emissive motion lines beside moving trains; and
- a compact in-world monitor showing a camera behind and above the currently selected train.

The uploaded art does not include mountains, trees, water, bridge pieces, signs, hands, controllers, arrows, or UI art. Build those from low-poly Unity primitives, procedural meshes, TMP, and new solid-color materials. Do not pull them from other project folders. The station and railway models should anchor the look; procedural scenery fills the missing roles.

Do not use blue versus red as the only distinction. Also use `A`/`B` labels, different panel icons/patterns, arrow directions, glow outlines, and text.

## 6. Player viewpoint, VR comfort, and input

Place the XR Origin on a fixed elevated operator platform about 2–3 metres in front of and above the central console. The player can naturally move their head within the tracked physical area, but cannot teleport, snap-turn, continuously move, ride a train, or be shaken by gameplay.

Required VR controls:

- Left ray + trigger: select Train A or Train B and press world-space UI.
- Right thumbstick Y above a `0.2` dead zone: continuously increase the selected train’s target speed.
- Right thumbstick Y below `-0.2`: continuously decrease target speed.
- Centered thumbstick: hold the selected train’s target speed.
- Physical direction lever for each train: request forward/reverse; it unlocks only when current and target speed are both at or below `0.05 m/s`.
- Start signal lever: arm and start the current challenge.
- Level 2 crossing signal lever and station gate controls.
- Level 3 red emergency-brake lever: apply strong braking to both trains.
- Haptic pulses: selection, throttle limit, rejected reversal, warning, level success. Keep them short and optional.

Create procedural low-poly controller/hand representations from primitive meshes because none are supplied. White glove-like hands and dark controller bodies may visually echo the reference, but they must follow tracked poses and must not use external art.

Provide a complete desktop fallback for development and testing:

- `1` selects Train A;
- `2` selects Train B;
- `W`/Up Arrow increases speed;
- `S`/Down Arrow decreases speed;
- `R` requests direction reversal for the selected train;
- `Enter` starts/continues;
- `Space` applies the Level 3 emergency brake;
- mouse ray presses world-space UI; and
- `Escape` pauses/resumes.

An unavailable XR device must not prevent desktop play. Put XR-specific code in a separate package-guarded assembly and expose it through a package-independent input interface.

## 7. Deterministic motion and physics model

Do not use WheelColliders, NavMesh, or free rigidbody driving. Use a deterministic scalar rail-distance model so the visual result and the educational calculations always agree.

Each `VelocityRailPath` has:

- origin transform;
- normalized positive world direction;
- minimum and maximum distance in metres;
- configurable world-units-per-metre presentation scale;
- optional station, checkpoint, crossing, and danger-zone anchors; and
- `EvaluatePosition(distanceMetres)` and `EvaluateRotation(directionSign)` methods.

Each train stores:

- nonnegative target speed in m/s;
- nonnegative current speed in m/s;
- direction sign `+1` or `-1`;
- signed scalar velocity;
- world velocity vector;
- scalar rail distance in metres;
- rail reference;
- selection, braking, stopped, arrived, failed, and emergency states; and
- total distance travelled and displacement from its attempt start.

Move a kinematic Rigidbody in `FixedUpdate` with `MovePosition` and `MoveRotation`:

```text
targetSpeed += throttleInput * adjustmentRate * fixedDeltaTime
targetSpeed = clamp(targetSpeed, 0, maximumSpeed)
currentSpeed = moveToward(targetSpeed, accelerationOrBraking * fixedDeltaTime)
signedVelocity = currentSpeed * directionSign
railDistance += signedVelocity * fixedDeltaTime
worldPosition = railPath.EvaluatePosition(railDistance)
worldVelocity = railPath.PositiveDirection * signedVelocity
```

Use configurable ScriptableObject values, with these reference defaults:

| Parameter | Default |
|---|---:|
| Maximum speed | `8 m/s` |
| Throttle target adjustment | `3 m/s²` |
| Normal acceleration | `2 m/s²` |
| Normal braking | `3 m/s²` |
| Emergency braking | `8 m/s²` |
| Stop/reversal threshold | `0.05 m/s` |
| Station stop tolerance | `0.35 m` |
| Catch-up position tolerance | `1.0 m` |
| Checkpoint time tolerance | `2.0 s` |

The visual railway may be spatially compressed through `worldUnitsPerMetre`, but every calculation, label, quiz value, success test, replay sample, and debrief must use real metres and seconds consistently.

Required calculations:

- signed velocity: `directionSign × speedMagnitude`;
- world velocity: `railDirection × signedVelocity`;
- same-axis relative closing speed: the derivative of separation, using actual signed velocities;
- opposite-direction closing speed: sum of approaching magnitudes;
- estimated braking distance: `v² / (2a)` for constant deceleration;
- displacement: final rail/world position minus attempt-start position; and
- average velocity: displacement divided by total elapsed time.

Selecting the other train must never alter, pause, or reset the first train. Direction reversal while moving must be rejected with a message, haptic pulse, and lever return animation. The UI arrow, train facing, headlights, signed number, and physical motion must always agree.

## 8. Game flow and state machine

Use one authoritative flow controller with these states:

```text
Boot
Introduction
LevelBriefing
LevelReady
LevelRunning
LevelFailed
FailureReplay
VariantTransition
LevelComplete
SuccessReplay
FinalQuiz
QuizResult
TopicComplete
Paused
```

Legal flow:

```text
Boot -> Introduction -> LevelBriefing -> LevelReady
LevelReady -> LevelRunning when the start signal is pulled
LevelRunning -> LevelFailed when the objective fails
LevelFailed -> FailureReplay -> LevelFailed
LevelFailed -> VariantTransition -> a new LevelBriefing for the same concept
LevelRunning -> LevelComplete when the objective succeeds
LevelComplete -> SuccessReplay -> LevelComplete
LevelComplete -> next LevelBriefing
Level 3 Complete -> FinalQuiz -> QuizResult
QuizResult -> TopicComplete, Level 2 review, or Level 1 restart
```

Pause both motors and disable throttle, selection, levers, and gates during blocking popups, replays, transitions, and the quiz. UI classes may request transitions but may not set game state directly.

The right-side level board must always show each level as `LOCKED`, `CURRENT`, or `COMPLETE`. Unlock sequentially.

## 9. Adaptive failure rule

If a learner fails, never reload the exact same setup. The next attempt remains on the same learning concept but uses a different variant.

On every failure:

1. Freeze safely before any violent collision.
2. Record the attempt and the causal moment.
3. Show one observation-based hint without giving exact values or a control sequence.
4. Offer `WATCH ATTEMPT` and `CONTINUE TO NEW CHALLENGE`.
5. Replay both trains synchronously if requested.
6. Select an unseen same-level variant.
7. Exclude the just-failed variant.
8. Reset trains, objective, signals, gates, occupancy, timers, alerts, and UI.
9. Start the new briefing.

Create at least three data-driven variants per level. Prefer unseen variants. The first alternate should be equal or slightly easier. After every variant has appeared, reshuffle while still excluding the immediately previous variant. A success on any variant completes that level.

Vary starting separation, checkpoint/station positions, track orientation, signal timing, speed bands, braking allowance, danger-zone width, and procedural scenery arrangement. Never change the concept or silently make a failed attempt identical with different colors.

## 10. Level 1 — Same Direction, Different Speeds

Title:

`LEVEL 1 — SAME DIRECTION, DIFFERENT SPEEDS`

Mission:

`CATCH-UP TRAIN CHALLENGE`

Build two straight parallel tracks. Train A starts ahead, Train B starts behind, and both positive arrows point east/forward. Use the reference values for the showcase variant:

- Train A: `+4 m/s east`;
- Train B: `+8 m/s east`;
- relative closing speed: `4 m/s`.

Player sequence:

1. Read the objective without being shown a full solution.
2. Select Train A and set a lower forward speed.
3. Select Train B and set a higher forward speed.
4. Pull the start signal.
5. Switch between trains while both continue independently.
6. Bring them side by side inside the blue checkpoint zone.

Primary success test:

- both direction signs are positive and the world vectors are parallel;
- Train B’s velocity magnitude is greater than Train A’s;
- their longitudinal positions become aligned within `1 m` inside the checkpoint zone; and
- their checkpoint crossing times differ by no more than `2 s` as a secondary tolerance.

Failures include a rail end, wrong direction, missed checkpoint window, collision, or variant timeout. Give causal feedback such as “Train B did not close the gap before the checkpoint,” without revealing the correct speeds.

Success popup:

```text
LEVEL 1 COMPLETE
The faster train closed the gap because both trains moved in the same direction.
Relative speed = faster speed - slower speed.
```

## 11. Level 2 — Different Directions

Title:

`LEVEL 2 — DIFFERENT DIRECTIONS`

Mission:

`TWO-WAY RAILWAY DELIVERY`

Build one east-west track and one north-south track that cross once. Train A travels east and Train B travels north toward separate stations. Protect the crossing with a signal/occupancy controller. Use the reference-image values in one variant and an equal-magnitude comparison in another:

- image-inspired variant: Train A `6 m/s east`, Train B `4 m/s north`;
- concept-isolation variant: both `5 m/s`, one east and one north.

Player sequence:

1. Confirm each direction lever.
2. Set both speed magnitudes.
3. Pull the start signal.
4. Operate the crossing signal so the crossing is occupied by only one train at a time.
5. Stop Train A in its east station zone.
6. Stop Train B in its north station zone.
7. Open each station gate only after that train has stopped correctly.

Success requires correct destinations, correct directions, safe crossing occupancy, both trains stopped within station tolerance, and both gates operated after arrival.

Failures include simultaneous crossing occupancy, collision, wrong direction, station overshoot, rail-end entry, premature gate use, or timeout.

Success popup:

```text
LEVEL 2 COMPLETE
Speed is magnitude. Velocity includes both magnitude and direction.
Equal speeds can still have different velocities when their directions differ.
```

## 12. Level 3 — Opposite Directions and Rescue

Title:

`LEVEL 3 — OPPOSITE DIRECTIONS`

Mission:

`HEAD-ON TRAIN EMERGENCY`

Use one shared straight collision path across a bridge, or two exactly collinear halves treated as one measured rail. Train A begins moving east and Train B begins moving west toward it. Show:

- Train A: `+5 m/s east`;
- Train B: `-5 m/s west` in signed-axis notation;
- closing speed: `10 m/s`;
- live separation;
- estimated braking distance for both trains; and
- a clearly marked red central danger zone.

Player sequence:

1. The emergency begins with both trains already approaching.
2. Select Train A and reduce its speed.
3. Switch to Train B and reduce its speed while Train A continues braking.
4. Use the emergency brake if necessary.
5. Stop both outside the protected danger zone.
6. Wait until both direction levers unlock at zero speed.
7. Reverse both trains.
8. Return each train to its original station.
9. Stop both within station tolerance and view the debrief.

Failure occurs on collision, unsafe danger-zone entry, rail-end entry, wrong return station, timeout, or an illegal reversal request that remains unresolved. Freeze immediately before visual impact; use alarms, red outlines, and feedback rather than a violent crash.

The emergency brake may save the attempt, but the learner must still reverse safely and complete the return.

Success popup:

```text
LEVEL 3 COMPLETE
Opposite velocities made the distance close at the sum of both speeds.
Each train returned to its starting point, so displacement and average velocity are zero.
The distance travelled and average speed are not zero.
```

## 13. Replays, camera monitor, and learner feedback

Record both trains at a fixed sampling interval using time, rail ID, rail distance, current speed, target speed, direction sign, and key event flags. Do not record raw scene transforms as the source of truth.

Failure replay:

- render transparent ghost trains or replay in a paused copy of the level;
- mark the causal moment;
- display actual velocity vectors and separation;
- state what was observed; and
- do not reveal the exact successful settings.

Success replay:

- show the decisive catch-up, crossing, or braking/reversal moment;
- overlay the relevant formula; and
- return to the completion popup when finished.

Create a secondary non-HMD camera behind and slightly above the selected train. Its RenderTexture appears on the operator console monitor. When selection changes, blend or switch the secondary feed. This camera must never drive or move the XR headset camera. Include an overview/follow toggle on the monitor.

## 14. Completion popups and final three-question assessment

Every successful level opens a blocking world-space popup:

```text
LEVEL COMPLETE
[short concept-specific explanation]

NEXT LEVEL
REPLAY SUCCESS
```

After all three levels are complete, ask exactly these three questions. Require one choice for every question before enabling `SUBMIT ANSWERS`. Do not reveal correctness until submission.

### Question 1

```text
Train A travels east at 4 m/s and Train B travels east at 8 m/s.
How quickly does Train B close the gap?

A. 4 m/s
B. 8 m/s
C. 12 m/s

Correct answer: A
```

### Question 2

```text
Two trains both move at 5 m/s. One moves east and the other north.
Do they have the same velocity?

A. Yes, because their speeds are equal
B. No, because their directions are different
C. Yes, because both are moving

Correct answer: B
```

### Question 3

```text
Train A moves east at 5 m/s and Train B moves west toward it at 5 m/s.
How quickly does the distance between them decrease?

A. 0 m/s
B. 5 m/s
C. 10 m/s

Correct answer: C
```

Score routing must be exact:

- `3 correct`: show `TOPIC COMPLETE`, then invoke a serialized `NextTopicRequested` UnityEvent or load a configured `nextTopicSceneName`.
- `2 correct`: preserve Level 1 completion, reset Levels 2 and 3, and begin an unseen Level 2 variant.
- `0 or 1 correct`: reset level completion and begin an unseen Level 1 variant.

Clear all previous quiz selections before a review route. Preserve accessibility/audio preferences. Never immediately repeat the most recent variant. If the next-topic scene is blank or absent from Build Settings, keep the completion screen open and log one clear warning instead of throwing an exception.

## 15. Required scene hierarchy

Construct this hierarchy in `Assets/Scenes/Velocity.unity`. Minor helper children are allowed, but keep these roots and names stable:

```text
Velocity_Root
├── Systems
│   ├── VelocityBootstrap
│   ├── VelocitySceneBindings
│   ├── VelocityGameFlow
│   ├── VelocityLevelManager
│   ├── VelocityAdaptiveVariantManager
│   ├── VelocityInputRouter
│   ├── VelocityReplaySystem
│   └── VelocityQuizSystem
├── Player
│   ├── XROrigin
│   │   ├── CameraOffset
│   │   │   └── Main Camera
│   │   ├── LeftController
│   │   └── RightController
│   └── DesktopFallbackRig
├── SharedEnvironment
│   ├── StationWrapper
│   ├── Mountains
│   ├── Trees
│   ├── Water
│   ├── Lamps
│   ├── OperatorPlatform
│   └── Lighting
├── LevelRoots
│   ├── Level01_CatchUp
│   ├── Level02_DirectionDelivery
│   └── Level03_HeadOnRescue
├── Trains
│   ├── TrainA_Blue
│   └── TrainB_Red
├── Interactions
│   ├── StartSignalLever
│   ├── DirectionLeverA
│   ├── DirectionLeverB
│   ├── CrossingSignalLever
│   ├── StationGateA
│   ├── StationGateB
│   └── EmergencyBrakeLever
├── Cameras
│   ├── OverviewCamera
│   ├── SelectedTrainFollowCamera
│   └── CameraMonitor
└── UI
    ├── TitleBoard
    ├── InstructionBoard
    ├── ThreeLevelsBoard
    ├── VelocityConsole
    ├── ActiveObjectivePanel
    ├── TrainWorldLabels
    ├── FailurePopup
    ├── LevelCompletePopup
    ├── FinalQuizPanel
    └── PausePanel
```

Keep all three level roots in the single required scene. Enable only the active level root. Reuse the two train controllers by assigning new rail/start data during a level transition; do not create uncontrolled duplicate runtime train systems.

## 16. New folder structure

Create this structure without moving the supplied source assets:

```text
Assets/Velocity/TrainVelocityVR/
├── Assemblies/
├── Scripts/
│   ├── Runtime/
│   │   ├── Core/
│   │   ├── Input/
│   │   ├── Trains/
│   │   ├── Levels/
│   │   ├── Interactions/
│   │   ├── Replay/
│   │   ├── UI/
│   │   ├── Cameras/
│   │   └── Audio/
│   └── Editor/
├── Input/
├── Data/
│   ├── Tuning/
│   ├── LevelDefinitions/
│   ├── Variants/
│   └── Quiz/
├── Generated/
│   ├── Meshes/
│   ├── Materials/
│   ├── Textures/
│   ├── Prefabs/
│   └── RenderTextures/
├── Prefabs/
│   ├── Runtime/
│   ├── UI/
│   └── Interactions/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

Create new runtime, XR, editor, EditMode-test, and PlayMode-test assembly definitions. The core runtime and desktop fallback must compile without an active XR device. Use assembly version defines or compiler guards for XR package code.

## 17. Required separate new C# files

Write each class in its own file. Keep UI, physics, objective logic, input, data, and editor tooling separate. At minimum, create these files and implement the stated responsibility fully.

### Core

| File | Responsibility |
|---|---|
| `VelocityGameState.cs` | Defines all legal flow states. |
| `VelocityGameEvents.cs` | Strongly typed gameplay event payloads/channels. |
| `VelocitySceneBindings.cs` | Explicit serialized references to systems, trains, roots, UI, cameras, and interactions; avoid runtime object searches. |
| `VelocityBootstrap.cs` | Validates bindings, initializes a clean session, and starts the state machine. |
| `VelocityGameFlowController.cs` | Owns legal transitions, pause behavior, level progression, and final assessment entry. |
| `VelocitySessionProgress.cs` | Stores active level, completed levels, seen variants, recent variant, quiz answers, and preferences. |
| `VelocityAssessmentRouter.cs` | Applies the exact `3`, `2`, and `0/1` score routes and safe next-topic behavior. |

### Input and interactions

| File | Responsibility |
|---|---|
| `IVelocityTrainControlInput.cs` | Package-independent selected-train, throttle, action, pointer, and pause input contract. |
| `VelocityInputRouter.cs` | Selects XR or desktop input and exposes one gameplay API. |
| `VelocityXRTrainControlInput.cs` | Reads left ray/trigger, right thumbstick, physical interaction actions, and haptics. Keep in the XR assembly. |
| `VelocityDesktopTrainControlInput.cs` | Implements all keyboard/mouse fallback controls. |
| `VelocityTrainSelectionInteractor.cs` | Selects A/B through ray, touch, or desktop click and publishes selection feedback. |
| `VelocityDirectionLever.cs` | Requests direction changes and snaps back when the train is moving. |
| `VelocityStartSignalLever.cs` | Starts an armed challenge. |
| `VelocityCrossingSignalLever.cs` | Controls Level 2 crossing permission. |
| `VelocityStationGate.cs` | Allows a gate to open only after a valid stopped arrival. |
| `VelocityEmergencyBrakeLever.cs` | Applies emergency braking to both trains in Level 3. |

### Train and rail systems

| File | Responsibility |
|---|---|
| `VelocityTrainId.cs` | Defines Train A and Train B identifiers. |
| `VelocityTrainRuntimeState.cs` | Immutable telemetry snapshot for one train. |
| `VelocityRailPath.cs` | Maps scalar metres to world position/orientation and exposes rail bounds/anchors. |
| `VelocityTrainMotor.cs` | Owns target/current speed, acceleration, braking, signed motion, travel totals, and safe reversal. |
| `VelocityTrainController.cs` | Coordinates motor, rail, selection, visuals, safety, and telemetry for one train. |
| `VelocityTrainRegistry.cs` | Holds explicit A/B references and guarantees independent control. |
| `VelocityTrainTelemetry.cs` | Publishes speed, direction, vectors, separation, displacement, and relative-velocity data. |
| `VelocityTrainVisualFeedback.cs` | Uses `MaterialPropertyBlock`, emission, labels, lights, trails, and patterns without editing source materials. |
| `VelocityTrainSafetySensor.cs` | Detects rail ends, collisions, zones, checkpoints, stations, and unsafe states. |
| `VelocityVectorVisualizer.cs` | Draws direction arrow, magnitude bar, and signed velocity label. |
| `VelocityBrakingDistanceVisualizer.cs` | Shows `v²/(2a)` distance and Level 3 stopping margin. |

### Level systems and data

| File | Responsibility |
|---|---|
| `VelocityTuningProfile.cs` | ScriptableObject for shared speed, acceleration, tolerance, replay, and comfort values. |
| `VelocityLevelDefinition.cs` | ScriptableObject for title, concept, allowed controls, objective type, and variants. |
| `VelocityLevelVariant.cs` | Data for start distances, directions, rails, goals, time limits, signals, zones, and scenery choices. |
| `VelocityLevelManager.cs` | Activates one level root, applies a variant, positions trains, and resets all level-local state. |
| `VelocityAdaptiveVariantManager.cs` | Prefers unseen variants and guarantees no immediate repeat after failure or assessment review. |
| `VelocityLevelObjectiveBase.cs` | Abstract initialize/brief/arm/run/evaluate/complete/fail lifecycle. |
| `VelocityLevel1CatchUpObjective.cs` | Implements parallel-vector catch-up and checkpoint alignment. |
| `VelocityLevel2DirectionDeliveryObjective.cs` | Implements perpendicular routes, safe crossing, station stops, and gates. |
| `VelocityLevel3HeadOnRescueObjective.cs` | Implements approach, stop, reverse, return, and debrief stages. |
| `VelocityCheckpointGoal.cs` | Records checkpoint position/times and alignment. |
| `VelocityStationGoal.cs` | Validates correct arrival, stop tolerance, and gate sequence. |
| `VelocityCrossingOccupancyController.cs` | Enforces exclusive crossing occupancy and signal logic. |
| `VelocityDangerZoneController.cs` | Tracks separation, protected-zone entry, pre-impact freeze, and warnings. |

### Replay, UI, assessment, and cameras

| File | Responsibility |
|---|---|
| `VelocityReplaySample.cs` | Serializable fixed-time sample for both trains and key events. |
| `VelocityReplayRecorder.cs` | Captures bounded attempt data without per-frame allocations. |
| `VelocityGhostReplayController.cs` | Replays samples without modifying live motors. |
| `VelocityHUDController.cs` | Updates A/B panels, speed bars, selection, signed velocity, separation, and status. |
| `VelocityBriefingPanel.cs` | Shows the active mission and observation-based directions. |
| `VelocityObjectivePanel.cs` | Updates live objective progress without exposing a solution. |
| `VelocityFailurePopup.cs` | Displays cause, replay action, and new-variant continuation. |
| `VelocityLevelCompletePopup.cs` | Displays the concept and blocks until next/replay. |
| `VelocityLevelBoard.cs` | Renders locked/current/complete states for all three levels. |
| `VelocityQuizQuestion.cs` | ScriptableObject question, choices, and correct choice index. |
| `VelocityQuizQuestionView.cs` | Presents one question and manages its local selection UI. |
| `VelocityQuizController.cs` | Requires all three answers, scores once, and requests routing. |
| `VelocityCameraFeedController.cs` | Switches overview/follow RenderTexture camera modes. |
| `VelocityTrainFollowCameraRig.cs` | Follows behind the selected train without touching the HMD rig. |
| `VelocityAudioHooks.cs` | Optional serialized clip/source hooks with safe silent behavior. |

### Editor-only import, assembly, and validation tools

| File | Responsibility |
|---|---|
| `VelocityGlbImportProfile.cs` | New import settings for scale, node exclusions, texture limits, and generated output. |
| `VelocityGlbImporter.cs` | Self-contained static GLB 2.0 reader for the supplied files only. |
| `VelocityGlbImportWindow.cs` | Editor command to import/reimport the three source GLBs into generated assets. |
| `VelocityModelNormalizer.cs` | Wraps imported train/track/station content and normalizes renderer bounds without editing sources. |
| `VelocityProceduralEnvironmentBuilder.cs` | Creates low-poly scenery, signs, hands/controllers, arrows, and helper meshes from primitives. |
| `VelocitySceneAssembler.cs` | Idempotently constructs and wires the required hierarchy in `Velocity.unity`. |
| `VelocitySceneValidator.cs` | Reports missing bindings, duplicate IDs, bad rails, missing colliders, external art dependencies, camera mistakes, and build-setting status. |

Do not collapse these responsibilities into one large manager. Avoid `FindObjectOfType`, `GameObject.Find`, tag searches, reflection-based discovery, and scene-name string logic during gameplay. Wire dependencies through `VelocitySceneBindings`, inspector references, constructors for pure logic, or explicit initialization methods.

## 18. GLB import requirements

The new Editor-only importer must parse the supplied GLB header, JSON chunk, and BIN chunk, then support the subset actually used by these files:

- scenes, nodes, meshes, primitives, transforms, buffers, bufferViews, and accessors;
- indexed and non-indexed triangle primitives;
- position, normal, tangent, UV, and vertex-color attributes when present;
- embedded JPEG/PNG images, textures, samplers, and the required PBR metallic-roughness material properties;
- node hierarchy and local transforms;
- correct coordinate/handedness conversion, winding, normals, and bounds; and
- deterministic reimport into new generated folders.

Ignore the nominal empty animation/skin entries if they do not affect the static meshes. Log unsupported required features with the exact asset and node rather than silently producing corrupt geometry.

For the train import, exclude `Plane_5` / `Object_13` from the movable prefab. For `Track.glb`, downsize generated texture copies to a mobile-safe maximum of 1024–2048 px, enable mipmaps, select suitable Android compression, and reuse materials. Preserve the source bytes. Do not duplicate embedded train textures for Train A and Train B.

Generate:

- one normalized base train prefab;
- blue A and red/orange B wrapper prefabs or runtime property-block profiles;
- reusable track segment/crossing prefabs;
- a normalized station wrapper prefab;
- shared generated materials/textures/meshes; and
- an import report listing included/excluded nodes and final bounds.

## 19. Detailed build procedure

Perform these steps in order and do not stop after creating a plan.

### Step 1 — Audit and protect the project

1. Confirm the Unity version, installed package versions, render pipeline, input handler, and target platform.
2. Inventory every supplied source asset under `Assets/Velocity`.
3. Record hashes for existing `.cs` files and supplied source assets so the final report can prove they were not changed.
4. Confirm `Assets/Scenes/Velocity.unity` is the target scene.
5. Create only the new folder tree and assemblies listed above.

### Step 2 — Import and normalize the supplied models

1. Implement the new Velocity-specific GLB importer.
2. Import the train, `Track_1`, and `Track` into `Generated`.
3. Exclude the train’s attached track node.
4. Extract/recreate only materials and texture copies derived from the supplied files.
5. Normalize model scale and forward axis through wrapper prefabs.
6. Import the FBX station through its existing Unity importer, wrap it, and attach its supplied atlas if necessary.
7. Open prefab previews and validate normals, UVs, material assignment, forward direction, and bounds.

### Step 3 — Create the visual language

1. Create shared URP materials for blue, red/orange, green, cyan, yellow, charcoal, stone, foliage, water, white, and warning red.
2. Create low-poly mountains, trees, clouds, rocks, river, bridge/viaduct helpers, tunnel mouths, lamps, fences, signs, and platform from primitives/procedural meshes.
3. Keep geometry simple, batch materials, enable instancing where appropriate, and use no external textures.
4. Build procedural controller/hand visuals and vector arrows.
5. Create a warm directional-light setup, sky color/gradient, baked or lightweight lighting as appropriate for standalone VR.

### Step 4 — Build deterministic rails and trains

1. Create rail-path data in measured metres independently of model scale.
2. Place visual rail meshes along each path and use hidden primitive colliders/triggers as needed.
3. Create the base train wrapper with kinematic Rigidbody, colliders, motor, telemetry, safety, visual feedback, and label anchors.
4. Instantiate Train A and Train B from the same source geometry.
5. Apply A/B color through material property blocks or new wrapper materials without modifying imported source materials.
6. Verify the unselected train keeps moving when selection changes.

### Step 5 — Build the fixed operator station and input

1. Configure XR Origin from installed package components without using sample art.
2. Keep the origin fixed and remove locomotion providers.
3. Add left ray selection, right thumbstick throttle, physical levers, haptic hooks, and world-space UI interaction.
4. Add the procedural tracked hand/controller visuals.
5. Add and test the complete desktop fallback.

### Step 6 — Build shared UI and the reference composition

1. Build the overhead title sign.
2. Build the left control-instruction board.
3. Build the right three-level progress board.
4. Build the dual Train A/Train B console with live speed bars.
5. Build floating velocity cards and vector arrows.
6. Build the objective, pause, failure, completion, and quiz panels.
7. Use large high-contrast TMP text and keep every panel clear of the main railway view.
8. Add the selected-train camera monitor and its overview/follow toggle.

### Step 7 — Build all three level roots

1. Level 1: parallel tracks, staggered starts, checkpoint, catch-up objective.
2. Level 2: perpendicular east/north rails, protected crossing, separate stations, gates.
3. Level 3: shared/collinear bridge route, approaching trains, danger zone, emergency brake, reverse-and-return goals.
4. Create at least three ScriptableObject variants for each level.
5. Keep only the active root enabled.

### Step 8 — Implement progression, replay, and adaptive retry

1. Implement the authoritative state machine.
2. Implement success/failure evaluation outside the UI.
3. Implement attempt recording and ghost replay.
4. Implement no-immediate-repeat variant selection.
5. Verify every failure transitions to a visibly different same-concept setup.
6. Verify every success shows a blocking concept popup.

### Step 9 — Implement the final assessment

1. Create the exact three question data assets.
2. Require all three answers before submission.
3. Route 3, 2, and 0/1 correct exactly as specified.
4. Ensure review routes use unseen variants and reset only the required level range.
5. Handle an unconfigured next topic safely.

### Step 10 — Optimize and validate

1. Target at least 72 FPS on the intended standalone headset.
2. Share materials/meshes, enable GPU instancing where useful, and avoid per-frame allocations/LINQ.
3. Keep the large `Track.glb` texture copies mobile-safe.
4. Run the scene validator and all applicable tests.
5. Test with XR hardware if available and always test the desktop fallback.
6. Save the final scene at the exact requested path and add it to Build Settings.
7. Recheck hashes to prove no existing `.cs` or supplied source asset changed.

## 20. Coding and performance rules

- Subscribe/unsubscribe in `OnEnable`/`OnDisable` or pair initialization/disposal explicitly.
- Avoid allocations, string building, LINQ, and component searches in `Update`/`FixedUpdate`.
- Use `MaterialPropertyBlock` instead of cloning materials per frame.
- Cache shader property IDs and component references.
- Use object pooling for reusable ghost/replay visuals and warnings.
- Keep pure velocity, braking, routing, and variant-selection logic testable without a scene.
- Use events to separate objective logic from UI.
- Validate serialized references once and log each configuration error once with a useful object path.
- Use unscaled time only for UI/replay behavior that must continue while simulation is paused.
- Keep learner-facing text about the physics task; never expose implementation details.
- Use text, arrows, patterns, and icons in addition to color.
- Keep world-space text legible at the fixed operator distance and avoid placing it behind moving trains.

## 21. Required tests

Create meaningful tests in the new test assemblies.

### EditMode

- Rail distance maps to the correct world position and direction.
- Speed target increases, decreases, holds on neutral input, and clamps correctly.
- Smooth acceleration/braking reaches expected boundary values.
- Selecting Train B does not change Train A’s motor state.
- Reversal fails while moving and succeeds at rest.
- Same-direction closing speed is the magnitude difference when catching up.
- Perpendicular equal-speed trains have different velocity vectors.
- Opposite-direction closing speed is the sum of approaching magnitudes.
- Braking distance uses `v²/(2a)` and handles zero acceleration safely.
- Adaptive selection never returns the immediately previous variant.
- Assessment scores `0`, `1`, `2`, and `3` route correctly.
- Illegal game-state transitions are rejected.
- Each level objective passes/fails at its tolerance boundaries.

### PlayMode

- `Velocity.unity` loads with no missing scripts or references.
- Exactly one Train A and one Train B registry entry exist.
- Both trains continue independently through selection changes.
- Floating arrows and signed labels match actual movement.
- Level 1 records alignment/checkpoint timing correctly.
- Level 2 prevents simultaneous crossing occupancy and validates both station stops.
- Level 3 detects danger, brakes, unlocks reversal only at rest, and validates the return trip.
- A failed challenge loads a different variant of the same level.
- Success/failure popups lock gameplay input.
- Completing Level 3 opens the three-question assessment.
- Quiz routing resets only the required levels and clears answers.
- Desktop fallback can complete the full critical flow.
- The follow camera updates the monitor without moving the XR Origin or HMD camera.

Do not add shallow tests that only duplicate constant values or mirror implementation line by line.

## 22. Acceptance checklist

The work is complete only when all of these are true:

- `Assets/Scenes/Velocity.unity` opens without console errors, missing scripts, or missing references.
- No existing `.cs` file was modified.
- Every implementation class is new, separate, and namespaced under `TeachingAndLearning.VelocityVR`.
- No source model or texture under `Assets/Velocity` was changed.
- No external art, sample art, downloaded asset, or asset from another topic appears in the game.
- The uploaded train, track, and station assets are visibly used.
- Procedural low-poly scenery recreates the reference mood without violating the asset rule.
- The learner sees first-person hands/controllers from a fixed comfortable viewpoint.
- Train A is blue; Train B is red/orange; labels and patterns also distinguish them.
- Both trains move independently and the unselected train never pauses.
- Right-thumbstick input changes only the selected train’s speed magnitude.
- Reversal is impossible above the stop threshold.
- Velocity arrows, signed values, facing direction, and movement always agree.
- Level 1 teaches same direction/different magnitude through catch-up.
- Level 2 clearly uses perpendicular world directions and a safe crossing.
- Level 3 clearly uses opposing collinear velocities, live closing distance, braking, reversal, and return.
- Failure always advances to a different same-concept variant.
- Each successful level displays a blocking concept popup.
- The monitor shows a behind-train/overview camera without affecting the headset view.
- The final assessment contains exactly three questions and follows the required score routing.
- Scene validation and all applicable meaningful tests pass.
- The intended headset build sustains a target of at least 72 FPS with no recurring train-motion garbage allocations.

## 23. Final implementation report

When finished, return a concise but complete report containing:

1. every new file created, grouped by folder;
2. confirmation that no existing `.cs` or source asset hash changed;
3. the final scene hierarchy;
4. all generated prefabs, materials, textures, render textures, ScriptableObjects, and input assets;
5. the exact Inspector wiring for `VelocitySceneBindings` and any serialized UnityEvents;
6. the GLB import report, including the excluded train track node and optimized texture sizes;
7. the variant table for all nine or more variants;
8. test and validator results;
9. desktop and XR control mappings;
10. Build Settings and next-topic configuration; and
11. any limitation caused by a genuinely missing uploaded asset, including the procedural helper used in its place.

Do not claim completion if the scene has not been assembled, wired, saved, and tested.
