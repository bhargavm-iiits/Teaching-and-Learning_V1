using System;
using System.IO;
using System.Linq;
using NumberLinePlayground.Level1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Checks the freshly built scene and writes what the player would see to Logs/NumberLineValidation:
///   result.txt        PASS or the first failure
///   preview.png       the headset view from the start position (the Level 1 popup is up)
///   overview.png      a wide view from behind and above the player
///   table.png         the view from the origin ring towards the table (Level 1)
///   table_level2.png  the same with Level 2 laid out (three spheres, a triangle)
///   table_level3.png  the same with Level 3 laid out (four spheres, a square)
///   topic.png, complete.png, quiz.png, result.png   each popup as the player sees it
public static class NumberLinePlaygroundValidation
{
    const string LogFolder = "Logs/NumberLineValidation";

    [MenuItem("Tools/Number Line Playground/Validate Saved Scene")]
    public static void Validate()
    {
        Directory.CreateDirectory(LogFolder);
        try
        {
            string summary = Check();
            File.WriteAllText(LogFolder + "/result.txt", "PASS: " + summary + "\n" + DateTime.Now);
            Debug.Log("Number Line Playground validation passed.");
        }
        catch (Exception e)
        {
            File.WriteAllText(LogFolder + "/result.txt", "FAIL: " + e.Message + "\n" + DateTime.Now);
            Debug.LogError("Number Line Playground validation failed: " + e.Message);
            TryCapture();
            throw;
        }
        TryCapture();
    }

    [MenuItem("Tools/Number Line Playground/Rebuild and Validate %#j")]
    public static void Rebuild()
    {
        NumberLinePlaygroundBuilder.Build();
        Validate();
    }

    static void Need(UnityEngine.Object o, string name)
    {
        if (o == null) throw new Exception("Missing reference: " + name);
    }

    static void Fail(string message) => throw new Exception(message);

