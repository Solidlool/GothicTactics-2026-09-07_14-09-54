using System;
using System.Collections.Generic;
using System.Linq;

namespace GothicTactics.Skirmish
{
    public sealed partial class SkirmishBattle
    {
        private Random random = new Random();
        public SkirmishBattle(IEnumerable<HeroLoadout> loadouts,int seed) : this()
        {
            if(loadouts==null) throw new ArgumentNullException(nameof(loadouts));
            var party=loadouts.ToList();
            if(party.Count<1 || party.Count>4) throw new ArgumentException("Choose 1 to 4 heroes.");
            if(party.Select(l=>l?.HeroId).Distinct().Count()!=party.Count) throw new ArgumentException("Each hero can only join once.");
            foreach(var loadout in party)
            { string error=HeroCards.Validate(loadout); if(error!=null) throw new ArgumentException(error); }
            random=new Random(seed);
            Units.RemoveAll(u=>!u.Enemy);
            while(Units.Count>party.Count+1) Units.RemoveAt(Units.Count-1);
            foreach(var enemy in Units)
            { enemy.MaxHP=Math.Max(1,(int)Math.Round(enemy.MaxHP*(.5f+.25f*party.Count))); enemy.HP=enemy.MaxHP; }
            for(int i=0;i<party.Count;i++)
            {
                var h=HeroCards.Hero(party[i].HeroId);
                var unit=new Unit { Name=h.Name,Role=h.Role,Hero=h,CellId=(i+2)*Width+1,HP=h.HP,MaxHP=h.HP,AP=MaxAP,Damage=h.Damage,Range=h.Range };
                unit.DrawPile.AddRange(party[i].Deck); Shuffle(unit.DrawPile); Draw(unit,HeroCards.HandSize); Units.Insert(i,unit);
            }
            Log.Clear(); Note("The party enters the sanctuary. Defeat every revenant.");
        }
        private void Shuffle(List<string> cards)
        {
            for(int i=cards.Count-1;i>0;i--) { int j=random.Next(i+1); string c=cards[i]; cards[i]=cards[j]; cards[j]=c; }
        }
        public void Draw(Unit unit,int count)
        {
            for(int i=0;i<count && unit.Hand.Count<HeroCards.HandLimit;i++)
            {
                if(unit.DrawPile.Count==0)
                {
                    if(unit.Discard.Count==0) break;
                    unit.DrawPile.AddRange(unit.Discard); unit.Discard.Clear(); Shuffle(unit.DrawPile);
                }
                int last=unit.DrawPile.Count-1; unit.Hand.Add(unit.DrawPile[last]); unit.DrawPile.RemoveAt(last);
            }
        }
        private void Gain(Unit u) { u.Resource=Math.Min(3,u.Resource+1); }
        private void MovementPassive(Unit u,int steps) { if(u.Hero?.Id=="graveblade" && steps>=2) Gain(u); }
        private int DealDamage(Unit source,Unit target,int power)
        {
            int incoming=Math.Max(1,power-(target.Guarding ? 2 : 0));
            int absorbed=Math.Min(target.Shield,incoming); target.Shield-=absorbed;
            int damage=Math.Min(target.HP,incoming-absorbed); target.HP-=damage;
            if(damage>0 && target.Hero?.Id=="warden") Gain(target);
            return damage;
        }
        public string CardError(Unit u,CardDefinition card,int targetId,bool innate=false)
        {
            if(!CanAct(u) || u.Enemy || u.Hero==null) return "This hero cannot act now.";
            if(!HeroCards.Allowed(u.Hero,card)) return "This card does not belong to this hero's affinities.";
            if(innate)
            { if(card.Id!=u.Hero.Innate || u.InnateUsed) return "Innate already used this turn."; }
            else if(!u.Hand.Contains(card.Id)) return "Card is not in hand.";
            if(u.AP<card.Cost) return "Not enough AP.";
            if(targetId<0 || targetId>=Cells.Length) return "Choose a target.";
            var target=At(targetId);
            if(card.Target==CardTarget.Self && target!=u) return "Choose yourself.";
            if(card.Target==CardTarget.Ally && (target==null || target.Enemy)) return "Choose a living hero.";
            if(card.Target==CardTarget.Enemy && (target==null || !target.Enemy)) return "Choose a living enemy.";
            if(card.Target==CardTarget.Hex)
            {
                var path=Path(u,targetId);
                if(path.Count==0 || path.Count>card.Power) return "Choose a reachable empty hex within "+card.Power+" steps.";
            }
            else if(Distance(u.CellId,targetId)>card.Range || !HasSight(u.CellId,targetId)) return "Target is out of range or behind a ruin.";
            if(card.Effect==CardEffect.Heal && target.HP==target.MaxHP) return "Target is already at full HP.";
            if(card.Effect==CardEffect.Draw && u.DrawPile.Count+u.Discard.Count==0) return "No cards left to draw.";
            if(card.Effect==CardEffect.Draw && u.Hand.Count-(innate ? 0 : 1)>=HeroCards.HandLimit) return "Hand is full.";
            return null;
        }
        public bool PlayCard(Unit u,CardDefinition card,int targetId,bool innate=false)
        {
            string error=CardError(u,card,targetId,innate);
            if(error!=null) { Note(error); return false; }
            var target=At(targetId);
            int power=card.Power+(card.SpendResource ? u.Resource : 0);
            u.AP-=card.Cost;
            if(innate) u.InnateUsed=true; else u.Hand.Remove(card.Id);
            if(card.SpendResource) u.Resource=0;
            switch(card.Effect)
            {
                case CardEffect.Damage:
                    u.Guarding=false;
                    int damage=DealDamage(u,target,power);
                    Note(u.Name+" / "+card.Name+": "+damage+" damage to "+target.Name+(target.Alive ? "." : ". Slain."));
                    break;
                case CardEffect.Shield:
                    target.Shield+=power; Note(target.Name+" gains "+power+" shield.");
                    if(u.Hero.Id=="paladin" && target!=u) Gain(u);
                    break;
                case CardEffect.Heal:
                    int restored=Math.Min(power,target.MaxHP-target.HP); target.HP+=restored;
                    Note(target.Name+" restores "+restored+" HP.");
                    if(restored>0 && (u.Hero.Id=="keeper" || (u.Hero.Id=="paladin" && target!=u))) Gain(u);
                    break;
                case CardEffect.Draw: Draw(u,power); Note(u.Name+" uses "+card.Name+"."); break;
                case CardEffect.Move:
                    int steps=Path(u,targetId).Count; u.CellId=targetId; u.Guarding=false; MovementPassive(u,steps);
                    Note(u.Name+" moves "+steps+" hexes with "+card.Name+"."); break;
            }
            if(u.Hero.Id=="sorcerer" && (card.Affinity & Affinity.Intellect)!=0 && !card.SpendResource) Gain(u);
            // A draw spell cannot immediately draw itself from an otherwise empty pile.
            if(!innate) u.Discard.Add(card.Id);
            return true;
        }
    }
}
