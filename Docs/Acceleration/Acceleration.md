# MASTER IMPLEMENTATION PROMPT — ROCKET LAUNCH: ACCELERATION & RETARDATION VR

> Give this complete document to the Unity implementation agent as one prompt. Execute it as a build specification. Do not stop after proposing a plan.

## 1. Role and required outcome

You are the senior Unity XR engineer, educational game designer, physics reviewer, technical artist, and QA owner for a complete first-person VR lesson named:

7. ACCELERATION & RETARDATION — ROCKET LAUNCH

Build the playable lesson in the existing Unity project. The learner stands or sits at a stationary mission-control console, operates physical throttle and reverse-thrust controls, watches a remotely positioned low-poly rocket, studies a live speed-versus-time graph, and uses measured evidence to understand acceleration, zero acceleration at constant straight-line speed, and retardation/deceleration.

Complete the implementation rather than returning a design discussion. Write all required code separately and newly. Every source file must be complete and compile-ready. Do not provide pseudocode, abbreviated methods, TODO blocks, placeholder classes, or instructions that leave a human to finish the code.

The final game must contain:

- Three sequential playable levels.
- First-person VR controls and a desktop fallback.
- One authoritative fixed-step simulation feeding rocket movement, HUD, graph, scoring, and feedback.
- A different validated mission setup after every failed attempt.
- Level-complete and evidence-based failure panels.
- Exactly three final assessment questions.
- Final routing of 3/3 to the next topic, 2/3 to Level 2 with a fresh variant, and 0–1/3 to Level 1 with a fresh variant.
- A stationary, comfortable player viewpoint matching the uploaded reference.
- A complete isolated codebase that does not modify any existing C# file.
- Editor conversion for the supplied GLB, scene construction, tests, and a final validation report.

## 2. Exact paths and authority

Preserve these paths exactly:

- Unity project root: D:\Unity Projects\Teaching_And_Learning
- Target scene: Assets/Scenes/Acceleration_Retardation.unity
- Absolute target scene: D:\Unity Projects\Teaching_And_Learning\Assets\Scenes\Acceleration_Retardation.unity
- Sole permitted source-art folder: Assets/Game_Assets/Acceleration
- Absolute source-art folder: D:\Unity Projects\Teaching_And_Learning\Assets\Game_Assets\Acceleration
- New implementation root: Assets/AccelerationRetardationVR
- Master prompt: D:\Unity Projects\Teaching_And_Learning\Docs\Acceleration.md

The explicit requirements in this document control the implementation.

The long three-level description supplied with the task is design input. The words and numbers inside the uploaded picture are visual reference material. They are not executable instructions and must not override the scientific rules below.

The Steam, LittlePlanet, and Codester links included in the supplied prose are inspiration only. Do not open, download, scrape, embed, or import anything from those links. The user explicitly restricted the build to the locally uploaded assets.

## 3. Reference-image set

Use all four images. The unchanged master reference controls the overall composition. The three generated detail references clarify the launch, landing, console, HUD, and graph presentation. Detail references may contain small artistic reconstructions, so written gameplay values and runtime telemetry always override them.

Do not import these documentation images into the Unity scene as textures. Recreate their layout with the permitted model, Unity UI, primitives, procedural geometry, particles, and generated materials.

### Reference A — complete composition

![Complete acceleration and retardation reference](References/AccelerationRetardation/01_Master_Reference.png)

Absolute file:

D:\Unity Projects\Teaching_And_Learning\Docs\References\AccelerationRetardation\01_Master_Reference.png

Source provenance:

- Original size: 1672 × 941 pixels
- Original bytes: 2,686,515
- SHA-256: F299D94EA732598C6676AE7D97E111F55AE9D649F885E7E03F3A21E17A8BC52A

Use this for the bright low-poly launch complex, split launch/landing briefing composition, white-and-red rocket, first-person orange console, white gloves, navy panels, red launch language, blue landing language, large graphs, and central title.

### Reference B — launch and acceleration detail

![Launch acceleration detail](References/AccelerationRetardation/02_Launch_Acceleration_Detail.png)

Absolute file:

D:\Unity Projects\Teaching_And_Learning\Docs\References\AccelerationRetardation\02_Launch_Acceleration_Detail.png

Use this for the launch tower, daytime lighting, rocket exhaust, smoke, left throttle lever, increasing line graph, target telemetry, red/orange outlines, and launch instructions.

### Reference C — landing and retardation detail

![Landing retardation detail](References/AccelerationRetardation/03_Landing_Retardation_Detail.png)

Absolute file:

D:\Unity Projects\Teaching_And_Learning\Docs\References\AccelerationRetardation\03_Landing_Retardation_Detail.png

Use this for the warm dusk lighting, descending first stage, deployed landing legs, landing pad, reverse-thrust control, falling speed graph, blue/cyan outlines, and the visibly separate final-touchdown segment.

### Reference D — first-person control console and HUD

![First-person control console and HUD](References/AccelerationRetardation/04_FirstPerson_Control_HUD.png)

Absolute file:

D:\Unity Projects\Teaching_And_Learning\Docs\References\AccelerationRetardation\04_FirstPerson_Control_HUD.png

Use this for the console proportions, comfortable reach, four primary telemetry modules, target-band meter, live graph, vertical-motion arrow, left throttle, right brake/retro lever, and stable far-field rocket view.

## 4. Verified project facts

Implement against these inspected facts:

- Unity editor: 6000.5.5f1.
- URP: 17.5.0.
- Input System: 1.19.0.
- XR Interaction Toolkit: 3.5.1.
- OpenXR: 1.17.1.
- XR Management: 4.5.4.
- XR Core Utils: 2.6.0.
- XR Hands: 1.8.1.
- uGUI: 2.5.0.
- Android minimum SDK: 26.
- Android target architecture: ARM64.
- The target scene exists and currently contains only the default Main Camera and Directional Light.
- The target scene is absent from Editor Build Settings.
- Four unrelated scenes are already enabled in Build Settings. Preserve them and their order.
- No glTF or GLB import package is installed.
- The project contains unrelated lesson managers and unrelated manual GLB readers. Do not modify, call, subclass, copy, or depend on them.

## 5. Verified source-asset facts

The permitted folder contains only:

- model.glb
- model.glb.meta

model.glb facts:

