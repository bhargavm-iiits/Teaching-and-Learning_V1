# MASTER IMPLEMENTATION PROMPT — BALL TARGET CHALLENGE: UNIFORM MOTION VR

Use this entire document as one implementation prompt. Build the complete Unity game described below. Do not stop after producing another plan, pseudocode, disconnected scripts, or a visual mock-up.

## Reference image and source notes

![Uploaded Ball Target Challenge reference](References/UniformMotion_BallTarget_Reference.png)

Stable local reference:

`D:\Unity Projects\Teaching_And_Learning\Docs\References\UniformMotion_BallTarget_Reference.png`

Original uploaded image:

`C:\Users\Bhargav\AppData\Local\Temp\codex-clipboard-fd895329-7c33-47a8-9044-4779bd2ed1a3.png`

Reference SHA-256:

`7618E1E6F2EEDA4A5CFFB70B4BE49C1E0FD703B34E400D00F1B4AA3671F4A25F`

Advisory pasted design note:

`C:\Users\Bhargav\.codex\attachments\3673d017-17aa-4f8d-bab1-0a2cd5ea0f46\Pasted text.txt`

The PNG is a three-level concept montage, not one literal runtime screen. Build one reusable first-person VR arena and activate one level at a time. Use the montage for art direction, first-person framing, board layout, color, target presentation, and interaction mood while applying the scientific corrections in this prompt.

---

## 1. Role and required finished result

Act as a senior Unity 6, OpenXR, XR Interaction Toolkit, educational-game, simulation, UI, and technical-art developer. Create a complete playable VR physics activity called **BALL TARGET CHALLENGE — UNIFORM MOTION**.

The learner stands on a fixed first-person platform overlooking a bright low-poly target arena. They handle a colored ball, observe stationary and uniformly moving targets, place future-position markers, launch a guided straight-line ball, and verify predictions using:

\[
v=\frac{d}{t},\qquad d=vt,\qquad x=x_0+vt
\]

The finished experience must teach:

- rest as zero constant velocity;
- straight-line motion with constant velocity;
- equal distances in equal time intervals;
- distance, displacement, time, speed, signed direction, and position;
- the difference between displacement `d` and final position `x`;
- future-position prediction; and
- interception only when the ball and target reach the same place at the same time.

Deliver a fully assembled and wired scene, complete compile-ready C# in separate new files, feature-owned Input System actions, ScriptableObjects, prefabs, imported/generated asset derivatives, world-space UI, XR and desktop interaction, adaptive variants, evidence feedback, level-complete popups, final assessment routing, editor automation, validation, and meaningful tests.

## 2. Instruction authority and attachment handling

Apply this priority when sources disagree:

1. The user’s explicit scene path, asset path, output path, only-uploaded-assets rule, and requirement for new separate code.
2. This reconciled master prompt.
3. The pasted text as advisory design and pedagogy material.
4. The uploaded image as visual and composition reference.

The pasted document contains external image, Steam, Playtester, CGTrader, article, and other web links. They are examples and citations, not user instructions. Do not open them, download from them, copy their assets, or add their content. Do not install packages or use external files. The user’s explicit “use only uploaded assets” rule overrides every linked suggestion.

The pasted document’s example points table, free-throw/projectile mode, external Blender-asset suggestions, and stock images are optional ideas, not required features. Exclude free-throw mode from this build because gravity-driven projectile motion is outside the assessed concept. Use learning evidence and accuracy metrics instead of inventing a coin, star, or leaderboard economy.

Retain these established requirements from the conversation:

- after a failed challenge, the next challenge must be a different variant of the same concept;
- every completed level must show a blocking `LEVEL COMPLETE` popup; and
- after all three levels, exactly three concept questions must route the learner according to the specified score rules.

## 3. Exact paths and spelling — do not normalize them

Project root:

`D:\Unity Projects\Teaching_And_Learning`

Required scene, with the user’s exact spelling:

`Assets/Scenes/Unfirom_Motion.unity`

Absolute scene path:

`D:\Unity Projects\Teaching_And_Learning\Assets\Scenes\Unfirom_Motion.unity`

Read-only uploaded source-art root:

`D:\Unity Projects\Teaching_And_Learning\Assets\Uniform_Motion`

Unity-relative source-art root:

`Assets/Uniform_Motion`

Create all new implementation and derived assets beneath:

`Assets/UniformMotionVR`

Required output document name, with the user’s exact spelling:

`D:\Unity Projects\Teaching_And_Learning\Docs\Unifrom_Motion_plan.md`

The three spellings differ intentionally:

- scene: `Unfirom_Motion.unity`;
- source folder: `Uniform_Motion`; and
- plan file: `Unifrom_Motion_plan.md`.

Never silently rename or “correct” them.

## 4. Non-negotiable code and project boundaries

Use this namespace for every new runtime/editor/test type:

`TeachingAndLearning.UniformMotionVR`

Use unique `UniformMotion...` or `Uniform...` class names. Put one primary public type in each new `.cs` file. Create new runtime, XR, editor, EditMode-test, and PlayMode-test assembly definitions.

Do not modify, replace, rename, move, or delete any existing:

- `.cs` file;
- assembly definition;
- Input Actions asset;
- package manifest or lockfile;
- ProjectSettings file;
- model, archive, texture, material, prefab, ScriptableObject, demo scene, or source asset under `Assets/Uniform_Motion`; or
- any scene other than the exact requested scene.

Do not depend on existing topic code such as generic `GameFlowManager`, `LevelManager`, `QuizQuestion`, `MotionMath`, `MotionTracker`, existing GLB importers, scene builders, or scripts under other lesson folders. Inspecting conventions is allowed; runtime reuse is not.

The only existing project asset intentionally changed by the implementation is:

`Assets/Scenes/Unfirom_Motion.unity`

At planning time it is an 8,179-byte default scene containing only `Main Camera` and `Directional Light`. It is not in Build Settings. All other implementation files must be newly created under `Assets/UniformMotionVR`. Do not automatically edit Build Settings; report the required manual scene entry at delivery.

Treat `Assets/Uniform_Motion` as a read-only source whitelist. Extract or copy derived content into `Assets/UniformMotionVR/Generated`; never unpack over the source folder. Do not hardcode absolute Windows paths in runtime code.

