using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GothicTactics.Grid;
using GothicTactics.Units;
using UnityEngine;

namespace GothicTactics.Combat
{
    public sealed class TurnManager : MonoBehaviour
    {
        [SerializeField] private HexGrid grid;
        [SerializeField, Min(0f)] private float enemyThinkingTime = 0.65f;

        private readonly List<HexUnit> turnOrder = new();
        private int activeIndex = -1;
        private int round;
        private bool enemyTurnRunning;

        public HexUnit ActiveUnit => activeIndex >= 0 && activeIndex < turnOrder.Count ? turnOrder[activeIndex] : null;
        public int Round => round;
        public IReadOnlyList<HexUnit> TurnOrder => turnOrder;

        private IEnumerator Start()
        {
            grid ??= FindFirstObjectByType<HexGrid>();
            yield return null;

            turnOrder.AddRange(FindObjectsByType<HexUnit>(FindObjectsSortMode.None)
                .OrderByDescending(unit => unit.Initiative)
                .ThenBy(unit => unit.name));

            if (turnOrder.Count == 0)
            {
                Debug.LogError("Turn Manager could not find any units.", this);
                yield break;
            }

            round = 1;
            activeIndex = 0;
            BeginActiveTurn();
        }

        public void RequestEndTurn()
        {
            if (ActiveUnit == null || ActiveUnit.Team != UnitTeam.Player || ActiveUnit.IsMoving) return;
            AdvanceTurn();
        }

        private void BeginActiveTurn()
        {
            ActiveUnit.RefreshActionPoints();
            grid.SetActiveUnit(ActiveUnit);
            Debug.Log($"Round {round}: {ActiveUnit.name}'s turn ({ActiveUnit.CurrentActionPoints} AP).", ActiveUnit);

            if (ActiveUnit.Team == UnitTeam.Enemy)
            {
                StartCoroutine(RunEnemyTurn(ActiveUnit));
            }
        }

        private void AdvanceTurn()
        {
            grid.SetActiveUnit(null);
            activeIndex++;
            if (activeIndex >= turnOrder.Count)
            {
                activeIndex = 0;
                round++;
            }
            BeginActiveTurn();
        }

        private IEnumerator RunEnemyTurn(HexUnit enemy)
        {
            if (enemyTurnRunning) yield break;
            enemyTurnRunning = true;
            yield return new WaitForSeconds(enemyThinkingTime);

            var target = turnOrder
                .Where(unit => unit.Team == UnitTeam.Player && unit.CurrentTile != null)
                .OrderBy(unit => enemy.CurrentTile.Coordinates.DistanceTo(unit.CurrentTile.Coordinates))
                .FirstOrDefault();

            if (target != null)
            {
                var bestDestination = enemy.CurrentTile.Coordinates;
                var bestDistance = bestDestination.DistanceTo(target.CurrentTile.Coordinates);

                for (var direction = 0; direction < 6; direction++)
                {
                    var candidateCoordinates = enemy.CurrentTile.Coordinates.Neighbour(direction);
                    if (!grid.TryGetTile(candidateCoordinates, out var candidate) || !candidate.IsWalkable || candidate.IsOccupied) continue;

                    var candidateDistance = candidateCoordinates.DistanceTo(target.CurrentTile.Coordinates);
                    if (candidateDistance < bestDistance)
                    {
                        bestDistance = candidateDistance;
                        bestDestination = candidateCoordinates;
                    }
                }

                var movementFinished = false;
                if (!bestDestination.Equals(enemy.CurrentTile.Coordinates) &&
                    grid.TryMoveUnitOneStep(enemy, bestDestination, () => movementFinished = true))
                {
                    yield return new WaitUntil(() => movementFinished);
                }
            }

            yield return new WaitForSeconds(enemyThinkingTime * 0.5f);
            enemyTurnRunning = false;
            AdvanceTurn();
        }
    }
}