- Size: 1,965,228 bytes.
- SHA-256: 55B8AB2F69CAE383D0E23CEEC1C7CFBCE6A29A822E3764E31323E804B3ABF4E6.
- Unity GUID: a165d2db56560b448bc83e46aa110bb1.
- Unity currently uses DefaultImporter, so it cannot be instantiated as a Unity model.
- Valid glTF 2.0 binary.
- Generator: 3dassets.dev ingest.
- One scene named Launch Complex Pad and Yard.
- The optional top-level default-scene property is missing; an importer must correctly fall back to scene index 0.
- 1,287 nodes.
- 395 mesh definitions and 395 triangle primitives.
- 10 textureless PBR materials.
- No images or textures.
- No UV0 data.
- No vertex colors.
- No animations.
- No skins.
- No cameras.
- No lights.
- No embedded author, copyright, licence, or attribution metadata.
- Full bounds approximately 98.35 × 51.841 × 80.4 metres.
- Mesh accessors use KHR_mesh_quantization.
- Positions are normalized signed SHORT values.
- Normals are present.
- Indices are unsigned SHORT values.
- Full unoptimized scene would produce roughly 1,023 mesh renderers and is unsuitable for mobile VR without selective extraction and material-based combination.

The ten material names are:

- scorch
- gravel
- concrete
- charcoal
- white
- yellow
- steel
- red
- glass
- cryo

The source glass material is opaque. If transparent glass is needed, create a derived URP material beneath the new implementation folder. Do not alter the source.

Do not invent a licence or attribution. Record the source filename, hash, generator string, and the fact that embedded rights metadata was not supplied in a local SourceAssetManifest and credits note.

## 6. Useful named source assemblies

The GLB is a modular pre-arranged launch yard rather than one rocket prefab. Preserve the untouched generated source hierarchy, then create derived prefabs from named source modules.

Launch stack near the main pad:

- Node 446: 121-launch-mount-with-clamps
- Node 466: 122-first-stage-core
- Node 472: 123-interstage
- Node 477: 124-second-stage
- Node 482: 125-payload-fairing-halves
- Node 499: 128-strongback

Main gantry roots:

- 588
- 597
- 601
- 605
- 609
- 613
- 619
- 625
- 642

Landing hardware displayed elsewhere in the catalogue:

- Node 971: 201-grid-fin-set
- Node 982: 202-landing-leg-set
- Node 997: 203-engine-cluster

Other useful components include flame trenches, flame deflector, launch table, hardstand, pad, apron, ground modules, tanks, water tower, floodlights, service vehicles, fences, pipes, towers, and office cabin.

The landing vehicle must be a new derived prefab. Duplicate the first-stage core into the generated area, create explicit anchors from renderer bounds, then attach cloned grid-fin, landing-leg, and engine-cluster modules. Do not animate or reparent nodes in the source GLB.

The source contains no:

- Exhaust flame or smoke.
- Cockpit or mission-control console.
- VR hand art.
- UI or graphs.
- Audio.
- Colliders.
- Motion clips.

Build these only from Unity UI, allowed procedural primitives, particles, line geometry, generated materials, and new code.

## 7. Isolation and file rules

Create all implementation content under:

Assets/AccelerationRetardationVR/

Use the namespace:

TeachingAndLearning.AccelerationRetardation

Prefix every public type with Ar. Put every public class, interface, enum, and struct in its own new file. Create separate Runtime, Editor, EditMode-test, and PlayMode-test assembly definitions.

Do not edit:

- Any existing .cs file.
- model.glb.
- model.glb.meta.
- Anything inside Assets/Game_Assets/Acceleration.
- Packages/manifest.json.
- Packages/packages-lock.json.
- Any unrelated scene.
- Any unrelated asset.
- Global URP, quality, input, or XR project settings.

The implementation may:

- Modify Assets/Scenes/Acceleration_Retardation.unity.
- Create new files under Assets/AccelerationRetardationVR/.
- Add the exact target scene to Editor Build Settings while preserving all existing enabled scenes and their order.

Do not remove or reorder existing Build Settings scenes.

Allowed visual/audio construction:

- The one permitted GLB.
- Installed Unity packages.
- Unity primitives.
- Procedurally generated meshes.
- Generated solid-color URP materials.
- Built-in shaders.
- uGUI and TextMeshPro already present.
- LineRenderer and custom Graphic geometry.
- ParticleSystem.
- Code-generated procedural audio.
- Assets derived locally from model.glb by the new editor tool.

Do not:

- Download assets.
- Use Asset Store content.
- Use the three linked inspiration images.
- Use XR sample assets.
- Use art, models, audio, materials, textures, prefabs, scripts, or UI from another lesson folder.
- Register a global ScriptedImporter for .glb.
- Parse the GLB at runtime.

## 8. Scientific truth contract

This is a simplified one-dimensional educational simulation. It is not a real rocket launch or landing specification.

Use these definitions accurately:

- Speed is a nonnegative scalar measured in m/s.
- Velocity includes speed and direction.
- Acceleration is the rate of change of velocity, measured in m/s².
- In the straight-line launch and cruise used here, unchanged speed and unchanged direction mean zero acceleration.
- Retardation, also called deceleration, means acceleration opposite the current velocity so speed decreases.
- Negative acceleration does not always mean slowing. Its meaning depends on the chosen positive axis and the velocity direction.

Declare the one-dimensional mission axis visibly:

- Levels 1 and 2: positive mission direction points upward.
- Level 3: positive mission direction points downward toward the pad.

In Level 3, the rocket’s along-path velocity changes from +40 m/s to +5 m/s while +s points toward the pad. Therefore:

a_average = (+5 − +40) / 7 = −5 m/s²

The negative sign means acceleration is opposite the declared positive landing direction. A separate arrow must always show the chosen axis.

Use Speed versus Time for graph titles unless signed velocity is deliberately displayed with a clear axis. Never put negative speed on a speed-time graph.

One authoritative simulation must feed:

- Rocket position.
- Rocket presentation.
- Current speed.
- Acceleration.
- Timer.
- Graph.
- Target-band progress.
- Success and failure evaluation.
- Evidence panels.
- Level summary.
- Final quiz data where generated from a run.

Do not animate a decorative graph independently. Do not hard-code a HUD value that differs from the solver. Validate success with unrounded numbers; round only for display.

## 9. Authoritative simulation

Use a deterministic one-dimensional fixed-step solver with a default educational step of 0.02 seconds.

