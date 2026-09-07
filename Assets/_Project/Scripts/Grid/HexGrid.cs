using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GothicTactics.Grid
{
    public sealed class HexGrid : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Min(1)] private int width = 8;
        [SerializeField, Min(1)] private int height = 10;
        [SerializeField, Min(0.25f)] private float outerRadius = 1f;
        [SerializeField, Min(0f)] private float gap = 0.06f;

        [Header("References")]
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private Material tileMaterial;

        private readonly Dictionary<HexCoordinates, HexTile> tiles = new();
        private HexTile hoveredTile;
        private HexTile selectedTile;
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
            tiles.Add(coordinates, tile);
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
            var nextHovered = Physics.Raycast(ray, out var hit) ? hit.collider.GetComponent<HexTile>() : null;

            if (nextHovered != hoveredTile)
            {
                hoveredTile?.SetHovered(false);
                hoveredTile = nextHovered;
                hoveredTile?.SetHovered(true);
            }

            if (hoveredTile != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                selectedTile?.SetSelected(false);
                selectedTile = hoveredTile;
                selectedTile.SetSelected(true);
                Debug.Log($"Selected hex {selectedTile.Coordinates}", selectedTile);
            }
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
