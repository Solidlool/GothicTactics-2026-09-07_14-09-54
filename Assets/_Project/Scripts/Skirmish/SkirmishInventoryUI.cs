using System;
using System.Linq;
using UnityEngine;

namespace GothicTactics.Skirmish
{
    public sealed partial class SkirmishController
    {
        private bool equipmentTab;
        private GearDefinition armedEquipment;
        private Vector2 armouryScroll,inventoryScroll;
        private string inventoryMessage="Choose an item from the armoury, then equip it from your backpack.";
        private string ArmedError(int targetId) => armedEquipment!=null ? battle.EquipmentError(selected,armedEquipment.Id,targetId) : battle.CardError(selected,armedCard,targetId,armedInnate);
        private void ArmEquipment(GearDefinition item)
        {
            if(armedEquipment==item && armedCard!=null) { armedCard=null; armedEquipment=null; Paint(); return; }
            armedEquipment=item; armedCard=item.Action; armedInnate=false;
            cardFeedbackUntil=0; hint=item.Name+" / "+item.Action.Text;
            if(!battle.Cells.Any(c=>battle.EquipmentError(selected,item.Id,c.Id)==null)) ShowCardFeedback("No valid targets for "+item.Action.Name+". Nothing spent. Esc cancels.",true);
            Paint();
        }
        private float DrawEquipmentActions(float y)
        {
            var items=selected.Inventory.Equipped.Where(g=>g.Action!=null).ToList();
            if(selected.Inventory[GearSlot.MainHand]==null) items.Insert(0,HeroEquipment.Unarmed);
            foreach(var item in items)
            {
                GUI.enabled=!busy && !battle.EnemyTurn && battle.CanAct(selected) && selected.AP>=item.Action.Cost;
                GUI.backgroundColor=armedEquipment==item ? new Color(.95f,.72f,.35f) : Color.white;
                string stat=item.Action.Effect==CardEffect.Shield ? "+"+battle.ActionPower(selected,item.Action,item)+" shield" : battle.ActionPower(selected,item.Action,item)+" dmg / R"+item.Action.Range;
                if(GUI.Button(new Rect(14,y,230,34),item.Action.Name+" / "+item.Action.Cost+" AP / "+stat,new GUIStyle(button) { fontSize=12 })) ArmEquipment(item);
                GUI.enabled=true; GUI.backgroundColor=Color.white; y+=38;
            }
            GUI.Label(new Rect(18,y,224,34),"Equipment actions: repeat while AP lasts.\nClick enemy: main-hand attack.",small); return y+38;
        }
        private void DrawInventory(float width,float height,HeroLoadout loadout)
        {
            var bag=loadout.Inventory;
            GUI.Label(new Rect(280,267,width-310,28),"+"+bag.Health+" max HP / +"+bag.SpellPower+" arcane damage / +"+bag.WardPower+" shield power",body);
            float leftWidth=355,rightX=660,rightWidth=width-rightX-24,listHeight=height-413;
            inventoryScroll=GUI.BeginScrollView(new Rect(280,300,leftWidth,listHeight),inventoryScroll,new Rect(0,0,leftWidth-22,660));
            int y=0;
            foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot)))
            {
                var item=HeroEquipment.Get(bag[slot]);
                bool reserved=slot==GearSlot.OffHand && HeroEquipment.Get(bag[GearSlot.MainHand])?.TwoHanded==true;
                GUI.Label(new Rect(0,y,310,22),slot+" / "+(item?.Name ?? (reserved ? "Occupied by two-handed weapon" : "Empty")),body);
                GUI.Label(new Rect(0,y+24,244,56),item?.Description ?? "",small);
                GUI.enabled=item!=null;
                if(GUI.Button(new Rect(252,y+25,70,31),"To bag",button))
                { inventoryMessage=bag.Unequip(slot,out string unequipError) ? "Item moved to backpack." : unequipError; }
                GUI.enabled=true; y+=90;
            }
            GUI.Label(new Rect(0,y,320,25),"BACKPACK / "+bag.Backpack.Count+" of "+HeroInventory.Capacity,body); y+=30;
            int equip=-1,remove=-1;
            for(int i=0;i<bag.Backpack.Count;i++)
            {
                var item=HeroEquipment.Get(bag.Backpack[i]);
                if(GUI.Button(new Rect(0,y,235,35),item.Name+" / Equip",button)) equip=i;
                if(GUI.Button(new Rect(242,y,80,35),"Store",button)) remove=i;
                y+=42;
            }
            GUI.EndScrollView();
            if(equip>=0) inventoryMessage=bag.Equip(equip,out string equipError) ? "Equipped. Check your deck's requirements." : equipError;
            if(remove>=0) { bag.Backpack.RemoveAt(remove); inventoryMessage="Returned item to the armoury."; }
            GUI.Label(new Rect(rightX,301,rightWidth,23),"ARMOURY / add to backpack",body);
            armouryScroll=GUI.BeginScrollView(new Rect(rightX,330,rightWidth,listHeight-30),armouryScroll,new Rect(0,0,rightWidth-22,HeroEquipment.Items.Length*105));
            for(int i=0;i<HeroEquipment.Items.Length;i++)
            {
                var item=HeroEquipment.Items[i];
                GUI.enabled=bag.Backpack.Count<HeroInventory.Capacity;
                if(GUI.Button(new Rect(0,i*105,rightWidth-27,32),item.Name+" / "+item.Slot,button)) inventoryMessage=bag.Add(item.Id,out string addError) ? "Added "+item.Name+" to backpack." : addError;
                GUI.enabled=true;
                GUI.Label(new Rect(7,i*105+35,rightWidth-40,65),item.Description,small);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(280,height-111,width-310,25),inventoryMessage,small);
        }
    }
}
