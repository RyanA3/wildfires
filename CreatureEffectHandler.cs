using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RW_19_Modding
{
    internal class CreatureEffectHandler
    {

        private AbstractCreature creature;
        private List<Effect> effects;

        public CreatureEffectHandler(AbstractCreature creature)
        {
            this.creature = creature;
            this.effects = new List<Effect>();
        }



        public void Update()
        {

            //General updating
            if (creature.realizedCreature == null)
                return;

            foreach(Effect effect in effects)
                effect.Update(creature.realizedCreature);

            effects.RemoveAll(EffectConstants.shouldRemoveThis);

        }

        public void AddEffect(Effect effect)
        {

        }

        public void HandleRoomUpdate()
        {

        }

    }
}
