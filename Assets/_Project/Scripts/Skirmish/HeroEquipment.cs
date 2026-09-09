using System;
using System.Collections.Generic;
using System.Linq;

namespace GothicTactics.Skirmish
{
    public enum GearSlot { MainHand, OffHand, Armour, Trinket }
    [Flags] public enum GearTag { None=0, Melee=1, Dagger=2, Axe=4, Bow=8, Shield=16, Staff=32, Tome=64 }
    public sealed class GearDefinition
    {
        public readonly string Id,Name,Description;
        public readonly GearSlot Slot;
        public readonly GearTag Tags;
        public readonly bool TwoHanded;
        public readonly int Health,SpellPower,WardPower,Bleed;
        public readonly CardDefinition Action;
        public GearDefinition(string id,string name,GearSlot slot,GearTag tags,string description,CardDefinition action=null,bool twoHanded=false,int health=0,int spellPower=0,int wardPower=0,int bleed=0)
        { Id=id; Name=name; Slot=slot; Tags=tags; Description=description; Action=action; TwoHanded=twoHanded; Health=health; SpellPower=spellPower; WardPower=wardPower; Bleed=bleed; }
    }
    public sealed class HeroInventory
    {
        public const int Capacity=6;
        private readonly string[] equipped=new string[4];
        public readonly List<string> Backpack=new List<string>();
        public string this[GearSlot slot] => equipped[(int)slot];
        public IEnumerable<GearDefinition> Equipped => equipped.Where(id=>id!=null).Select(HeroEquipment.Get);
        public GearTag Tags => Equipped.Aggregate(GearTag.None,(tags,item)=>tags|item.Tags);
        public int Health => Equipped.Sum(g=>g.Health);
        public int SpellPower => Equipped.Sum(g=>g.SpellPower);
        public int WardPower => Equipped.Sum(g=>g.WardPower);
        public bool Meets(GearTag any) => any==GearTag.None || (Tags & any)!=0;
        public HeroInventory Clone()
        { var result=new HeroInventory(); Array.Copy(equipped,result.equipped,4); result.Backpack.AddRange(Backpack); return result; }
        public bool Add(string id,out string error)
        {
            error=HeroEquipment.Get(id)==null ? "Unknown item." : Backpack.Count>=Capacity ? "Backpack is full (6 items)." : null;
            if(error!=null) return false;
            Backpack.Add(id); return true;
        }
        public bool Equip(int index,out string error)
        {
            error=null;
            if(index<0 || index>=Backpack.Count) { error="Choose an item in your backpack."; return false; }
            var item=HeroEquipment.Get(Backpack[index]);
            if(item==null) { error="Unknown item."; return false; }
            var displaced=new List<int> { (int)item.Slot };
            if(item.TwoHanded) displaced.Add((int)GearSlot.OffHand);
            if(item.Slot==GearSlot.OffHand && HeroEquipment.Get(this[GearSlot.MainHand])?.TwoHanded==true) displaced.Add((int)GearSlot.MainHand);
            int returned=displaced.Count(slot=>equipped[slot]!=null);
            if(Backpack.Count-1+returned>Capacity) { error="Make space: this swap returns "+returned+" equipped items to your backpack."; return false; }
            Backpack.RemoveAt(index);
            foreach(int slot in displaced)
            { if(equipped[slot]!=null) Backpack.Add(equipped[slot]); equipped[slot]=null; }
            equipped[(int)item.Slot]=item.Id;
            return true;
        }
        public bool Unequip(GearSlot slot,out string error)
        {
            error=equipped[(int)slot]==null ? "Slot is empty." : Backpack.Count>=Capacity ? "Backpack is full." : null;
            if(error!=null) return false;
            Backpack.Add(equipped[(int)slot]); equipped[(int)slot]=null; return true;
        }
        public string Validate()
        {
            if(Backpack.Count>Capacity || Backpack.Any(id=>HeroEquipment.Get(id)==null)) return "Invalid backpack.";
            for(int slot=0;slot<equipped.Length;slot++)
                if(equipped[slot]!=null && (HeroEquipment.Get(equipped[slot])==null || (int)HeroEquipment.Get(equipped[slot]).Slot!=slot)) return "Item is in the wrong slot.";
            if(HeroEquipment.Get(this[GearSlot.MainHand])?.TwoHanded==true && this[GearSlot.OffHand]!=null) return "Two-handed weapons occupy both hands.";
            return null;
        }
        public static HeroInventory Starter(string hero)
        {
            var bag=new HeroInventory();
            string[] items=hero=="sorcerer" ? new[]{"staff"} : hero=="graveblade" ? new[]{"bow"} : hero=="keeper" ? new[]{"dagger","tome"} : new[]{"sword","shield"};
            foreach(string id in items) { bag.Add(id,out _); bag.Equip(0,out _); }
            return bag;
        }
    }
    public static class HeroEquipment
    {
        private static CardDefinition Attack(string id,string name,int cost,int range,int damage,string text) =>
            new CardDefinition("gear_"+id,name,Affinity.Neutral,cost,range,damage,CardEffect.Damage,CardTarget.Enemy,text);
        public static readonly GearDefinition Unarmed=new GearDefinition("unarmed","Unarmed",GearSlot.MainHand,GearTag.None,"Fallback when no weapon is equipped.",Attack("unarmed","Punch",1,1,1,"Deal 1 damage. Range 1. Repeatable."));
        public static readonly GearDefinition[] Items =
        {
            new GearDefinition("dagger","Iron Dagger",GearSlot.MainHand,GearTag.Melee|GearTag.Dagger,"1 AP / 2 damage / range 1. One hand.",Attack("dagger","Stab",1,1,2,"Deal 2 damage. Range 1. Repeatable.")),
            new GearDefinition("sword","Arming Sword",GearSlot.MainHand,GearTag.Melee,"2 AP / 4 damage / range 1. One hand.",Attack("sword","Slash",2,1,4,"Deal 4 damage. Range 1. Repeatable.")),
            new GearDefinition("axe","Double Axe",GearSlot.MainHand,GearTag.Melee|GearTag.Axe,"3 AP / 7 damage / range 1. Two hands. HP damage applies Bleed for 2 turns.",Attack("axe","Rending Chop",3,1,7,"Deal 7 damage. On HP damage: Bleed 2 turns."),true,bleed:2),
            new GearDefinition("bow","Hunting Bow",GearSlot.MainHand,GearTag.Bow,"2 AP / 3 damage / range 4. Two hands.",Attack("bow","Loose Arrow",2,4,3,"Deal 3 damage. Range 4. Repeatable."),true),
            new GearDefinition("staff","Ash Staff",GearSlot.MainHand,GearTag.Staff,"2 AP / 3 damage / range 3. Two hands. Enables arcane spells.",Attack("staff","Arcane Spark",2,3,3,"Deal 3 damage. Range 3. Repeatable."),true),
            new GearDefinition("tome","Worn Tome",GearSlot.OffHand,GearTag.Tome,"2 AP / 2 damage / range 3. Enables arcane spells alongside a one-handed weapon.",Attack("tome","Runic Bolt",2,3,2,"Deal 2 damage. Range 3. Repeatable.")),
            new GearDefinition("shield","Iron Shield",GearSlot.OffHand,GearTag.Shield,"1 AP / gain 3 shield. Enables shield cards.",new CardDefinition("gear_shield","Raise Shield",Affinity.Neutral,1,0,3,CardEffect.Shield,CardTarget.Self,"Gain 3 shield until next turn. Repeatable.")),
            new GearDefinition("leather","Leather Armour",GearSlot.Armour,GearTag.None,"+3 maximum HP.",health:3),
            new GearDefinition("plate","Plate Armour",GearSlot.Armour,GearTag.None,"+5 maximum HP.",health:5),
            new GearDefinition("robes","Scholar's Robes",GearSlot.Armour,GearTag.None,"+1 damage to Intellect damage cards and staff/tome attacks.",spellPower:1),
            new GearDefinition("charm","Warding Charm",GearSlot.Trinket,GearTag.None,"+1 shield whenever you grant shield through a card, innate or equipment.",wardPower:1),
        };
        public static GearDefinition Get(string id) => Items.FirstOrDefault(g=>g.Id==id);
        public static GearDefinition Weapon(HeroInventory inventory) => Get(inventory?[GearSlot.MainHand]) ?? Unarmed;
        public static string Requirement(GearTag tags)
        {
            if(tags==(GearTag.Staff|GearTag.Tome)) return "Staff OR tome";
            return tags==GearTag.None ? "None" : tags.ToString();
        }
    }
}
