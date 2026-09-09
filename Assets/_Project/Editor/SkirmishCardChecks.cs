using System;
using System.Linq;
using GothicTactics.Skirmish;
using UnityEditor;
using UnityEngine;

namespace GothicTactics.Editor
{
    public static class SkirmishCardChecks
    {
        private static void Check(bool condition,string message)
        { if(!condition) throw new Exception("Card rules check failed: "+message); }
        private static SkirmishBattle Battle() => new SkirmishBattle(new[] { new HeroLoadout("warden"),new HeroLoadout("sorcerer"),new HeroLoadout("paladin") },123);
        private static void InHand(SkirmishBattle.Unit u,string id)
        {
            if(u.Hand.Contains(id)) return;
            Check(u.DrawPile.Remove(id) || u.Discard.Remove(id),"test card must exist in deck");
            u.Hand.Add(id);
        }
        private static int Cards(SkirmishBattle.Unit u) => u.Hand.Count+u.DrawPile.Count+u.Discard.Count;
        private static string State(SkirmishBattle b) => b.Round+"/"+b.EnemyTurn+"/"+string.Join(";",b.Units.Select(u=>
            u.CellId+","+u.AP+","+u.HP+","+u.Shield+","+u.Resource+","+u.InnateUsed+","+u.Guarding+"/"+
            string.Join(",",u.Hand)+"/"+string.Join(",",u.DrawPile)+"/"+string.Join(",",u.Discard)));
        private static void Reject(SkirmishBattle b,SkirmishBattle.Unit u,string card,int target,string reason)
        {
            InHand(u,card); string before=State(b);
            Check(!b.TryPlayCard(u,HeroCards.Card(card),target,out string error),"reject "+reason);
            Check(!string.IsNullOrEmpty(error),"rejection supplies feedback: "+reason);
            Check(State(b)==before,"no AP, cards, resources, positions or health change: "+reason);
        }
        [MenuItem("Gothic Tactics/Run Hero and Card Checks")]
        public static void Run()
        {
            foreach(var hero in HeroCards.Heroes)
                Check(HeroCards.Validate(new HeroLoadout(hero.Id))==null,"valid starter: "+hero.Id);
            Check(HeroCards.Allowed(HeroCards.Hero("paladin"),HeroCards.Card("judgement")),"dual-affinity hero can use both-colour card");
            Check(!HeroCards.Allowed(HeroCards.Hero("warden"),HeroCards.Card("judgement")),"both affinities are required");
            Check(!HeroCards.Allowed(HeroCards.Hero("paladin"),HeroCards.Card("retaliate")),"signature is hero-specific");
            Check(HeroCards.Heroes.All(h=>HeroCards.Allowed(h,HeroCards.Card("brace"))),"neutral access");
            var illegal=new HeroLoadout("warden"); illegal.Deck[9]="crush";
            Check(HeroCards.Validate(illegal)!=null,"shared-card copy limit");
            illegal=new HeroLoadout("warden"); illegal.Deck[0]="brace";
            Check(HeroCards.Validate(illegal)!=null,"signature required");
            var b=Battle(); var w=b.Units[0]; var s=b.Units[1]; var p=b.Units[2];
            Check(b.Units.Count==7 && b.Units.Take(3).All(u=>u.Hand.Count==5 && Cards(u)==10),"opening hands");
            int ap=w.AP,hp=w.HP,hand=w.Hand.Count;
            Check(!b.PlayCard(w,HeroCards.Card("mend"),w.CellId) && w.AP==ap && w.HP==hp && w.Hand.Count==hand,"invalid play is atomic");
            Check(b.PlayCard(p,HeroCards.Innate("protect"),w.CellId,true) && w.Shield==3 && p.Resource==1 && p.AP==5,"paladin protects ally and gains Conviction");
            Check(!b.PlayCard(p,HeroCards.Innate("protect"),w.CellId,true) && p.AP==5,"innate once per turn");
            b.EndTurn();
            Check(w.Hand.Count==0 && w.Discard.Count==5 && w.Shield==3,"discard at end turn, shield survives enemy phase");
            var enemy=b.Units.First(u=>u.Enemy); enemy.CellId=w.CellId+1;
            int before=w.HP;
            Check(b.Attack(enemy,w) && w.HP==before && w.Shield==1 && w.Resource==0,"shield absorbs before HP and Resolve");
            Check(b.Attack(enemy,w) && w.HP==before-1 && w.Resource==1,"HP damage triggers Resolve");
            Check(!b.PlayCard(p,HeroCards.Innate("protect"),w.CellId,true),"enemy phase blocks hero card play");
            b.EndTurn();
            Check(w.Shield==0 && !p.InnateUsed && w.Hand.Count==5 && Cards(w)==10,"turn resets shield, innate and hand");
            b.EndTurn(); b.EndTurn();
            Check(w.Hand.Count==5 && Cards(w)==10,"reshuffle conserves cards");

            b=Battle(); s=b.Units[1]; enemy=b.Units.First(u=>u.Enemy); enemy.CellId=s.CellId+1; enemy.HP=enemy.MaxHP=30;
            InHand(s,"bolt");
            Check(b.PlayCard(s,HeroCards.Card("bolt"),enemy.CellId) && s.Resource==1 && s.AP==4,"Intellect card builds Heat");
            InHand(s,"release"); int enemyHP=enemy.HP;
            Check(b.PlayCard(s,HeroCards.Card("release"),enemy.CellId) && enemy.HP==enemyHP-5 && s.Resource==0 && s.AP==2,"signature spends Heat for damage");
            Check(Cards(s)==10,"played cards enter discard");
            InHand(s,"bolt"); s.AP=0; enemyHP=enemy.HP; hand=s.Hand.Count;
            Check(!b.PlayCard(s,HeroCards.Card("bolt"),enemy.CellId) && enemy.HP==enemyHP && s.Hand.Count==hand,"insufficient AP consumes nothing");

            var agile=new SkirmishBattle(new[]{new HeroLoadout("graveblade")},1); var g=agile.Units[0];
            Check(agile.PlayCard(g,HeroCards.Innate("slip"),g.CellId+2,true) && g.Resource==1 && g.AP==5,"movement card costs flat AP and builds Momentum");
            var spirit=new SkirmishBattle(new[]{new HeroLoadout("keeper")},1); var k=spirit.Units[0];
            Check(!spirit.PlayCard(k,HeroCards.Innate("prayer"),k.CellId,true) && !k.InnateUsed,"full-health heal rejected without spending innate");
            k.HP-=4;
            Check(spirit.PlayCard(k,HeroCards.Innate("prayer"),k.CellId,true) && k.HP==k.MaxHP-1 && k.Resource==1,"actual healing builds Grace");
            var four=new SkirmishBattle(HeroCards.Heroes.Take(4).Select(h=>new HeroLoadout(h.Id)),1);
            Check(four.Units.Count==8 && four.Units.Select(u=>u.CellId).Distinct().Count()==8,"four-hero roster has legal occupancy");
            b=Battle(); w=b.Units[0]; s=b.Units[1]; p=b.Units[2]; enemy=b.Units.First(u=>u.Enemy);
            Reject(b,w,"crush",enemy.CellId,"legal Might card on distant enemy");
            w.Resource=3;
            Reject(b,w,"retaliate",enemy.CellId,"out-of-range signature retains its resource");
            Reject(b,w,"lunge",77,"movement beyond card distance");
            Reject(b,w,"lunge",s.CellId,"movement onto occupied hex");
            Reject(b,w,"crush",w.CellId,"wrong team");
            Reject(b,w,"crush",-1,"click outside board");
            w.CellId=0; w.HP-=1;
            Reject(b,p,"mend",w.CellId,"healing beyond range");
            s.CellId=15; enemy.CellId=17;
            Reject(b,s,"bolt",enemy.CellId,"within range but ruin blocks sight");
            b=Battle(); w=b.Units[0]; enemy=b.Units.First(u=>u.Enemy);
            Reject(b,w,"crush",enemy.CellId,"first click too far");
            enemy.CellId=w.CellId+1; int beforeAP=w.AP,beforeHand=w.Hand.Count,beforeDiscard=w.Discard.Count;
            Check(b.TryPlayCard(w,HeroCards.Card("crush"),enemy.CellId,out string successError),"same card plays on a subsequent valid target");
            Check(successError==null && w.AP==beforeAP-3 && w.Hand.Count==beforeHand-1 && w.Discard.Count==beforeDiscard+1,"successful play spends exactly once");
            Debug.Log("All hero and card rules checks passed.");
        }
    }
}
