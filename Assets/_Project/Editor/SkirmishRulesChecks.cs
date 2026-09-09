using System;
using System.Linq;
using GothicTactics.Skirmish;
using UnityEditor;
using UnityEngine;

namespace GothicTactics.Editor
{
    public static class SkirmishRulesChecks
    {
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Skirmish check failed: " + message);
        }
        [MenuItem("Gothic Tactics/Run Skirmish Rules Checks")]
        public static void Run()
        {
            var b = new SkirmishBattle(); var u = b.Units[0]; var enemy = b.Units[3];
            Check(b.Cells.Length == 99 && b.Units.Count == 7, "encounter setup");
            Check(b.Units.All(x => !b.Cells[x.CellId].Blocked) && b.Units.Select(x => x.CellId).Distinct().Count() == 7, "legal spawns");
            Check(b.Neighbours(0).Count() == 2 && b.Distance(0,12) == 2, "axial geometry");
            Check(!b.Move(u, b.Units[1].CellId) && !b.Move(u,16), "occupied and blocked tiles");
            var path = b.Path(u,35);
            Check(path.Count == 1 && b.Move(u,35) && u.AP == 5, "movement spends exact path cost");
            Check(!b.Move(enemy, enemy.CellId-1), "wrong team cannot move");
            Check(!b.Attack(u,enemy), "out-of-range attack rejected");
            enemy.CellId = 36; int hp = enemy.HP;
            Check(b.Attack(u,enemy) && enemy.HP == hp-4 && u.AP == 3, "attack damage and AP");
            Check(b.Guard(u) && u.AP == 0 && u.Guarding, "guard spends remaining AP");
            b.EndTurn();
            Check(b.EnemyTurn && enemy.AP == 6 && u.Guarding, "enemy turn preserves player guard");
            int playerHP = u.HP;
            Check(b.Attack(enemy,u) && u.HP == playerHP-1, "guard reduces damage with minimum one");
            b.EndTurn();
            Check(!b.EnemyTurn && b.Round == 2 && u.AP == 6 && !u.Guarding, "round restores AP and expires guard");
            u.HP = 5;
            Check(b.Heal(u) && u.HP == 11 && u.AP == 4 && u.Tonics == 0 && !b.Heal(u), "one-use tonic");
            Check(!b.HasSight(15,17) && b.HasSight(0,2), "ruins block sight");
            enemy.HP = 1; enemy.CellId = 36;
            Check(b.Attack(u,enemy) && !enemy.Alive && b.At(36) == null, "death releases occupancy");
            foreach (var e in b.Units.Where(x => x.Enemy)) e.HP = 0;
            Check(b.Finished && b.Victory && !b.Move(u,34), "victory locks actions");
            var loss = new SkirmishBattle();
            foreach (var ally in loss.Units.Where(x => !x.Enemy)) ally.HP = 0;
            Check(loss.Finished && !loss.Victory, "defeat");
            var ai = new SkirmishBattle(); ai.EndTurn();
            foreach (var e in ai.Units.Where(x => x.Enemy))
            {
                for (int action = 0; action < 7; action++)
                {
                    int before = e.AP;
                    if (!ai.EnemyAction(e)) break;
                    Check(e.AP < before && e.AP >= 0, "AI action terminates and respects AP");
                }
            }
            Check(ai.Units.Where(x => x.Alive).Select(x => x.CellId).Distinct().Count() == ai.Units.Count(x => x.Alive), "AI preserves occupancy");
            Debug.Log("All Ashen Bell rules checks passed.");
        }
    }
}
