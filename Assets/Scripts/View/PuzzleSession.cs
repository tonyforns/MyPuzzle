using System.Collections;
using System.Collections.Generic;
using MyPuzzle.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyPuzzle.View
{
    public sealed class PuzzleSession : MonoBehaviour
    {
        [SerializeField] PuzzleSettings settings;
        [SerializeField] Texture2D image;
        [SerializeField] Material pieceMaterial;
        [SerializeField] int seed = 1;

        readonly List<Mesh> meshes = new List<Mesh>();
        readonly Dictionary<CellId, PieceView> pieces = new Dictionary<CellId, PieceView>();
        Material faceMaterial;
        Material rimMaterial;
        Material shadowMaterial;
        Material frameMaterial;
        Material referenceMaterial;
        Texture2D whiteTexture;
        Texture2D shadowTexture;
        MeshRenderer referenceRenderer;
        MeshRenderer completeRenderer;
        Material completeMaterial;
        PuzzleSettings activeSettings;
        Texture2D fallbackImage;
        Texture2D sourceImage;
        Texture2D boardImage;
        Texture2D ownedImage;
        string sourcePath = string.Empty;
        Coroutine presentation;
        PuzzleState state;
        float snapDistance;
        int draggedGroupId = -1;
        int topSortingOrder;
        bool hasRotateTarget;
        CellId rotateTarget;
        Vector2 grabOffset;

        public event System.Action RoundBegan;

        public event System.Action RoundCompleted;

        public event System.Action ReferenceChanged;

        public bool ReferenceVisible => referenceRenderer != null && referenceRenderer.enabled;

        public PuzzleSettings ActiveSettings => activeSettings;

        public Texture2D ActiveImage => sourceImage != null ? sourceImage : image;

        public Texture2D FallbackImage => fallbackImage != null ? fallbackImage : image;

        public int ActiveSeed => seed;

        void Awake()
        {
            fallbackImage = image;
        }

        public bool Pick(Vector2 worldPoint)
        {
            if (state == null || state.Phase == SessionPhase.Completo || !TryFindGroup(worldPoint, out var group, out var cell))
                return false;

            draggedGroupId = group.Id;
            rotateTarget = cell;
            hasRotateTarget = true;
            grabOffset = new Vector2(group.Pose.X - worldPoint.x, group.Pose.Y - worldPoint.y);
            BringToFront(group);
            return true;
        }

        public void Drag(Vector2 worldPoint)
        {
            if (state == null || state.Phase == SessionPhase.Completo || draggedGroupId < 0)
                return;

            var group = FindGroup(draggedGroupId);
            if (group == null)
                return;

            var desired = new MyPuzzle.Core.Pose(
                worldPoint.x + grabOffset.x,
                worldPoint.y + grabOffset.y,
                group.Pose.QuarterTurns);
            MoveGroup(group.Id, ClampToCamera(group, desired));
        }

        public void Release()
        {
            if (state != null && draggedGroupId >= 0 && state.Phase != SessionPhase.Completo)
            {
                var group = FindGroup(draggedGroupId);
                var marker = group != null ? group.Anchor : default(CellId);
                state = SnapSystem.Resolve(state, draggedGroupId, snapDistance);
                if (group != null)
                {
                    var joined = FindGroupContaining(marker);
                    if (joined != null)
                        BringToFront(joined);
                }

                if (state.Phase == SessionPhase.Completo)
                {
                    RoundCompleted?.Invoke();
                    presentation = StartCoroutine(PresentComplete());
                }
            }

            draggedGroupId = -1;
        }

        public void RotateHeld(int steps = 1)
        {
            if (state == null || state.Phase == SessionPhase.Completo)
                return;

            GroupState group = null;
            if (draggedGroupId >= 0)
                group = FindGroup(draggedGroupId);
            else if (hasRotateTarget)
                group = FindGroupContaining(rotateTarget);

            if (group == null)
                return;

            TurnGroup(group, steps);
        }

        public void RotateAt(Vector2 worldPoint)
        {
            if (state == null || state.Phase == SessionPhase.Completo)
                return;

            if (draggedGroupId >= 0)
            {
                RotateHeld();
                return;
            }

            if (!TryFindGroup(worldPoint, out var group, out var cell))
                return;

            rotateTarget = cell;
            hasRotateTarget = true;
            TurnGroup(group, 1);
        }

        public bool TryPieceAt(Vector2 worldPoint, out CellId cell)
        {
            cell = default;
            if (state == null)
                return false;

            return TryFindGroup(worldPoint, out _, out cell);
        }

        public void DetachAt(Vector2 worldPoint)
        {
            if (!TryPieceAt(worldPoint, out var cell))
                return;

            rotateTarget = cell;
            hasRotateTarget = true;
            DetachHeld();
        }

        public void DetachHeld()
        {
            if (state == null || state.Phase == SessionPhase.Completo || !hasRotateTarget)
                return;

            var group = FindGroupContaining(rotateTarget);
            if (group == null || group.Members.Count < 2)
                return;

            var cell = rotateTarget;
            var oldPose = group.Pose;
            var oldId = group.Id;
            var cellSize = state.Definition.CellSize;
            state = GroupOperations.Detach(state, cell);
            var detached = FindGroupContaining(cell);
            if (detached == null)
                return;

            if (draggedGroupId == oldId)
            {
                draggedGroupId = detached.Id;
                grabOffset = new Vector2(
                    detached.Pose.X - (oldPose.X - grabOffset.x),
                    detached.Pose.Y - (oldPose.Y - grabOffset.y));
            }
            else
            {
                state = GroupOperations.Move(
                    state,
                    detached.Id,
                    new MyPuzzle.Core.Pose(
                        detached.Pose.X + (cellSize * 0.5f),
                        detached.Pose.Y + (cellSize * 0.5f),
                        detached.Pose.QuarterTurns));
                detached = FindGroupContaining(cell);
            }

            rotateTarget = cell;
            hasRotateTarget = true;
            if (detached != null)
                BringToFront(detached);
        }

        void TurnGroup(GroupState group, int steps)
        {
            state = GroupOperations.Rotate(state, group.Id, steps);
            BringToFront(group);
        }

        public void MoveGroup(int groupId, MyPuzzle.Core.Pose destination)
        {
            if (state == null)
                return;

            state = GroupOperations.Move(state, groupId, destination);
        }

        void Start()
        {
            if (settings == null || image == null || pieceMaterial == null)
            {
                Debug.LogError("PuzzleSession needs a preset, a test image, and a piece material.");
                return;
            }

            Begin(settings, image, seed, ownImage: false, imagePath: string.Empty);
        }

        public void Shuffle()
        {
            if (activeSettings == null || sourceImage == null)
                return;

            Begin(activeSettings, sourceImage, unchecked(seed + 1), ownedImage != null);
        }

        public string Export()
        {
            if (state == null || activeSettings == null)
                return null;

            return SessionArchive.Write(new SavedSession(activeSettings.ToConfig(), sourcePath, state));
        }

        public void Restore(SavedSession saved, PuzzleSettings settingsForNextRound, Texture2D picture, bool ownImage)
        {
            if (saved == null || settingsForNextRound == null || picture == null || pieceMaterial == null)
                return;

            if (presentation != null)
            {
                StopCoroutine(presentation);
                presentation = null;
            }

            draggedGroupId = -1;
            hasRotateTarget = false;
            state = null;
            ClearView();
            UseImage(picture, ownImage);
            sourcePath = saved.ImagePath ?? string.Empty;
            activeSettings = settingsForNextRound;
            seed = saved.State.Definition.Seed;
            if (boardImage != null)
                Destroy(boardImage);

            var config = saved.Config;
            boardImage = ImageImportService.FitToBoard(sourceImage, config.Columns, config.Rows, config.CellSize);
            snapDistance = config.SnapThreshold * config.CellSize;
            state = saved.State;
            var zone = TableLayout.PieceZone(state.Definition, config.Margin);
            FrameCamera(zone, config.Margin * 0.5f);
            SpawnTable(state.Definition);
            SpawnPieces(state.Definition);
            if (state.Phase == SessionPhase.Completo)
                SettleComplete();

            ApplyState();
            RoundBegan?.Invoke();
            if (state.Phase == SessionPhase.Completo)
                RoundCompleted?.Invoke();
        }

        public void Begin(PuzzleSettings nextSettings, Texture2D nextImage, int nextSeed, bool ownImage, string imagePath = null)
        {
            if (nextSettings == null || nextImage == null || pieceMaterial == null)
                return;

            if (presentation != null)
            {
                StopCoroutine(presentation);
                presentation = null;
            }

            draggedGroupId = -1;
            hasRotateTarget = false;
            state = null;
            ClearView();
            UseImage(nextImage, ownImage);
            if (imagePath != null)
                sourcePath = imagePath;
            activeSettings = nextSettings;
            seed = nextSeed;

            var config = nextSettings.ToConfig();
            if (boardImage != null)
                Destroy(boardImage);

            boardImage = ImageImportService.FitToBoard(sourceImage, config.Columns, config.Rows, config.CellSize);
            snapDistance = config.SnapThreshold * config.CellSize;
            var definition = PuzzleGenerator.Create(config, seed);
            state = PuzzleStateFactory.CreateSolved(definition);
            var frame = TableLayout.AssemblyFrame(definition);
            var zone = TableLayout.PieceZone(definition, config.Margin);
            var poses = TableLayout.Scatter(definition, seed, frame.Inflated(config.Margin), zone);
            state = PlaceGroups(state, poses);
            state = ScatterTurns(state, seed);
            FrameCamera(zone, config.Margin * 0.5f);
            SpawnTable(definition);
            SpawnPieces(definition);
            ApplyState();
            RoundBegan?.Invoke();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.rKey.wasPressedThisFrame)
                ToggleReference();

            if (keyboard.eKey.wasPressedThisFrame)
                RotateHeld();

            if (keyboard.qKey.wasPressedThisFrame)
                DetachHeld();
        }

        public void ToggleReference()
        {
            if (referenceRenderer == null)
                return;

            referenceRenderer.enabled = !referenceRenderer.enabled;
            ReferenceChanged?.Invoke();
        }

        void LateUpdate()
        {
            ApplyState();
        }

        void OnDestroy()
        {
            if (presentation != null)
                StopCoroutine(presentation);

            ClearView();
            if (ownedImage != null)
                Destroy(ownedImage);
            if (boardImage != null)
                Destroy(boardImage);
        }

        void ClearView()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            for (var i = 0; i < meshes.Count; i++)
            {
                if (meshes[i] != null)
                    Destroy(meshes[i]);
            }

            meshes.Clear();
            pieces.Clear();
            DestroyMaterial(ref faceMaterial);
            DestroyMaterial(ref rimMaterial);
            DestroyMaterial(ref shadowMaterial);
            DestroyMaterial(ref frameMaterial);
            DestroyMaterial(ref referenceMaterial);
            DestroyMaterial(ref completeMaterial);
            if (whiteTexture != null)
            {
                Destroy(whiteTexture);
                whiteTexture = null;
            }

            if (shadowTexture != null)
            {
                Destroy(shadowTexture);
                shadowTexture = null;
            }

            referenceRenderer = null;
            completeRenderer = null;
        }

        static void DestroyMaterial(ref Material material)
        {
            if (material == null)
                return;

            Destroy(material);
            material = null;
        }

        void UseImage(Texture2D nextImage, bool ownImage)
        {
            if (ownImage)
            {
                if (ownedImage != null && ownedImage != nextImage)
                    Destroy(ownedImage);

                ownedImage = nextImage;
            }
            else if (ownedImage != null && ownedImage != nextImage)
            {
                Destroy(ownedImage);
                ownedImage = null;
            }

            image = nextImage;
            sourceImage = nextImage;
        }

        IEnumerator PresentComplete()
        {
            var group = state.Groups[0];
            var cellSize = state.Definition.CellSize;
            var start = group.Pose;
            var home = new MyPuzzle.Core.Pose(
                (group.Anchor.Column + 0.5f) * cellSize,
                (group.Anchor.Row + 0.5f) * cellSize,
                group.Pose.QuarterTurns);

            if (rimMaterial != null)
            {
                var color = rimMaterial.color;
                rimMaterial.color = new Color(color.r, color.g, color.b, 0f);
            }

            foreach (var piece in pieces.Values)
            {
                if (piece.Shadow != null)
                    piece.Shadow.enabled = false;
            }

            if (completeRenderer != null)
                completeRenderer.enabled = true;

            const float duration = 0.45f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var k = (t * t) * (3f - (2f * t));
                MoveGroup(group.Id, new MyPuzzle.Core.Pose(
                    Mathf.Lerp(start.X, home.X, k),
                    Mathf.Lerp(start.Y, home.Y, k),
                    start.QuarterTurns));
                SetCompleteAlpha(k);
                yield return null;
            }

            SettleComplete();
        }

        void SettleComplete()
        {
            if (state == null || state.Groups.Count == 0)
                return;

            var group = state.Groups[0];
            var cellSize = state.Definition.CellSize;
            var home = new MyPuzzle.Core.Pose(
                (group.Anchor.Column + 0.5f) * cellSize,
                (group.Anchor.Row + 0.5f) * cellSize,
                group.Pose.QuarterTurns);
            MoveGroup(group.Id, home);
            var upright = (4 - group.Pose.QuarterTurns) % 4;
            if (upright != 0)
                state = GroupOperations.Rotate(state, group.Id, upright);

            if (rimMaterial != null)
            {
                var color = rimMaterial.color;
                rimMaterial.color = new Color(color.r, color.g, color.b, 0f);
            }

            foreach (var piece in pieces.Values)
            {
                if (piece.Shadow != null)
                    piece.Shadow.enabled = false;
            }

            if (completeRenderer != null)
                completeRenderer.enabled = true;

            SetCompleteAlpha(1f);
        }

        void SetCompleteAlpha(float alpha)
        {
            if (completeMaterial == null)
                return;

            completeMaterial.color = new Color(1f, 1f, 1f, alpha);
        }

        void SpawnTable(PuzzleDefinition definition)
        {
            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();

            var width = definition.Columns * definition.CellSize;
            var height = definition.Rows * definition.CellSize;
            var gap = definition.CellSize * 0.03f;
            var thickness = definition.CellSize * 0.045f;

            frameMaterial = Solid(new Color(0.22f, 0.18f, 0.16f, 1f));
            var frameMesh = FrameMesh(width, height, gap, thickness);
            meshes.Add(frameMesh);
            var frameObject = new GameObject("AssemblyFrame");
            frameObject.transform.SetParent(transform, false);
            frameObject.AddComponent<MeshFilter>().sharedMesh = frameMesh;
            var frameRenderer = frameObject.AddComponent<MeshRenderer>();
            frameRenderer.sharedMaterial = frameMaterial;
            frameRenderer.sortingOrder = -20;

            referenceMaterial = new Material(pieceMaterial);
            referenceMaterial.mainTexture = boardImage;
            referenceMaterial.color = new Color(1f, 1f, 1f, 0.28f);
            var referenceMesh = Quad(0f, 0f, width, height, 0.02f);
            meshes.Add(referenceMesh);
            var referenceObject = new GameObject("Reference");
            referenceObject.transform.SetParent(transform, false);
            referenceObject.AddComponent<MeshFilter>().sharedMesh = referenceMesh;
            referenceRenderer = referenceObject.AddComponent<MeshRenderer>();
            referenceRenderer.sharedMaterial = referenceMaterial;
            referenceRenderer.sortingOrder = -10;

            completeMaterial = new Material(pieceMaterial);
            completeMaterial.mainTexture = boardImage;
            completeMaterial.color = new Color(1f, 1f, 1f, 0f);
            var completeMesh = Quad(0f, 0f, width, height, -0.02f);
            meshes.Add(completeMesh);
            var completeObject = new GameObject("CompleteImage");
            completeObject.transform.SetParent(transform, false);
            completeObject.AddComponent<MeshFilter>().sharedMesh = completeMesh;
            completeRenderer = completeObject.AddComponent<MeshRenderer>();
            completeRenderer.sharedMaterial = completeMaterial;
            completeRenderer.sortingOrder = 30000;
            completeRenderer.enabled = false;
        }

        void SpawnPieces(PuzzleDefinition definition)
        {
            faceMaterial = new Material(pieceMaterial);
            faceMaterial.mainTexture = boardImage;
            faceMaterial.color = Color.white;
            rimMaterial = Solid(new Color(0.73f, 0.58f, 0.42f, 1f));

            shadowTexture = SoftShadow(64);
            shadowMaterial = new Material(pieceMaterial);
            shadowMaterial.mainTexture = shadowTexture;
            shadowMaterial.color = Color.white;
            var shadowMesh = ShadowQuad(definition.CellSize * 1.35f);
            meshes.Add(shadowMesh);

            foreach (var cell in GridTopology.AllCells(definition.Columns, definition.Rows))
            {
                var outline = definition.OutlineOf(cell);
                var loop = PieceOutline.Build(outline, definition.CellSize);
                var centerX = (outline.NorthWest.X + outline.NorthEast.X) * 0.5f;
                var centerY = (outline.SouthWest.Y + outline.NorthWest.Y) * 0.5f;
                var face = new Vector2[loop.Count];
                for (var i = 0; i < loop.Count; i++)
                    face[i] = new Vector2(loop[i].X - centerX, loop[i].Y - centerY);

                var mesh = PieceMeshBuilder.Build(outline, definition.Columns, definition.Rows, cell);
                meshes.Add(mesh);

                var pieceObject = new GameObject($"Piece {cell.Column},{cell.Row}");
                pieceObject.transform.SetParent(transform, false);
                pieceObject.AddComponent<MeshFilter>().sharedMesh = mesh;

                var pieceRenderer = pieceObject.AddComponent<MeshRenderer>();
                pieceRenderer.sharedMaterials = new[] { rimMaterial, faceMaterial };
                pieceRenderer.sortingOrder = 0;

                var shadowObject = new GameObject("Shadow");
                shadowObject.transform.SetParent(pieceObject.transform, false);
                shadowObject.transform.localPosition = new Vector3(definition.CellSize * 0.04f, definition.CellSize * -0.07f, 0f);
                shadowObject.AddComponent<MeshFilter>().sharedMesh = shadowMesh;
                var shadowRenderer = shadowObject.AddComponent<MeshRenderer>();
                shadowRenderer.sharedMaterial = shadowMaterial;
                shadowRenderer.sortingOrder = -1;

                pieces.Add(cell, new PieceView(pieceObject.transform, pieceRenderer, shadowRenderer, face));
            }
        }

        Material Solid(Color color)
        {
            var material = new Material(pieceMaterial);
            material.mainTexture = whiteTexture;
            material.color = color;
            return material;
        }

        void ApplyState()
        {
            if (state == null)
                return;

            var cellSize = state.Definition.CellSize;
            for (var groupIndex = 0; groupIndex < state.Groups.Count; groupIndex++)
            {
                var group = state.Groups[groupIndex];
                for (var memberIndex = 0; memberIndex < group.Members.Count; memberIndex++)
                {
                    var cell = group.Members[memberIndex];
                    var world = GroupOperations.CellWorldPose(group, cell, cellSize);
                    pieces[cell].Transform.localPosition = new Vector3(world.X, world.Y, 0f);
                    pieces[cell].Transform.localRotation = Quaternion.Euler(0f, 0f, world.QuarterTurns * 90f);
                    if (pieces[cell].Shadow != null)
                    {
                        var offsetX = cellSize * 0.04f;
                        var offsetY = cellSize * -0.07f;
                        Unrotate(ref offsetX, ref offsetY, world.QuarterTurns);
                        var shadow = pieces[cell].Shadow.transform;
                        shadow.localPosition = new Vector3(offsetX, offsetY, 0f);
                        shadow.localRotation = Quaternion.Euler(0f, 0f, -world.QuarterTurns * 90f);
                    }
                }
            }
        }

        bool TryFindGroup(Vector2 worldPoint, out GroupState found, out CellId cell)
        {
            found = null;
            cell = default;
            var bestOrder = int.MinValue;
            var cellSize = state.Definition.CellSize;

            for (var groupIndex = 0; groupIndex < state.Groups.Count; groupIndex++)
            {
                var group = state.Groups[groupIndex];
                for (var memberIndex = 0; memberIndex < group.Members.Count; memberIndex++)
                {
                    var candidate = group.Members[memberIndex];
                    var world = GroupOperations.CellWorldPose(group, candidate, cellSize);
                    var localX = worldPoint.x - world.X;
                    var localY = worldPoint.y - world.Y;
                    Unrotate(ref localX, ref localY, world.QuarterTurns);
                    if (!ContainsFace(pieces[candidate].Face, localX, localY))
                        continue;

                    var order = pieces[candidate].Renderer.sortingOrder;
                    if (found != null && order <= bestOrder)
                        continue;

                    found = group;
                    cell = candidate;
                    bestOrder = order;
                }
            }

            return found != null;
        }

        void BringToFront(GroupState group)
        {
            topSortingOrder += 2;
            for (var i = 0; i < group.Members.Count; i++)
            {
                var piece = pieces[group.Members[i]];
                piece.Renderer.sortingOrder = topSortingOrder;
                piece.Shadow.sortingOrder = topSortingOrder - 1;
            }
        }

        MyPuzzle.Core.Pose ClampToCamera(GroupState group, MyPuzzle.Core.Pose desired)
        {
            var camera = Camera.main;
            if (camera == null || !camera.orthographic)
                return new MyPuzzle.Core.Pose(desired.X, desired.Y, group.Pose.QuarterTurns);

            var height = camera.orthographicSize * 2f;
            var width = height * camera.aspect;
            var covered = TopBarCoverage();
            var center = camera.transform.position;
            var minY = center.y - camera.orthographicSize;
            var maxY = center.y + (camera.orthographicSize * (1f - (2f * covered)));
            return GroupOperations.ClampTranslation(
                group,
                state.Definition.CellSize,
                desired,
                center.x - (width * 0.5f),
                minY,
                center.x + (width * 0.5f),
                maxY);
        }

        GroupState FindGroup(int groupId)
        {
            for (var i = 0; i < state.Groups.Count; i++)
            {
                if (state.Groups[i].Id == groupId)
                    return state.Groups[i];
            }

            return null;
        }

        GroupState FindGroupContaining(CellId cell)
        {
            for (var i = 0; i < state.Groups.Count; i++)
            {
                var group = state.Groups[i];
                for (var member = 0; member < group.Members.Count; member++)
                {
                    if (group.Members[member].Equals(cell))
                        return group;
                }
            }

            return null;
        }

        static PuzzleState PlaceGroups(PuzzleState state, IReadOnlyList<MyPuzzle.Core.Pose> poses)
        {
            var definition = state.Definition;
            var current = state;
            var index = 0;
            foreach (var cell in GridTopology.AllCells(definition.Columns, definition.Rows))
            {
                current = GroupOperations.Move(current, cell.Index(definition.Columns), poses[index]);
                index++;
            }

            return current;
        }

        static PuzzleState ScatterTurns(PuzzleState puzzle, int scatterSeed)
        {
            var random = new System.Random(unchecked(scatterSeed * 397) ^ 0x5f3759df);
            var current = puzzle;
            for (var i = 0; i < puzzle.Groups.Count; i++)
            {
                var steps = random.Next(4);
                if (steps == 0)
                    continue;

                current = GroupOperations.Rotate(current, puzzle.Groups[i].Id, steps);
            }

            return current;
        }

        static void FrameCamera(BoardRect view, float padding)
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            var minX = view.MinX - padding;
            var maxX = view.MaxX + padding;
            var minY = view.MinY - padding;
            var maxY = view.MaxY + padding;
            var covered = TopBarCoverage();
            var visible = Mathf.Max(0.55f, 1f - covered);
            camera.orthographic = true;
            var verticalHalf = (maxY - minY) * 0.5f;
            var horizontalHalf = (maxX - minX) * 0.5f / Mathf.Max(camera.aspect, 0.01f);
            var size = Mathf.Max(verticalHalf / visible, horizontalHalf);
            camera.orthographicSize = size;
            camera.transform.position = new Vector3(
                (minX + maxX) * 0.5f,
                ((minY + maxY) * 0.5f) + (size * covered),
                -10f);
        }

        static float TopBarCoverage()
        {
            var canvasObject = GameObject.Find("HudCanvas");
            if (canvasObject == null || Screen.height <= 0)
                return 0f;

            var canvas = canvasObject.GetComponent<Canvas>();
            var bar = canvasObject.transform.Find("Bar") as RectTransform;
            if (canvas == null || bar == null)
                return 0f;

            Canvas.ForceUpdateCanvases();
            var pixels = bar.rect.height * canvas.scaleFactor;
            return Mathf.Clamp(pixels / Screen.height, 0f, 0.45f);
        }

        sealed class PieceView
        {
            public PieceView(Transform pieceTransform, MeshRenderer pieceRenderer, MeshRenderer shadowRenderer, Vector2[] face)
            {
                Transform = pieceTransform;
                Renderer = pieceRenderer;
                Shadow = shadowRenderer;
                Face = face;
            }

            public Transform Transform { get; }

            public MeshRenderer Renderer { get; }

            public MeshRenderer Shadow { get; }

            public Vector2[] Face { get; }
        }

        static bool ContainsFace(Vector2[] polygon, float x, float y)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var current = polygon[i];
                var previous = polygon[j];
                if ((current.y > y) != (previous.y > y)
                    && x < (((previous.x - current.x) * (y - current.y) / (previous.y - current.y)) + current.x))
                    inside = !inside;
            }

            return inside;
        }

        static void Unrotate(ref float x, ref float y, int quarterTurns)
        {
            switch (quarterTurns & 3)
            {
                case 1:
                    var turnedY = x;
                    x = y;
                    y = -turnedY;
                    return;
                case 2:
                    x = -x;
                    y = -y;
                    return;
                case 3:
                    var turnedX = y;
                    y = x;
                    x = -turnedX;
                    return;
            }
        }

        static Texture2D SoftShadow(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radius = center;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt(((x - center) * (x - center)) + ((y - center) * (y - center))) / radius;
                    var alpha = distance >= 1f ? 0f : (1f - distance) * (1f - distance) * 0.4f;
                    pixels[(y * size) + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Mesh ShadowQuad(float size)
        {
            var half = size * 0.5f;
            return Quad(-half, -half, half, half, 0.03f);
        }

        static Mesh FrameMesh(float width, float height, float gap, float thickness)
        {
            var outerMinX = -gap - thickness;
            var outerMinY = -gap - thickness;
            var outerMaxX = width + gap + thickness;
            var outerMaxY = height + gap + thickness;
            var innerMinX = -gap;
            var innerMinY = -gap;
            var innerMaxX = width + gap;
            var innerMaxY = height + gap;

            var mesh = new Mesh { name = "AssemblyFrame" };
            mesh.vertices = new[]
            {
                new Vector3(outerMinX, outerMinY, 0.02f), new Vector3(outerMaxX, outerMinY, 0.02f),
                new Vector3(innerMaxX, innerMinY, 0.02f), new Vector3(innerMinX, innerMinY, 0.02f),
                new Vector3(innerMinX, innerMaxY, 0.02f), new Vector3(outerMinX, outerMaxY, 0.02f),
                new Vector3(outerMaxX, outerMaxY, 0.02f), new Vector3(innerMaxX, innerMaxY, 0.02f),
                new Vector3(outerMinX, innerMinY, 0.02f), new Vector3(innerMinX, innerMinY, 0.02f),
                new Vector3(innerMinX, innerMaxY, 0.02f), new Vector3(outerMinX, innerMaxY, 0.02f),
                new Vector3(innerMaxX, innerMinY, 0.02f), new Vector3(outerMaxX, innerMinY, 0.02f),
                new Vector3(outerMaxX, innerMaxY, 0.02f), new Vector3(innerMaxX, innerMaxY, 0.02f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh Quad(float minX, float minY, float maxX, float maxY, float z)
        {
            var mesh = new Mesh { name = "Quad" };
            mesh.vertices = new[]
            {
                new Vector3(minX, minY, z),
                new Vector3(maxX, minY, z),
                new Vector3(maxX, maxY, z),
                new Vector3(minX, maxY, z)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
