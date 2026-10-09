# Rest and Motion â€” Elevator Ride

## Project brief

Build a first-person Unity learning experience that teaches rest and motion through an elevator ride. Match the supplied reference image: a stylized low-poly elevator with a broad glass front, a city visible outside, a floor indicator and button panel on the right, and a three-step lesson panel aligned along the right side of the screen.

The learner stands inside the elevator, chooses floor 12, and observes the outside world as the elevator rises. The lesson uses stable buildings and landmarks as visual reference points, then points out the apparent motion of those landmarks relative to the learner. The central idea is that motion is described relative to a reference point: the learner and elevator are at rest relative to one another while moving relative to the city.

## Interactive VR revision

The current deliverable is an immersive OpenXR 3D game, not a fixed-camera lesson with screen-space buttons. The reference image guides the cabin and city environment only. Replace the fixed viewport and right-side HUD with room-scale head/controller tracking, physical interaction targets on the supplied elevator panel, selectable supplied building landmarks, and a cabin-mounted 3D lesson board. The learner selects a reference building, selects floor 12, starts/pauses/replays the ride, and completes two reference-point questions. Both relative motion and relative rest are scored correctly.

Use the existing XR Interaction Toolkit Starter Assets rig and configured OpenXR runtime. Keep the camera full-viewport and stereo. Do not lock the learner's head or attach lesson panels to the headset. Artificial locomotion is disabled in VR for cabin comfort; a bounded mouse/WASD desktop 3D preview remains available. See `Docs/Rest and Motion/rest_and_motion_vr.md` for controls and hardware verification.

This VR revision supersedes the screen-space UI and fixed-view composition instructions below.

## Supplied assets

Use the supplied models for the scene, retaining their geometry and textures:

- Cabin: `Assets/Rest_and_Motion/sci-fi-elevator/source/winda.fbx`.
- Control panel: `Assets/Rest_and_Motion/elevator---elevator-key-5-mb/source/Elevator _ elevator key.fbx`.
- Buildings and ground: `Assets/Rest_and_Motion/residential_building_set.glb`.

Keep the source files intact. Convert their materials to URP and the GLB into Unity assets under `Assets/Rest_and_Motion/Imported`. Adapt the cabin's front faces to a glass material on a mesh copy so outside reference points are visible. Do not substitute primitive cabins, controls or buildings. Render the lesson-card illustrations from these supplied assets.

## Learning goals

By the end of the experience, the learner can:

1. Identify a reference point for describing motion.
2. Distinguish being at rest relative to the elevator from moving relative to the outside buildings.
3. Explain that apparent motion depends on the observer's frame of reference.

## Reference image requirements

- Show the elevator interior as the main scene, viewed from the standing passenger's eye level.
- Use a large glass window across the front wall. The city, roads, trees, bridge, distant skyline, and mountains are visible outside in daylight.
- Place the elevator controls on the right wall. Include numbered floor buttons, a highlighted floor 12 button, a green upward direction indicator, and a current-floor display.
- Keep the learner's hands and sleeves visible at the bottom foreground to establish the first-person viewpoint.
- Place a dark, rounded title panel at the top left with a small blue cube icon, the title â€œRest and Motion,â€ and subtitle â€œElevator Ride.â€
- Place a vertical stack of three dark rounded instructional cards on the right. Each card has a blue numbered badge, a bold white heading, and an illustration.
- Preserve a clear view through the window; UI and controls must not obscure the main exterior reference points.
- Use a polished low-poly 3D art style, warm elevator lighting, cool daylight outdoors, strong readable silhouettes, and high-contrast text.

## Lesson sequence

### 1. Stand Inside the Elevator

Start with the elevator stationary and the learner facing the front window. Show the hands, interior rails, doors, and control panel. The floor display reads the current floor. The learner's position remains fixed relative to the elevator cabin.

**Prompt:** â€œStand inside the elevator.â€

**Teaching point:** The learner and the cabin are at rest relative to each other.

### 2. Use Visual Reference Points Outside

Highlight recognizable objects outside the window, such as the tall central building, bridge, road, and nearby rooftops. Ask the learner to watch one landmark while the elevator moves.

**Prompt:** â€œChoose a building or landmark outside to use as a reference point.â€

