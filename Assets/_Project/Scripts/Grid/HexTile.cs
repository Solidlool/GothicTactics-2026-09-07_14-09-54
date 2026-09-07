using UnityEngine;
using GothicTactics.Units;

namespace GothicTactics.Grid
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
    public sealed class HexTile : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color normalColour = new(0.16f, 0.17f, 0.19f);
        [SerializeField] private Color hoverColour = new(0.32f, 0.36f, 0.40f);
        [SerializeField] private Color selectedColour = new(0.55f, 0.18f, 0.12f);
        [SerializeField] private Color reachableColour = new(0.18f, 0.42f, 0.34f);

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock properties;
        private bool isHovered;
        private bool isSelected;
        private bool isReachable;

        public HexCoordinates Coordinates { get; private set; }
        public HexUnit Occupant { get; private set; }
        public bool IsOccupied => Occupant != null;

        public void Initialise(HexCoordinates coordinates)
        {
            Coordinates = coordinates;
            name = $"Hex {coordinates.Q}, {coordinates.R}";
            meshRenderer = GetComponent<MeshRenderer>();
            properties = new MaterialPropertyBlock();
            RefreshColour();
        }

        public void SetHovered(bool value)
        {
            if (isHovered == value) return;
            isHovered = value;
            RefreshColour();
        }

        public void SetSelected(bool value)
        {
            if (isSelected == value) return;
            isSelected = value;
            RefreshColour();
        }

        public void SetReachable(bool value)
        {
            if (isReachable == value) return;
            isReachable = value;
            RefreshColour();
        }

        public void SetOccupant(HexUnit occupant)
        {
            Occupant = occupant;
        }

        private void RefreshColour()
        {
            if (meshRenderer == null) return;
            properties ??= new MaterialPropertyBlock();
            var colour = isSelected ? selectedColour :
                isHovered ? hoverColour :
                isReachable ? reachableColour : normalColour;
            properties.SetColor(BaseColor, colour);
            meshRenderer.SetPropertyBlock(properties);
        }
    }
}