For each step:

vNext = max(0, v + a × dt)

distanceStep = (v + vNext) × 0.5 × dt

sNext = s + distanceStep

averageAcceleration = (vFinal − vInitial) / elapsedTime

Use trapezoidal distance integration so the displayed endpoint and travelled distance agree.

Keep simulation metres separate from compressed display motion. Use a configurable metresToWorldUnits value, initially about 0.1 Unity unit per simulated metre, so a stationary player can see the complete flight. The HUD always displays true simulation values.

Interpolate visual rocket movement between fixed simulation states. Never calculate educational acceleration by differentiating noisy rendered transforms.

Landing model:

netAlongPathAcceleration = gravityContribution − reverseThrust

Use an intentionally simplified authored setup such as:

- gravityContribution: +1 m/s² along the downward mission axis.
- reverseThrust: brakeInput × 7 m/s².
- available net acceleration: +1 to −6 m/s².
- brake input near 6/7 produces net −5 m/s².

State in the level briefing that the HUD displays net path acceleration. Real mass loss, changing gravity, aerodynamics, and orbital mechanics are outside this lesson.

Speed may not become negative. If the rocket reaches zero above the pad, it hovers or resumes downward motion according to the simplified controls; it must not reverse through the pad.

For touchdown:

- Integrate remaining path distance.
- Detect the first crossing of the landing plane.
- Evaluate the actual speed at contact.
- Evaluate pad-center offset and tilt at contact.
- Do not fake touchdown with a timer or animation event.

## 10. Player experience and state flow

Use a stationary first-person mission-control station. The player does not ride the rocket. The far-field rocket moves while the cockpit, horizon, and headset remain stable.

Use this attempt flow:

Boot → Briefing → Target Preview → Armed → Active Control → Observation → Evaluation → Reflection → Result

Briefing:

- Show the objective, positive-axis arrow, target values, time, control, and success rules.
- Hide final answers.
- Let the player choose comfort settings and recenter.

Target Preview:

- Show the target corridor and formula structure.
- Ask the learner to predict whether the speed-time line will rise, remain horizontal, or fall.
- Record the prediction.

Armed:

- Require an intentional Start button or launch-lever detent.
- Reset the solver, graph, evaluator, particles, and timers together.

Active Control:

- Read the physical lever or desktop command.
- Advance the fixed-step model.
- Animate the rocket from the same state.
- Record telemetry at 10 Hz.
- Update the HUD and graph from telemetry.
- Use haptic detents and target-band feedback without steering the answer automatically.

Observation:

- Keep the graph and target band visible.
- Show plain-language state such as SPEEDING UP, CONSTANT SPEED, or SLOWING DOWN.
- Do not pause the timer for normal mistakes.

Evaluation:

- Freeze the educational clock at the intended boundary.
- Evaluate raw values.
- Record numerical evidence.

Reflection:

- Compare the prediction, target, and measured run.
- Show the calculation with units.
- Explain one misconception at a time.

Result:

- On success, show LEVEL COMPLETE.
- On failure, show evidence and offer TRY NEW MISSION.

## 11. Level 1 — Basic Acceleration

Mission:

Launch safely and increase speed gradually.

Canonical first attempt:

- Starting speed: 0 m/s.
- Target speed: 20 m/s.
- Deadline: 5.0 s.
- Required average acceleration: +4 m/s².
- Desired instantaneous net-acceleration band: +3 to +5 m/s².
- Hard overspeed threshold: greater than 25 m/s.
- Constant-acceleration distance at +4 m/s²: 50 m.

Control:

- Left red launch-throttle lever.
- Range maps from 0 to approximately +6 m/s².
- Pulling beyond the start detent arms and begins the launch.
- Add visible neutral, start, safe-band, and maximum detents.

HUD:

- CURRENT SPEED.
- TARGET SPEED: 20 m/s.
- NET ACCELERATION.
- TIME REMAINING.
- Green 3–5 m/s² band.
- Speed target marker.
- Rising live speed-time graph.
- Upward mission-axis arrow.

Success requires all:

- Speed between 19 and 21 m/s at 5.0 seconds.
- Peak speed no greater than 25 m/s.
- At least 80 percent of active mission time inside +3 to +5 m/s².

Do not fail on one isolated sampled-frame excursion. Evaluate band occupancy across time.

On success show:

(20 − 0) / 5 = +4 m/s²

Then explain:

Acceleration is positive because velocity increased in the declared positive direction.

On failure report the actual endpoint, measured average acceleration, band occupancy, and peak speed.

## 12. Level 2 — Accelerate and Cruise

Mission:

Increase speed to a target, then maintain straight-line constant speed.

Canonical first attempt:

- Starting speed: 20 m/s.
- Target cruise speed: 40 m/s.
- Acceleration time: 4.0 s.
- Required average acceleration during ramp: +5 m/s².
- Ramp distance at the target: 120 m.
- Cruise band: 38–42 m/s.
- Continuous cruise duration: 5.0 s.
- Cruise distance at exactly 40 m/s: 200 m.
- Total canonical distance: 320 m.
- Maximum time to establish the five-second streak: 12 s after the ramp boundary.

Control:

- Center-detent bidirectional throttle.
- Forward of neutral maps to 0 through +6 m/s².
- Neutral maps to 0 m/s².
- Behind neutral maps to 0 through −3 m/s² fine trim.

Acceleration phase qualification:

- Speed is between 38 and 42 m/s at 4.0 seconds.
- Measured average acceleration is within ±0.5 m/s² of +5 m/s².

Cruise phase:

- Streak timer advances only when speed is 38–42 m/s.
- Applied net acceleration must be between −0.5 and +0.5 m/s².
- Leaving either band resets the continuous hold streak.
- Display CURRENT STREAK / 5.0 s.
- Target corridor turns green only when both speed and acceleration qualify.

Use the statement:

The rocket moves in a straight line without changing direction. When its speed remains constant, its acceleration is zero.

Do not teach the broader false claim that constant speed always means zero acceleration; direction is deliberately unchanged in this level.

On success show the piecewise graph:

- Rising line from 20 to 40 m/s over four seconds.
- Horizontal line at 40 m/s during the five-second cruise.
- Phase boundary at t = 4 s.

## 13. Level 3 — Retardation and Safe Landing

Mission:

