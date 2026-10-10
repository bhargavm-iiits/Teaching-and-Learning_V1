# MASTER PROMPT: Parcel Sort VR, Speed Challenge (Unity 3D interactive VR)

> **How to use this file:** paste everything below the line into a fresh Claude Code session opened at
> `D:\Unity Projects\Teaching_And_Learning`. It is a self-contained build brief. The agent must follow it
> section by section and must not invent any requirement that contradicts it.

---

## 0. Role and mission

You are a senior Unity XR engineer and technical artist. Build a **complete, playable, 3-level VR game**
called **"Parcel Sort VR – Speed Challenge"**, which teaches **speed and uniform straight-line motion**
(NCERT Class 9, *Motion*: v = d / t). The scene must **look like the reference image in section 3**:
a bright low-poly warehouse, belts with yellow-black hazard rails running away from the player toward an
open loading dock with a blue truck, glossy coloured parcels with tape and barcodes, big colour-coded
sorting bins with white label plates, a navy/cyan **CONVEYOR SPEED 0.2 / 0.4 / 0.6 / 0.8 / 1.0 m/s** bar across
the top, a coloured **LEVEL N** header with »»» chevrons and an instruction card under it, and
white cartoon VR gloves with black-and-orange cuffs.

The player stands still at the near end of the belts. They point at, press, grab, carry, and throw
parcels with real VR hands. There is no locomotion.

**Platform (user decision): DESKTOP FIRST, VR-READY.** The game must be fully playable on a PC with
mouse + keyboard (section 9.5) with no headset connected. The XR rig and XRI grab code are built in at
the same time, so a headset can be plugged in later without rebuilding: when an XR display is running,
VR mode is used; otherwise desktop mode is used automatically.

Unity 6000.5.5f1, URP 17.5.0, XR Interaction Toolkit 3.5.1, OpenXR 1.17.1, XR Hands 1.8.1,
Input System 1.19.0, uGUI 2.5.0 + TextMeshPro. All of these are already installed. **Do not add, remove,
or upgrade packages.**

---

## 1. NON-NEGOTIABLE RULES: create new, change nothing existing

1. **Do not modify, move, rename, re-import with new settings, or delete any existing file** in the
   project. That covers other games' scenes, scripts, editors, prefabs, materials, the XRI samples,
   `Assets/XR*`, `Assets/XRI`, `ProjectSettings/*`, `Packages/*`, Build Settings, the Tag Manager,
   physics layers, XRI Interaction Layer settings, and the `.meta` files of the conveyor asset.
2. **The only exception is the target scene `Assets/Scenes/Speed.unity`.** It already exists and holds
   only the default *Main Camera* and *Directional Light*. Fill it in through the builder. Never touch
   any other `.unity` file.
3. **Every new file goes under `Assets/Speed/`** (game code and assets). The exceptions are
   `Work/Speed/` (renders, logs, reports; Unity does not import it) and `Docs/` (documentation).
   **Leave `Assets/Speed/conveyor-belt/` as it is: read it, never write to it.**
4. **Do not reference or call code from other games**: `Assets/Scripts`, `Assets/Editor`,
   `Assets/ZeroReturn`, `Assets/SpeedTimeGraph*`, `Assets/DistanceTimeGraph`, `Assets/Rest_and_Motion`,
   and so on. If you need a helper, write a fresh copy inside `Assets/Speed/`.
   Third-party packages and the **XRI Starter Assets sample** may be *referenced* (as prefab instances,
   input actions, or shaders) but never edited.
5. Use a single namespace, **`SpeedParcelSort`**, with its own assembly definitions (section 14) and its
   own editor menu, **Tools ▸ Speed ▸ …**
6. **No new Tags and no new Layers.** Both would change `ProjectSettings/TagManager.asset`. Identify
   objects through components such as `Parcel`, `SortBin`, and `ConveyorBelt`. Use the **Default** XRI
   interaction layer.
7. **Integrity proof:** before writing anything, save `git status --porcelain` and a SHA-256 list of
   every existing `*.cs`, `*.unity`, `*.asset`, `*.prefab`, `*.mat`, and `*.meta` file to
   `Work/Speed/integrity-before.json`. At the end, compare. Apart from
   `Assets/Scenes/Speed.unity` (and its `.meta` only if Unity rewrites it), **only new paths** may appear.
   Write the result to `Work/Speed/integrity-report.txt`.
8. **Plan before building:** first render the planned scene (section 16, Phase 0), show it to the user
   next to the reference crops, and **wait for approval** before generating the scene.
9. The user's Editor may have the project open. If batch mode is refused because the project is locked,
   build from the menu inside the open Editor, or build in an isolated copy under `Work/Speed/BuildProject`
   and copy only `Assets/Scenes/Speed.unity` plus the new `Assets/Speed/**` back.

---

## 2. Paths (exact)

