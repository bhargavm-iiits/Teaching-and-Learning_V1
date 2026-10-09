# Rest and Motion - interactive VR lift

Open `Assets/Scenes/Rest_and_Motion.unity`. You stand inside the supplied glass-front lift (`winda.fbx`) with the supplied button panel on the right wall and a lesson board on the left wall. Outside are the supplied residential towers, roads, a river and bridge, trees, a mountain ring and clouds (read-only Synty and SimpleNaturePack instances). There is also a landing at every floor in front of the doors.

## How the lift behaves (like a real lift)

- **Press a floor button.** Its cap sinks about 4 mm and springs back, you hear a click and feel a haptic pulse, and an amber ring plus the printed number light up. The lamp **stays lit until the lift stops at that floor**, then fades off with an arrival "ding".
- **Several floors pressed:** the lift uses collective control. It keeps going in one direction and stops in floor order, not press order. For example, at floor 1, pressing 9, 4 and 12 gives stops at 4, 9, 12. A floor pressed behind the car is served on the way back.
- **The floor you are already on:** pressing it reopens the doors, and no lamp lights.
- **Press a lit floor twice quickly** (within 0.6 s): the call is cancelled and its lamp goes off.
- **OPEN (◀▶):** lit while held, and keeps the doors open. **CLOSE (▶◀):** closes the doors at once.
- **BELL:** alarm bell, lit while held. **STOP:** pauses the ride; the car and the city freeze and STOP glows red. **GO:** resumes the ride, or closes the doors when stopped at a floor.
- **Display:** floor number (P, 01-12) with green up and down arrows, plus a status line (GOING UP, DOORS OPEN, STOPPED...).
- Floors are 3.2 m apart. Top speed is 1.6 m/s with smooth acceleration and braking. The cabin and your head never move; the outside world moves instead.
- Floors 10-12 are copies of the supplied P button placed in the empty slots above the grid (the supplied panel only has P and 1-9).

## The game: 3 levels

Each level shows an intro pop-up with a START button, then plays out, then asks a pop-up question. A right answer flashes green, plays a chime and fades to the next level. A wrong answer explains the physics and replays the level's scene, then asks again. The score counts first-try answers. The end card has PLAY AGAIN, and RESTART on the cabin board starts over at any time.

1. **Which lift is moving? (easy, outside).** You stand on a plaza in front of a twin-lift tower with glass shafts. Lift A and Lift B are copies of the supplied cabin, each with a passenger. After START, one lift, chosen at random, rises and falls 5 floors while the other stays still. *Which lift is in motion?* Answer Lift A or Lift B.
2. **Inside the moving lift (medium).** The game fades you into the cabin. Press START, then any floor button at least 2 floors away, and ride. On arrival: *Were you at rest or in motion relative to the LIFT?* The answer is **At Rest**. The feedback adds that you and the lift both moved relative to the building.
3. **Emergency free fall (hard).** You start at floor 12 and press START. The alarm sounds, the lights flicker and the red emergency light comes on. The cable snaps and the car **free-falls at g = 9.81 m/s²** from floor 12. The city rushes up past the glass, and the display counts floors down in red. A loose ball and clipboard float in the cabin, because everything falls together. At 9.6 m the safety brakes clamp (sparks and a screech) and stop the car exactly at the ground floor with a thud. The peak speed is about 23.5 m/s. *During the fall, who was in motion relative to the ground?* The answer is **Both**.

For comfort, your head is never moved: the outside world moves instead. There is no vignette, as you requested.
## Controls

- **Headset:** room-scale tracking with no artificial walking. Point a controller at a button or sign and pull the trigger or grip, or press panel buttons directly with your hand (poke).
- **Desktop preview (no headset):** right-drag to look and WASD to move inside the cabin. Left-click presses a button; hold the click for OPEN or BELL. Tab / Shift+Tab moves a focus highlight, Enter or Space presses the focused item, and Esc clears the focus. Keys 0-9 press P and floors 1-9; F10-F12 press 10-12.

## Rebuild and checks

- **In the Editor:** Tools > Rest and Motion > Build Interactive VR Elevator.
- **Batch:** `Unity -batchmode -projectPath <project> -executeMethod TeachingAndLearning.RestAndMotion.Editor.ElevatorSceneBuilder.BatchBuild`. It writes renders to `Work/RestAndMotion/VR/renders/`.
- **Play-mode checks:** `-executeMethod TeachingAndLearning.RestAndMotion.Editor.ElevatorVRChecks.BatchPlayChecks` writes `Work/RestAndMotion/VR/play-validation.txt`. All 23 checks pass: the three levels with their wrong-answer replays, free fall at g with the brakes stopping exactly at the ground, floor-order stops, lamps, door reopen, OPEN hold, cancel, CLOSE, STOP/GO, the press animation and PLAY AGAIN. Screenshots of each level are in `Work/RestAndMotion/VR/levels/`.

Headset tracking, real controller input and on-device frame rate have not been tested on hardware.