Allowed infrastructure includes Unity primitives, newly generated procedural meshes, TextMeshPro, UGUI, LineRenderer, TrailRenderer, new URP materials, and already-installed package APIs/components. These do not introduce outside source art. Do not use art from any other project folder or XR sample visuals.

## 5. Verified project and uploaded-asset inventory

Project versions:

- Unity `6000.5.5f1`;
- URP `17.5.0`, configured in Graphics/Quality settings;
- linear color space;
- Input System `1.19.0`, new Input System active;
- XR Interaction Toolkit `3.5.1`;
- OpenXR `1.17.1`;
- XR Management `4.5.4`;
- XR Hands `1.8.1`;
- UGUI `2.5.0`;
- Localization `1.5.12`; and
- Test Framework `1.7.0`.

The uploaded source root contains 117 non-meta files. Use only the following supplied asset groups.

### 5.1 Juggling-ball GLB

`Assets/Uniform_Motion/Juggling ball by Poly by Google - cYbPVv0xBbK.glb`

Verified properties:

- valid static glTF 2.0/GLB;
- 11,644 bytes;
- one node and one mesh;
- four primitives/materials;
- approximately 82 position vertices per primitive;
- flat blue, yellow/orange, green, and red base-color materials;
- no image textures and no animations; and
- currently assigned Unity `DefaultImporter`, so it cannot be instantiated directly.

Write a small new Editor-only importer specifically for this file. Do not install glTFast/UnityGLTF and do not reuse an importer from another lesson.

### 5.2 Archery-target archive

`Assets/Uniform_Motion/archery-target-low-poly/source/archery target.rar`

Verified archive contents:

- `Archery Target.fbx`;
- `Archery Target.obj`;
- `Archery Target.mtl`;
- BaseColor, Height, Metallic, Normal, and Roughness PNG maps.

Loose BaseColor, Metallic, Normal, and Roughness PNG files also exist under the target textures folder. Unity does not import RAR directly. Extract the archive non-destructively into a new generated/imported folder and use the FBX as the primary target source. Preserve the RAR unchanged. Ignore the orphan `archery-target-low-poly.zip.meta`; the ZIP itself is absent, and that metadata must not be deleted or repurposed.

The source target is one object named approximately `Target_Cylinder`, with about 11,104 vertices and 8,459 faces. Wrap and normalize it. If its art does not produce the bold red-and-white concentric face seen in the reference, add removable primitive ring overlays and colliders in the generated prefab; do not edit the source mesh or textures.

### 5.3 LowPoly Environment Pack

The supplied pack contains 31 FBX files and 31 matching prefabs:

- `Bush_1`–`Bush_3`;
- `Environment_1`;
- `Grass_1`–`Grass_2`;
- `Log_1`–`Log_3`;
- `Mounting_1`–`Mounting_3` using the source asset’s spelling;
- `Plant_1`–`Plant_7`;
- `Rock_1`–`Rock_6`;
- `Stone_1`;
- `Terrain_1`–`Terrain_2`; and
- `Tree_1`–`Tree_3`.

It also contains legacy materials, a legacy skybox, three demo scenes, Timeline playables, and old PPS assets.

Use the supplied FBX/prefab geometry for terrain, canyon/mountain shapes, trees, bushes, rocks, logs, grass, and plants. Do not use or copy the demo scenes, Timeline assets, or PPS assets. The 39 environment materials use the legacy Standard shader, and the skybox uses a legacy built-in shader. Create new URP/Simple Lit or URP/Lit material copies under `Assets/UniformMotionVR/Generated/Materials` and remap only new wrapper prefabs/scene instances. Never alter the original materials.

### 5.4 Missing asset categories

No audio, VR hand/controller model, wooden platform/bridge, measurement rail, UI art, or dedicated target-motion mechanism is supplied. Create those from Unity primitives/procedural meshes and new solid-color URP materials. Do not download or borrow replacements. Implement audio hooks with a silent fallback; do not synthesize or reuse clips.

## 6. Visual target and reference corrections

Recreate the reference’s friendly saturated low-poly language:

- fixed first-person VR view with white glove/controller proxies at the lower edge;
- warm orange/tan low-poly foreground platform and canyon forms;
- blue water and sky with cool distant mountain silhouettes;
- supplied trees, bushes, rocks, logs, grass, and plants arranged as reusable environment dressing;
- red-and-white bullseyes on wooden-looking stands or straight measurement rails;
- faceted red Level 1 ball, blue Level 2 ball, and yellow Level 3 ball derived from the supplied ball mesh;
- navy world-space panels with thick brown frames;
- bold white headings with yellow highlighted words;
- Level 1 cyan-blue, Level 2 orange, and Level 3 green identity accents;
- cyan arrows for observed velocity and yellow arrows/markers for prediction;
- readable metre ticks, time markers, counters, and speed displays; and
- soft URP lighting, simple shadows, shared materials, and low overdraw suitable for standalone VR.

Persistent runtime composition:

- upper left/centre: `2. BALL TARGET CHALLENGE`;
- subtitle: `Observe constant motion and predict where the target will be.`;
- upper centre/right: `HOW TO PLAY`;
- upper right: `PHYSICS`;
- left: active level and objective;
- right: target count, speed, timer, or future-position information;
- centre: clear target range; and
- near the learner: ball rack/guided launcher, marker controls, and evidence panel.

Correct the image in the game:

- every assessed ball path is straight and level, never a curved dotted arc;
- the Physics board says `Constant velocity`, `Equal distance in equal time`, `d = vt`, and `x = x0 + vt`;
- the Physics board uses a straight position/velocity diagram, not a parabola;
- Level 3 time labels read `0 s`, `1 s`, `2 s`, `3 s`;
- solid target = current/actual target;
- translucent colored target = learner’s selected prediction;
- differently styled reveal target = mathematically correct position, shown only after submission; and
- only one level is active, rather than showing the three-band montage during play.

## 7. Scientific model — mandatory rules

Use one Unity unit as one metre and seconds as the simulation unit. Every target, ball, ruler, HUD value, marker, graph sample, replay sample, and evaluator must read the same analytical model and shared clock.

Uniform rectilinear motion:

\[
\mathbf p(t)=\mathbf p_0+\mathbf v t
\]

One-dimensional measured-lane form:

\[
d=vt,\qquad x=x_0+vt
\]

Travel time:

\[
t=\frac{d}{v}
\]

Timed intercept:

\[
\mathbf p_{target}(T)=\mathbf p_{target,0}+\mathbf v_{target}T
\]

\[
\mathbf v_{ball}=\frac{\mathbf p_{target}(T)-\mathbf p_{ball,0}}{T}
\]

Scientific constraints:

- constant velocity means constant speed and unchanged direction;
- a stationary target has constant zero velocity but does not demonstrate nonzero motion;
- target and ball centres remain at a common height during assessed movement;
- no assessed Rigidbody projectile, gravity, drag, bounce, force, impulse, easing, acceleration, deceleration, NavMesh, root motion, tween, animation-curve translation, or WheelCollider is allowed;
- optional visual ball rotation cannot affect translation;
- the ball and target are kinematic/analytical during assessed movement;
- calculate position from recorded origin, constant velocity, and authoritative elapsed time; never accumulate `transform.position += velocity * deltaTime` as the educational source of truth;
- use a sphere/capsule sweep between previous and next analytic positions to prevent missed high-speed collision detection;
- a target does not ping-pong, wrap, or reverse during a measured run;
- a target resets only while hidden between attempts;
- equal-time markers are separated by exactly `|v| × Δt`;
- distinguish displacement from final position whenever `x0` is nonzero;
- reject `T <= 0`, zero-denominator travel-time requests, off-rail future positions, and unachievable ball speeds before a variant can run; and
- displayed values must come from the same data used for evaluation.

Do not include the pasted document’s gravity-based free-throw mode in this build. Explain once that the guided training ball is idealized to isolate uniform motion.

## 8. Player learning loop, VR comfort, and controls

Core loop:

`Observe -> calculate/predict -> place marker -> guided launch -> compare evidence -> explain -> retry or progress`

Keep the XR Origin fixed at a comfortable standing/seated overlook. Allow natural tracked head and hand movement but no artificial locomotion, teleportation, snap turn, moving platform, follow camera, or camera shake.

Required VR interactions:

- grip: pick up the supplied ball and place it into the guided launcher socket;
- direct interactor: ball and launcher handling;
- ray interactor: ruler positions, answer choices, buttons, and panels;
- trigger: select ruler/answer positions and operate UI;
- primary/confirm: submit a prediction and arm the run;
- secondary/reset: request reset before a trial is running;
- menu: pause; and
- optional short haptics for valid socketing, marker confirmation, hit, warning, and completion.

The measured outcome must not depend on physical throwing skill. The player physically handles the ball, but the guided launcher controls the exact measured velocity. Disable XRI throw-on-detach and discard any release velocity before starting the analytic ball motor.

Create simple white hand/controller proxies from primitives because none are supplied. Do not use XRI sample art.

Complete desktop fallback:

- mouse: ray aim and UI selection;
- left click: select/place marker or press world UI;
- `E`: pick up/socket the ball;
- `Enter`: submit or launch;
- `R`: reset before/after an attempt where allowed;
- `P` or Escape: pause/resume; and
- number keys: choose answer/marker options when applicable.

XR and desktop input must feed one package-independent semantic interface. XR loader failure must not prevent desktop play.

## 9. State machine and progression

Use one authoritative flow controller with these states:

```text
Boot
Introduction
LevelBriefing
Demonstration
Prediction
ReadyToLaunch
AttemptRunning
AttemptEvaluating
Feedback
VariantTransition
LevelComplete
SuccessReplay
FinalQuiz
QuizResult
TopicComplete
Paused
```

Required transitions:

```text
Boot -> Introduction -> LevelBriefing
LevelBriefing -> Demonstration or Prediction
Demonstration -> Prediction or ReadyToLaunch
Prediction -> ReadyToLaunch
ReadyToLaunch -> AttemptRunning
AttemptRunning -> AttemptEvaluating
AttemptEvaluating -> ReadyToLaunch when another successful trial is required
AttemptEvaluating -> Feedback on failure
Feedback -> VariantTransition -> LevelBriefing for a different same-level variant
AttemptEvaluating -> LevelComplete when the level objective is complete
LevelComplete -> SuccessReplay -> LevelComplete
LevelComplete -> next LevelBriefing
Level 3 Complete -> FinalQuiz -> QuizResult
QuizResult -> TopicComplete, Level 2 remediation, or Level 1 remediation
```

Pause the feature-owned simulation clock and gameplay input during modal UI while keeping UI input live. Do not set global `Time.timeScale`. Only the flow controller changes state; views request transitions through explicit events.

## 10. Adaptive retry system

A failed challenge must never repeat the identical setup.

Create at least four curated, validated variants per level. Use a shuffled bag/deck:

- prefer unseen variants;
- exclude the immediately previous variant;
- after exhausting the set, reshuffle while excluding the last ID;
- stay on the same level and concept;
- keep approximate difficulty stable; and
- change at least two physics parameters plus one visible layout/property after failure.

Possible changes:

- Level 1: target distance, ball speed, lane, target order, platform placement;
- Level 2: `x0`, signed target velocity, observation/prediction interval, rail orientation, ball speed, prop arrangement; and
- Level 3: `x0`, target velocity, horizon `T`, intercept coordinate, ball start, feasible speed, ruler direction, scenery arrangement.

On failure:

1. finish/freeze the evidence safely;
2. record a reason such as wrong time, wrong marker, missed hit, temporal miss, or timeout;
3. show the learner’s value, actual value, error, and relevant known quantities;
4. offer `WATCH ATTEMPT` and `CONTINUE TO NEW CHALLENGE`;
5. never reveal the next variant’s answer;
6. select a different validated variant;
7. reset ball, target, graph, clock, markers, counters, feedback, and launcher; and
8. show the new briefing.

Progressive hints may highlight equal intervals, then show the formula with known values, then show a partial substitution. The learner must still choose the final answer/marker.

## 11. Level 1 — Stationary Target Calibration

Title:

`LEVEL 1 — STATIONARY TARGETS`

Visual ball:

`Red`

Reference objective:

