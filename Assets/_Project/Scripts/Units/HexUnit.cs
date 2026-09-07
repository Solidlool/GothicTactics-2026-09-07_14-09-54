using GothicTactics.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GothicTactics.Units
{
    public sealed class HexUnit : MonoBehaviour
    {
        [SerializeField] private HexGrid grid;
        [SerializeField] private int startingQ;
        [SerializeField] private int startingR;
        [SerializeField, Min(1)] private int maximumActionPoints = 5;
        [SerializeField, Min(0.1f)] private float movementSpeed = 4f;

        public HexTile CurrentTile { get; private set; }
        public int CurrentActionPoints { get; private set; }
        public int MaximumActionPoints => maximumActionPoints;
        public bool IsMoving { get; private set; }

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

        public void MoveAlongPath(IReadOnlyList<HexTile> path, Action onComplete)
        {
            if (!IsMoving && path.Count > 0)
            {
                StartCoroutine(MoveRoutine(path, onComplete));
            }
        }

        private IEnumerator MoveRoutine(IReadOnlyList<HexTile> path, Action onComplete)
        {
            IsMoving = true;
            foreach (var tile in path)
            {
                var destination = tile.transform.position + Vector3.up;
                while ((transform.position - destination).sqrMagnitude > 0.001f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, destination, movementSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = destination;
            }

            SetCurrentTile(path[path.Count - 1]);
            IsMoving = false;
            onComplete?.Invoke();
        }
    }
}
