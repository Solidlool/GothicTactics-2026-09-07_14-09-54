using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GothicTactics.Skirmish
{
    public sealed partial class SkirmishController
    {
        private readonly List<HeroLoadout> loadouts=new List<HeroLoadout>
        { new HeroLoadout("warden"),new HeroLoadout("sorcerer"),new HeroLoadout("paladin") };
        private bool preparing=true,armedInnate;
        private CardDefinition armedCard;
        private int editing;
        private Vector2 libraryScroll,deckScroll,handScroll;
        private static string SpriteRole(SkirmishBattle.Unit u)
        {
            if(u.Hero==null) return u.Role;
            switch(u.Hero.Id)
            {
                case "warden": case "paladin": return "Warden";
                case "graveblade": return "Arbalist";
                case "sorcerer": return "Invoker";
                default: return "Hexblade";
            }
        }
        private static Color CardColour(Affinity affinity)
        {
            switch(affinity)
            {
                case Affinity.Might:return new Color(.73f,.28f,.23f);
                case Affinity.Agility:return new Color(.32f,.66f,.39f);
                case Affinity.Intellect:return new Color(.32f,.52f,.88f);
                case Affinity.Spirit:return new Color(.90f,.85f,.67f);
                case Affinity.Neutral:return new Color(.56f,.55f,.52f);
                default:return new Color(.82f,.58f,.26f);
            }
        }
        private void DrawPreparation(float width,float height)
        {
            Panel(new Rect(0,0,width,height));
            GUI.Label(new Rect(24,16,width-48,38),"THE ASHEN BELL / HEROES & DECKS",title);
            GUI.Label(new Rect(24,55,width-48,25),"Local party: 1–4 heroes. Select a hero, customise their 10-card deck, then enter the sanctuary.",body);
            GUI.Label(new Rect(24,95,228,25),"YOUR PARTY",body);
            for(int i=0;i<loadouts.Count;i++)
            {
                var member=HeroCards.Hero(loadouts[i].HeroId);
                GUI.backgroundColor=i==editing ? new Color(.85f,.66f,.37f) : Color.white;
                if(GUI.Button(new Rect(24,128+i*57,228,50),member.Name+" / "+member.Role+"\n"+loadouts[i].Deck.Count+" / 10 cards",button))
                { editing=i; libraryScroll=deckScroll=Vector2.zero; }
            }
            GUI.backgroundColor=Color.white;
            GUI.enabled=loadouts.Count<4;
            if(GUI.Button(new Rect(24,365,110,32),"Add hero",button))
            { loadouts.Add(new HeroLoadout(HeroCards.Heroes.First(h=>loadouts.All(l=>l.HeroId!=h.Id)).Id)); editing=loadouts.Count-1; }
            GUI.enabled=loadouts.Count>1;
            if(GUI.Button(new Rect(142,365,110,32),"Remove",button)) { loadouts.RemoveAt(editing); editing=Mathf.Min(editing,loadouts.Count-1); }
            GUI.enabled=true;
            GUI.Label(new Rect(24,414,228,23),"CHANGE SELECTED HERO",small);
            for(int i=0;i<HeroCards.Heroes.Length;i++)
            {
                var h=HeroCards.Heroes[i];
                GUI.enabled=!loadouts.Where((l,n)=>n!=editing).Any(l=>l.HeroId==h.Id);
                GUI.backgroundColor=CardColour(h.Affinities);
                if(GUI.Button(new Rect(24,442+i*42,228,36),h.Role,button))
                { loadouts[editing]=new HeroLoadout(h.Id); libraryScroll=deckScroll=Vector2.zero; }
            }
            GUI.enabled=true; GUI.backgroundColor=Color.white;
            var loadout=loadouts[editing]; var hero=HeroCards.Hero(loadout.HeroId);
            GUI.Label(new Rect(280,98,width-310,32),hero.Name+" / "+hero.Role+" / "+hero.Affinities,title);
            GUI.Label(new Rect(280,137,width-310,44),hero.HP+" HP.  "+hero.Passive,body);
            GUI.Label(new Rect(280,182,width-310,38),"Innate: "+HeroCards.Innate(hero.Innate).Name+" — "+HeroCards.Innate(hero.Innate).Text,small);
            GUI.Label(new Rect(280,224,300,25),"DECK / "+loadout.Deck.Count+" OF 10",body);
            GUI.Label(new Rect(615,224,width-639,25),"AVAILABLE CARDS / click to add",body);
            float listHeight=height-355;
            deckScroll=GUI.BeginScrollView(new Rect(280,258,310,listHeight),deckScroll,new Rect(0,0,287,loadout.Deck.Count*60));
            int remove=-1;
            for(int i=0;i<loadout.Deck.Count;i++)
            {
                var card=HeroCards.Card(loadout.Deck[i]);
                GUI.backgroundColor=CardColour(card.Affinity); GUI.enabled=card.HeroId==null;
                if(GUI.Button(new Rect(0,i*60,280,54),card.Name+" / "+card.Cost+" AP\n"+(card.HeroId!=null ? "Hero signature (required)" : "Click to remove"),button)) remove=i;
            }
            GUI.enabled=true; GUI.backgroundColor=Color.white; GUI.EndScrollView();
            if(remove>=0) loadout.Deck.RemoveAt(remove);
            var available=HeroCards.Cards.Where(c=>HeroCards.Allowed(hero,c) && c.HeroId==null).ToList();
            float libraryWidth=width-639;
            libraryScroll=GUI.BeginScrollView(new Rect(615,258,libraryWidth,listHeight),libraryScroll,new Rect(0,0,libraryWidth-22,available.Count*88));
            for(int i=0;i<available.Count;i++)
            {
                var card=available[i]; int copies=loadout.Deck.Count(id=>id==card.Id);
                GUI.enabled=loadout.Deck.Count<HeroCards.DeckSize && copies<2;
                GUI.backgroundColor=CardColour(card.Affinity);
                if(GUI.Button(new Rect(0,i*88,libraryWidth-27,35),card.Name+" / "+card.Cost+" AP / "+copies+" of 2",button)) loadout.Deck.Add(card.Id);
                GUI.enabled=true; GUI.backgroundColor=Color.white;
                GUI.Label(new Rect(7,i*88+37,libraryWidth-40,47),card.Affinity+" — "+card.Text,small);
            }
            GUI.EndScrollView();
            string error=loadouts.Select(HeroCards.Validate).FirstOrDefault(e=>e!=null);
            GUI.Label(new Rect(280,height-88,width-570,60),error ?? "Decks ready. Unplayed cards discard at turn end. Draw 5 each turn; reshuffle when empty. Remove a card before adding a replacement.",small);
            if(GUI.Button(new Rect(24,height-69,228,40),"Restore starter deck",button)) loadouts[editing]=new HeroLoadout(hero.Id);
            GUI.enabled=error==null;
            if(GUI.Button(new Rect(width-265,height-69,240,43),"ENTER THE SANCTUARY",button)) { preparing=false; Restart(); }
            GUI.enabled=true;
        }
        private void Arm(CardDefinition card,bool innate=false)
        {
            if(armedCard==card && armedInnate==innate) { armedCard=null; armedInnate=false; Paint(); return; }
            armedCard=card; armedInnate=innate;
            hint=card.Name+": "+card.Text+" Click a target; Esc cancels.";
            Paint();
        }
        private void UseArmedCard(int targetId)
        {
            var card=armedCard; var actor=selected;
            var path=card.Effect==CardEffect.Move ? battle.Path(actor,targetId) : null;
            var target=battle.At(targetId);
            if(!battle.PlayCard(actor,card,targetId,armedInnate)) { hint=battle.Log[0]; return; }
            armedCard=null; armedInnate=false;
            hint=battle.Log[0];
            if(card.Effect==CardEffect.Move) StartCoroutine(Walk(actor,path));
            else if(card.Effect==CardEffect.Damage) StartCoroutine(Strike(actor,target));
            Refresh();
        }
        private void DrawHand(float width,float height)
        {
            Panel(new Rect(0,height-225,width,225));
            string context=armedCard!=null ? armedCard.Name+" / "+armedCard.Text+" Click target or press Esc." : hint;
            if(armedCard!=null && hover>=0)
            {
                string error=battle.CardError(selected,armedCard,hover,armedInnate);
                context=error ?? armedCard.Name+" / valid target / "+armedCard.Cost+" AP";
            }
            else if(hover>=0 && selected!=null)
            {
                var target=battle.At(hover);
                if(target!=null) context=target.Name+" / "+target.HP+" HP / "+target.Shield+" shield"+(battle.CanAttack(selected,target) ? " / Basic attack: "+battle.AttackDamage(selected,target)+" damage, 2 AP" : "");
                else if(costs.TryGetValue(hover,out int cost)) context="Move: "+cost+" AP / "+(selected.AP-cost)+" remaining";
            }
            GUI.Label(new Rect(20,height-216,width-310,30),context,small);
            if(selected?.Hero!=null)
            {
                GUI.Label(new Rect(20,height-184,width-310,25),selected.Name+" / HAND "+selected.Hand.Count+" / DRAW "+selected.DrawPile.Count+" / DISCARD "+selected.Discard.Count,small);
                float areaWidth=width-290,cardWidth=166;
                handScroll=GUI.BeginScrollView(new Rect(16,height-156,areaWidth,145),handScroll,new Rect(0,0,Mathf.Max(areaWidth-20,selected.Hand.Count*(cardWidth+8)),122));
                for(int i=0;i<selected.Hand.Count;i++)
                {
                    var card=HeroCards.Card(selected.Hand[i]); float x=i*(cardWidth+8);
                    GUI.enabled=!busy && !battle.EnemyTurn && battle.CanAct(selected) && selected.AP>=card.Cost;
                    GUI.backgroundColor=armedCard==card ? Color.white : CardColour(card.Affinity);
                    if(GUI.Button(new Rect(x,0,cardWidth,120),"",button)) Arm(card);
                    GUI.enabled=true; GUI.backgroundColor=Color.white;
                    GUI.Label(new Rect(x+8,5,cardWidth-16,39),card.Name+" / "+card.Cost+" AP",body);
                    GUI.Label(new Rect(x+8,43,cardWidth-16,22),card.HeroId!=null ? "SIGNATURE" : card.Affinity.ToString(),small);
                    GUI.Label(new Rect(x+8,65,cardWidth-16,53),card.Text,small);
                }
                GUI.EndScrollView();
            }
            GUI.Label(new Rect(width-260,height-178,237,63),"Click card, then target.\nEsc cancels. Tab switches hero.\nAP also pays for movement.",small);
            GUI.enabled=!busy && !battle.EnemyTurn && !battle.Finished;
            if(GUI.Button(new Rect(width-265,height-95,240,45),"END TURN / SPACE",button)) BeginEnemyTurn();
            GUI.enabled=true;
            GUI.Label(new Rect(width-260,height-43,237,25),"WASD pan / wheel zoom",small);
        }
    }
}