`Hit 3 stationary targets with a straight constant-speed guided ball.`

Scientific purpose:

The targets are at rest. The ball demonstrates uniform motion and teaches `t=d/v`.

Build three supplied bullseye target instances at known measured distances and the same height as the guided ball lane. The target counter reads `TARGETS: 0/3` through `3/3`. Activate one target at a time while the others remain visible as in the reference.

For each trial display:

- target distance;
- prescribed ball speed;
- selectable predicted travel time;
- timer; and
- one-second straight-path breadcrumbs.

Reference trial:

```text
Distance = 6 m
Ball speed = 3 m/s
Predicted travel time = 2 s
```

Player flow:

1. Read distance and speed.
2. Select the expected arrival time.
3. Grip and socket the red ball.
4. Aim the fixed straight guide at the active target.
5. Launch.
6. Compare predicted and observed arrival time.
7. Repeat for all three targets.

Success requires both the valid time prediction and target hit; a lucky hit with a wrong answer does not count. Use generous aim snap/tolerance so the level tests the relationship, not throwing precision.

Level-complete explanation:

```text
LEVEL 1 COMPLETE
The guided ball moved in a straight line at constant speed.
Travel time = distance / speed.
```

## 12. Level 2 — Equal Distances in Equal Times

Title:

`LEVEL 2 — CONSTANT-SPEED TARGETS`

Visual ball:

`Blue`

Reference objective:

`Observe, predict, and hit targets moving at constant velocity.`

Use one supplied bullseye target at a time on a straight horizontal measured rail. The target starts each measured run already at its constant signed velocity. Show a numeric velocity and direction arrow, metre ruler, timer, and one-second ghost footprints separated by exactly `|v|` metres.

Reference trial:

```text
x0 = 4 m
v = +2 m/s
t = 3 s
d = 6 m
x = 10 m
```

Use three explicit phases without violating uniform motion:

1. **Demonstrate:** run the target uniformly through the measured interval so the learner sees equal spacing.
2. **Predict:** after the demonstration ends, hide/reset the target to its start. Pause the feature clock and let the learner place a translucent ghost marker for the next run.
3. **Test:** restart the target from `x0` and the guided blue ball from their recorded starts on one shared clock. The target keeps its constant velocity. The ball is guided to the selected point so the learner can test the prediction against motion.

Require both an accurate prediction and a valid moving-target hit. Complete three trials. A reflex hit without a correct marker does not pass the trial.

The target must exit/hide after a miss or completion. It never pauses inside a measured run, bounces, or reverses.

Level-complete explanation:

```text
LEVEL 2 COMPLETE
The target covered equal distances in equal times.
Its speed and direction stayed constant, so its velocity was constant.
```

## 13. Level 3 — Future-Position Interception

Title:

`LEVEL 3 — FUTURE POSITION`

Visual ball:

`Yellow`

Reference objective:

`Calculate the future position and make the ball and target arrive together.`

Before a trial show:

- current/starting coordinate `x0`;
- signed target velocity and arrow;
- time horizon `T`;
- numbered ruler positions; and
- an empty prediction marker.

Reference trial:

```text
Target x0 = 2 m
Target velocity = +2 m/s
T = 3 s
Target displacement = 2 x 3 = 6 m
Future position = 2 + 6 = 8 m
Ball start = 0 m
Required guided-ball speed = 8 / 3 ≈ 2.67 m/s
```

Player flow:

1. Observe the known quantities.
2. Calculate `d=vt` and `x=x0+d`.
3. Point at the ruler and place a translucent prediction target.
4. Confirm without seeing the correct ghost.
5. Socket the yellow ball and arm the guided launch.
6. Start target and ball from the same clock event.
7. Both move on straight, level, constant-velocity paths.
8. Compare spatial and temporal interception evidence.

For a selected intercept position and horizon, compute the guided ball velocity once:

\[
\mathbf v_{ball}=\frac{\mathbf p_{selected}-\mathbf p_{ball,0}}{T}
\]

Then keep it constant. If the learner selects the correct point, ball and target meet at `T`. If the marker is wrong, the ball reaches the learner’s point at `T` while the target reaches the true point, making the error visible.

Use serialized tolerances, suggested near `0.25 m` spatial prediction error and `0.15 s` temporal error. Complete three validated prediction/interception trials.

Reveal solid/translucent samples at `0 s`, `1 s`, `2 s`, and `3 s` only after commitment. Never repeat the image’s erroneous second `1 s` label.

Level-complete explanation:

```text
LEVEL 3 COMPLETE
Displacement is d = vt.
Future position is x = x0 + vt.
The ball and target met because they reached the same position at the same time.
```

## 14. Evidence, graph, performance metrics, and accessibility

Use the same analytical data to drive:

- target and ball position;
- timer and ruler;
- velocity arrows;
- time breadcrumbs;
- hit/intercept evaluation;
- replay;
- feedback values; and
- a compact position-time graph.

The graph is a secondary evidence panel, not the control mechanic. Use `Time (s)` on the horizontal axis and `Position (m)` on the vertical axis. Uniform motion produces a straight line whose slope equals signed velocity. A stationary target produces a horizontal line. Reveal/expand the graph during demonstration and feedback so it does not obstruct aiming.

Show educational metrics rather than the pasted note’s arbitrary point table:

- prediction accuracy;
- arrival-time error;
- hit/interception success;
- attempts;
- hints used; and
- evidence/replay availability.

Success feedback: green pulse, straight-path highlight, concise explanation, optional haptic pulse. Failure feedback: learner value, correct substituted equation after resolution, displacement, final position, absolute error, and the actual/predicted markers. Do not reveal a correct value before the first commitment.

Accessibility:

- seated/standing height calibration;
- generous target and marker tolerance option;
- large high-contrast text inside the forward comfort cone;
- labels/icons/patterns as well as color;
- optional haptics and visual equivalents;
- no forced head motion; and
- desktop completion path.

## 15. Level-complete popups and final assessment

Every successful level shows a blocking popup:

```text
LEVEL COMPLETE
[short concept explanation]

NEXT LEVEL
REPLAY SUCCESS
```

After Level 3, ask exactly these three single-answer questions. Require all answers before enabling `SUBMIT ANSWERS`. Randomize answer order while preserving correctness. Do not reveal correctness before submission.