| What | Path |
|---|---|
| Project root | `D:\Unity Projects\Teaching_And_Learning` |
| **Target scene (overwrite via builder only)** | `Assets/Scenes/Speed.unity` |
| **Conveyor belt asset folder (read-only)** | `D:\Unity Projects\Teaching_And_Learning\Assets\Speed\conveyor-belt` |
| Conveyor model (binary FBX, single mesh "Conveyor belt", 1 material, embedded take "Take 001") | `Assets/Speed/conveyor-belt/source/Conveyor Belt.fbx` |
| Conveyor texture (2048×2048 RGBA) | `Assets/Speed/conveyor-belt/textures/Conveyor_Belt.png` |
| New game root (all new code and assets) | `Assets/Speed/` |
| XR rig prefab to instance (read-only) | `Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab` |
| XRI samples folder (read-only; use the XR Device Simulator from here for desktop testing if present) | `Assets/Samples/XR Interaction Toolkit/3.5.1/` |
| Reference images (for humans, in this doc) | `Docs/Speed_Reference/*.png` |
| Reference images (for the agent's render comparison) | `Work/Speed/reference/*.png` |
| Renders, logs, validation reports | `Work/Speed/` |
| This master prompt | `Docs/Speed.md` |

⚠ **Texture-name mismatch:** the FBX's material asks for `Conveyor Belt.png` (with a **space**), but the
file on disk is `Conveyor_Belt.png` (with an **underscore**). Do not rename it and do not change the
importer. Build a new URP Lit material, `Assets/Speed/Materials/M_ConveyorFrame.mat`, with this texture
as Base Map, and assign it to the conveyor renderers in the scene.

---

## 3. Reference images (the visual target)

Match these. The image is a **triptych**: one first-person frame per level, all three under the same
top speed bar. In VR, **one warehouse station** is re-configured for each level (one belt, then two
belts, then two mixed belts with four bins). Every level must reproduce its own panel of the image from
the player's default eye position.

**Full reference (all three levels + speed bar):**

![Full reference](Speed_Reference/00_full_reference.png)

**Top bar: CONVEYOR SPEED selector** (selected button glows cyan and shows a stopwatch icon):

![Speed bar](Speed_Reference/01_speed_bar.png)

| Level 1: Observe the belt speed | Level 2: Sort by Color | Level 3: Sort Mixed Boxes |
|---|---|---|
| ![L1](Speed_Reference/02_level1.png) | ![L2](Speed_Reference/03_level2.png) | ![L3](Speed_Reference/04_level3.png) |

**Top-down layout plan (Unity metres; the player is at the origin facing +Z; parcels move toward −Z):**

![Top-down layout](Speed_Reference/05_topdown_layout.png)

Copies of the same files for automated comparison:
`Work/Speed/reference/00_full_reference.png`, `01_speed_bar.png`, `02_level1.png`, `03_level2.png`,
`04_level3.png`, `05_topdown_layout.png`.

### 3.1 What the image contains (checklist, all required)

**Shared (all levels)**
- A huge, bright warehouse. The floor is polished, light grey-white, with yellow safety lane lines. Tall
  **dark-blue pallet racks** run along both sides, with **orange/yellow beams**, full of tan cardboard
  boxes and yellow/orange crates on pallets. Rows of bright rectangular ceiling lights, plus warm light.
- At the far end, an **open loading-dock door** with bright sky-blue daylight and a **blue box truck**
  backed in. This is the vanishing point of the belts.
- The belts: a dark charcoal ribbed belt surface between **yellow-and-black hazard-striped side rails**,
  slightly raised. **White chevron arrows (^)** painted on the belt point toward the player and move
  with the belt.
- The parcels are glossy cubes in a saturated colour, each with a **lighter tape stripe** over the top and a
  **white barcode label** on the side. The front-most parcel is large and close, at grabbing distance.
- **VR hands:** white cartoon gloves with a **black cuff and an orange band**, entering from the bottom
  corners. They show open, pointing, and gripping poses.
- **Top bar** (dark navy panel, glowing blue outline): an orange »»» icon, the text **CONVEYOR SPEED**,
  then five rounded-rectangle buttons. **The image shows 5 10 15 20 25; this game shows 0.2 0.4 0.6 0.8 1.0
  with the unit "m/s"** (user decision, section 19). The selected button is bright blue with a cyan
  glow and a ⏱ stopwatch icon. The others are dark navy with thin blue outlines.
- **Level header:** a coloured tab, "LEVEL N", in big white bold italic, with three »»» chevrons on the
  right. Under it, a dark navy **instruction card** with a coloured outline.

**Level 1 (blue theme #0142C5):** one belt in the centre with identical **blue** parcels. A grey
**"ZONE A"** sign hangs on the left rack. Card: magnifier icon, **"Observe the belt speed."** /
*"Is this belt fast or slow?"* The right hand points at the belt.

**Level 2 (green theme #1FBF3A):** two belts side by side. The left belt carries only **blue**, the right
only **red**. A big **BLUE** bin is on the left and a big **RED** bin on the right. Each is a cabinet
with a white label plate that has a cube icon and the colour name, and an open mouth facing the belts.
Arcs show parcels flying into the bins. Card: blue cube icon, **"Sort by Color"**, then *"Left belt has
BLUE boxes. Right belt has RED boxes. Grab the boxes and put them in the matching bins."* Both hands
grip parcels.

**Level 3 (purple theme #8A3CF0):** two belts, **both carrying mixed blue / red / yellow / green**
parcels. There are four bins: **BLUE** (upper/further) and **YELLOW** (lower/nearer) on the left, and
**RED** (upper/further) and **GREEN** (lower/nearer) on the right. Card: four cube icons (blue, red,
yellow, green), **"Sort Mixed Boxes"**, then *"Both belts have mixed colors. Sort each box into the
matching color bin."* Both hands grip parcels: blue on the left, yellow on the right.

### 3.2 Palette (sampled from the reference; use these as material and UI colours)

| Token | Hex | Use |
|---|---|---|
| PanelNavy | `#0D1D55` | speed bar background |
| CardNavy | `#021951` | instruction cards (idle button fill `#012260`) |
| SelectBlue | `#0245C7` + glow `#29B6FF` | selected speed button |
| L1Blue | `#0142C5` | Level 1 header and outline |
| L2Green | `#1FBF3A` (dark `#005623`) | Level 2 header and outline |
| L3Purple | `#8A3CF0` (dark `#461097`) | Level 3 header and outline |
| ParcelBlue | `#1F5BFF` (shade `#004FEF`, tape `#48A2FC`) | blue parcel and bin |
| ParcelRed | `#E0262E` (shade `#CB1C25`) | red parcel and bin |
| ParcelYellow | `#FFC21A` (shade `#F2A500`) | yellow parcel and bin |
| ParcelGreen | `#2DBE5C` (shade `#37A25E`) | green parcel and bin |
| ChevronOrange | `#FF8A1E` | »»» icon, glove cuff band |
| Hazard | `#FFC21A` / `#111111` | rail stripes (45°, 8 cm period) |
| Belt | `#2B2E3A` | belt surface |
| RackBlue | `#132D6D` | racks |
| Daylight | `#8AC4FF` | dock door sky |
| Floor | `#E9EEF5` | floor base |

---

## 4. Physics and learning model (must be exact)

- **Scale:** 1 Unity unit = **1 m**. Speeds are shown and applied in **m/s**: **0.2, 0.4, 0.6, 0.8, 1.0 m/s**.
  Never fake the speed. A parcel at 0.6 m/s must move exactly 0.6 m per second of game time.
- **Uniform motion:** a parcel on a belt follows `z(t) = z_spawn − v · (t − t_spawn)`, computed from time
  (not by summing frame deltas, so there is no drift) and applied with `Rigidbody.MovePosition` in
  `FixedUpdate`. `x` and `y` stay constant.
- **The belt surface and the chevrons must visibly move at the same v.** Scroll the belt texture by
  `offset = (v · t / tileLength_m) mod 1`. A parcel and the chevron under it must stay locked together
  (validation test V-04).
- **Observation section:** two white marker lines across the belt, **A** (z = 4.50) and **B** (z = 2.50),
  exactly **2.00 m** apart. They have floor posts labelled "A" and "B" and a bracket labelled "2 m".
- Teaching table (shown on the results panels and used to check answers):

| Belt speed | Time to cover 2 m (A → B) | Catch window in the 1.30 m grab zone | Parcel spacing |
|---|---|---|---|
| 0.2 m/s | 10.00 s | 6.50 s | 0.45 m |
| 0.4 m/s | 5.00 s | 3.25 s | 0.60 m |
| 0.6 m/s | 3.33 s | 2.17 s | 0.90 m |
| 0.8 m/s | 2.50 s | 1.63 s | 1.20 m |
| 1.0 m/s | 2.00 s | 1.30 s | 1.50 m |

- Parcel spacing on a belt = `max(0.45 m, v × 1.5 s)`, so a new parcel reaches the player every 1.5 s at
  most (2.25 s at 0.2 m/s). That keeps the fast belts catchable with one mouse hand.
- Closing message after every level: **"The faster the conveyor moves, the less time you have to catch a
  parcel over the same distance. Speed = Distance ÷ Time."**
- A thrown parcel follows a curved path under gravity. That is part of the game interaction, not the
  lesson. Show this tip once in the tutorial: *"On the belt, boxes move in a straight line at constant
  speed. In the air, gravity curves their path."*
---

## 5. Exact scene layout (world coordinates, metres)

The player's origin is **(0, 0, 0), facing +Z**. Parcels travel along **−Z**, toward the player.
Belt top surface: **y = 0.90** (waist height, so the hands reach it comfortably). Any object the player
must physically touch sits **within 0.75 m of the shoulder line** (x ±0.20, y 1.40, z 0).

### 5.1 Station objects

| Object | Level 1 | Level 2 | Level 3 |
|---|---|---|---|
| Belt(s), centre x | `Belt_Center` x 0 | `Belt_Left` x −0.38, `Belt_Right` x +0.38 | same as L2 |
| Belt span | z 0.25 → 8.25 (8.0 m), width 0.66 incl. rails | same | same |
| Grab zone (where parcels are grabbable) | z 0.30 → 1.60 | same | same |
| Belt end / missed chute | z 0.25: parcel tips into an under-belt **Missed chute** (red light strip + "MISSED" decal) | same | same |
| Markers A / B | z 4.50 / z 2.50 + "2 m" bracket | hidden | hidden |
| Bins (centre of opening, opening 0.55 × 0.55, rim height) | none (one **practice bin** in the tutorial only) | BLUE (−1.10, 0.95 rim, 0.95) • RED (+1.10, 0.95, 0.95) | BLUE (−1.10, 1.05, 1.25) • YELLOW (−1.10, 0.85, 0.45) • RED (+1.10, 1.05, 1.25) • GREEN (+1.10, 0.85, 0.45) |
| ZONE A sign | (−2.25, 2.6, 3.5), on the left rack, yaw +70° | kept | kept |

Bin mouths are angled 20° toward the player's centre, so a gentle toss or a direct placement both work.
Each bin is 0.70 W × 1.20 H × 0.70 D. It has a white label plate (0.42 × 0.20) above the mouth with a
coloured cube icon, the colour name in bold, **and a distinct shape icon** (accessibility; see 7.2).

### 5.2 Floating world-space panels (all face the eye point (0, 1.6, 0))

| Panel | Centre (x, y, z) | Size (m) | Interaction |
|---|---|---|---|
| `SpeedBar` (CONVEYOR SPEED, 5 buttons) | (0, 2.55, 3.40) | 3.00 × 0.42 | ray select (trigger) |
| `BeltSelector` (L2/L3 only: [LEFT BELT] [RIGHT BELT] tabs + the chosen speed for each) | (0, 2.24, 3.38) | 1.60 × 0.18 | ray select |
| `LevelHeader` + `InstructionCard` | (0, 2.02, 3.20) | 2.20 × 0.62 | read only |
| `TimerBoard` (stopwatch in L1; countdown in L2/L3) | (−1.75, 1.95, 2.70), yaw −30° | 0.80 × 0.45 | read only |
| `ScoreBoard` (score, streak, correct, mistakes, missed) | (+1.75, 1.95, 2.70), yaw +30° | 0.80 × 0.55 | read only |
| `QuestionBoard` (L1 challenge; 5 answer buttons) | replaces `InstructionCard` at (0, 2.02, 3.20) | 2.20 × 0.80 | ray select |
| `ResultsPanel` | (0, 1.75, 2.30) | 1.80 × 1.10 | ray select (RETRY / NEXT LEVEL / MENU) |
| `GuideBubble` (virtual guide text + voice line) | (−0.95, 1.55, 1.60), yaw −25° | 0.70 × 0.30 | read only |

**Physical pedestal console** (poke + direct press; this is the "reach and press" from the brief), on a
yellow-black post at **(−0.62, 1.00, 0.10)**, top tilted 30° toward the player:
`START` (big green dome, Ø 0.12), `RESTART` (orange), and in L1 **five chunky 3D speed keys 0.2–1.0** that
duplicate the SpeedBar. Pressing either the console key or the SpeedBar button does the same thing.

### 5.3 Environment (procedural low-poly, generated by the builder into `Assets/Speed/Generated/`)

- Hall: floor 24 × 30 m (x −12…12, z −4…26) with yellow lane lines at x ±1.6; walls light grey; ceiling
  at y 9 with **8 rows of emissive light panels** (1.6 × 0.3) and two skylight strips.
- Racks: 2 rows per side at x ±2.45 and ±4.0, z 1.5 → 20. Each rack has 4 levels; uprights `#132D6D`,
  beams `#FF8A1E`, and pallets with randomised tan cardboard boxes and yellow crates (fixed seed 4242,
  GPU-instanced, ≤ 400 instances).
- Loading dock at z 22: an opening 6 m wide × 5 m high, an emissive sky backdrop `#8AC4FF`, and a
  **blue low-poly box truck** (cab + container) backed in at z 24.
- Lighting: one directional "sun" (warm white, soft shadows only on belts, parcels, and bins) + ambient
  gradient (sky `#CFE3FF`, equator `#E9EEF5`, ground `#B8C2D0`) + 6 baked-look point fills (no realtime
  shadows). Light fog from z 14, so the dock glows, as in the image.
- URP post-processing in a **new** Volume profile `Assets/Speed/Settings/SpeedVolume.asset`: mild
  bloom (threshold 1.0, intensity 0.6) so emissive buttons glow, slight vignette, ACES. Do **not**
  change the project's URP asset or renderer.

### 5.4 Reference camera (to reproduce the image for comparison renders)

`ReferenceCamera` (disabled in play; used only by the validator): position **(0, 1.58, −0.15)**, pitch
**+17° down**, yaw 0, vertical FOV **68°**, aspect 552:821 (portrait crop, as in each level panel).
Render each level to `Work/Speed/renders/level{1,2,3}.png` and put it next to
`Work/Speed/reference/0{2,3,4}_level{1,2,3}.png` in `Work/Speed/renders/compare_level{N}.png`.
The XR Origin starts at (0, 0, −0.15), facing +Z. **In desktop mode the game camera uses exactly this
reference camera pose and FOV**, so what the player sees on the monitor is the reference composition.

---

## 6. Conveyor belt (using the supplied FBX)

1. Instantiate `Assets/Speed/conveyor-belt/source/Conveyor Belt.fbx` **as a model instance in the scene,
   never as a modified import**. If you need a prefab, create a **new** prefab,
   `Assets/Speed/Prefabs/ConveyorModule.prefab`, that contains the FBX as a nested model.
2. At build time, measure `Renderer.bounds`. Find the long axis, rotate so it runs along Z, and scale
   uniformly so the frame width equals **0.66 m**. Log the measured bounds, the scale, and the module
   length to `Work/Speed/conveyor-fbx-report.txt`.
3. Tile modules end to end to cover **z 0.25 → 8.25**. If one module is longer, scale it along Z only, so
   the frame is not distorted. Set the height so the belt top is y = 0.90.
4. The FBX has one material and one mesh, so its texture cannot scroll on its own. Overlay a **new
   procedural belt-surface mesh** (`BeltSurface`, 0.58 × 8.0 m, 2 mm above the FBX belt top) with
   `M_BeltSurface_Scroll.mat` (ribbed charcoal `#2B2E3A` + white chevrons every 0.60 m, generated
   texture `T_BeltChevron.png`, tile length 0.60 m). Its offset is driven by `ConveyorBelt` (section 4).
5. Add **yellow-black hazard rails** (new mesh, 4 cm wide × 6 cm high) on both edges, with
   `M_HazardStripe.mat` (generated texture). For two belts, the shared middle rail also serves as the
   divider.
6. Add rollers at both ends that spin at ω = v / r (r = 0.05 m), purely visual.
7. Add a belt hum `AudioSource`, pitch = 0.8 + 0.4 × v (m/s), volume 0.25.

---

## 7. Parcels

### 7.1 Model and physics
- A procedural low-poly box: **0.28 m cube** (big enough to grab easily), 1 cm bevel, with a tape stripe
  (8 cm, lighter tint) over the top and down the front and back, and a white barcode label on ±X sides
  (generated texture `T_Barcode.png`).
- `Rigidbody` (mass 1.2 kg, interpolate, continuous-speculative), `BoxCollider`, and **`ParcelGrabInteractable`**
  (section 9.3), plus a `Parcel` component (colour, beltId, state, spawnTime, value).
- States: `OnBelt` (kinematic, moved by its belt) → `Held` → `Flying` (dynamic) → `Sorted` | `WrongBin` |
  `Missed` | `Dropped`. A parcel that lands back on a belt surface while `Flying` **re-joins that belt**:
  it snaps to the belt's x and y and continues at that belt's v.
- Spawning: object-pooled (pool of 48), one material per colour (GPU instancing, no per-instance material
  copies). Parcels on each belt are **evenly spaced by `max(0.45 m, v × 1.5 s)`** (section 4). At round start the
  belt is **pre-filled** from z 0.70 to 8.0 at that spacing, so the image's full belt shows immediately.
  New parcels spawn at z 8.10 every `spacing / v` seconds.

### 7.2 Colours and shape icons (colour-blind support)
| Colour | Icon printed on the top + front of the parcel and on the bin label |
|---|---|
| Blue | ■ square |
| Red | ▲ triangle |
| Yellow | ● circle |
| Green | ✚ plus |

### 7.3 Colour sequences
- L1: all blue. L2: left all blue, right all red.
- L3: both belts mixed, from a **seeded shuffle bag** (each run of 8 has 2 of each colour; no colour 3 in
  a row on the same belt). The seed is shown on the results panel for reproducibility.

---

## 8. Sorting bins and missed zone

- `SortBin` (colour) has a **trigger volume** 0.10 m below the mouth (0.50 × 0.25 × 0.50). A parcel counts
  **once**, when its centre enters the trigger **and it is not held**. Correct: state `Sorted`, +points,
  bin flashes its colour (emission pulse 0.4 s), sparks in its colour, chime, haptic on the last-holding
  hand, a floating "+10" (or "+15 SPEED BONUS"). Wrong: state `WrongBin`, the bin flashes **red**, a
  buzzer, a double haptic, and floating **"Wrong Color!"**. After 0.6 s the parcel returns to the pool.
- Placing directly (opening the hand inside the mouth) and throwing both count.
- `MissedZone` trigger at the belt end (z < 0.25), plus the floor: `Missed` when a parcel leaves the belt
  end without being grabbed; `Dropped` when a released parcel rests on the floor for 1.5 s. Both cost −3,
  increment **Missed**, and break the streak.

---

## 9. VR rig and interactions

### 9.1 Rig
- Place a **prefab instance** of `XR Origin (XR Rig).prefab` from the XRI 3.5.1 Starter Assets at
  (0, 0, −0.15), facing +Z. Use **overrides on the instance only** (never edit the sample prefab):
  - Disable locomotion providers (move, turn, teleport, climb). The player is **stationary**; allow real
    room-scale head movement only.
  - Keep the **Near-Far Interactor** (ray + direct grab), the **Poke Interactor**, and the UI ray; the UI
    uses `TrackedDeviceGraphicRaycaster` on every world canvas.
  - Hide the controller models and parent a **new procedural glove** (`Assets/Speed/Generated/Glove_L/R`):
    a white palm and fingers, a black cuff with an orange band `#FF8A1E`. Three poses (Open, Point,
    Grip), blended from the trigger and grip input values (`GloveHandPose.cs`).
- **Mode switch (`PlatformModeSwitcher.cs`):** at start, if an XR display subsystem is running, enable the
  XR Origin and disable `DesktopRig`; otherwise disable the XR Origin and enable `DesktopRig` (section 9.5).
  Both rigs exist in the scene. Game logic never talks to input directly; it goes through `IHandInput`,
  implemented by `XRHandInput` and `MouseHandInput`.

### 9.2 Controls (from the brief)
| VR action | In-game |
|---|---|
| Point with controller | Aim at speed buttons, answers, Start / Restart (ray + reticle) |
| Trigger / finger poke | Press panel buttons and pedestal buttons |
| Grip | Grab a parcel (direct within 0.15 m, or a short-range far grab ≤ 1.2 m pulls it to the hand) |
| Move controller | Carry the parcel |
| Release grip | Drop or throw (throwOnDetach) |
| Both controllers | Hold two parcels at once |
| Turn head | Watch belts, bins, timer, and scoreboard |

### 9.3 `ParcelGrabInteractable : XRGrabInteractable` (important XRI detail)
XRGrabInteractable caches the Rigidbody's `isKinematic` when grabbed and restores it on release. Parcels
on a belt are kinematic, so a plain grab would restore kinematic and kill the throw. Fix: **override
`OnSelectEntering`**. Make the parcel leave its belt and set `rb.isKinematic = false` **before** calling
`base.OnSelectEntering(args)`. Then release restores the dynamic state and `throwOnDetach` works.
Settings: `movementType = VelocityTracking` (Instantaneous if jitter appears), `throwOnDetach = true`,
`throwVelocityScale = 1.3`, `throwSmoothingDuration = 0.25`, `attachEaseInTime = 0.08`,
`useDynamicAttach = true`. Before you rely on these names, check them against the XRI 3.5.1 source in
`Library/PackageCache/com.unity.xr.interaction.toolkit@*`.
Grabbing is only allowed while the parcel's z ≤ 1.60 (grab zone) or while it is Flying.

### 9.4 Haptics (`SendHapticImpulse` on the interacting controller)
grab 0.15 / 0.05 s • button press 0.25 / 0.04 s • correct sort 0.45 / 0.10 s • wrong bin 0.8 / 0.12 s ×2 •
missed parcel 0.3 / 0.08 s.

---

### 9.5 Desktop mode (PRIMARY for now; user decision): "mouse hand"
- Camera: fixed at the reference eye (0, 1.58, −0.15), FOV 68° (section 5.4). **Right-mouse drag** looks
  around (yaw ±60°, pitch −40°…+20°); releasing it does **not** snap back. Middle-click recentres.
- **Mouse hand:** the right white glove follows the mouse. Cast the mouse ray onto an invisible
  *hand plane* (y = belt top + 0.25 = 1.15) inside the reach volume (x −1.5…1.5, z 0.2…1.8); the glove
  eases to that point (smooth 0.05 s). The mouse wheel raises or lowers the hand ±0.30 m, so it can reach
  bin mouths. The left glove rests idle at the bottom-left, as in the image.
- **Left-click hold = grab** the nearest grabbable parcel within 0.25 m of the glove (Grip pose). Move the
  mouse to carry it. **Release = drop or throw**: the release velocity is the glove velocity averaged
  over the last 0.10 s, × 1.3, clamped to 6 m/s. Placing straight into a bin works too.
- **Left-click on a button** (SpeedBar, BeltSelector, answers, pedestal, panels) presses it. Hovering
  highlights it, and the glove switches to the Point pose.
- **Keyboard shortcuts:** `1`–`5` = speeds 0.2–1.0 m/s, `Tab` = switch active belt (L2/L3),
  `Space` = START, `R` = restart level, `Esc` = menu, `H` = toggle the help card.
- One hand on desktop: a single mouse can hold only one parcel at a time. The goals stay as in the brief;
  feasibility check V-10 must pass for one hand at 1.5 s per sort.
- The desktop rig uses the same `Parcel`, `SortBin`, and button components as VR (through `IHandInput`), so
  behaviour is identical. Haptics are replaced by a short glove shake and sound.

## 10. UI: panels, exact text, behaviour

All UI is **world-space TextMeshPro on uGUI canvases** (no screen-space HUD), in a bold rounded font (use
the TMP font already in the project, read-only; generate a new SDF asset under `Assets/Speed/Fonts/` if
needed). Headings are italic heavy, as in the image.

- **SpeedBar:** orange »»» icon + "CONVEYOR SPEED" + small "m/s", then buttons **0.2 0.4 0.6 0.8 1.0**.
  Selected = SelectBlue fill + cyan glow outline + ⏱ icon left of the number. Idle = CardNavy with a thin
  `#2A6BFF` outline. Hover = brighter outline. Under the bar, a small readout: *"Selected belt speed
  **0.6 m/s** • Time to travel 2 m **3.33 s**"*, updated live as `2 / v` to 2 decimals. When locked
  (round running): buttons dim to 40 % with a 🔒 icon. During the L1 challenge, the readout shows
  **"? m/s"**.
- **BeltSelector (L2/L3):** two tabs, `LEFT BELT • 0.4 m/s` and `RIGHT BELT • 0.8 m/s`. The active tab
  is highlighted, and the matching belt's rails pulse cyan. Pressing a speed sets the active belt only.
  Defaults: left 0.4, right 0.8 (L2); left 0.6, right 1.0 (L3).
- **LevelHeader:** tab colour per level, "LEVEL 1/2/3" + »»» chevrons; the card text is exactly as in 3.1.
- **TimerBoard:** L1 shows a stopwatch `00.00 s` that runs while the test parcel goes from A to B.
  L2/L3 show a countdown `00:60` / `01:15`, which turns red and pulses for the last 10 s.
- **ScoreBoard:** `SCORE 120`, `STREAK ×3`, `CORRECT 7 / 8`, `MISTAKES 1 / 2`, `MISSED 2`. In L3 it adds
  `LEFT 0.6 m/s • RIGHT 1.0 m/s` (as in the brief's "00:38 left" example).
- **GuideBubble:** short lines with an optional voice clip (generated beeps; leave audio slots
  `Assets/Speed/Audio/VO_*.wav` for recorded voice later).
- **ResultsPanel:** stars ★★★, score, accuracy %, speeds used, parcels per belt, the 2 m table (section 4)
  with the used speeds highlighted, the closing message, and buttons [RETRY] [NEXT LEVEL] (or [FINISH]).

---

## 11. Game flow (state machine `GameFlow`)

```
Boot → Welcome → Tutorial → L1.Explore → L1.Challenge(Q1..Q5) → L1.Results
     → L2.Setup → L2.Countdown(3-2-1) → L2.Run → L2.Results
     → L3.Setup → L3.Countdown → L3.Run → L3.Results → FinalResults
(RETRY returns to the level's Setup/Explore; MENU returns to Welcome)
```
- **Welcome:** panel "Welcome to Parcel Sort VR" + subtitle "Speed Challenge", with [START TUTORIAL] and [SKIP].
  Level 2 and Level 3 are unlocked by passing the previous level. Progress is kept in `PlayerPrefs`,
  with keys prefixed `SpeedParcelSort.` (no project file is written).
- **Tutorial** (each step ticks a checklist): ① *Point* at the glowing TEST button on the SpeedBar and
  pull the trigger (desktop: left-click it). ② *Poke* the green START dome on the pedestal with your finger
  (desktop: click it or press `Space`). ③ *Grab* the practice
  parcel on the small table at (0.30, 0.95, 0.70). ④ *Drop or throw* it into the grey practice bin at
  (−0.85, 0.95, 0.80). ⑤ The tip about straight-line motion versus gravity curves (section 4).
  Then go to Level 1.

---

## 12. Levels in detail

### LEVEL 1: Speed Observation Lab (Beginner, blue theme)
**Objective:** understand how fast an object travels along one straight conveyor.
**Setup:** one belt, blue parcels, SpeedBar + pedestal keys, markers A/B (2.00 m apart), stopwatch board,
question board.

**Explore:**
1. Guide: *"Press a speed button to see how fast the boxes move."* The belt is stopped and pre-filled.
2. The player presses any speed. The belt runs at that v. Each time a parcel's front face crosses A, the
   stopwatch starts; at B it stops and shows `2 m in 3.33 s → 0.6 m/s` on the TimerBoard.
3. The checklist asks the player to try **0.2**, then **0.6**, then **1.0** m/s. Each is ticked after one
   parcel crosses A→B at that speed.
4. Once all 3 are ticked, **START CHALLENGE** lights up on the pedestal and the question board.

**Challenge:** 5 questions, scored on answer buttons **0.2 0.4 0.6 0.8 1.0** (m/s).
- **Q1, Q3, Q5 – Observation:** the belt is cleared. One **test parcel** with a glowing outline spawns at
  z 4.90 (0.40 m before A) and runs at a **hidden** speed (drawn without replacement from
  {0.2, 0.4, 0.6, 0.8, 1.0}, seeded). The SpeedBar shows "? m/s" and is locked. The stopwatch times A→B automatically. Answers unlock once the
  parcel passes B. [WATCH AGAIN] replays it (max 2 replays). Correct answer = 2 / t.
- **Q2, Q4 – Word problems** (pick 2 per attempt, seeded, from the bank; every answer is in the set):
  "A box takes 5 seconds to move 2 m. What is its speed?" → 0.4 •
  "A box moves 4 m in 4 seconds. What is its speed?" → 1.0 •
  "A box moves 3 m in 5 seconds. What is its speed?" → 0.6 •
  "A box moves 1.6 m in 2 seconds. What is its speed?" → 0.8 •
  "A box moves 2 m in 10 seconds. What is its speed?" → 0.2 •
  "A box moves 1.2 m in 3 seconds. What is its speed?" → 0.4 •
  "Which speed covers 2 m in 2.5 seconds?" → 0.8.
- Header: `QUESTION 1 / 5` + "Observation Challenge".
- Feedback: correct gives a green flash and *"Correct! 2 m ÷ 5.00 s = 0.4 m/s"*. Wrong gives an orange
  flash and *"Not quite: 2 m ÷ 3.33 s = 0.6 m/s"*. Move to the next question after 2.5 s (no retry
  within a question).
- **Pass:** ≥ 3 / 5 correct. Score +10 per correct answer. Stars: 3/5 ★, 4/5 ★★, 5/5 ★★★.
- **Learned:** v = d / t. A higher speed means less time to travel the same distance.

### LEVEL 2: Dual Conveyor Sorting (Intermediate, green theme)
**Objective:** use both hands to sort parcels from two belts at player-chosen speeds.
**Setup:** left belt = blue only, right belt = red only; BLUE bin left, RED bin right.
1. Select LEFT BELT, press a speed (e.g. 0.4). Select RIGHT BELT, press a different speed (e.g. 0.8).
2. Press the big green **START** (pedestal or panel). A 3-2-1 countdown runs, then **speeds lock**.
3. Both belts run. Grab blue from the left into BLUE and red from the right into RED, with either hand or
   both at once.
4. The round ends when the **order is complete (8 correct)**, the **60 s** timer runs out, or **mistakes > 2**
   (fail).
- **Pass:** ≥ 8 correct within 60 s and ≤ 2 mistakes (**mistake = parcel in the wrong bin**; missed or
  dropped parcels cost points but are tracked separately; see 19.3).
- Order-complete time bonus: +2 per remaining second.

### LEVEL 3: Mixed Color Speed Rush (Advanced, purple theme)
**Objective:** sort mixed colours from two belts at different speeds into four bins.
**Setup:** both belts mixed (blue/red/yellow/green); bins BLUE + YELLOW (left) and RED + GREEN (right);
two independent speed selectors (defaults: left 0.6, right 1.0).
1. Choose both speeds → START → countdown → speeds lock.
2. Identify each parcel's colour and shape icon, grab it with either hand, and place or throw it into the
   matching bin.
3. Prioritise: a parcel near the end of the **faster** belt must be caught first. Show a subtle red pulse
   on any parcel with < 1.5 s left before the belt end ("about to fall").
4. The round ends on **12 correct**, **75 s**, or **mistakes > 3**.
- **Pass:** ≥ 12 correct in 75 s and ≤ 3 mistakes.
- Optional config flag `level3TwoBinMode` (default **false**): a mixed red/blue variant with just two
  bins, as suggested in the brief.

### Level config values (ScriptableObjects in `Assets/Speed/Data/`)
| Field | L1 | L2 | L3 |
|---|---|---|---|
| belts | 1 (centre) | 2 | 2 |
| colours | Blue | L: Blue / R: Red | both: B, R, Y, G |
| bins | — | Blue, Red | Blue, Yellow, Red, Green |
| duration | — | 60 s | 75 s |
| goal | 3 of 5 correct | 8 correct | 12 correct |
| max mistakes | — | 2 | 3 |
| default speeds (m/s) | 0.2 | 0.4 / 0.8 | 0.6 / 1.0 |
| parcel spacing | max(0.45 m, v × 1.5 s) | same | same |

---

## 13. Scoring, stars, and feedback (exact)

| Event | Points |
|---|---|
| Correct sort | **+10** |
| Correct sort of a parcel from a belt at **0.8 or 1.0 m/s** | **+5 speed bonus** |
| Every 3rd consecutive correct (streak 3, 6, 9 …) | **+10 combo** |
| Wrong bin | **−5** (streak resets; mistakes +1) |
| Parcel missed off the belt end / dropped on floor | **−3** (streak resets; missed +1) |
| L1 correct answer | +10 |
| Order complete early (L2/L3) | +2 per remaining second |

The score never goes below 0. **Stars (L2/L3):** ★ = pass; ★★ = pass + accuracy ≥ 80 %;
★★★ = pass + accuracy ≥ 90 % + average belt speed ≥ 0.6 m/s, where
`accuracy = correct / (correct + wrong + missed)`. A level-complete fanfare plays and the next level
unlocks.

**The results panel** must show: score, stars, accuracy, the speeds used, correct per belt, the average
catch window `1.30 m / v` for each belt, the 2 m table with the used speeds highlighted, and the
closing message from section 4.

---

## 14. Code architecture (all new; namespace `SpeedParcelSort`)

```
Assets/Speed/
├─ conveyor-belt/                      (EXISTING – read-only, untouched)
├─ README.md                           how to play / rebuild / validate
├─ Scripts/
│  ├─ Logic/   SpeedParcelSort.Logic.asmdef   (noEngineReferences = true, unit-testable)
│  │   SpeedMath.cs        TravelTime(d,v), SpeedFrom(d,t), CatchWindow(len,v), CmToM
│  │   BeltKinematics.cs   z(t), spawn schedule, end-of-belt time
│  │   ScoreRules.cs  ScoreLedger.cs  StarRating.cs
│  │   LevelRules.cs       goals, fail conditions, feasibility check
│  │   QuestionBank.cs  Question.cs   (seeded selection, answer = d/t, all answers ∈ {5..25})
│  │   ColorBag.cs         seeded shuffle bag, no-3-in-a-row
│  │   GameFlow.cs         pure state machine (section 11)
│  ├─ Runtime/ SpeedParcelSort.Runtime.asmdef  (refs: Logic, Unity.XR.Interaction.Toolkit,
│  │           Unity.XR.CoreUtils, Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI,
│  │           Unity.RenderPipelines.Universal.Runtime)
│  │   GameManager.cs  LevelDirector.cs  Level1Observation.cs  SortingRound.cs
│  │   ConveyorBelt.cs  BeltSurfaceScroller.cs  RollerSpinner.cs
│  │   ParcelSpawner.cs  ParcelPool.cs  Parcel.cs  ParcelGrabInteractable.cs
│  │   SortBin.cs  MissedZone.cs  FloorDropDetector.cs
│  │   SpeedPanel.cs  SpeedButton.cs  BeltSelector.cs  PedestalButton.cs
│  │   LevelHeader.cs  TimerBoard.cs  ScoreBoard.cs  QuestionBoard.cs  ResultsPanel.cs
│  │   GuideBubble.cs  WelcomePanel.cs  TutorialController.cs
│  │   FeedbackFX.cs  FloatingText.cs  HapticsService.cs  AudioService.cs
│  │   GloveHandPose.cs  PlatformModeSwitcher.cs  IHandInput.cs  XRHandInput.cs
│  │   MouseHandInput.cs  DesktopRig.cs  FacePoint.cs  SpeedTheme.cs (ScriptableObject)
│  └─ Editor/  SpeedParcelSort.Editor.asmdef  (Editor platform only)
│      SpeedSceneBuilder.cs (+ partials: .Environment .Conveyor .Parcels .Bins .Panels .Rig .Levels)
│      SpeedProceduralMeshes.cs  SpeedTextureGen.cs  SpeedAudioGen.cs  SpeedMaterials.cs
│      SpeedValidator.cs  SpeedReferenceRender.cs  SpeedIntegrityCheck.cs
├─ Tests/
│  ├─ EditMode/ SpeedParcelSort.Tests.EditMode.asmdef   (Logic tests)
│  └─ PlayMode/ SpeedParcelSort.Tests.PlayMode.asmdef   (V-tests, section 17)
├─ Data/       LevelConfig_L1/L2/L3.asset, Questions.json, SpeedTheme.asset
├─ Materials/  M_ConveyorFrame, M_BeltSurface_Scroll, M_HazardStripe, M_Parcel_{Blue,Red,Yellow,Green},
│              M_Bin_*, M_Glove, M_Cuff, M_Rack, M_Floor, M_Emissive_*, M_Sky …
├─ Generated/  procedural meshes, textures (barcode, chevron, hazard, icons, labels), glove meshes
├─ Prefabs/    ConveyorModule, Parcel, SortBin, SpeedButton, PedestalButton, Panels, Glove_L/R
├─ Audio/Generated/  click, chime, buzz, whoosh, countdown, fanfare, belt-hum loop (synthesised WAV)
├─ Fonts/      (only if a new TMP SDF asset is needed)
└─ Settings/   SpeedVolume.asset (URP volume profile)
```

**Menus:** `Tools ▸ Speed ▸ Build Parcel Sort Scene`, `… ▸ Validate Parcel Sort`,
`… ▸ Render Reference Comparison`, `… ▸ Integrity Check`, `… ▸ Batch Build` (for `-executeMethod`
`SpeedParcelSort.Editor.SpeedSceneBuilder.BatchBuild`).
The builder is **idempotent**: it clears and regenerates only `Assets/Speed/Generated/**` and the
contents of `Assets/Scenes/Speed.unity`, then saves the scene. It never touches other paths.

Key implementation rules:
- `ConveyorBelt` holds a `List<Parcel>`, a `speedCmPerSec`, and `isLocked`. `FixedUpdate` moves each `OnBelt`
  parcel to `BeltKinematics.Z(t)`. If z < 0.25, it hands the parcel to `MissedZone`.
- One `LevelDirector` turns station objects on or off per level (belts, bins, markers, panels). No scene
  reload, so the transition is seamless (fade to navy 0.3 s).
- No `FindObjectOfType` every frame; references are wired by the builder. No allocations in `FixedUpdate`.
- Each script starts with a header comment explaining its purpose.

---

## 15. Audio, VFX, performance, comfort

- **Audio** (synthesised by `SpeedAudioGen` into new WAVs): button click, correct chime (C-E-G arpeggio),
  wrong buzz (low square wave 0.25 s), whoosh on throw (speed-scaled), countdown beeps, level fanfare,
  belt hum loop. Spatial 3D audio on bins and belts; UI audio is 2D.
- **VFX:** colour sparks (URP particles, ≤ 40 particles per burst), bin emission flash, floating score text,
  red pulse on "about to fall" parcels, cyan rail pulse on the selected belt.
- **Performance target:** desktop at **60+ fps** now; keep the Quest-class budget (72 fps) so VR
  works later. ≤ 150 draw calls and ≤ 200 k triangles in
  view; GPU instancing for parcels and rack boxes; static batching for the environment; no realtime
  shadows except the sun on the station; textures ≤ 1024 for generated assets.
- **Comfort:** stationary player, no camera motion or forced head movement. All grab targets are within
  0.75 m. Large parcels and big buttons. Colour + shape icons. Text ≥ 1.2° visual angle (≥ 4 cm cap
  height at 2 m). Optional **seated mode** toggle on the welcome panel lowers the station by 0.25 m.

---

## 16. Build phases (do them in order; each has a gate)

| Phase | Work | Gate / output |
|---|---|---|
| **0. Preview** | Snapshot the integrity baseline (rule 1.7). Inspect the FBX (bounds, axes). Render a quick blockout preview of the three level views from the reference camera (section 5.4) and the top-down plan. | `Work/Speed/preview/level{1,2,3}_preview.png` + side-by-side with the reference crops. **Show the user and wait for approval.** |
| 1. Logic | `Scripts/Logic` + EditMode tests (SpeedMath, BeltKinematics, ScoreRules, StarRating, QuestionBank, ColorBag, GameFlow). | All EditMode tests green. |
| 2. Assets | Procedural meshes, textures, materials, audio, glove, `M_ConveyorFrame` from the supplied texture. | Files exist only under `Assets/Speed/`. |
| 3. Station | Conveyor tiling from the FBX + scroll overlay, rails, rollers, parcels, bins, missed chute, markers, pedestal. | Reference render L1 belt matches the image's composition. |
| 4. Environment | Hall, racks + boxes, lights, dock + truck, ZONE A sign, volume profile. | Reference renders L1-L3 look like section 3. |
| 5. VR rig | XR Origin instance + overrides, gloves + poses, interactors, `ParcelGrabInteractable`, haptics, desktop fallback. | Grab, throw, and poke work in the simulator. |
| 6. UI | SpeedBar, BeltSelector, LevelHeader/card, Timer, Score, Question, Results, Guide, Welcome, Tutorial. | All buttons are ray-reachable and wired. |
| 7. Gameplay | GameFlow, Level 1 explore + challenge, L2/L3 rounds, scoring, stars, unlocks. | A full playthrough via the AutoPlayer bot. |
| 8. Polish | VFX, audio, about-to-fall pulse, transitions, comfort options. | Performance budget met. |
| 9. Validate | Section 17 + integrity check + final reference comparisons. | `Work/Speed/validation-report.txt` all PASS. |

---

## 17. Validation (automated; `Tools ▸ Speed ▸ Validate Parcel Sort`)

| ID | Check |
|---|---|
| V-01 | Every required object exists in `Speed.unity` (belts, bins per level, panels, pedestal, rig, markers, dock, truck, sign). |
| V-02 | Every button has an XRI interactable + listener and is hit by a ray from the eye (0, 1.6, −0.15) with nothing in the way, and is clickable by the mouse in desktop mode. |
| V-03 | For v ∈ {0.2, 0.4, 0.6, 0.8, 1.0} m/s, a parcel covers 2.00 m in 2 / v s (± 0.02 s), with no drift over 60 s. |
| V-04 | Belt texture scroll speed = parcel speed (± 1 %). The chevron under a parcel stays under it for 5 s. |
| V-05 | A correct bin counts once; a wrong bin counts once; a held parcel inside the trigger does not count. |
| V-06 | A parcel past the belt end → Missed once; a parcel resting on the floor 1.5 s → Dropped once. |
| V-07 | Grab → release with velocity → the parcel is dynamic and flies (`ParcelGrabInteractable` fix works). Landing on a belt re-joins it. |
| V-08 | ScoreRules: +10, +5 speed bonus at ≥ 0.8 m/s, combo every 3rd, −5 wrong, −3 missed, floor at 0, time bonus. |
| V-09 | Every L1 question answer ∈ {0.2, 0.4, 0.6, 0.8, 1.0} and equals d / t. Hidden speeds are distinct within an attempt. |
| V-10 | Feasibility: for every speed pair, parcels reaching the grab zone within the round ≥ 1.5 × goal, and one desktop hand at 1.5 s per sort can reach the goal. |
| V-11 | Reach: every bin mouth, belt grab zone, and pedestal button is ≤ 0.75 m from the nearer shoulder. |
| V-12 | Reference renders saved, each next to its reference crop (`compare_level{N}.png`). Report palette distance per region. A human confirms the final look. |
| V-13 | Integrity: no existing file changed except `Assets/Scenes/Speed.unity` (rule 1.7). |
| V-14 | Performance: draw calls / triangles per level view within the budget (section 15). |
| V-16 | Desktop mode: with no XR device, `DesktopRig` is active, the XR Origin is off, and mouse grab/throw/click work (simulated input). With an XR display running, the XR Origin is active. |
| V-15 | An AutoPlayer bot (driving the desktop mouse hand) plays the tutorial → L3 at mixed speeds for 4 minutes with **0 console errors or warnings** from `SpeedParcelSort`. |

Also run the EditMode and PlayMode test assemblies with the Unity Test Runner (batch:
`-runTests -testPlatform EditMode|PlayMode -assemblyNames SpeedParcelSort.Tests.*`). Be honest in the
report: say that real headset tracking, controller feel, and on-device frame rate **were not tested** unless they actually were.

---

## 18. Definition of done

- `Assets/Scenes/Speed.unity` opens and plays the complete Welcome → Tutorial → L1 → L2 → L3 → Final flow
  on desktop with mouse + keyboard (primary). The XR rig is in place and switches on automatically when
  a headset is connected (headset testing comes later).
- From the default eye, each level visually matches its panel of the reference image (section 3 checklist).
- Speed physics are exact (V-03, V-04), and scoring is exact (V-08).
- Only new files under `Assets/Speed/**`, `Work/Speed/**`, and `Docs/**` exist, plus the rebuilt
  `Assets/Scenes/Speed.unity`. The integrity report shows PASS.
- `Assets/Speed/README.md` explains how to play (controls table), rebuild, validate, and what was and
  was not tested.
- Final message to the user: a short summary, the three comparison renders, the validation table, and
  any open issues.

---

## 19. Decisions and assumptions (already made; change them only if the user says so)

1. **One station, re-configured per level.** The reference image is a three-panel montage; in VR the
   player stays in one spot and the station changes (1 belt → 2 belts → 2 mixed belts + 4 bins).
2. **Speeds in m/s (user decision):** 0.2 / 0.4 / 0.6 / 0.8 / 1.0 m/s, applied literally at 1 unit = 1 m,
   with markers 2 m apart, so the A→B times (10 / 5 / 3.33 / 2.5 / 2 s) are physically true. The buttons
   therefore differ from the "5 10 15 20 25" in the reference image; everything else matches it.
3. **"Mistake" = wrong bin.** Missed and dropped parcels cost −3 and are shown separately, so a learner
   who picks 1.0 m/s is not failed just for misses. The config flag `countMissedAsMistake` (default
   false) enables a stricter rule.
4. **Level 3 uses four bins**, as in the image. `level3TwoBinMode` is available for the simpler two-bin
   variant from the brief.
5. **The image's flat overlays become world-space panels** above the belts at the same apparent places
   (speed bar on top, level header + card under it). The speed bar is ray-pressed; a waist-height
   pedestal gives physical "reach and press" buttons for START, RESTART, and the L1 speed keys.
6. **The warehouse, parcels, bins, gloves, and truck are generated procedurally** by the builder (no
   warehouse prop pack exists in the project). Only the conveyor uses the supplied FBX + texture.
7. Speeds are locked from START until the round ends, in L2/L3 and during L1 challenge questions.
8. **Desktop first, VR-ready (user decision):** the game is played with the mouse hand (9.5) now; the XR
   rig and XRI grab code are built too and switch on automatically when a headset is connected.
9. **Plan file location (user decision):** `Docs/Speed.md` (the existing Docs folder).

---

## Appendix A: Original design brief (source of truth for gameplay intent)

Game title: **Parcel Sort VR – Speed Challenge**. Concept: speed and uniform straight-line motion.
Style: VR grab & throw + observation + speed control + colour sorting. Low-poly 3D (originally planned in
Blender; here generated procedurally) + Unity VR interaction.

- **Level 1, Observe & Identify:** one belt; learn how changing the speed changes straight-line motion;
  identify the hidden speed; pass with 3 / 5.
- **Level 2, Grab & Sort:** two belts, one colour each (blue left, red right), player-set speeds, both
  hands; sort 8 in 60 s with ≤ 2 mistakes. A faster belt gives less time per catch. Unsorted parcels
  fall into a missed zone. A correct sort gives sparks, a sound, and a vibration.
- **Level 3, Mixed Parcel Rush:** two belts with mixed colours and different speeds, four bins (Blue and
  Yellow left, Red and Green right), timer, score, streak, missed counter; sort 12 in 75 s with ≤ 3
  mistakes. Decision-making: catch the parcel about to fall from the faster belt first.
- Scoring: +10 correct, +5 bonus at the two fastest speeds (originally 20/25 cm/s; here 0.8/1.0 m/s), +10 per 3-streak, −5 wrong bin, −3 fallen parcel;
  stars + unlock.
- Flow: Start → Tutorial (point, press, grab) → L1 → L2 → L3 → Results (score, speed analysis, stars).
- Rules: parcels move at constant speed; grabbable in the interaction area; once grabbed, a parcel leaves
  the belt and follows the hand while the belt keeps running; it counts only in the correct bin; parcels
  that reach the belt end are missed. Stationary player, everything within reach, large parcels,
  placing as well as throwing, a distinct icon per colour.
- Unity systems: XR Origin, constant-speed conveyor movement, XR Grab Interactable, bin trigger
  detection, VR buttons, level manager, world-space UI, distance/time observation system.