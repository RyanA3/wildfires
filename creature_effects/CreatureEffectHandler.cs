using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RW_19_Modding
{
    internal class CreatureEffectHandler : IComparable<CreatureEffectHandler>
    {

        private readonly AbstractCreature creature;
        private readonly List<Effect> effects;
        public bool should_remove = false;


        public CreatureEffectHandler(AbstractCreature creature)
        {
            this.creature = creature;
            this.effects = new List<Effect>();
        }


        //General updating
        public void Update()
        {

            //Don't do anything if the creature isn't in the world
            if (creature.realizedCreature == null || creature.slatedForDeletion)
            {
                should_remove = true;
                return;
            }

            foreach(Effect effect in effects)
                effect.Update(creature.realizedCreature);

            effects.RemoveAll(Effect.shouldRemoveThis);

        }

        public void AddEffect(Effect effect)
        {
            //Add time to the effect if it's already applied
            foreach(Effect check in effects)
            {
                if(check.type == effect.type)
                {
                    check.time += effect.time;
                    return;
                }
            }

            //Otherwise apply the effect if it's a new one
            effects.Add(effect);
        }

        public void AddEffect(int type, int time)
        {
            Effect effect = GetEffect(type);
            if (effect != null) effect.time += time;
            else
            {
                switch (type)
                {
                    case Effect.BURNING:
                        effects.Add(new BurningEffect(time));
                        break;
                    default:
                        break;
                }
            }
        }

        public Effect GetEffect(int type)
        {
            foreach (Effect check in effects)
                if (check.type == type)
                    return check;
            return null;
        }



        public int CompareTo(CreatureEffectHandler other)
        {
            return other.creature.ID.number - creature.ID.number;
        }

        public static bool shouldRemoveThis(CreatureEffectHandler handler)
        {
            return handler.should_remove;
        }
        
    }
}