    static string Check()
    {
        var root = GameObject.Find("NumberLinePlayground");
        if (root == null) Fail("Playground scene is not open.");
        if (root.GetComponent<NumberLineTracker>() != null) Fail("The old coin-hunt tracker is still on the playground root.");
        var c = root.GetComponent<LineLevelController>();
        if (c == null) Fail("LineLevelController is missing from the playground root.");
        var flow = root.GetComponent<ConceptFlow>();
        if (flow == null) Fail("ConceptFlow is missing from the playground root.");
        if (root.GetComponent<GrabComfort>() == null) Fail("GrabComfort is missing; spheres could only be held while a button is held down.");

        // ---- everything wired ----
        foreach (var pair in new (UnityEngine.Object, string)[]
        {
            (c.root, "level root"), (c.head, "level head"), (c.machineTitle, "machineTitle"), (c.machineCount, "machineCount"),
            (c.ringIdleMaterial, "ringIdleMaterial"), (c.ringArmedMaterial, "ringArmedMaterial"),
            (c.feetLabel, "feetLabel"), (c.tripPathLabel, "tripPathLabel"), (c.tripDisplacementLabel, "tripDisplacementLabel"),
            (c.toastBox, "toastBox"), (c.toastText, "toastText"), (c.positionText, "positionText"), (c.confetti, "confetti"),
            (c.table, "table"), (c.post, "post (under the raised spot)"), (flow.slideHint, "slideHint"),
            (c.machineNote, "machineNote"), (c.splash, "splash"), (c.markObject, "markObject"), (c.markButton, "markButton"),
            (c.markText, "markText"), (c.carryPoint, "carryPoint (where a sphere picked up with G rides)"),
            (c.promptBox, "promptBox (what G does now)"), (c.promptText, "promptText"),
            (flow.level, "flow.level"), (flow.root, "flow.root"), (flow.rig, "flow.rig (the XR Origin)"), (flow.head, "flow.head"),
            (flow.fade, "flow.fade"), (flow.popup, "flow.popup"),
            (flow.topicPanel, "topicPanel"), (flow.slidePanel, "slidePanel"), (flow.slideHeader, "slideHeader"), (flow.slideBody, "slideBody"),
            (flow.startButton, "startButton"), (flow.completePanel, "completePanel"), (flow.completeTitle, "completeTitle"),
            (flow.completeFigure, "completeFigure"), (flow.completePath, "completePath"), (flow.completeDisplacement, "completeDisplacement"),
            (flow.completeStars, "completeStars"), (flow.completeLesson, "completeLesson"), (flow.continueButton, "continueButton"),
            (flow.quizPanel, "quizPanel"), (flow.quizHeader, "quizHeader"), (flow.quizPrompt, "quizPrompt"), (flow.quizFeedback, "quizFeedback"),
            (flow.resultPanel, "resultPanel"), (flow.resultTitle, "resultTitle"), (flow.resultMessage, "resultMessage"),
            (flow.resultButton, "resultButton"), (flow.resultButtonText, "resultButtonText"),
        })
            Need(pair.Item1, pair.Item2);
        if (flow.level != c) Fail("The flow drives a different level controller.");
        if (c.spheres == null || c.spheres.Length != 4 || c.sockets == null || c.sockets.Length != 4)
            Fail("The scene needs four sphere slots and four spot slots (Level 3 uses all of them).");
        if (c.blueprintBars == null || c.blueprintBars.Length != 4 || c.weldBars == null || c.weldBars.Length != 4)
            Fail("The table needs four blueprint bars and four weld bars (a square has four edges).");
        if (flow.optionButtons == null || flow.optionButtons.Length != 4 || flow.optionTexts == null || flow.optionTexts.Length != 4)
            Fail("A question needs four option buttons.");
        if (c.leftArrow == null || c.leftArrow.Length == 0 || c.rightArrow == null || c.rightArrow.Length == 0)
            Fail("The ring needs its left and right arrows.");
        if (flow.slideChips == null || flow.slideChips.Length != 3) Fail("The Level popup needs its three reminder chips.");
        foreach (var chip in flow.slideChips) Need(chip, "slide chip");
        foreach (var button in flow.optionButtons) Need(button, "option button");
        foreach (var text in flow.optionTexts) Need(text, "option text");

        // ---- the number line ----
        var line = root.transform.Find("NumberLine");
        if (line == null) Fail("Missing NumberLine.");
        if (line.GetComponentsInChildren<TextMesh>().Length != Concept.LineHalfLength + 1) Fail("Expected a number-line label at every second number from -" + Concept.LineHalfLength + " to +" + Concept.LineHalfLength + ".");
        foreach (var label in line.GetComponentsInChildren<TextMesh>())
            if (label.GetComponent<Billboard>() != null) Fail("A fixed number-line label has a billboard: " + label.name);
        for (int n = -Concept.LineHalfLength; n <= Concept.LineHalfLength; n++)
            if (n != 0 && line.Find("Tick_" + n) == null) Fail("Missing tick at " + n);
        var ground = line.GetComponent<GroundLabels>();
        if (ground == null || ground.labels == null || ground.labels.Length != Concept.LineHalfLength + 1)
            Fail("The number line needs GroundLabels for all " + (Concept.LineHalfLength + 1) + " ground numbers, so they read upright as you walk along the line.");
        if (ground.head != c.head) Fail("GroundLabels follows a different camera from the level.");
        if (root.transform.Find("Coins") != null || root.transform.Find("NumberLine/Box_0") != null)
            Fail("Old coin-hunt pieces are still in the scene.");
        // A sphere in the hand or on the table must be small enough not to fill the view or hide its ring.
        if (c.carryScale > 0.4f) Fail("A carried sphere is too big (carryScale " + c.carryScale + ").");
        if (FetchSphere.PlacedScale > 0.7f) Fail("A placed sphere covers its ring.");

        // ---- the origin ring and its flag ----
        if (c.originRing == null || c.originRing.Length != 48) Fail("The origin ring should have 48 segments.");
        var ring = root.transform.Find("OriginRing");
        if (ring == null || ring.localPosition.x != 0f || ring.localPosition.z != 0f) Fail("The origin ring should sit exactly at x = 0.");
        if (root.transform.Find("ZeroFlag") == null) Fail("Missing zero flag.");

        // ---- the sphere slots ----
        foreach (var s in c.spheres)
        {
            Need(s.body, "sphere body");
            Need(s.tagText, "sphere tag text");
            Need(s.tagBacking, "sphere tag backing");
            Need(s.marker, "sphere marker");
            if (s.markerParts == null || s.markerParts.Length < 2) Fail("A sphere's marker needs its beam and ring.");
            if (s.negativeMarker == null || s.positiveMarker == null || s.negativeTag == null || s.positiveTag == null)
                Fail("A sphere needs both colours of marker and tag, so it can change sign between levels.");
            Need(s.GetComponent<SphereCollider>(), "sphere collider");
            var grab = s.GetComponent<XRGrabInteractable>();
            Need(grab, "grab interactable");
            if (grab.throwOnDetach) Fail("A released sphere must not fly off.");
            if (grab.farAttachMode != UnityEngine.XR.Interaction.Toolkit.Attachment.InteractableFarAttachMode.Near)
                Fail("A sphere would stay at ray length instead of coming to the hand.");
            var body = s.GetComponent<Rigidbody>();
            if (body == null || !body.isKinematic) Fail("A sphere should have a kinematic rigidbody.");
            if (s.GetComponentInChildren<Billboard>(true) == null) Fail("A sphere has no floating tag.");
        }

        // ---- the levels: every set of numbers, every layout ----
        Transform surface = c.sockets[0].transform.parent;
        if (surface == null || c.sockets.Any(sk => sk.transform.parent != surface)) Fail("The spots must all sit on the table surface.");
        if (Concept.Levels.Length != 3) Fail("The topic has three levels.");
        for (int i = 0; i < Concept.Levels.Length; i++)
        {
            var level = Concept.Levels[i];
            if (level.number != i + 1) Fail("Level " + (i + 1) + " is numbered " + level.number + ".");
            if (level.Count != i + 2) Fail("Level " + level.number + " should use " + (i + 2) + " spheres (line, triangle, square).");
            for (int v = 0; v < level.variants.Length; v++)
            {
                var values = level.Values(v);
                if (values.Length != level.Count) Fail($"Level {level.number} set {v} has {values.Length} numbers for {level.Count} spots.");
                if (values.Distinct().Count() != values.Length) Fail($"Level {level.number} set {v} repeats a number.");
                if (values.Any(x => x == 0 || Mathf.Abs(x) > Concept.MaxNumber)) Fail($"Level {level.number} set {v}: numbers must be whole, not 0, and within -{Concept.MaxNumber} to +{Concept.MaxNumber}.");
                for (int k = 1; k < values.Length; k++)
                    if (values[k] - values[k - 1] < 2) Fail($"Level {level.number} set {v}: two spheres would sit too close on the line.");
                if (level.pairRule && !PairRule.IsSquareSet(values))
                    Fail($"Level {level.number} set {v}: {string.Join(", ", values)} do not make exactly two pairs a side apart.");
                if (level.HintFor(v).Contains("{")) Fail($"Level {level.number} set {v}: the hint still has a {{d}} in it.");
            }
            for (int a = 0; a < level.layout.Length; a++)
            {
                Vector2 p = level.layout[a];
                if (Mathf.Abs(p.x) > 1.9f || p.y < -0.9f || p.y > 0.85f) Fail($"Level {level.number}: spot {a} is off the working part of the table.");
                for (int b = a + 1; b < level.layout.Length; b++)
                    if (Vector2.Distance(p, level.layout[b]) < 0.8f) Fail($"Level {level.number}: spots {a} and {b} are too close together.");
                // Standing in the ring, as near to the table as its collider allows, every spot must be within reach
                // (by hand, or by pointing at it, which also places a sphere).
                float lift = level.LiftAt(a);
                Vector3 local = root.transform.InverseTransformPoint(surface.TransformPoint(new Vector3(p.x, TableSocket.RestHeight + 0.3f + lift, p.y)));
                float distance = Vector2.Distance(new Vector2(Mathf.Clamp(local.x, -0.6f, 0.6f), 0.65f), new Vector2(local.x, local.z));
                if (distance > 2.05f) Fail($"Level {level.number}: spot {a} is {distance:F2} m from the nearest place to stand.");
                // A sphere carried with G rides 0.28 m to the right and 0.8 m ahead of the head. The head that puts it right
                // over the spot (the back row of the square is the far one) must still count as being at the origin, or the
                // sphere cannot be dropped there: this once left the back row impossible to fill.
                if (!c.IsInZone(new Vector3(local.x - 0.28f, 0f, local.z - 0.8f)))
                    Fail($"Level {level.number}: standing where a carried sphere reaches spot {a} ({local.x - 0.28f:F2}, {local.z - 0.8f:F2}) is outside the placing zone.");                // Where a placed sphere sits: waist to chest height for the spots on the table, up to the chest on the post.
                float highest = lift > 0f ? 1.8f : 1.55f;
                if (local.y < 0.9f || local.y > highest) Fail($"Level {level.number}: a sphere in spot {a} would sit at a height of {local.y:F2} m.");
                if (lift < 0f || lift > 0.4f) Fail($"Level {level.number}: spot {a} is raised by {lift:F2} m; a post that tall is not reachable.");
            }
        }
        // Level 3 is the square from the reference: the spots carry no numbers, and the four numbers make two pairs a
        // side apart, one pair for the front edge and one for the back edge.
        var square = Concept.Levels[2];
        if (!square.pairRule || square.rows == null || square.rows.Length != 2) Fail("Level 3 should have the pair rule with two edges.");
        if (!square.Values(0).SequenceEqual(new[] { -8, -4, 2, 6 }) || square.SideLength(0) != 4) Fail("Level 3 should start with -8, -4, +2 and +6, side 4 m.");
        if (square.chips[2] != "Match the distance") Fail("Level 3's third reminder should read \"Match the distance\".");
        if (!square.markTool) Fail("Level 3 should offer the mark tool on replays.");
        if (!square.hint.Contains("{d}")) Fail("Level 3's hint should give the side of the square.");
        if (Concept.Levels[0].pairRule || Concept.Levels[1].pairRule || Concept.Levels[0].markTool || Concept.Levels[1].markTool)
            Fail("Only Level 3 has the pair rule and the mark tool.");
        var edgeSpots = square.rows.SelectMany(r => r).OrderBy(x => x).ToArray();
        if (!edgeSpots.SequenceEqual(new[] { 0, 1, 2, 3 })) Fail("Level 3's two edges should use each of the four spots once.");
        foreach (var edge in square.rows)
        {
            if (edge.Length != 2 || Vector2.Distance(square.layout[edge[0]], square.layout[edge[1]]) > 1.5f)
                Fail("The two spots of a Level 3 edge should be neighbours on the table.");
        }
        // Level 2 is the triangle from the reference: -6, +2 and +9 (three trips of 12, 4 and 18 metres, 34 m in all), the
        // top corner on a post, and the facing check at the ring.
        var triangle = Concept.Levels[1];
        if (!triangle.Values(0).SequenceEqual(new[] { -6, 2, 9 })) Fail("Level 2 should start with -6, +2 and +9.");
        if (!triangle.headingGate) Fail("Level 2 should ask the player to face the way they will go.");
        if (Enumerable.Range(0, triangle.Count).Count(k => triangle.LiftAt(k) > 0f) != 1 || triangle.LiftAt(1) <= 0f)
            Fail("Level 2 should raise exactly one spot, the top of the triangle, which is the middle number.");
        if (Concept.Levels[0].headingGate) Fail("Level 1 does not ask for the facing check.");
        if (!triangle.tallBeams) Fail("Level 2 has far spheres and needs tall beams.");
        // Each level laid out for real: the right spheres shown, on their numbers, the spots stamped to match.
        for (int i = 0; i < Concept.Levels.Length; i++)
        {
            var level = Concept.Levels[i];
            for (int variant = 0; variant < 2; variant++)
            {
                c.ApplyLayout(level, variant);
                var values = level.Values(variant);
                for (int k = 0; k < 4; k++)
                {
                    bool used = k < values.Length;
                    if (c.spheres[k].gameObject.activeSelf != used) Fail($"Level {level.number}: sphere slot {k} should be {(used ? "shown" : "hidden")}.");
                    if (c.sockets[k].gameObject.activeSelf != used) Fail($"Level {level.number}: spot slot {k} should be {(used ? "shown" : "hidden")}.");
                    if (c.spheres[k].marker.gameObject.activeSelf != used) Fail($"Level {level.number}: marker {k} should be {(used ? "shown" : "hidden")}.");
                    if (!used) continue;
                    if (c.spheres[k].value != values[k]) Fail($"Level {level.number}: sphere {k} carries {c.spheres[k].value}, not {values[k]}.");
                    float x = root.transform.InverseTransformPoint(c.spheres[k].transform.position).x;
                    if (Mathf.Abs(x - values[k]) > 0.01f) Fail($"Level {level.number}: sphere {values[k]} waits at x = {x}.");
                    float markerX = root.transform.InverseTransformPoint(c.spheres[k].marker.position).x;
                    if (Mathf.Abs(markerX - values[k]) > 0.01f) Fail($"Level {level.number}: the marker of sphere {values[k]} is at x = {markerX}.");
                    if (c.sockets[k].expectedValue != values[k]) Fail($"Level {level.number}: spot {k} is stamped {c.sockets[k].expectedValue}, not {values[k]}.");
                    string wantStamp = level.pairRule ? "?" : Concept.Signed(values[k]);
                    if (c.sockets[k].stamp.text != wantStamp) Fail($"Level {level.number}: spot {k} shows \"{c.sockets[k].stamp.text}\", not \"{wantStamp}\".");
                    if (c.spheres[k].tagText.text != "x = " + Concept.Signed(values[k]) + " m") Fail($"Level {level.number}: tag {k} reads \"{c.spheres[k].tagText.text}\".");
                    Vector2 laid = new Vector2(c.sockets[k].transform.localPosition.x, c.sockets[k].transform.localPosition.z);
                    if (Vector2.Distance(laid, level.layout[k]) > 0.001f) Fail($"Level {level.number}: spot {k} is not where the layout puts it.");
                    float wantHeight = TableSocket.RestHeight + level.LiftAt(k);
                    if (Mathf.Abs(c.sockets[k].transform.localPosition.y - wantHeight) > 0.001f) Fail($"Level {level.number}: spot {k} is at the wrong height.");
                    if (c.spheres[k].pillar.gameObject.activeSelf != level.tallBeams) Fail($"Level {level.number}: the tall beam of sphere {k} should be {(level.tallBeams ? "shown" : "hidden")}.");
                }
                if (c.machineTitle.text != level.machineTitle) Fail($"Level {level.number}: the machine says \"{c.machineTitle.text}\".");
                string wantNote = level.pairRule ? "side d = " + level.SideLength(variant) + " m" : "";
                if (c.machineNote.text != wantNote) Fail($"Level {level.number}: the machine's note says \"{c.machineNote.text}\", not \"{wantNote}\".");
                if (c.markButton.gameObject.activeSelf || c.markObject.gameObject.activeSelf) Fail("The mark tool should be hidden until a replay offers it.");
                if (c.machineCount.text != "0 / " + values.Length) Fail($"Level {level.number}: the machine says \"{c.machineCount.text}\".");
                bool anyRaised = Enumerable.Range(0, values.Length).Any(k => level.LiftAt(k) > 0f);
                if (c.post.gameObject.activeSelf != anyRaised) Fail($"Level {level.number}: the post should be {(anyRaised ? "standing" : "hidden")}.");
                foreach (var arrow in c.leftArrow.Concat(c.rightArrow))
                    if (arrow.gameObject.activeSelf != level.headingGate) Fail($"Level {level.number}: the ring arrows should be {(level.headingGate ? "shown" : "hidden")}.");
                int edges = values.Length < 3 ? 1 : values.Length;
                for (int e = 0; e < 4; e++)
                {
                    if (c.blueprintBars[e].gameObject.activeSelf != (e < edges)) Fail($"Level {level.number}: blueprint bar {e} is {(e < edges ? "missing" : "left over")}.");
                    if (c.weldBars[e].gameObject.activeSelf) Fail("The welded figure should be hidden until the level is done.");
                }
            }
        }
        c.ApplyLayout(Concept.Levels[0], 0);       // back to what the scene is saved as

        // Scripts wake up in no fixed order, and the flow configures the level after they have all woken; but a sphere
        // reset before it has woken must still land on the playground (it once went to world (-4, 0.9, 0), under the ground).
        // (Laying the levels out above already made each sphere remember its root, so forget it again to mimic a sphere
        // that has not woken yet.)
        var homeParentField = typeof(FetchSphere).GetField("homeParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (homeParentField == null) Fail("FetchSphere no longer has a homeParent field, so the reset-before-wake check cannot run.");
        foreach (var s in c.spheres.Where(sp => sp.gameObject.activeSelf))
        {
            homeParentField.SetValue(s, null);
            Vector3 before = s.transform.localPosition;
            s.ResetToHome();
            if (s.transform.parent != root.transform || (s.transform.localPosition - before).magnitude > 0.01f)
                Fail("Resetting sphere " + s.value + " before it has woken takes it off the playground: " + s.transform.position);
        }

        // ---- the table ----
        var table = root.transform.Find("Table");
        if (table == null) Fail("Missing table.");
        if (table.GetComponentInChildren<BoxCollider>() == null) Fail("The table needs a solid collider so the player stops at it.");
        foreach (var socket in c.sockets)
        {
            Need(socket.anchor, "socket anchor");
            Need(socket.stamp, "socket stamp");
            if (socket.ring == null || socket.ring.Length == 0) Fail("A spot has no ring.");
        }

        // ---- the player ----
        if (c.head.GetComponent<Camera>() == null) Fail("controller.head is not a camera.");
        if (Camera.main == null) Fail("No camera is tagged MainCamera.");
        if (flow.head != c.head) Fail("The flow and the level follow different heads.");
        if (!c.head.IsChildOf(flow.rig)) Fail("The camera is not part of the XR Origin that the flow moves back to the start.");
        Vector3 start = root.transform.InverseTransformPoint(c.head.position);
        if (Mathf.Abs(start.x) > 9.5f || start.z < -9f || start.z > 0f) Fail("The player starts outside the activity ground: " + start);
        if (new Vector2(start.x, start.z).magnitude <= c.zoneRadius) Fail("The player must start outside the ring; the first trip begins with walking to the origin.");
        if (Vector2.Distance(new Vector2(start.x, start.z), new Vector2(flow.startLocal.x, flow.startLocal.z)) > 0.6f)
            Fail("The starting point the flow returns to is not where the player starts: " + flow.startLocal + " vs " + start);
        if (UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(FindObjectsSortMode.None).Length == 0)
            Fail("No XR interactors in the scene; the controllers cannot grab anything.");
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            Fail("No XRInteractionManager in scene; the controllers' ray interactors have nothing to register with.");
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            Fail("No EventSystem in scene; the buttons' clicks would never register.");

        // ---- the popups ----
        var canvas = flow.popup.GetComponent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            Fail("Panels should be on a world-space canvas (so the player's hands can occlude it).");
        if (canvas.worldCamera == null) Fail("The world-space canvas needs its worldCamera set for click raycasting to work.");
        if (canvas.GetComponent<NumberLinePlayground.Flow.PopupManager>() == null)
            Fail("The world-space canvas needs a PopupManager to follow the player's gaze.");
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            Fail("The canvas lost its plain GraphicRaycaster; mouse clicks (Simulator, Editor testing) would stop working.");
        if (canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
            Fail("The canvas needs a TrackedDeviceGraphicRaycaster so controller rays can hit it.");
        foreach (var panel in new[] { flow.topicPanel, flow.slidePanel, flow.completePanel, flow.quizPanel, flow.resultPanel })
            if (panel.GetComponent<CanvasGroup>() == null) Fail("Popup " + panel.name + " needs a CanvasGroup to fade.");

        // Play, then the topic name, then the Level 1 popup: the flow shows the topic first.
        if (flow.topicPanel.activeSelf) Fail("The topic popup should be hidden in the saved scene; the flow shows it when Play starts.");
        var topicText = flow.topicPanel.GetComponentInChildren<Text>(true);
        if (topicText == null || topicText.text.Replace("\n", " ") != Concept.TopicName)
            Fail("The topic popup should read \"" + Concept.TopicName + "\".");
        if (topicText.color != Color.black || topicText.fontStyle != FontStyle.Bold || topicText.fontSize < 100)
            Fail("The topic name should be big, bold and black.");
        if (!flow.slidePanel.activeSelf) Fail("The Level 1 popup should be up in the saved scene.");
        if (flow.slideHeader.text != "Level 1") Fail("The popup should be headed \"Level 1\".");
        if (flow.slideBody.text != Concept.Levels[0].task) Fail("The Level 1 popup should say what to do.");
        for (int k = 0; k < 3; k++)
            if (flow.slideChips[k].text != Concept.Levels[0].chips[k]) Fail("The Level 1 popup's reminder chips are not Level 1's.");
        if (!flow.startButton.interactable || !flow.startButton.gameObject.activeInHierarchy) Fail("START should be visible and clickable.");
        foreach (var panel in new[] { flow.completePanel, flow.quizPanel, flow.resultPanel })
            if (panel.activeSelf) Fail("Popup " + panel.name + " should stay hidden until its turn.");
        if (c.toastBox.activeSelf) Fail("The toast should stay hidden until there is something to say.");
        if (c.promptBox.activeSelf) Fail("The G prompt should stay hidden until a level is being played.");
        if (c.confetti.isPlaying) Fail("Confetti should not be playing before the level is complete.");
        if (flow.fade.veilCanvas.activeSelf || flow.fade.veil.color.a > 0f) Fail("The black veil should be clear.");
        if (flow.optionTexts.Any(t => t.text.Length > 56)) Fail("An option is too long for its button.");
        if (!EditorBuildSettings.scenes.First(s => s.enabled).path.EndsWith("NumberLinePlayground.unity"))
            Fail("Wrong startup scene.");

        // ---- the rules of the flow itself ----
        if (RetryRules.FirstLevelToReplay(0, 3) != 0 || RetryRules.FirstLevelToReplay(1, 3) != 3 ||
            RetryRules.FirstLevelToReplay(2, 3) != 2 || RetryRules.FirstLevelToReplay(3, 3) != 1)
            Fail("Retry rules: 1 wrong should replay Level 3, 2 wrong Level 2, 3 wrong Level 1, none goes on.");
        for (int q = 0; q < QuizBank.QuestionCount; q++)
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var question = QuizBank.Get(q, attempt);
                if (question.options.Length != 4 || question.options.Distinct().Count() != 4) Fail("Question " + (q + 1) + " needs four different options.");
                if (question.options.Any(o => o.Length > 52)) Fail("Question " + (q + 1) + " has an option that is too long for a button.");
            }

        return "old coin hunt removed; number line from -10 to +10 with its labels and ticks; origin ring and flag with left and right arrows; " +
               "four sphere and four spot slots, " +
               "each of the three levels (line, triangle, square) laid out and checked with two sets of numbers, Level 2 as in the " +
               "reference (-6, +2, +9, top corner on a post, tall beams, facing check), Level 3 as in the reference (unlabelled spots, " +
               "-8, -4, +2, +6 with side 4 m, every set of numbers makes exactly two pairs a side apart, mark tool for replays); " +
               "spheres survive a reset before they wake; table with spots within reach; player starts outside the ring and the " +
               "flow returns to that point; topic popup (big, bold, black), Level 1 popup with START, completion, quiz and result " +
               "popups wired; black veil clear; retry rules (1 wrong: Level 3, 2 wrong: Level 2, 3 wrong: Level 1); " +
               "world-space gaze-following canvas with both raycasters, EventSystem, XR interaction manager and interactors.";
    }

    // ---------- pictures ----------

    static void TryCapture()
    {
        try
        {
            var main = Camera.main;
            if (main == null) return;
            Render(main, LogFolder + "/preview.png");

            var root = GameObject.Find("NumberLinePlayground");
            if (root == null) return;
            var level = root.GetComponent<LineLevelController>();
            var flow = root.GetComponent<ConceptFlow>();

            // Each popup, alone, as the player would see it.
            if (flow != null)
            {
                bool slideWasUp = flow.slidePanel.activeSelf;
                foreach (var shot in new[] { ("topic", flow.topicPanel), ("complete", flow.completePanel), ("quiz", flow.quizPanel), ("result", flow.resultPanel) })
                {
                    flow.slidePanel.SetActive(false);
                    shot.Item2.SetActive(true);
                    try { Render(main, LogFolder + "/" + shot.Item1 + ".png"); }
                    finally { shot.Item2.SetActive(false); }
                }
                flow.slidePanel.SetActive(slideWasUp);

                // The Level popup for Levels 2 and 3 (their extra line and reminder chips), then back to Level 1's.
                foreach (int number in new[] { 2, 3, 1 })
                {
                    var config = Concept.Levels[number - 1];
                    flow.slideHeader.text = "Level " + number;
                    flow.slideBody.text = config.task;
                    flow.slideHint.text = config.HintFor(0);
                    for (int k = 0; k < 3; k++) flow.slideChips[k].text = config.chips[k];
                    if (number != 1 && slideWasUp) Render(main, LogFolder + "/slide_level" + number + ".png");
                }
            }

            var go = new GameObject("__ValidationCamera");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.CopyFrom(main);
                cam.tag = "Untagged";

                // Wide view from behind and above the start position.
                cam.fieldOfView = 62f;
                go.transform.SetPositionAndRotation(root.transform.TransformPoint(new Vector3(0f, 4.6f, -10.5f)), root.transform.rotation);
                go.transform.LookAt(root.transform.TransformPoint(new Vector3(0f, 0.7f, 1.0f)), Vector3.up);
                Render(cam, LogFolder + "/overview.png");

                // Standing in the ring with the table ahead, for each level.
                cam.fieldOfView = 75f;
                go.transform.position = root.transform.TransformPoint(new Vector3(0f, 1.55f, 0.15f));
                go.transform.LookAt(root.transform.TransformPoint(new Vector3(0f, 0.95f, 1.6f)), Vector3.up);
                Render(cam, LogFolder + "/table.png");
                if (level != null)
                {
                    foreach (int number in new[] { 2, 3 })
                    {
                        level.ApplyLayout(Concept.Levels[number - 1], 0);
                        Render(cam, LogFolder + "/table_level" + number + ".png");
                    }
                    level.ApplyLayout(Concept.Levels[0], 0);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        catch (Exception e) { Debug.LogWarning("Number Line Playground: could not capture a preview picture: " + e.Message); }
    }

    static void Render(Camera camera, string path)
    {
        var old = camera.targetTexture;
        var active = RenderTexture.active;
        var rt = new RenderTexture(1536, 1024, 24);
        var image = new Texture2D(1536, 1024, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = old;
            RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
