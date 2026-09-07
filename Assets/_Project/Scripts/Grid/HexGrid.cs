using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GothicTactics.Units;

namespace GothicTactics.Grid
{
    public sealed class HexGrid : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Min(1)] private int width = 8;
        [SerializeField, Min(1)] private int height = 10;
        [SerializeField, Min(0.25f)] private float outerRadius = 1f;
        [SerializeField, Min(0f)] private float gap = 0.06f;
        [SerializeField] private Vector2Int[] blockedCoordinates;

        [Header("References")]
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private Material tileMaterial;

        private readonly Dictionary<HexCoordinates, HexTile> tiles = new();
        private HexTile hoveredTile;
        private HexTile selectedTile;
        private HexUnit selectedUnit;
        private readonly HashSet<HexTile> reachableTiles = new();
        private readonly Dictionary<HexTile, int> movementCosts = new();
        private readonly Dictionary<HexTile, HexTile> movementParents = new();
        private Mesh sharedHexMesh;

        public IReadOnlyDictionary<HexCoordinates, HexTile> Tiles => tiles;

        private void Awake()
        {
            interactionCamera ??= Camera.main;
            Generate();
        }

        private void Update()
        {
            UpdatePointerInteraction();
        }

        [ContextMenu("Generate Grid")]
        public void Generate()
        {
            ClearGeneratedTiles();
            sharedHexMesh = BuildHexMesh(outerRadius - gap);

            for (var r = 0; r < height; r++)
            {
                for (var q = 0; q < width; q++)
                {
                    CreateTile(new HexCoordinates(q, r));
                }
            }
        }

        public bool TryGetTile(HexCoordinates coordinates, out HexTile tile) => tiles.TryGetValue(coordinates, out tile);

        public bool PlaceUnit(HexUnit unit, HexCoordinates coordinates)
        {
            if (!TryGetTile(coordinates, out var tile) || !tile.IsWalkable || tile.IsOccupied) return false;

            unit.CurrentTile?.SetOccupant(null);
            tile.SetOccupant(unit);
            unit.SetCurrentTile(tile);
            unit.transform.position = tile.transform.position + Vector3.up;
            return true;
        }

        private void CreateTile(HexCoordinates coordinates)
        {
            var tileObject = new GameObject();
            tileObject.transform.SetParent(transform, false);
            tileObject.transform.localPosition = AxialToWorld(coordinates);

            var filter = tileObject.AddComponent<MeshFilter>();
            filter.sharedMesh = sharedHexMesh;
            var renderer = tileObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = tileMaterial;
            var collider = tileObject.AddComponent<MeshCollider>();
            collider.sharedMesh = sharedHexMesh;

            var tile = tileObject.AddComponent<HexTile>();
            tile.Initialise(coordinates);
            if (IsConfiguredAsBlocked(coordinates))
            {
                tile.SetWalkable(false);
                CreateObstacleMarker(tileObject.transform);
            }
            tiles.Add(coordinates, tile);
        }

        private bool IsConfiguredAsBlocked(HexCoordinates coordinates)
        {
            if (blockedCoordinates == null) return false;
            foreach (var blocked in blockedCoordinates)
            {
                if (blocked.x == coordinates.Q && blocked.y == coordinates.R) return true;
            }
            return false;
        }

        private static void CreateObstacleMarker(Transform tileTransform)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Obstacle";
            marker.transform.SetParent(tileTransform, false);
            marker.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            marker.transform.localScale = new Vector3(0.85f, 0.9f, 0.85f);
        }

        private Vector3 AxialToWorld(HexCoordinates coordinates)
        {
            var x = outerRadius * Mathf.Sqrt(3f) * (coordinates.Q + coordinates.R * 0.5f);
            var z = outerRadius * 1.5f * coordinates.R;
            return new Vector3(x, 0f, z);
        }

        private void UpdatePointerInteraction()
        {
            if (interactionCamera == null || Mouse.current == null) return;

            var ray = interactionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hasHit = Physics.Raycast(ray, out var hit);
            var nextHovered = hasHit ? hit.collider.GetComponent<HexTile>() : null;

            if (nextHovered != hoveredTile)
            {
                hoveredTile?.SetHovered(false);
                hoveredTile = nextHovered;
                hoveredTile?.SetHovered(true);
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame || !hasHit) return;

            var clickedUnit = hit.collider.GetComponentInParent<HexUnit>();
            if (clickedUnit != null)
            {
                SelectUnit(clickedUnit);
                return;
            }

            if (hoveredTile == null) return;

            if (selectedUnit != null && !selectedUnit.IsMoving && movementCosts.TryGetValue(hoveredTile, out var movementCost))
            {
                if (selectedUnit.TrySpendActionPoints(movementCost))
                {
                    MoveSelectedUnit(BuildPathTo(hoveredTile));
                }
                return;
            }

            selectedTile?.SetSelected(false);
            selectedTile = hoveredTile;
            selectedTile.SetSelected(true);
            Debug.Log($"Selected hex {selectedTile.Coordinates}", selectedTile);
        }

        private void SelectUnit(HexUnit unit)
        {
            selectedUnit = unit;
            selectedTile?.SetSelected(false);
            selectedTile = unit.CurrentTile;
            selectedTile?.SetSelected(true);
            RefreshReachableTiles();
            Debug.Log($"Selected {unit.name}: {unit.CurrentActionPoints}/{unit.MaximumActionPoints} AP", unit);
        }

        private void RefreshReachableTiles()
        {
            foreach (var tile in reachableTiles) tile.SetReachable(false);
            reachableTiles.Clear();
            movementCosts.Clear();
            movementParents.Clear();

            if (selectedUnit?.CurrentTile == null) return;

            var frontier = new Queue<HexTile>();
            var cost = new Dictionary<HexTile, int>();
            frontier.Enqueue(selectedUnit.CurrentTile);
            cost[selectedUnit.CurrentTile] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                for (var direction = 0; direction < 6; direction++)
                {
                    var neighbourCoordinates = current.Coordinates.Neighbour(direction);
                    if (!TryGetTile(neighbourCoordinates, out var neighbour) || !neighbour.IsWalkable || neighbour.IsOccupied) continue;

                    var nextCost = cost[current] + 1;
                    if (nextCost > selectedUnit.CurrentActionPoints || cost.ContainsKey(neighbour)) continue;

                    cost[neighbour] = nextCost;
                    frontier.Enqueue(neighbour);
                    reachableTiles.Add(neighbour);
                    movementCosts[neighbour] = nextCost;
                    movementParents[neighbour] = current;
                    neighbour.SetReachable(true);
                }
            }
        }

        private List<HexTile> BuildPathTo(HexTile destination)
        {
            var path = new List<HexTile> { destination };
            var current = destination;
            while (movementParents.TryGetValue(current, out var parent) && parent != selectedUnit.CurrentTile)
            {
                path.Add(parent);
                current = parent;
            }
            path.Reverse();
            return path;
        }

        private void MoveSelectedUnit(IReadOnlyList<HexTile> path)
        {
            if (path.Count == 0) return;

            var destination = path[path.Count - 1];
            selectedUnit.CurrentTile.SetOccupant(null);
            destination.SetOccupant(selectedUnit);

            foreach (var tile in reachableTiles) tile.SetReachable(false);
            reachableTiles.Clear();
            movementCosts.Clear();
            movementParents.Clear();

            selectedUnit.MoveAlongPath(path, () => SelectUnit(selectedUnit));
        }

        private void ClearGeneratedTiles()
        {
            tiles.Clear();
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

        private static Mesh BuildHexMesh(float radius)
        {
            var vertices = new Vector3[7];
            var triangles = new int[18];
            vertices[0] = Vector3.zero;

            for (var i = 0; i < 6; i++)
            {
                var angle = Mathf.Deg2Rad * (60f * i + 30f);
                vertices[i + 1] = new Vector3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle));
                var triangle = i * 3;
                triangles[triangle] = 0;
                // Wind clockwise when viewed from above so the visible face and
                // generated normals point towards the isometric camera.
                triangles[triangle + 1] = i == 5 ? 1 : i + 2;
                triangles[triangle + 2] = i + 1;
            }

            var mesh = new Mesh { name = "Generated Hex Tile" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