Use reverse thrust to reduce speed, then complete a separate fine-landing stage.

Canonical braking stage:

- Positive mission direction points downward toward the pad.
- Initial speed: +40 m/s.
- Target speed after 7.0 s: +5 m/s.
- Required average path acceleration: −5 m/s².
- Qualification speed: 4–6 m/s at 7.0 s.
- Average-acceleration tolerance: ±0.5 m/s².
- Braking-stage distance at the target: 157.5 m.

Final approach is a separate stage:

- Start from the actual braking-stage endpoint.
- Target touchdown speed: 0–2 m/s.
- Maximum pad-center offset: 1.5 m.
- Maximum tilt: 8 degrees.
- Final-approach time limit: 8 s.

Controls:

- Right brake/reverse-thrust lever controls the one-dimensional net path acceleration.
- Left thumbstick, desktop A/D, or a small alignment stick controls gentle lateral correction.
- Lateral alignment must not alter the authoritative vertical speed calculation.
- Add optional auto-alignment assistance because lateral dexterity is secondary to the physics objective.

HUD:

- CURRENT SPEED.
- TARGET SPEED.
- NET PATH ACCELERATION.
- TIME REMAINING.
- DISTANCE TO PAD.
- PAD OFFSET.
- TILT.
- Downward positive-axis arrow.
- Falling speed-time graph.
- A visibly separate final-approach region.

The first graph segment must represent:

(+5 − +40) / 7 = −5 m/s²

The 5 m/s endpoint is not touchdown. Shade or mark the final flare segment separately and continue to 0–2 m/s at first pad contact.

Fail for:

- Braking too late.
- Braking so early that the rocket stops above the pad and times out.
- Speed outside the stage-one target at seven seconds.
- Hard impact above 2 m/s.
- Landing outside the pad zone.
- Tilt greater than 8 degrees.

On success explain:

Retardation means the acceleration is opposite the rocket’s velocity, so its speed decreases. The negative sign comes from the declared downward-positive mission axis.

## 14. Retry variants

The first attempt at each level uses the canonical values. Every failed retry must use a different, same-concept validated variant. Never immediately repeat a failed variant.

Create at least these authored variants:

| Level | Variant | Start | Target | Time | Target acceleration | Extra requirement |
|---|---|---:|---:|---:|---:|---|
| 1 | A | 0 m/s | 20 m/s | 5 s | +4 m/s² | band 3–5, overspeed 25 |
| 1 | B | 0 m/s | 16 m/s | 4 s | +4 m/s² | band 3–5, overspeed 21 |
| 1 | C | 0 m/s | 24 m/s | 6 s | +4 m/s² | band 3–5, overspeed 29 |
| 1 | D | 0 m/s | 12 m/s | 3 s | +4 m/s² | band 3–5, overspeed 17 |
| 2 | A | 20 m/s | 40 m/s | 4 s | +5 m/s² | hold 40±2 for 5 s |
| 2 | B | 15 m/s | 35 m/s | 4 s | +5 m/s² | hold 35±2 for 5 s |
| 2 | C | 25 m/s | 45 m/s | 4 s | +5 m/s² | hold 45±2 for 5 s |
| 2 | D | 20 m/s | 45 m/s | 5 s | +5 m/s² | hold 45±2 for 6 s |
| 3 | A | 40 m/s | 5 m/s | 7 s | −5 m/s² | centered pad |
| 3 | B | 35 m/s | 5 m/s | 6 s | −5 m/s² | announced pad offset |
| 3 | C | 45 m/s | 5 m/s | 8 s | −5 m/s² | wider starting offset |
| 3 | D | 30 m/s | 5 m/s | 5 s | −5 m/s² | mild deterministic crosswind |

For every variant, recalculate:

- Target line and band.
- Mission distance.
- Graph axes.
- Thresholds.
- Formula values.
- Question text.
- Feedback.
- Spawn height.
- Final-approach starting distance.

Do not randomize arbitrary physics values. Prevalidate every combination so it is achievable with the available lever ranges and has one unambiguous outcome.

ArAdaptiveVariantSelector must:

- Track used variant IDs per level.
- Exclude the just-failed ID.
- Select an unseen ID when possible.
- Shuffle deterministically when all are used.
- Still exclude the immediately previous ID after reshuffling.
- Persist enough session state to avoid an identical scene-reload retry.
- Accept a seed in tests.

Failure evidence example:

At 5.0 s your speed was 16.7 m/s.

Measured average acceleration: +3.34 m/s².

Target average acceleration: +4.00 m/s².

Provide one scaffold at a time:

1. First failure: concept hint and measured evidence.
2. Second failure: substitute the endpoints into Δv / Δt.
3. Third failure: display a guided ghost target corridor and stronger lever detents.

Keep the same scientific goal. Do not reveal an exact control-motion script.

## 15. Level result panels

Successful level:

- Pause the solver and educational timer.
- Show LEVEL COMPLETE.
- Show initial speed.
- Show final speed.
- Show elapsed time.
- Show actual average acceleration equation.
- Show peak and minimum speed.
- Show the actual graph.
- Explain the concept.
- Offer NEXT LEVEL and REPLAY NEW VARIANT.

Failed level:

- Pause safely.
- Show MISSION REVIEW.
- Name the actual failure reason.
- Show target versus actual evidence.
- Highlight the relevant graph interval.
- Give one misconception-specific explanation.
- Offer TRY NEW MISSION.

Never silently reload the same setup.

## 16. Final three-question assessment

After Levels 1, 2, and 3 are completed, ask exactly three questions, one at a time:

1. A rocket changes from 0 to 20 m/s in 5 s. What is its average acceleration?
   - Correct: +4 m/s².

2. A rocket maintains 40 m/s in a straight line without changing direction. What is its acceleration?
   - Correct: 0 m/s².

3. Positive direction is toward the landing pad. A rocket changes from +40 m/s to +5 m/s in 7 s. What is its average acceleration?
   - Correct: −5 m/s².

Shuffle answer order without changing meaning.

For later assessment attempts, draw equivalent prevalidated numbers from a local question bank so the learner cannot memorize positions.

Routing:

- 3 correct: invoke ArNextTopicGateway.
- 2 correct: restart from Level 2 with a fresh Level 2 variant.
- 0 or 1 correct: restart from Level 1 with a fresh Level 1 variant.

After remediation, require the remaining level sequence and then present a fresh equivalent three-question assessment.