### Question 1

```text
Which describes uniform straight-line motion?

A. Equal distances in equal times with constant direction
B. A curved path at constant speed
C. Speed increasing every second

Correct: A
```

### Question 2

```text
A target moves at 2 m/s for 3 s. What is its displacement?

A. 5 m
B. 6 m
C. 8 m

Correct: B
```

### Question 3

```text
A target starts at 2 m and moves at +2 m/s for 3 s. What is its final position?

A. 6 m
B. 8 m
C. 10 m

Correct: B
```

Route scores exactly:

- `3 correct`: show `TOPIC COMPLETE`, invoke a serialized `NextTopicRequested` UnityEvent, and optionally load a configured `nextTopicSceneName`.
- `2 correct`: preserve Level 1 completion, reset Levels 2 and 3, and begin an unseen Level 2 variant.
- `0 or 1 correct`: reset level completion and begin an unseen Level 1 variant.

Clear previous answers on remediation. Preserve accessibility preferences and variant history. If the next scene name is blank or absent from Build Settings, keep the safe Topic Complete panel open, invoke the event, and log one warning instead of throwing.

## 16. Required scene hierarchy

Create this hierarchy in the exact requested scene:

```text
UniformMotionVR_Root
├── Systems
│   ├── UniformMotionBootstrap
│   ├── UniformMotionSceneBindings
│   ├── UniformSimulationClock
│   ├── UniformMotionGameFlow
│   ├── UniformMotionLevelDirector
│   ├── UniformVariantDeck
│   ├── UniformInputRouter
│   ├── UniformReplaySystem
│   ├── UniformAssessment
│   ├── XRInteractionManager
│   └── EventSystem
├── Player
│   ├── XROrigin
│   │   ├── CameraOffset
│   │   │   └── Main Camera
│   │   ├── LeftController
│   │   └── RightController
│   └── DesktopFallback
├── Arena
│   ├── PlayDeck
│   ├── Terrain
│   ├── CanyonBackdrop
│   ├── Water
│   ├── Mountains
│   ├── TreesBushesPlants
│   ├── RocksLogsGrass
│   ├── WoodenPlatforms
│   └── Lighting
├── MeasurementLane
│   ├── StraightRail
│   ├── MetreRuler
│   ├── TimeBreadcrumbs
│   ├── TargetRoot
│   ├── LearnerPredictionMarker
│   ├── CorrectPositionReveal
│   ├── GuidedLauncher
│   ├── BallPool
│   └── MissBoundary
├── LevelRoots
│   ├── Level01_StationaryTargets
│   ├── Level02_ConstantVelocity
│   └── Level03_FutureInterception
└── WorldUI
    ├── HeaderBoard
    ├── HowToPlayBoard
    ├── PhysicsBoard
    ├── ObjectiveBoard
    ├── TelemetryBoard
    ├── PositionTimeGraph
    ├── EvidencePanel
    ├── FailurePopup
    ├── LevelCompletePopup
    ├── QuizPanel
    ├── TopicCompletePanel
    └── PausePanel
```

Keep one active camera, AudioListener, EventSystem, and level root. Reuse/reparent the existing Main Camera into the XR Origin and configure the existing Directional Light. The fixed view should resemble the reference’s target-range overlook.

## 17. New folder structure

Create:

