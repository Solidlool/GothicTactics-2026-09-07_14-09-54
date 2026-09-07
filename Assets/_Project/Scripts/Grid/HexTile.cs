using UnityEngine;

namespace GothicTactics.Grid
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
    public sealed class HexTile : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color normalColour = new(0.16f, 0.17f, 0.19f);
        [SerializeField] private Color hoverColour = new(0.32f, 0.36f, 0.40f);
        [SerializeField] private Color selectedColour = new(0.55f, 0.18f, 0.12f);

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock properties;
        private bool isHovered;
        private bool isSelected;

        public HexCoordinates Coordinates { get; private set; }

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

        private void RefreshColour()
        {
            if (meshRenderer == null) return;
            properties ??= new MaterialPropertyBlock();
            properties.SetColor(BaseColor, isSelected ? selectedColour : isHovered ? hoverColour : normalColour);
            meshRenderer.SetPropertyBlock(properties);
        }
    }
}
