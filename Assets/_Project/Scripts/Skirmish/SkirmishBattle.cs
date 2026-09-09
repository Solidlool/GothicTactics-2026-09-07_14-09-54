using System;
using System.Collections.Generic;
using System.Linq;

namespace GothicTactics.Skirmish
{
    // Pure rules: no Unity objects, rendering, input, or frame timing.
    public sealed partial class SkirmishBattle
    {
        public const int Width = 11, Height = 9, MaxAP = 6, AttackCost = 2;
        private static readonly int[,] Directions = { {1,0}, {1,-1}, {0,-1}, {-1,0}, {-1,1}, {0,1} };
        public sealed class Cell
        {
            public int Q, R;
            public bool Blocked;
            public int Id => R * Width + Q;
        }
        public sealed class Unit
        {
            public string Name, Role;
            public bool Enemy, Guarding;
            public int CellId, HP, MaxHP, AP, Damage, Range, Tonics = 1;
            public HeroDefinition Hero;
            public int Shield, Resource, BleedTurns;
            public HeroInventory Inventory;
            public bool InnateUsed;
            public readonly List<string> Hand = new List<string>();
            public readonly List<string> DrawPile = new List<string>();
            public readonly List<string> Discard = new List<string>();
            public bool Alive => HP > 0;
        }
        public readonly Cell[] Cells = new Cell[Width * Height];
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<string> Log = new List<string>();
        public int Round { get; private set; } = 1;
        public bool EnemyTurn { get; private set; }
        public bool Finished => !Units.Any(u => u.Alive && !u.Enemy) || !Units.Any(u => u.Alive && u.Enemy);
        public bool Victory => Finished && Units.Any(u => u.Alive && !u.Enemy);