ArNextTopicGateway must expose:

- A UnityEvent.
- An optional nextTopicSceneName.

If no valid next scene is configured:

- Do not throw.
- Show TOPIC COMPLETE.
- Record the 3/3 result.
- Invoke the completion event.
- Explain the missing configuration in the editor validation report.

## 17. Presentation and environment

Use the reference’s split composition only for the overview briefing. Active gameplay is sequential:

- Levels 1 and 2 show the launch zone in bright daytime.
- Level 3 shows the landing zone with warm late-afternoon or dusk lighting.
- Never run both rocket simulations simultaneously.

Environment:

- Imported and optimized low-poly launch complex.
- Main launch pad and tower.
- Distant angular mountains created from allowed primitives or procedural meshes.
- Evergreen silhouettes created procedurally if needed.
- Bright blue launch sky.
- Warm orange-pink landing sky.
- Fences, roads, tanks, service buildings, lights, and warning stripes from the permitted GLB.

Title board:

- Blue square badge: 7.
- ACCELERATION in yellow/orange.
- & in white.
- RETARDATION in blue.
- Subtitle: Control the rocket and observe how speed changes during launch, cruise, and landing.

Launch visual language:

- Red and orange outlines.
- Warm flame and smoke.
- Rising graph.
- Upward arrow.

Cruise visual language:

- Green and cyan target band.
- Horizontal graph segment.
- Stable audio and particles.

Landing visual language:

- Blue and cyan outlines.
- Warm dusk background.
- Falling graph.
- Downward mission-axis arrow.
- Blue target corridor and magenta final-touchdown region.

Panels:

- Dark navy faces.
- Thick beveled brown or orange frames.
- Large white body text.
- Yellow headings.
- Cyan numerical labels.
- Green valid, amber warning, red failure.
- Pair every color with words and icons.

## 18. First-person console

Build a comfortable orange industrial console from Unity primitives and generated materials.

Required controls:

- Left launch/throttle lever with red grip.
- Right black brake/retro lever with red top.
- Small landing alignment stick.
- Start/confirm button.
- Pause button.
- Recenter button.
- Graph toggle.
- Comfort/settings button.

Required monitors:

- CURRENT SPEED.
- TARGET SPEED or target band.
- NET ACCELERATION.
- TIME REMAINING or ELAPSED TIME.
- Stage name.
- Direction arrow.
- Target-band meter.
- Compact speed-time trace.
- Cruise streak in Level 2.
- Distance, offset, and tilt in Level 3.

Create simple white procedural glove/controller-hand visuals because no hand art is supplied. These visuals may follow XR controllers but must not replace tracked interaction logic.

Physical controls need:

- Visible movement range.
- Neutral and target detents.
- Haptic ticks.
- Hover and grab affordance.
- Numeric accessibility alternative through buttons or thumbstick.
- Left-handed layout option.

## 19. HUD and speed-time graph

Sample authoritative telemetry at 10 Hz.

Each ArTelemetrySample stores:

- Mission time.
- Along-path distance.
- Speed.
- Net along-path acceleration.
- Normalized control input.
- Phase.
- Target-band state.
- Rocket world position.
- Lateral offset.
- Tilt.
- Variant ID.

Graph requirements:

- X-axis: mission time in seconds.
- Y-axis: speed in m/s starting at zero.
- Current-run line.
- Current point marker.
- Target line or shaded corridor.
- Phase boundaries.
- Checkpoint markers.
- Level 1: red/orange rising line.
- Level 2 ramp: red/orange.
- Level 2 cruise: green/cyan.
- Level 3 braking: blue.
- Level 3 final approach: magenta or patterned region.
- Text and line patterns in addition to color.

Build the graph through a custom uGUI Graphic mesh or one bounded reusable mesh. Do not install a chart package. Do not create one GameObject for every sample.

Keep the vertical scale fixed during one attempt. Derive it from the mission definition before launch so the curve does not jump.

Stop graph sampling whenever the simulation is paused. Preserve samples after evaluation for the evidence panel.

## 20. XR, desktop input, comfort, and accessibility

Stationary VR design:

- Keep the player at the console.
- Preserve headset position and rotation tracking.
- Do not parent the headset to the rocket.
- No forced locomotion.
- No forced head rotation.
- No camera roll.
- No camera shake by default.
- Maintain a stable cockpit frame and horizon.

XR input:

- Direct grab for levers and stick.
- Ray input for world UI.
- Trigger for buttons and answers.
- Grip for controls.
- Primary action for confirm.
- Secondary action for pause/settings.
- Short haptic detents and warnings.

Desktop fallback:

- Space or Enter: start/confirm.
- W/S or Up/Down: throttle.
- B or Down: brake.
- A/D: landing alignment.
- Escape: pause.
- Mouse: point and select.
- R: recenter or reset current attempt.
- G: toggle graph.
- Number keys 1–3: assessment choices.

Use one ArControlInputRouter so XR and desktop issue identical abstract commands.

Accessibility:

- Seated and standing calibration.
- Left/right control swap.
- Large text at comfortable distances.
- High contrast.
- No required color-only cue.
- Reduced particles.
- Reduced haptics.
- Mute and volume controls.
- Captions or visual equivalent for every audio cue.
- Adjustable lever sensitivity.
- Optional landing alignment assistance.
- Pause, retry, and exit at any time.
- Avoid flashing exhaust or warning effects.

## 21. Required scene hierarchy

Build this hierarchy in Assets/Scenes/Acceleration_Retardation.unity:

~~~text
AccelerationRetardation_Root
├── Environment
│   ├── ImportedLaunchComplex_Optimized
│   ├── ProceduralBackdrop
│   ├── LaunchZone
│   │   ├── LaunchPadAnchor
│   │   ├── LaunchRocket
│   │   └── LaunchFX
│   └── LandingZone
│       ├── LandingPadAnchor
│       ├── LandingRocket
│       └── LandingFX
├── PlayerStation
│   ├── XR Origin
│   │   ├── Camera Offset
│   │   │   └── Main Camera
│   │   ├── Left Controller
│   │   └── Right Controller
│   ├── Desktop Fallback Rig
│   └── ComfortReferenceFrame
├── ControlConsole
│   ├── ConsoleBody
│   ├── LaunchThrottle
│   ├── BrakeLever
│   ├── AlignmentStick
│   ├── StartButton
│   ├── PauseButton
│   ├── LeftProceduralHand
│   └── RightProceduralHand
├── WorldUI
│   ├── TitleBoard
│   ├── InstructionBoard
│   ├── MissionHUD
│   ├── SpeedTimeGraph
│   ├── TargetBand
│   ├── LevelResultPanel
│   ├── FailureEvidencePanel
│   ├── QuizPanel
│   └── ComfortPanel
├── Systems
│   ├── GameBootstrap
│   ├── GameStateMachine
│   ├── LevelDirector
│   ├── SimulationClock
│   ├── RocketSimulation
│   ├── TelemetryRecorder
│   ├── AdaptiveVariantSelector
│   ├── QuizController
│   ├── InputRouter
│   └── ProceduralAudio
└── Lighting
    ├── DirectionalLight
    ├── LaunchLighting
    └── LandingLighting
~~~

Keep exactly one enabled Camera and AudioListener in the active mode. Reuse the existing Main Camera and Directional Light inside this generated hierarchy.

## 22. Required new folder structure

~~~text
Assets/AccelerationRetardationVR/
├── Art/
│   └── Generated/
│       ├── Materials/
│       ├── Meshes/
│       └── SourcePrefabs/
├── Audio/
│   └── Generated/
├── Data/
├── Prefabs/
│   ├── Environment/
│   ├── Interaction/
│   ├── Rocket/
│   └── UI/
├── Scripts/
│   ├── Core/
│   ├── Data/
│   ├── Evaluation/
│   ├── Flow/
│   ├── Input/
│   ├── Presentation/
│   ├── Simulation/
│   ├── UI/
│   └── TeachingAndLearning.AccelerationRetardation.Runtime.asmdef
├── Editor/
│   └── TeachingAndLearning.AccelerationRetardation.Editor.asmdef
├── Tests/
│   ├── EditMode/
│   ├── PlayMode/
│   ├── TeachingAndLearning.AccelerationRetardation.EditModeTests.asmdef
│   └── TeachingAndLearning.AccelerationRetardation.PlayModeTests.asmdef
├── Settings/
└── Documentation/
~~~

## 23. Required separate runtime code files

Every item is a separate new .cs file.

### Core and data

- ArMissionPhase.cs
- ArMissionOutcome.cs
- ArQuizRoute.cs
- ArRocketMotionState.cs
- ArRocketControlCommand.cs
- ArTelemetrySample.cs
- ArMissionResult.cs
- ArMissionEvaluationSnapshot.cs
- ArRocketMissionCatalog.cs
- ArRocketMissionDefinition.cs
- ArRocketMissionVariant.cs
- ArQuizQuestionDefinition.cs
- ArQuizBank.cs
- ArAccessibilitySettings.cs
- ArSourceAssetManifestData.cs

Use ScriptableObjects for mission catalog, mission definitions, variants, question bank, and default accessibility settings. Use serializable immutable-style value types for motion, commands, telemetry, and results.

### Simulation and evaluation

- ArSimulationClock.cs
- ArRocketKinematicsSolver.cs
- ArRocketSimulationController.cs
- ArLandingAlignmentSolver.cs
- ArTelemetryRecorder.cs
- ArIMissionEvaluator.cs
- ArAccelerationMissionEvaluator.cs
- ArCruiseMissionEvaluator.cs
- ArLandingMissionEvaluator.cs

Evaluators consume telemetry and mission definitions. They do not read UI text or presentation transforms.

### Flow

- ArRocketGameBootstrap.cs
- ArRocketGameStateMachine.cs
- ArRocketLevelDirector.cs
- ArAdaptiveVariantSelector.cs
- ArFailureFeedbackFormatter.cs
- ArQuizController.cs
- ArQuizRoutingPolicy.cs
- ArSessionProgressStore.cs
- ArNextTopicGateway.cs

### Input and interaction

- ArIControlInteractable.cs
- ArControlInputRouter.cs
- ArLeverInteractable.cs
- ArPushButtonInteractable.cs
- ArAlignmentStickInteractable.cs
- ArControllerRayInteractor.cs
- ArXRInputBridge.cs
- ArDesktopInputBridge.cs
- ArHapticFeedbackService.cs
- ArControlAction.cs

Create a new Input System .inputactions asset under Assets/AccelerationRetardationVR/Settings or generate actions in the new runtime code. Do not depend on sample assets.

### Presentation

- ArRocketAssetAssembly.cs
- ArRocketView.cs
- ArRocketEffectsView.cs
- ArLaunchSiteView.cs
- ArLandingPadView.cs
- ArMissionEnvironmentController.cs
- ArConsoleView.cs
- ArProceduralHandView.cs
- ArProceduralPrimitiveFactory.cs
- ArProceduralAudioService.cs

### UI

- ArMissionHudPresenter.cs
- ArSpeedTimeGraphPresenter.cs
- ArSpeedTimeGraphGraphic.cs
- ArTargetBandGraphic.cs
- ArInstructionBoardPresenter.cs
- ArLevelResultPresenter.cs
- ArFailureEvidencePresenter.cs
- ArQuizPresenter.cs
- ArComfortSettingsPresenter.cs
- ArWorldSpaceBillboard.cs
- ArPausePresenter.cs

## 24. Required separate editor code files

Every item is a separate new .cs file in the Editor assembly.

### Narrow source converter

- ArGlbBinaryReader.cs
- ArGlbJsonParser.cs
- ArGlbDocument.cs
- ArGlbAccessorDecoder.cs
- ArGlbCoordinateConverter.cs
- ArGlbMaterialBuilder.cs
- ArGlbPrefabBuilder.cs
- ArRocketPrefabAssembler.cs
- ArLaunchSiteOptimizer.cs
- ArSourceAssetManifest.cs

### Content generation and validation

- ArMissionDataFactory.cs
- ArMaterialLibraryBuilder.cs
- ArProceduralPrefabBuilder.cs
- ArAccelerationRetardationSceneBuilder.cs
- ArAccelerationRetardationValidator.cs
- ArValidationReportWriter.cs
- ArAccelerationBuildMenu.cs

## 25. GLB converter contract

Because model.glb uses DefaultImporter, create an explicit editor-only GLB 2.0 converter. Do not create a global .glb ScriptedImporter.

The converter must:

