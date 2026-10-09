# Rest and Motion asset sources

- **Cabin:** `Assets/Rest_and_Motion/sci-fi-elevator/source/winda.fbx`. A mesh copy assigns the front faces to glass, and those glass faces are split into two sliding door leaves.
- **Control panel:** `Assets/Rest_and_Motion/elevator---elevator-key-5-mb/source/Elevator _ elevator key.fbx`. Each of its 15 printed button caps is cut out of a mesh copy (`Imported/LiftPanel/Cap_*.asset`) so it can physically press in and light up. Floors 10-12 reuse a copy of the supplied P button.
- **City, buildings and ground:** `Assets/Rest_and_Motion/residential_building_set.glb`, converted by `Editor/RamGlbImporter.cs`, a copy of the project's GLB reader in this game's own namespace. Buildings get pastel tint material copies.
- **Scenery (read-only prefab instances, never edited):** Synty PolygonGeneric roads, mountains, clouds and a tree; SimpleNaturePack trees, bushes and rocks.
- **Generated in the builder:** grass, river, bridge, the landing tower with one landing per floor, lamp rings, display arrows and the lesson board.

The source models, textures and their import settings are unchanged. All derived meshes and materials are in `Assets/Rest_and_Motion/Imported`.