        public SkirmishBattle()
        {
            for (int r = 0; r < Height; r++)
                for (int q = 0; q < Width; q++) Cells[r * Width + q] = new Cell { Q = q, R = r };
            foreach (int id in new[] { 16, 27, 38, 60, 71, 82, 46, 48, 50 }) Cells[id].Blocked = true;
            Add("Voss", "Warden", false, 1, 3, 16, 4, 1);
            Add("Mara", "Arbalist", false, 1, 4, 10, 3, 4);
            Add("Sister Ash", "Hexblade", false, 1, 5, 12, 3, 2);
            Add("Grave thrall", "Thrall", true, 8, 2, 7, 2, 1);
            Add("Hollow knight", "Knight", true, 8, 4, 12, 3, 1);
            Add("Bell keeper", "Invoker", true, 9, 4, 8, 2, 3);
            Add("Crypt hound", "Hound", true, 8, 6, 7, 2, 1);
            Note("The bell has fallen silent. Destroy the four revenants.");
        }
        private void Add(string name, string role, bool enemy, int q, int r, int hp, int damage, int range)
        {
            Units.Add(new Unit { Name = name, Role = role, Enemy = enemy, CellId = r * Width + q,
                HP = hp, MaxHP = hp, AP = MaxAP, Damage = damage, Range = range });
        }
        public void Note(string message)
        {
            Log.Insert(0, message);
            if (Log.Count > 7) Log.RemoveAt(Log.Count - 1);
        }
        public Unit At(int id) => Units.FirstOrDefault(u => u.Alive && u.CellId == id);
        public int Distance(int a, int b)
        {
            var x = Cells[a]; var y = Cells[b];
            return (Math.Abs(x.Q-y.Q) + Math.Abs(x.R-y.R) + Math.Abs(x.Q+x.R-y.Q-y.R))/2;
        }
        public IEnumerable<int> Neighbours(int id)
        {
            var c = Cells[id];
            for (int d = 0; d < 6; d++)
            {
                int q = c.Q + Directions[d,0], r = c.R + Directions[d,1];
                if (q >= 0 && q < Width && r >= 0 && r < Height) yield return r * Width + q;
            }
        }
        public List<int> Path(Unit unit, int destination)
        {
            var result = new List<int>();
            if (destination < 0 || destination >= Cells.Length || Cells[destination].Blocked ||
                (At(destination) != null && destination != unit.CellId)) return result;
            var previous = new Dictionary<int,int> { [unit.CellId] = -1 };
            var queue = new Queue<int>(); queue.Enqueue(unit.CellId);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == destination) break;
                foreach (int next in Neighbours(current))
                {
                    if (Cells[next].Blocked || At(next) != null || previous.ContainsKey(next)) continue;
                    previous[next] = current; queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(destination)) return result;
            for (int id = destination; id != unit.CellId; id = previous[id]) result.Add(id);
            result.Reverse(); return result;
        }
        // Sample the centre-to-centre ray in cube coordinates. Ruins block ranged attacks.
        public bool HasSight(int a, int b)
        {
            int distance = Distance(a, b);
            for (int i = 1; i < distance * 8; i++)
            {
                double t = (double)i / (distance * 8);
                double q = Cells[a].Q + (Cells[b].Q-Cells[a].Q)*t + 0.000001;
                double r = Cells[a].R + (Cells[b].R-Cells[a].R)*t + 0.000001;
                double s = -q-r;
                int rq = (int)Math.Round(q), rr = (int)Math.Round(r), rs = (int)Math.Round(s);
                double dq = Math.Abs(rq-q), dr = Math.Abs(rr-r), ds = Math.Abs(rs-s);
                if (dq > dr && dq > ds) rq = -rr-rs;
                else if (dr > ds) rr = -rq-rs;
                if (rq >= 0 && rq < Width && rr >= 0 && rr < Height && Cells[rr*Width+rq].Blocked) return false;
            }
            return true;
        }
        public bool CanAct(Unit u) => !Finished && u != null && u.Alive && u.Enemy == EnemyTurn;
        public bool CanAttack(Unit u, Unit target) => CanAct(u) && target != null && target.Alive &&
            target.Enemy != u.Enemy && u.AP >= BasicAttackCost(u) && Distance(u.CellId, target.CellId) <= BasicAttackRange(u) && HasSight(u.CellId, target.CellId);
        public int AttackDamage(Unit u, Unit target) => Math.Max(0, Math.Max(1, BasicAttackPower(u) - (target.Guarding ? 2 : 0)) - target.Shield);
        public bool Move(Unit u, int destination)
        {
            if (!CanAct(u)) return false;
            var path = Path(u, destination);
            if (path.Count == 0 || path.Count > u.AP) return false;
            u.CellId = destination; u.AP -= path.Count; u.Guarding = false; MovementPassive(u,path.Count);
            return true;
        }
        public bool Attack(Unit u, Unit target)
        {
            if (!CanAttack(u, target)) return false;
            if(u.Hero!=null) return TryUseEquipment(u,HeroEquipment.Weapon(u.Inventory).Id,target.CellId,out _);
            u.AP -= AttackCost; u.Guarding = false;
            int damage = DealDamage(u,target,u.Damage);
            Note(u.Name + " hits " + target.Name + " for " + damage + (target.Alive ? "." : ". Slain."));
            return true;
        }
        public bool Guard(Unit u)
        {
            if (!CanAct(u) || u.AP < 1) return false;
            u.AP = 0; u.Guarding = true; Note(u.Name + " guards: incoming damage reduced by 2."); return true;
        }
        public bool Heal(Unit u)
        {
            if (!CanAct(u) || u.AP < 2 || u.Tonics < 1 || u.HP >= u.MaxHP) return false;
            u.AP -= 2; u.Tonics--; u.HP = Math.Min(u.MaxHP, u.HP+6);
            Note(u.Name + " drinks a tonic (+6 HP, up to maximum)."); return true;
        }
        public void EndTurn()
        {
            if (Finished) return;
            if (!EnemyTurn)
                foreach (var hero in Units.Where(u=>!u.Enemy && u.Hero!=null))
                { hero.Discard.AddRange(hero.Hand); hero.Hand.Clear(); }
            EnemyTurn = !EnemyTurn;
            if (!EnemyTurn) Round++;
            foreach(var bleeding in Units.Where(u=>u.Alive && u.Enemy==EnemyTurn && u.BleedTurns>0))
            {
                bleeding.BleedTurns--; bleeding.HP=Math.Max(0,bleeding.HP-1);
                Note(bleeding.Name+" loses 1 HP to Bleed"+(bleeding.Alive ? "." : ". Slain."));
            }
            foreach (var u in Units.Where(u => u.Alive && u.Enemy == EnemyTurn))
            {
                u.AP = MaxAP; u.Guarding = false; u.Shield=0; u.InnateUsed=false;
                if(u.Hero!=null) Draw(u,HeroCards.HandSize);
            }
            Note(EnemyTurn ? "The revenants advance." : "Round " + Round + ": your squad is ready.");
        }
        // One bounded AI action. False means the unit has completed its turn.
        public bool EnemyAction(Unit u)
        {
            if (!EnemyTurn || !CanAct(u) || u.AP == 0) return false;
            var targets = Units.Where(t => t.Alive && !t.Enemy).OrderBy(t => t.HP).ToList();
            var target = targets.FirstOrDefault(t => CanAttack(u, t));
            if (target != null) return Attack(u, target);
            List<int> best = null;
            foreach (var t in targets)
                foreach (var cell in Cells)
                {
                    if (cell.Blocked || At(cell.Id) != null || Distance(cell.Id,t.CellId) > u.Range || !HasSight(cell.Id,t.CellId)) continue;
                    var path = Path(u,cell.Id);
                    if (path.Count > 0 && (best == null || path.Count < best.Count)) best = path;
                }
            if (best != null && Move(u, best[0])) return true;
            Guard(u); return false;
        }
    }
}