1. Validate magic, GLB version, total length, JSON chunk, and BIN chunk.
2. Fall back to scene index 0 when the top-level scene property is absent.
3. Parse scenes, nodes, meshes, primitives, accessors, buffer views, buffers, materials, and extensions used.
4. Support SCALAR, VEC2, VEC3, and VEC4.
5. Support the component types used by the source and reject unsupported combinations clearly.
6. Honor accessor byte offsets.
7. Honor buffer-view byte offsets.
8. Honor byte stride.
9. Decode normalized signed SHORT positions and normals for KHR_mesh_quantization.
10. Decode unsigned SHORT indices.
11. Preserve node matrices and node translation/rotation/scale.
12. Convert glTF right-handed coordinates to Unity left-handed coordinates.
13. Reflect Z consistently.
14. Correct triangle winding.
15. Preserve node names and hierarchy.
16. Build generated Unity Mesh assets.
17. Build textureless URP/Lit materials from PBR factors.
18. Build one untouched generated source prefab.
19. Record source path, byte length, generator, and SHA-256 in ArSourceAssetManifest.
20. Leave model.glb and model.glb.meta byte-identical.

Do not build unused support for textures, skinning, morph targets, or animation. The inspected source does not use them.

Reject malformed or unsupported data rather than silently corrupting geometry.

## 26. Derived prefabs and optimization

Create:

- Untouched generated source-catalog prefab.
- Optimized launch-complex environment prefab.
- Launch rocket prefab.
- Landing first-stage prefab.
- Console prefab.
- World-UI prefab.

Launch rocket:

- Use 122-first-stage-core.
- Use 123-interstage.
- Use 124-second-stage.
- Use 125-payload-fairing-halves.
- Preserve their relative vertical placement.
- Keep launch-mount clamps separate.

Landing rocket:

- Duplicate only generated assets beneath the new feature folder.
- Use a first-stage core.
- Attach 201-grid-fin-set.
- Attach 202-landing-leg-set.
- Attach 203-engine-cluster.
- Create named anchors from renderer bounds.
- Fine-tune placement in the generated prefab.
- Add simple colliders only where touchdown or effects require them.

Environment:

- Selectively include the useful launch yard.
- Combine static renderers by shared material.
- Exclude rocket modules, moving clamps, controls, effects anchors, and other animated props.
- Preserve a source-to-generated node manifest.
- Do not add MeshColliders to all geometry.
- Use simple box, capsule, or plane colliders only where interaction or landing evaluation needs them.

The source scene can expand to roughly 1,023 renderers. The optimized active environment should target a small fraction of that count. Report renderer, material, triangle, and batch counts.

## 27. Idempotent editor menu

Expose these commands:

~~~text
Tools/Teaching & Learning/Acceleration/01 Import Source GLB
Tools/Teaching & Learning/Acceleration/02 Build Data and Prefabs
Tools/Teaching & Learning/Acceleration/03 Build Scene
Tools/Teaching & Learning/Acceleration/04 Validate
Tools/Teaching & Learning/Acceleration/Build All
~~~

Build All must be safe to run repeatedly.

It may replace only:

- Assets it previously generated beneath Assets/AccelerationRetardationVR/Art/Generated.
- Data and prefabs it owns beneath the new root.
- The clearly named AccelerationRetardation_Root in the exact target scene.

It must:

- Use Undo for scene mutations.
- Avoid duplicate EventSystems.
- Avoid duplicate cameras.
- Avoid duplicate AudioListeners.
- Preserve the target scene GUID.
- Save only the target scene.
- Add the target scene to Build Settings without removing or reordering existing scenes.

## 28. Tests

Create these separate files at minimum:

- ArRocketKinematicsSolverTests.cs
- ArMissionEvaluatorTests.cs
- ArAdaptiveVariantSelectorTests.cs
- ArQuizRoutingPolicyTests.cs
- ArGlbCoordinateConversionTests.cs
- ArGlbQuantizationTests.cs
- ArSceneContractTests.cs
- ArRocketMissionPlayModeTests.cs

### Required numerical tests

Level 1:

- v = 0 + 4 × 5 = 20 m/s.
- distance = 50 m.
- target window boundaries are handled correctly.
- overspeed at greater than 25 m/s fails.

Level 2 acceleration:

- v = 20 + 5 × 4 = 40 m/s.
- distance = 120 m.

Level 2 cruise:

- acceleration = 0 m/s².
- speed remains 40 m/s for five seconds.
- added distance = 200 m.
- total canonical distance = 320 m.
- leaving the speed or acceleration band resets the continuous streak.

Level 3 braking:

- v = 40 − 5 × 7 = 5 m/s.
- distance = 157.5 m.
- signed-axis explanation is correct.
- touchdown stage remains separate.

Also test:

- Speed clamps at zero.
- Fixed-step output is independent of render frame rate.
- Trapezoidal distance integration.
- Telemetry samples exactly match solver state.
- Graph points exactly match telemetry.
- Pause adds no time or samples.
- All authored variants are reachable with available control ranges.
- Failure variant never repeats immediately.
- Exhausted variant bag reshuffles safely.
- Quiz routes 3, 2, 1, and 0 correctly.
- Landing speed boundaries.
- Pad-radius boundaries.
- Tilt boundaries.
- First pad-plane crossing is used.
- Missing next-topic scene is safe.
- GLB scene-0 fallback.
- Normalized signed-short decoding.
- KHR_mesh_quantization handling.
- Z reflection and triangle winding.
- Repeated Build All creates no duplicates.
- Target scene has no missing scripts.
- Only one active AudioListener exists.
- Source GLB and meta hashes remain unchanged.
- No existing C# or package file changed.

## 29. Performance requirements

- Target 90 FPS on desktop VR and at least 72 FPS on standalone-class hardware.
- Use single-pass instanced XR where already configured.
- Use SRP Batcher-compatible generated materials.
- Use GPU instancing where useful.
- Combine static environment geometry by material.
- Avoid real-time MeshColliders.
- Limit active particles to a few hundred.
- Pool smoke, flame, warning, and marker effects.
- Avoid per-frame allocations in simulation, graphing, HUD, and input.
- Interpolate visuals between fixed states.
- Update numeric text only when its displayed value changes.
- Use a bounded telemetry buffer.
- Render the graph as one reusable mesh.
- Use inexpensive lighting.
- Respect the current project quality settings.
- Do not change global URP or quality assets.

The validator must report:

- Active renderer count.
- Material count.
- Estimated triangles.
- Active lights.
- Maximum active particles.
- Missing references.
- Duplicate cameras.
- Duplicate AudioListeners.
- Duplicate EventSystems.
- Runtime allocations detected by project tests where feasible.

## 30. Detailed build sequence

Follow this order.

### Phase 1 — protect and audit

1. Record hashes for all existing .cs files.
2. Record hashes for model.glb and model.glb.meta.
3. Record package-file hashes.
4. Record the target scene GUID and pre-build hash.
5. Confirm no destination-type collisions.

### Phase 2 — establish isolation

1. Create Assets/AccelerationRetardationVR.
2. Create the folder hierarchy.
3. Create the four assembly definitions.
4. Set editor and test references correctly.
5. Verify the project still compiles before adding behavior.

### Phase 3 — implement source conversion

1. Write the narrow GLB reader and data model.
2. Decode quantized positions and normals.
3. Convert coordinates and winding.
4. Build textureless URP materials.
5. Build the full generated source prefab.
6. Save ArSourceAssetManifest.
7. Unit-test the converter.

### Phase 4 — build optimized art

1. Extract named rocket modules.
2. Assemble the launch rocket.
3. Assemble the landing first stage.
4. Optimize the static launch site by material.
5. Create simple colliders.
6. Build procedural console, controls, hands, graph frame, and UI frames.
7. Create local materials and particle prefabs.

### Phase 5 — implement physics first

1. Write ArSimulationClock.
2. Write ArRocketKinematicsSolver.
3. Write ArLandingAlignmentSolver.
4. Write ArTelemetryRecorder.
5. Write the three evaluators.
6. Run all numerical tests before writing presentation logic.

### Phase 6 — create mission data

1. Create the canonical Level 1 definition.
2. Create the canonical Level 2 definition.
3. Create the canonical Level 3 definition.
4. Create at least four variants per level.
5. Simulate every variant automatically.
6. Reject unreachable, ambiguous, or unsafe variants.
7. Create the quiz bank.

### Phase 7 — flow and adaptive retry

1. Implement the state machine.
2. Implement level sequencing.
3. Implement variant selection.
4. Implement failure evidence.
5. Implement scaffold progression.
6. Implement final quiz and exact routing.
7. Implement session persistence under a unique lesson key.

### Phase 8 — interaction

1. Build lever, button, and alignment-stick interaction.
2. Configure direct XR grabbing.
3. Configure ray UI.
4. Implement haptics.
5. Implement the desktop bridge.
6. Verify both paths issue identical commands.

### Phase 9 — presentation

1. Build launch, cruise, and landing environment states.
2. Build rocket visual interpolation.
3. Build exhaust, smoke, dust, warning, and success effects.
4. Build the HUD.
5. Build the target-band display.
6. Build the speed-time graph.
7. Build instruction, evidence, result, comfort, and quiz panels.
8. Build optional procedural audio.

### Phase 10 — scene construction

1. Open only Assets/Scenes/Acceleration_Retardation.unity.
2. Reuse the existing Main Camera and Directional Light.
3. Build AccelerationRetardation_Root.
4. Wire every serialized dependency.
5. Verify one EventSystem.
6. Verify one active AudioListener.
7. Add the target scene to Build Settings while preserving all existing scenes.
8. Save the target scene.

### Phase 11 — validate

1. Run EditMode tests.
2. Run PlayMode tests.
3. Run the content validator.
4. Test without an XR device.
5. Test XR bindings with the available simulator or device.
6. Recompute protected-file hashes.
7. Write the validation report.

## 31. Validation report

Save a report under:

Assets/AccelerationRetardationVR/Documentation/

Include:

- Unity and relevant package versions.
- Target scene path and GUID.
- Source file path, bytes, generator, and SHA-256.
- Statement that embedded rights metadata was absent.
- Source hash before and after.
- Existing-code hash comparison.
- Package-file hash comparison.
- Created-file list.
- Generated-prefab list.
- Renderer, material, and triangle statistics.
- All mission variants with their calculated expected endpoints and validation results.
- Graph-to-telemetry consistency.
- Scene camera, AudioListener, and EventSystem counts.
- Build Settings result.
- EditMode test result.
- PlayMode test result.
- Desktop fallback result.
- XR checks completed.
- Checks still requiring the target headset.
- Next-topic gateway configuration.

## 32. Definition of done

The feature is complete only when:

- Assets/Scenes/Acceleration_Retardation.unity opens without errors.
- The target scene is present in Build Settings without disturbing existing scenes.
- New code compiles with no errors or warnings introduced by the feature.
- No existing C# file changed.
- No package file changed.
- model.glb and its .meta remain byte-identical.
- No external link asset was downloaded or used.
- Only permitted source art and locally generated content are referenced.
- The stationary first-person composition matches the reference.
- Level 1 reaches the canonical 20 m/s in 5 s at +4 m/s² when controlled correctly.
- Level 2 demonstrates a rising graph followed by a horizontal graph at straight-line constant speed.
- Level 3 separately demonstrates 40→5 m/s in 7 s and a 0–2 m/s touchdown.
- The Level 3 sign convention is visible and correct.
- Rocket movement, HUD, graph, evaluation, evidence, and results agree with the same telemetry.
- Every failed attempt loads a different validated variant.
- Every successful level shows LEVEL COMPLETE.
- The final check contains exactly three questions.
- 3/3 invokes the next-topic gateway.
- 2/3 restarts Level 2 with a fresh variant.
- 0–1/3 restarts Level 1 with a fresh variant.
- XR and desktop controls both work.
- Comfort and accessibility settings work.
- Required EditMode and PlayMode tests pass.
- The final validation report exists.

## 33. Required completion response

After implementation, report:

1. The final learner experience.
2. Every created file, grouped by folder.
3. Exact changes made to Assets/Scenes/Acceleration_Retardation.unity.
4. The source-conversion and optimization result.
5. All mission variants and measured validation endpoints.
6. EditMode and PlayMode results.
7. Content-validator results.
8. Protected-file hash comparison.
9. Build Settings result.
10. Remaining checks that require the physical headset.
11. The configured next-topic destination or completion-event fallback.

Do not claim completion unless compilation, scene construction, tests, and validation were actually run. If a genuine blocker remains, describe it precisely and leave the project compiling and reviewable.
