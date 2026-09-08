using System;
using System.Collections.Generic;
using System.Linq;

namespace GothicTactics.Skirmish
{
    [Flags] public enum Affinity { Neutral = 0, Might = 1, Agility = 2, Intellect = 4, Spirit = 8 }
    public enum CardEffect { Damage, Shield, Heal, Draw, Move }
    public enum CardTarget { Self, Ally, Enemy, Hex }

    public sealed class CardDefinition
    {
        public readonly string Id, Name, Text, HeroId;
        public readonly Affinity Affinity;
        public readonly int Cost, Range, Power;
        public readonly CardEffect Effect;
        public readonly CardTarget Target;
        public readonly bool SpendResource;
        public CardDefinition(string id,string name,Affinity affinity,int cost,int range,int power,CardEffect effect,CardTarget target,string text,string heroId=null,bool spend=false)
        { Id=id; Name=name; Affinity=affinity; Cost=cost; Range=range; Power=power; Effect=effect; Target=target; Text=text; HeroId=heroId; SpendResource=spend; }
    }
    public sealed class HeroDefinition
    {
        public readonly string Id, Name, Role, Resource, Passive, Signature, Innate;
        public readonly Affinity Affinities;
        public readonly int HP, Damage, Range;
        public readonly string[] Starter;
        public HeroDefinition(string id,string name,string role,Affinity affinities,int hp,int damage,int range,string resource,string passive,string signature,string innate,params string[] starter)
        { Id=id; Name=name; Role=role; Affinities=affinities; HP=hp; Damage=damage; Range=range; Resource=resource; Passive=passive; Signature=signature; Innate=innate; Starter=starter; }
    }
    public sealed class HeroLoadout
    {
        public string HeroId;
        public readonly List<string> Deck = new List<string>();
        public HeroLoadout(string heroId) { HeroId=heroId; Deck.AddRange(HeroCards.Hero(heroId).Starter); }
    }
    public static class HeroCards
    {
        public const int DeckSize=10, HandSize=5, HandLimit=8;
        public static readonly CardDefinition[] Cards =
        {
            new CardDefinition("prepare","Prepare",Affinity.Neutral,1,0,2,CardEffect.Draw,CardTarget.Self,"Draw 2 cards (hand limit 8)."),
            new CardDefinition("bandage","Field Dressing",Affinity.Neutral,1,0,3,CardEffect.Heal,CardTarget.Self,"Restore 3 HP to yourself."),
            new CardDefinition("brace","Brace",Affinity.Neutral,1,0,3,CardEffect.Shield,CardTarget.Self,"Gain 3 shield until your next turn."),
            new CardDefinition("crush","Crushing Blow",Affinity.Might,3,1,6,CardEffect.Damage,CardTarget.Enemy,"Deal 6 damage. Range 1."),
            new CardDefinition("bulwark","Bulwark",Affinity.Might,2,1,6,CardEffect.Shield,CardTarget.Ally,"Give yourself or an ally 6 shield. Range 1."),
            new CardDefinition("lunge","Lunge",Affinity.Might,1,2,2,CardEffect.Move,CardTarget.Hex,"Move up to 2 hexes along a clear path."),
            new CardDefinition("pierce","Precise Shot",Affinity.Agility,2,4,4,CardEffect.Damage,CardTarget.Enemy,"Deal 4 damage. Range 4."),
            new CardDefinition("dash","Shadow Step",Affinity.Agility,1,3,3,CardEffect.Move,CardTarget.Hex,"Move up to 3 hexes along a clear path."),
            new CardDefinition("evade","Evasive Stance",Affinity.Agility,1,0,4,CardEffect.Shield,CardTarget.Self,"Gain 4 shield until your next turn."),
            new CardDefinition("bolt","Ember Bolt",Affinity.Intellect,2,4,4,CardEffect.Damage,CardTarget.Enemy,"Deal 4 damage. Range 4."),
            new CardDefinition("study","Arcane Study",Affinity.Intellect,1,0,3,CardEffect.Draw,CardTarget.Self,"Draw 3 cards (hand limit 8)."),
            new CardDefinition("barrier","Arcane Barrier",Affinity.Intellect,2,3,5,CardEffect.Shield,CardTarget.Ally,"Give yourself or an ally 5 shield. Range 3."),
            new CardDefinition("mend","Mending Light",Affinity.Spirit,2,3,5,CardEffect.Heal,CardTarget.Ally,"Restore 5 HP to yourself or an ally. Range 3."),
            new CardDefinition("ward","Ancestral Ward",Affinity.Spirit,1,3,3,CardEffect.Shield,CardTarget.Ally,"Give yourself or an ally 3 shield. Range 3."),
            new CardDefinition("smite","Spirit Lance",Affinity.Spirit,2,3,3,CardEffect.Damage,CardTarget.Enemy,"Deal 3 damage. Range 3."),
            new CardDefinition("judgement","Divine Judgement",Affinity.Might|Affinity.Spirit,3,2,6,CardEffect.Damage,CardTarget.Enemy,"Requires Might AND Spirit. Deal 6 damage. Range 2."),
            new CardDefinition("retaliate","Iron Reckoning",Affinity.Might,2,1,4,CardEffect.Damage,CardTarget.Enemy,"Deal 4 + Resolve damage; spend all Resolve.","warden",true),
            new CardDefinition("ambush","Grave Ambush",Affinity.Agility,2,3,4,CardEffect.Damage,CardTarget.Enemy,"Deal 4 + Momentum damage; spend all Momentum.","graveblade",true),
            new CardDefinition("release","Cinder Release",Affinity.Intellect,2,4,4,CardEffect.Damage,CardTarget.Enemy,"Deal 4 + Heat damage; spend all Heat.","sorcerer",true),
            new CardDefinition("communion","Ancestral Communion",Affinity.Spirit,1,3,4,CardEffect.Shield,CardTarget.Ally,"Give 4 + Grace shield; spend all Grace.","keeper",true),
            new CardDefinition("verdict","Oathkeeper's Verdict",Affinity.Might|Affinity.Spirit,2,2,4,CardEffect.Damage,CardTarget.Enemy,"Deal 4 + Conviction damage; spend all Conviction.","paladin",true),
        };
        public static readonly CardDefinition[] Innates =
        {
            new CardDefinition("stand","Stand Firm",Affinity.Might,1,0,3,CardEffect.Shield,CardTarget.Self,"Gain 3 shield. Once per turn.","warden"),
            new CardDefinition("slip","Slip Away",Affinity.Agility,1,2,2,CardEffect.Move,CardTarget.Hex,"Move up to 2 hexes. Once per turn.","graveblade"),
            new CardDefinition("focus","Gather Embers",Affinity.Intellect,1,0,1,CardEffect.Draw,CardTarget.Self,"Draw 1 card and gain 1 Heat. Once per turn.","sorcerer"),
            new CardDefinition("prayer","Ancestral Prayer",Affinity.Spirit,1,3,3,CardEffect.Heal,CardTarget.Ally,"Restore 3 HP. Range 3. Once per turn.","keeper"),
            new CardDefinition("protect","Protect",Affinity.Spirit,1,3,3,CardEffect.Shield,CardTarget.Ally,"Give 3 shield. Range 3. Once per turn.","paladin"),
        };
        public static readonly HeroDefinition[] Heroes =
        {
            new HeroDefinition("warden","Voss","Warden",Affinity.Might,16,4,1,"Resolve","Gain 1 Resolve when an attack damages your HP (max 3).","retaliate","stand","retaliate","crush","crush","bulwark","bulwark","lunge","lunge","prepare","bandage","brace"),
            new HeroDefinition("graveblade","Mara","Graveblade",Affinity.Agility,11,3,3,"Momentum","Gain 1 Momentum when you move at least 2 hexes in one action (max 3).","ambush","slip","ambush","pierce","pierce","dash","dash","evade","evade","prepare","bandage","brace"),
            new HeroDefinition("sorcerer","Cinder","Ash Sorcerer",Affinity.Intellect,10,2,3,"Heat","Gain 1 Heat after an Intellect card or innate. Cinder Release spends it instead (max 3).","release","focus","release","bolt","bolt","study","study","barrier","barrier","prepare","bandage","brace"),
            new HeroDefinition("keeper","Sister Ash","Ancestor Keeper",Affinity.Spirit,12,2,2,"Grace","Gain 1 Grace whenever your cards or innate restore HP (max 3).","communion","prayer","communion","mend","mend","ward","ward","smite","smite","prepare","bandage","brace"),
            new HeroDefinition("paladin","Aldric","Oathbound Paladin",Affinity.Might|Affinity.Spirit,14,3,1,"Conviction","Gain 1 Conviction when your cards or innate heal or shield another hero (max 3).","verdict","protect","verdict","crush","bulwark","lunge","mend","ward","smite","judgement","prepare","bandage"),
        };
        public static HeroDefinition Hero(string id) => Heroes.FirstOrDefault(h=>h.Id==id);
        public static CardDefinition Card(string id) => Cards.FirstOrDefault(c=>c.Id==id);
        public static CardDefinition Innate(string id) => Innates.FirstOrDefault(c=>c.Id==id);
        public static bool Allowed(HeroDefinition hero,CardDefinition card) => hero!=null && card!=null &&
            (card.HeroId==null || card.HeroId==hero.Id) && (hero.Affinities & card.Affinity)==card.Affinity;
        public static string Validate(HeroLoadout loadout)
        {
            if(loadout==null || Hero(loadout.HeroId)==null) return "Choose a valid hero.";
            var hero=Hero(loadout.HeroId);
            if(loadout.Deck.Count!=DeckSize) return "Deck must contain exactly 10 cards.";
            if(loadout.Deck.Any(id=>!Allowed(hero,Card(id)))) return "Deck contains an unavailable card.";
            if(loadout.Deck.Count(id=>id==hero.Signature)!=1) return "Include exactly one hero signature card.";
            if(loadout.Deck.GroupBy(id=>id).Any(g=>g.Count()>(Card(g.Key).HeroId==null ? 2 : 1))) return "Maximum 2 copies of shared cards and 1 signature.";
            return null;
        }
    }
}
