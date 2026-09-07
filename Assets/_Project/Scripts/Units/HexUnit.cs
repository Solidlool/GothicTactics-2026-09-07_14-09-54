using GothicTactics.Grid;
using UnityEngine;

namespace GothicTactics.Units
{
    public sealed class HexUnit : MonoBehaviour
    {
        [SerializeField] private HexGrid grid;
        [SerializeField] private int startingQ;
        [SerializeField] private int startingR;
        [SerializeField, Min(1)] private int maximumActionPoints = 5;

        public HexTile CurrentTile { get; private set; }
        public int CurrentActionPoints { get; private set; }
        public int MaximumActionPoints => maximumActionPoints;

        private void Start()
        {
            grid ??= FindFirstObjectByType<HexGrid>();
            CurrentActionPoints = maximumActionPoints;

            if (grid == null || !grid.PlaceUnit(this, new HexCoordinates(startingQ, startingR)))
            {
                Debug.LogError($"Could not place {name} on starting hex ({startingQ}, {startingR}).", this);
            }
        }

        public bool TrySpendActionPoints(int amount)
        {
            if (amount < 0 || amount > CurrentActionPoints) return false;
            CurrentActionPoints -= amount;
            return true;
        }

        public void RefreshActionPoints()
        {
            CurrentActionPoints = maximumActionPoints;
        }

        public void SetCurrentTile(HexTile tile)
        {
            CurrentTile = tile;
        }
    }
}