```text
Assets/UniformMotionVR/
├── Assemblies/
├── Scripts/
│   ├── Runtime/
│   │   ├── Core/
│   │   ├── Data/
│   │   ├── Gameplay/
│   │   ├── Levels/
│   │   ├── Input/
│   │   ├── XR/
│   │   ├── UI/
│   │   ├── Visualization/
│   │   ├── Replay/
│   │   └── Audio/
│   └── Editor/
├── Input/
├── Data/
│   ├── Levels/
│   ├── Variants/
│   ├── Quiz/
│   └── Settings/
├── Generated/
│   ├── Imported/
│   │   ├── Ball/
│   │   └── ArcheryTarget/
│   ├── Materials/
│   ├── Meshes/
│   ├── Prefabs/
│   └── ImportReports/
├── Prefabs/
│   ├── Gameplay/
│   ├── Environment/
│   ├── UI/
│   └── XR/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

Create feature-owned Runtime, XR, Editor, EditMode-test, and PlayMode-test asmdefs. XR code must be guarded by package/version defines so core and desktop code remain testable without an active headset.

## 18. Required separate new C# files

Create complete implementations for at least the following files. Keep one primary public type per file.

### Core and data

| File | Responsibility |
|---|---|
| `UniformMotionMath.cs` | Pure displacement, future-position, travel-time, required-velocity, spatial-error, and temporal-error functions with validation. |
| `IUniformSimulationClock.cs` | Package-independent authoritative-clock contract. |
| `UniformSimulationClock.cs` | Pausable feature-owned clock with fixed logical simulation steps. |
| `UniformManualClock.cs` | Deterministic test clock. |
| `UniformMotionGameState.cs` | All legal game states. |
| `UniformMotionGameEvents.cs` | Strongly typed event payloads. |
| `UniformMotionSceneBindings.cs` | Explicit serialized scene references; no runtime searches. |
| `UniformMotionBootstrap.cs` | Validates configuration and starts a new session. |
| `UniformMotionGameFlowController.cs` | Sole authority for legal state transitions and progression. |
| `UniformMotionSessionProgress.cs` | Level completion, attempts, hints, variant history, quiz answers, and preferences. |
| `UniformMotionGameSettings.cs` | ScriptableObject with tolerances, limits, clock settings, accessibility defaults, and next-topic configuration. |
| `UniformMotionLevelId.cs` | Level identifiers. |
| `UniformMotionLevelDefinition.cs` | ScriptableObject with level copy, theme, successes required, and variant list. |
| `UniformMotionChallengeVariant.cs` | Curated problem data and stable variant ID. |
| `UniformMotionChallengeValidator.cs` | Rejects invalid time, bounds, speed, and unreachable-intercept data. |
| `UniformMotionAssessmentRouter.cs` | Exact 3, 2, and 0/1 routes. |
| `UniformMotionNextTopicRouter.cs` | Safe event/optional scene load behavior. |

### Analytical gameplay

| File | Responsibility |
|---|---|
| `UniformMotionSegment.cs` | Immutable origin, constant velocity, start time, duration, and analytic evaluation. |
| `UniformMotionBody.cs` | Kinematic body driven only from a segment and shared clock. |
| `UniformTargetMover.cs` | One-way target motion and hidden between-trial reset. |
| `UniformGuidedBall.cs` | Straight constant-velocity ball flight and lifecycle. |
| `UniformGuidedLauncher.cs` | Sockets ball, discards throw velocity, validates/starts guided motion. |
| `UniformSweptHitDetector.cs` | Sphere/capsule cast across each analytic step. |
| `UniformTargetHitSensor.cs` | Target collider identity and hit forwarding. |
| `UniformMotionAttemptResult.cs` | Immutable prediction/hit/time/error evidence. |
| `UniformMotionAttemptEvaluator.cs` | Separately scores prediction and physical interception. |
| `UniformPredictionMarker.cs` | Learner-selected coordinate and translucent view. |
| `UniformRulerInteractor.cs` | Ray-to-coordinate conversion, snapping, bounds, and confirmation. |
| `UniformBallPool.cs` | Allocation-free red/blue/yellow ball instances. |
| `UniformTargetPool.cs` | Allocation-free target/ghost instances. |
| `UniformTrialCoordinator.cs` | Starts/reset trials from one clock event and forwards one result. |

### Levels and variants

| File | Responsibility |
|---|---|
| `IUniformMotionLevelMode.cs` | Prepare/Demonstrate/Predict/Launch/Evaluate/Reset contract. |
| `UniformMotionLevelDirector.cs` | Activates one level root and applies selected data. |
| `UniformMotionLevelControllerBase.cs` | Common trial/counter/failure/completion lifecycle. |
| `UniformMotionLevel01Controller.cs` | Stationary-target time prediction and three hits. |
| `UniformMotionLevel02Controller.cs` | Demonstrate/predict/test constant-speed target trials. |
| `UniformMotionLevel03Controller.cs` | Future-position calculation and synchronized interception. |
| `UniformMotionVariantDeck.cs` | Shuffled curated variants with unseen-first/no-immediate-repeat rules. |

### Input and XR

| File | Responsibility |
|---|---|
| `IUniformMotionInputSource.cs` | Semantic grab/select/confirm/launch/reset/pause input contract. |
| `UniformMotionInputRouter.cs` | Selects XR or desktop source and publishes one gameplay API. |
| `UniformMotionXRInputSource.cs` | Feature-owned XRI/Input System adapter in guarded XR assembly. |
| `UniformMotionDesktopInputSource.cs` | Full mouse/keyboard fallback. |
| `UniformMotionXRModeSelector.cs` | Detects usable XR and enables the proper presentation safely. |
| `UniformLauncherSocket.cs` | XR socket bridge that hands the ball to the guided launcher. |
| `UniformMotionHapticFeedback.cs` | Optional short haptics with capability checks. |

### Visualization, replay, UI, and assessment

| File | Responsibility |
|---|---|
| `UniformStraightGuideLine.cs` | Straight-only launch guide; API cannot create a parabola. |
| `UniformVelocityArrow.cs` | Signed direction/magnitude arrow and label. |
| `UniformTimeBreadcrumbs.cs` | Equally spaced 0/1/2/3-second markers from analytical values. |
| `UniformPositionTimeGraph.cs` | Straight graph driven from the shared motion model. |
| `UniformTargetView.cs` | Solid, prediction, reveal, glow, and hit visual states only. |
| `UniformMotionBillboard.cs` | Comfortable camera-facing labels without physics decisions. |
| `UniformReplaySample.cs` | Logical-time ball/target/marker evidence sample. |
| `UniformReplayRecorder.cs` | Bounded allocation-free recording. |
| `UniformReplayController.cs` | Reconstructs evidence without moving live bodies. |
| `UniformMotionHUD.cs` | Target counter, speed, direction, time, coordinates, and active level. |
| `UniformBriefingPanel.cs` | Level objective and Observe/Predict/Test instructions. |
| `UniformPhysicsPanel.cs` | Correct formula/straight-motion board. |
| `UniformEvidencePanel.cs` | Prediction, actual value, substitution, graph, and errors after resolution. |
| `UniformFailurePanel.cs` | Attempt evidence, replay, and new-variant action. |
| `UniformLevelCompletePanel.cs` | Modal explanation, next level, replay. |
| `UniformQuizQuestion.cs` | ScriptableObject question data. |
| `UniformQuizQuestionView.cs` | One question’s answer selection UI. |
| `UniformQuizController.cs` | Requires all three answers and submits once. |
| `UniformMotionAudioHooks.cs` | Optional references with safe silent fallback. |

### Editor-only source preparation and scene construction

| File | Responsibility |
|---|---|
| `UniformMotionSourceAssetCatalog.cs` | Exact read-only source paths and existence validation. |
| `UniformMotionGlbBallImporter.cs` | Minimal static GLB reader for the supplied ball only. |
| `UniformMotionTargetArchiveGuide.cs` | Validates extracted RAR contents and reports the local extraction step; it does not modify the original archive. |
| `UniformMotionMaterialFactory.cs` | Creates URP material copies/palette without editing legacy materials. |
| `UniformMotionPrefabFactory.cs` | Creates normalized ball variants, target, launcher, rail, hands, controls, and UI prefabs. |
| `UniformMotionDataFactory.cs` | Creates settings, levels, variants, and quiz assets. |
| `UniformMotionEnvironmentBuilder.cs` | Builds arena from supplied environment geometry plus allowed primitives. |
| `UniformMotionSceneBuilder.cs` | Idempotently constructs/wires only `UniformMotionVR_Root` in the exact scene. |
| `UniformMotionDependencyAudit.cs` | Reports every asset dependency outside Packages, source whitelist, and new feature root. |
| `UniformMotionProjectValidator.cs` | Checks scene path, references, physics rules, variants, cameras/listeners, UI, and build readiness. |

Avoid `FindObjectOfType`, `GameObject.Find`, tag searches, reflection-based discovery, and string-based scene-object lookup at runtime. Wire all dependencies explicitly through `UniformMotionSceneBindings`, prefabs, and ScriptableObjects.

## 19. Asset-preparation workflow

### Ball

Implement `UniformMotionGlbBallImporter` for only the supplied static subset:

- GLB header and JSON/BIN chunks;
- scene/node/mesh structure;
- float VEC3 positions;
- float VEC2 UVs when present;
- unsigned-short indices;
- four triangle primitives/submeshes;
- four base-color factors;
- coordinate conversion and corrected winding;
- recalculated normals/tangents/bounds where needed; and
- URP/Lit or URP/Simple Lit materials.

Generate one normalized base ball mesh/prefab and red, blue, and yellow level variants under `Assets/UniformMotionVR/Generated`. Preserve the source GLB and its metadata.

### Target

Extract the supplied RAR locally into:

`Assets/UniformMotionVR/Generated/Imported/ArcheryTargetSource`

Do not extract into or overwrite `Assets/Uniform_Motion`. Preserve the original archive. Use the extracted FBX and maps. Create new URP materials, wrapper scale/rotation, colliders, center/bullseye hit zones, stand/rail anchors, solid/prediction/reveal render profiles, and optional primitive ring overlays.

If archive extraction cannot be performed on the implementation host, stop with one actionable message identifying the exact archive and destination. Do not download a substitute and do not use a target from another project folder.

### Environment

Use supplied `LowPoly Environment Pack/Prefabs` geometry. Build wrapper instances and replace their renderer materials with new URP copies using a material-remap table. Do not edit source prefabs/materials. Use `Terrain`, `Environment`, `Mounting`, `Tree`, `Rock`, `Bush`, `Log`, `Grass`, and `Plant` assets to compose the canyon/lake range.

Use primitives/procedural geometry for water, wooden decks, bridge/stands, measurement rail, ruler, panel frames, hands/controllers, arrows, and marker discs. Do not use the pack’s demo scenes, playables, PPS assets, or legacy skybox.

## 20. Required data and variants

Create:

- `UniformMotionGameSettings.asset`;
- three level-definition assets;
- at least four validated variants per level;
- exactly three quiz-question assets or one bank containing them; and
- material/prefab/source-import reports.

Suggested curated values:

### Level 1

- `d=6 m`, `v=3 m/s`, `t=2 s`;
- `d=8 m`, `v=4 m/s`, `t=2 s`;
- `d=9 m`, `v=3 m/s`, `t=3 s`; and
- `d=10 m`, `v=5 m/s`, `t=2 s`.

### Level 2

- `x0=4 m`, `v=+2 m/s`, `T=3 s`, `x=10 m`;
- `x0=8 m`, `v=-2 m/s`, `T=3 s`, `x=2 m`;
- `x0=1 m`, `v=+1.5 m/s`, `T=4 s`, `x=7 m`; and
- `x0=9 m`, `v=-1 m/s`, `T=4 s`, `x=5 m`.

### Level 3

- `x0=2 m`, `v=+2 m/s`, `T=3 s`, intercept `8 m`, ball `8/3 m/s` from 0 m;
- `x0=1 m`, `v=+3 m/s`, `T=2 s`, intercept `7 m`, ball `3.5 m/s` from 0 m;
- `x0=8 m`, `v=-2 m/s`, `T=3 s`, intercept `2 m`, feasible ball start chosen on the same lane; and
- `x0=3 m`, `v=+1 m/s`, `T=4 s`, intercept `7 m`, feasible ball speed from configured origin.

Validate rail bounds, time, signed direction, correct position, and ball-speed limits before allowing any asset to save or run.

## 21. Detailed implementation sequence

Complete these steps in order.

### Step 1 — Audit and protect

1. Verify Unity/package versions and exact paths.
2. Hash all existing `.cs`, the requested scene, and every supplied source file.
3. Confirm the source whitelist and blank target scene.
4. Create only the new `Assets/UniformMotionVR` folders and asmdefs.
5. Record the intended changed-file boundary.

### Step 2 — Prepare uploaded assets

1. Implement and run the fresh ball GLB importer.
2. Extract the target RAR to the new generated destination.
3. Import/wrap the FBX target and supplied maps.
4. Create new URP materials for the target and environment.
5. Create normalized ball/target/environment wrapper prefabs.
6. Generate an import report with source paths, hashes, bounds, and outputs.

### Step 3 — Build and test analytical motion

1. Implement pure math, clocks, segments, body, target, guided ball, sweep, and evaluator.
2. Add deterministic EditMode tests before scene/UI work.
3. Prove frame-rate-independent position, constant speed, constant height, correct signed direction, and shared-clock interception.

### Step 4 — Build flow, levels, and variant deck

1. Implement the legal state machine and session progress.
2. Implement all three separate level controllers.
3. Implement the shuffled curated variant deck.
4. Implement feedback, remediation routes, and final quiz routing.
5. Run flow/variant/routing tests.

### Step 5 — Build input and VR interaction

1. Create `UniformMotionVR.inputactions` under the new feature root.
2. Implement desktop input first.
3. Implement guarded OpenXR/XRI adapters, socket handling, ray/direct interactors, and haptics.
4. Confirm physical release velocity is discarded.
5. Confirm no XR device is required for desktop completion.

### Step 6 — Build the reusable arena

1. Compose terrain/canyon/mountains/trees/rocks/logs/plants from supplied geometry.
2. Build water, decks, bridge/stands, rails, ruler, frames, and hand/controller proxies from allowed primitives.
3. Configure new URP materials and warm lighting.
4. Keep the player fixed and the range readable.
5. Pool reusable balls, targets, ghosts, lines, and markers.

### Step 7 — Recreate and correct the reference UI

1. Build Header, How to Play, Physics, objective, telemetry, graph, evidence, feedback, completion, quiz, topic-complete, and pause panels.
2. Use corrected straight-motion diagrams and formulas.
3. Apply red/blue/yellow ball identity and blue/orange/green level identity.
4. Test text at headset distance and seated/standing heights.

### Step 8 — Assemble the exact scene

1. Open `Assets/Scenes/Unfirom_Motion.unity`.
2. Reparent/configure Main Camera and Directional Light.
3. Build `UniformMotionVR_Root` and wire every serialized reference.
4. Create one active EventSystem, camera, AudioListener, and level root.
5. Make the builder idempotent: a second run adds no duplicates and may rebuild only its own root/generated outputs.
6. Save the exact scene path.

### Step 9 — Wire levels, feedback, graph, and quiz

1. Create all level/variant/question data assets.
2. Test Level 1 time prediction and hits.
3. Test Level 2 demonstrate/predict/test flow.
4. Test Level 3 synchronized future interception.
5. Verify graph/markers use the same analytical values.
6. Verify failure always selects a new variant.
7. Verify all score routes.

### Step 10 — Validate and deliver

1. Run scene builder twice.
2. Run dependency audit and project validator.
3. Run all EditMode and PlayMode tests.
4. Complete the entire game in desktop mode.
5. Smoke-test on OpenXR hardware if available.
6. Target at least 72 FPS on the intended standalone headset.
7. Re-hash source code/assets to prove they were not modified.
8. List the manual Build Settings step.
9. Produce the final implementation report.

## 22. Coding and performance requirements

- Use explicit serialized bindings and initialization; no runtime object searches.
- Subscribe/unsubscribe symmetrically.
- Avoid allocations, LINQ, repeated string building, instantiate/destroy loops, and GetComponent searches during movement.
- Pool balls, targets, ghosts, breadcrumbs, line elements, and feedback effects.
- Use shared meshes/materials and `MaterialPropertyBlock` where appropriate.
- Cache shader IDs and component references.
- Keep physics/model logic independent of input and UI.
- Keep success/failure evaluation independent of presentation.
- Keep level flow independent of buttons.
- Validate configuration once and log precise hierarchy/data paths.
- Use the feature clock, never global `Time.timeScale`.
- Use labels, arrows, and patterns in addition to color.
- Keep learner-facing copy about physics, not implementation.
- Avoid excessive transparency, real-time lights, and overdraw.

## 23. Required tests

### EditMode

- `d=vt` for positive, zero, and negative signed velocity.
- `x=x0+vt`, including `x0=2`, `v=2`, `t=3`, result `8 m`.
- `6 m / 3 m/s = 2 s`.
- `x0=4`, `v=2`, `t=3`, result `10 m`.
- required Level 3 ball speed `8/3 ≈ 2.67 m/s`.
- invalid zero/negative time and speed handling.
- variable rendered frame durations do not change analytic position.
- equal-time samples are equally spaced.
- target and ball share the same clock start.
- variant validation rejects off-rail/unreachable data.
- variant deck never repeats the immediate previous ID.
- unseen variants are preferred before cycling.
- legal/illegal state transitions.
- quiz routes for scores 3, 2, 1, and 0.
- level success/failure at tolerance boundaries.

### PlayMode

- ball and target remain at constant height.
- motion samples are collinear and constant-speed.
- gravity, drag, impulses, and XR release velocity do not alter assessed movement.
- swept collision detects a target crossed within one logical step.
- target does not visibly pause, wrap, reverse, or ease in a measured run.
- reset occurs only while hidden between runs.
- Level 1 requires correct time prediction plus three hits.
- Level 2 requires correct marker plus moving-target hit.
- Level 3 hides the answer before commitment and synchronizes both bodies.
- graph and breadcrumbs match live model values.
- failure loads a visibly and numerically different same-level variant.
- modal popup pauses feature motion but remains clickable.
- desktop mode completes every level and quiz.
- XR and desktop use the same semantic gameplay interface.
- exactly one Main Camera, AudioListener, EventSystem, and level root are active.
- scene has no missing scripts/references.
- all asset dependencies stay within Packages, `Assets/Uniform_Motion`, and `Assets/UniformMotionVR`.

Do not add shallow tests that only repeat constants or mirror implementation line by line.

## 24. Acceptance criteria

The implementation is complete only when:

- the exact scene `Assets/Scenes/Unfirom_Motion.unity` opens cleanly;
- all code is newly created under `Assets/UniformMotionVR` and uses `TeachingAndLearning.UniformMotionVR`;
- no existing code, package, ProjectSettings asset, source-art asset, or unrelated scene changed;
- only uploaded source art, allowed Unity primitives, and installed package components are used;
- the supplied ball, target, and environment assets are visibly used;
- original archives, GLB, FBX, textures, prefabs, and legacy materials remain untouched;
- generated materials are URP-compatible;
- the fixed first-person scene matches the reference’s low-poly arena and UI language;
- red, blue, and yellow ball variants identify Levels 1, 2, and 3;
- no assessed curved trajectory or ordinary projectile motion exists;
- all assessed motion is straight, level, analytical, and constant-velocity;
- time markers read `0 s`, `1 s`, `2 s`, `3 s` and have correct spacing;
- displacement and final position are never confused;
- Level 1 teaches `t=d/v` with stationary targets;
- Level 2 demonstrates and tests equal distance in equal time;
- Level 3 performs a synchronized future-position intercept;
- a lucky hit cannot pass Level 2 or Level 3 without the required prediction;
- each failure leads to a different same-concept variant;
- every level ends with a blocking concept popup;
- the graph is straight and uses the same motion data;
- the three-question assessment routes scores exactly;
- desktop fallback covers the full flow;
- validators and meaningful tests pass;
- analytic movement loops allocate no recurring garbage; and
- the intended headset build targets at least 72 FPS.

## 25. Final implementation report

When complete, provide:

1. every new file created, grouped by folder;
2. source and existing-code hash comparison proving no changes;
3. final scene hierarchy;
4. ball GLB import report;
5. target archive extraction/import report;
6. environment material-remap report;
7. all generated meshes, materials, prefabs, Input Actions, ScriptableObjects, and variants;
8. exact Inspector wiring for `UniformMotionSceneBindings` and serialized events;
9. table of all challenge variants and correct results;
10. validator, dependency-audit, EditMode, PlayMode, desktop, and XR smoke-test results;
11. desktop/XR control map;
12. manual Build Settings and next-topic configuration; and
13. any limitation caused by a missing uploaded asset and the allowed procedural substitute used.

Do not claim completion until the scene is assembled, wired, saved at the exact requested path, scientifically correct, and playable from introduction through final assessment routing.
