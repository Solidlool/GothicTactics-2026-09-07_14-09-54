using System;
using System.Linq;

namespace GothicTactics.Skirmish
{
    public sealed partial class SkirmishBattle
    {
        public int BasicAttackCost(Unit u) => u.Hero==null ? AttackCost : HeroEquipment.Weapon(u.Inventory).Action.Cost;
        public int BasicAttackRange(Unit u) => u.Hero==null ? u.Range : HeroEquipment.Weapon(u.Inventory).Action.Range;
        public int BasicAttackPower(Unit u) => u.Hero==null ? u.Damage : ActionPower(u,HeroEquipment.Weapon(u.Inventory).Action,HeroEquipment.Weapon(u.Inventory));
        public int ActionPower(Unit u,CardDefinition card,GearDefinition gear=null)
        {
            int bonus=0;
            if(u.Inventory!=null)
            {
                if(card.Effect==CardEffect.Shield) bonus=u.Inventory.WardPower;
                if(card.Effect==CardEffect.Damage && ((card.Affinity & Affinity.Intellect)!=0 || (gear!=null && (gear.Tags & (GearTag.Staff|GearTag.Tome))!=0))) bonus=u.Inventory.SpellPower;
            }
            return card.Power+bonus+(card.SpendResource ? u.Resource : 0);
        }
        public GearDefinition UsableEquipment(Unit u,string id)
        {
            if(u?.Inventory==null) return null;
            if(id=="unarmed" && u.Inventory[GearSlot.MainHand]==null) return HeroEquipment.Unarmed;
            return u.Inventory.Equipped.FirstOrDefault(g=>g.Id==id && g.Action!=null);
        }
        public string EquipmentError(Unit u,string id,int targetId)
        {
            if(!CanAct(u) || u.Enemy || u.Hero==null) return "This hero cannot act now.";
            var item=UsableEquipment(u,id);
            if(item==null) return "That item is not equipped.";
            if(u.AP<item.Action.Cost) return "Not enough AP: "+item.Action.Name+" costs "+item.Action.Cost+" AP.";
            return TargetError(u,item.Action,targetId);
        }
        public bool TryUseEquipment(Unit u,string id,int targetId,out string error)
        {
            error=EquipmentError(u,id,targetId);
            if(error!=null) { Note(error); return false; }
            var item=UsableEquipment(u,id); var target=At(targetId);
            int power=ActionPower(u,item.Action,item);
            u.AP-=item.Action.Cost;
            if(item.Action.Effect==CardEffect.Damage)
            {
                u.Guarding=false;
                int damage=DealDamage(u,target,power);
                bool bleed=damage>0 && target.Alive && item.Bleed>0;
                if(bleed) target.BleedTurns=Math.Max(target.BleedTurns,item.Bleed);
                Note(u.Name+" / "+item.Action.Name+": "+damage+" damage to "+target.Name+(bleed ? "; Bleed for "+item.Bleed+" turns." : target.Alive ? "." : ". Slain."));
            }
            else if(item.Action.Effect==CardEffect.Shield)
            { target.Shield+=power; Note(u.Name+" / "+item.Action.Name+": +"+power+" shield."); }
            return true;
        }
    }
}
