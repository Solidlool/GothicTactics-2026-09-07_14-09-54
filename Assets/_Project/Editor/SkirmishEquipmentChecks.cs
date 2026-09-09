using System;
using System.Linq;
using GothicTactics.Skirmish;
using UnityEditor;
using UnityEngine;

namespace GothicTactics.Editor
{
    public static class SkirmishEquipmentChecks
    {
        private static void Check(bool valid,string message) { if(!valid) throw new Exception("Equipment check failed: "+message); }
        private static void Equip(HeroInventory bag,string id)
        { Check(bag.Add(id,out _),"add "+id); Check(bag.Equip(bag.Backpack.Count-1,out _),"equip "+id); }
        private static string Snapshot(HeroInventory bag) => string.Join("/",Enum.GetValues(typeof(GearSlot)).Cast<GearSlot>().Select(s=>bag[s]))+"|"+string.Join(",",bag.Backpack);
        private static SkirmishBattle Battle() => new SkirmishBattle(new[]{new HeroLoadout("warden"),new HeroLoadout("sorcerer")},15);
        [MenuItem("Gothic Tactics/Run Equipment Checks")]
        public static void Run()
        {
            foreach(var hero in HeroCards.Heroes) Check(HeroCards.Validate(new HeroLoadout(hero.Id))==null,"starter equipment and deck: "+hero.Id);
            var bag=HeroInventory.Starter("warden"); Equip(bag,"axe");
            Check(bag[GearSlot.MainHand]=="axe" && bag[GearSlot.OffHand]==null && bag.Backpack.Contains("sword") && bag.Backpack.Contains("shield"),"two-handed swap returns both items");
            Check(bag.Equip(bag.Backpack.IndexOf("shield"),out _) && bag[GearSlot.MainHand]==null && bag[GearSlot.OffHand]=="shield" && bag.Backpack.Contains("axe"),"off-hand returns two-handed weapon to bag");
            bag=HeroInventory.Starter("warden"); Check(bag.Add("axe",out _),"add axe");
            while(bag.Backpack.Count<HeroInventory.Capacity) Check(bag.Add("dagger",out _),"fill bag");
            string before=Snapshot(bag);
            Check(!bag.Equip(0,out string reason) && reason!=null && before==Snapshot(bag),"full-backpack swap rejects atomically");
            Check(!bag.Unequip(GearSlot.OffHand,out _) && before==Snapshot(bag),"full-backpack unequip retains item");
            bag.Backpack.RemoveAt(5); Check(bag.Equip(0,out _) && bag.Backpack.Count==6,"swap succeeds once space is made");
            var clone=bag.Clone(); clone.Backpack.Clear(); Check(bag.Backpack.Count==6,"inventory copies are independent");

            var build=new HeroLoadout("warden"); Equip(build.Inventory,"axe");
            Check(HeroCards.Validate(build)!=null,"shield cards cannot launch without equipped shield");
            Check(!build.Inventory.Meets(GearTag.Shield),"carried shield does not meet equipped requirement");
            var mage=new HeroLoadout("sorcerer"); Equip(mage.Inventory,"dagger"); Equip(mage.Inventory,"tome");
            Check(HeroCards.Validate(mage)==null,"tome fulfils staff OR tome requirement");
            Equip(mage.Inventory,"plate");
            var withArmour=new SkirmishBattle(new[]{mage},4);
            Check(withArmour.Units[0].MaxHP==HeroCards.Hero("sorcerer").HP+5,"armour adds max HP once");
            mage.Inventory.Unequip(GearSlot.Armour,out _);
            Check(withArmour.Units[0].Inventory.Health==5,"battle snapshots equipped loadout");

            var b=Battle(); var w=b.Units[0]; var enemy=b.Units.First(u=>u.Enemy);
            Equip(w.Inventory,"dagger"); enemy.CellId=w.CellId+1; enemy.MaxHP=enemy.HP=40;
            w.Discard.AddRange(w.Hand); w.Hand.Clear();
            string cards=string.Join(",",w.Hand)+"/"+string.Join(",",w.DrawPile)+"/"+string.Join(",",w.Discard);
            Check(b.TryUseEquipment(w,"dagger",enemy.CellId,out _) && b.TryUseEquipment(w,"dagger",enemy.CellId,out _) && w.AP==4 && enemy.HP==36,"repeatable dagger uses 1 AP and 2 damage each");
            Check(cards==string.Join(",",w.Hand)+"/"+string.Join(",",w.DrawPile)+"/"+string.Join(",",w.Discard),"equipment actions leave all card piles untouched");
            int ap=w.AP,hp=enemy.HP; enemy.CellId=30;
            Check(!b.TryUseEquipment(w,"dagger",enemy.CellId,out _) && w.AP==ap && enemy.HP==hp,"out-of-range gear action consumes nothing");
            Check(!b.TryUseEquipment(w,"staff",enemy.CellId,out _) && w.AP==ap,"unequipped gear cannot act");
            Equip(w.Inventory,"charm");
            Check(b.TryUseEquipment(w,"shield",w.CellId,out _) && b.TryUseEquipment(w,"shield",w.CellId,out _) && w.Shield==8 && !w.InnateUsed,"repeatable shield includes charm bonus and does not spend innate");

            b=Battle(); w=b.Units[0]; enemy=b.Units.First(u=>u.Enemy);
            Equip(w.Inventory,"axe"); enemy.CellId=w.CellId+1; enemy.HP=enemy.MaxHP=40; enemy.Shield=100;
            Check(b.TryUseEquipment(w,"axe",enemy.CellId,out _) && enemy.BleedTurns==0,"fully absorbed hit does not apply Bleed");
            enemy.Shield=0;
            Check(b.TryUseEquipment(w,"axe",enemy.CellId,out _) && enemy.HP==33 && enemy.BleedTurns==2 && w.AP==0,"axe applies Bleed after HP damage");
            Check(!b.TryUseEquipment(w,"axe",enemy.CellId,out _) && enemy.HP==33,"insufficient AP consumes nothing");
            enemy.Shield=100; b.EndTurn();
            Check(enemy.HP==32 && enemy.BleedTurns==1,"Bleed ticks once at target turn start, bypassing shield");
            b.EndTurn(); Check(enemy.HP==32,"no Bleed tick on opposing turn");
            b.EndTurn(); Check(enemy.HP==31 && enemy.BleedTurns==0,"Bleed expires after second tick");
            b.EndTurn(); b.EndTurn(); Check(enemy.HP==31,"expired Bleed does not tick");

            b=Battle(); var sorcerer=b.Units[1]; enemy=b.Units.First(u=>u.Enemy);
            Equip(sorcerer.Inventory,"robes"); enemy.CellId=sorcerer.CellId+1; enemy.HP=30;
            Check(b.TryUseEquipment(sorcerer,"staff",enemy.CellId,out _) && enemy.HP==26,"robes improve staff action");
            if(!sorcerer.Hand.Contains("bolt")) { sorcerer.DrawPile.Remove("bolt"); sorcerer.Hand.Add("bolt"); }
            sorcerer.Inventory.Unequip(GearSlot.MainHand,out _); ap=sorcerer.AP; int hand=sorcerer.Hand.Count;
            Check(!b.TryPlayCard(sorcerer,HeroCards.Card("bolt"),enemy.CellId,out _) && sorcerer.AP==ap && sorcerer.Hand.Count==hand,"runtime card equipment requirement consumes nothing");
            Debug.Log("All inventory and equipment checks passed.");
        }
    }
}