**Teaching point:** We describe motion by comparing an object's position with a reference point.

### 3. Detect Motion

Move the elevator upward toward floor 12. Animate the outside view downward relative to the stationary cabin, add subtle vertical movement cues, and advance the floor display. Keep elevator controls, rails, hands, and cabin geometry fixed in the learner's frame.

**Prompt:** â€œNotice how the buildings appear to move as the elevator rises.â€

**Teaching point:** The learner is moving relative to the city, even while appearing at rest relative to the elevator.

## Interaction and feedback

- Let the learner activate the floor 12 button, with a visible pressed state and a clear confirmation cue.
- Start the upward ride after floor selection. Show the direction arrow and update the floor display during travel.
- Allow the learner to pause or replay the motion observation.
- After the ride, ask: â€œAre you at rest or in motion?â€ Follow with: â€œWhat reference point are you using?â€
- Accept â€œat rest relative to the elevatorâ€ and â€œmoving relative to the city/buildingsâ€ as the intended distinction.
- Give concise corrective feedback if the learner describes motion without naming a reference point.

## Unity scene and implementation

### Scene objects

- `ElevatorCabin`: cabin shell, glass window, doors, floor, ceiling lights, and handrails.
- `ElevatorControls`: floor buttons, selected-floor highlight, direction arrow, and digital floor display.
- `ExteriorCity`: modular buildings, road, trees, bridge, skyline, and distant mountains, arranged as readable landmarks.
- `FirstPersonView`: camera at standing eye height, with optional foreground hands and sleeves.
- `LessonCanvas`: title panel and three right-side instructional cards, using a screen-space canvas for consistent legibility.
- `RideController`: manages idle, selected, ascending, arrived, and review states.
- `LessonController`: displays prompts, advances lesson steps, and evaluates the final reference-point question.

### Motion setup

Keep the cabin and learner camera stationary during the ride. Animate the exterior city vertically downward relative to the window, or use a moving elevator/camera rig with the exterior fixed; choose one approach and keep all relative motion visually consistent. For the reference composition, moving the city is the simpler, reliable option. Drive the floor readout and arrow from the same ride state so visual cues stay synchronized.

Use a short acceleration, steady ascent, and gentle stop. Avoid camera shake that could imply the learner is moving within the cabin. Add restrained parallax between nearby buildings and distant skyline to make upward travel easy to perceive.

### UI layout

- Use a 16:9 reference layout and scale the canvas for other aspect ratios.
- Anchor the title panel to the upper-left corner.
- Reserve a right-side column for the three lesson cards; maintain margins and consistent spacing.
- Use rounded near-black panels, blue step badges, bold white headings, and concise copy.
- Keep card illustrations consistent with the main scene and readable at presentation size.
- Provide a safe-area margin and ensure the right column does not cover the elevator controls or key window landmarks.

## Audio and accessibility

- Use optional narration for each prompt, with captions that match the on-screen wording.
- Provide text alternatives for visual motion cues and button states.
- Use more than color alone to indicate selection: add a glow, outline, label, or animation.
- Keep movement slow and predictable, with a pause/replay option and no forced camera movement.
- Support keyboard/controller navigation and a clear interaction focus state.

## Acceptance checklist

- The opening composition matches the supplied reference: first-person glass elevator, daylight city, right-side controls, upper-left title, and three numbered lesson cards.
- Floor 12 is visibly selected; the direction indicator points up and the floor display updates during ascent.
- The exterior moves relative to the cabin in a way that clearly communicates upward elevator travel.
- The cabin, hands, and learner viewpoint remain stable relative to each other.
- The lesson explicitly names the reference point and distinguishes relative rest from relative motion.
- Prompts, captions, controls, and feedback remain readable at the target display size.
- The ride can be paused and replayed, and the final question checks the concept rather than memorization.

## Suggested build order

1. Block out the cabin, window, controls, and city landmarks.
2. Set the first-person camera and match the reference composition.
3. Implement floor selection, ascent, floor display, and arrival states.
4. Add the title panel and three instructional cards.
5. Add prompts, feedback, captions, pause, and replay.
6. Tune lighting, low-poly materials, parallax, readability, and motion pacing against the reference image.
