using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RW_19_Modding
{
    internal class GlobalEffectHandler
    {

        private GlobalFireManager fireman;
        private List<CreatureEffectHandler> effected_creatures;

        public GlobalEffectHandler(GlobalFireManager fireman)
        {
            this.effected_creatures = new List<CreatureEffectHandler>();
            this.fireman = fireman;
        }




        public void ClearAllEffects()
        {
            effected_creatures.Clear();
        }



        public void Update()
        {

            foreach (CreatureEffectHandler eh in effected_creatures)
            {
                eh.Update();
            }

            effected_creatures.RemoveAll(CreatureEffectHandler.shouldRemoveThis);

        }

        public void HandleCreatureUpdate(Creature creature)
        {

            //Add effects to creature if they're on fire
            if (fireman.room_fires == null || creature == null || creature.room == null) return;
            RoomFire cur_room_fire = fireman.room_fires[creature.room.abstractRoom.name];

            //Don't bother if there's no fire in their current room
            if (cur_room_fire == null)
                return;
            
            foreach(BodyChunk body_chunk in creature.bodyChunks)
            {
                TileData firedat = cur_room_fire.getFireData((int)(body_chunk.pos.x / 20), (int)(body_chunk.pos.y / 20));

                //Again don't bother if the main body chunk of the creature is not on a burning tile
                if (!firedat.burning)
                    continue;

                //Apply the burning effect (or add time to it if it's already applied)
                ApplyEffect(creature.abstractCreature, Effect.BURNING, 3);
                break; //No point in checking the other body chunks if they're on fire
            }

        }



        public void ApplyEffect(AbstractCreature creature, Effect effect)
        {
            CreatureEffectHandler effect_handler = GetEffectHandler(creature);
            effect_handler.AddEffect(effect);
        }

        public void ApplyEffect(AbstractCreature creature, int type, int time)
        {
            CreatureEffectHandler effect_handler = GetEffectHandler(creature);
            effect_handler.AddEffect(type, time);
        }

        public CreatureEffectHandler GetEffectHandler(AbstractCreature abstractCreature)
        {

            CreatureEffectHandler search_handler = new CreatureEffectHandler(abstractCreature);
            int index = effected_creatures.BinarySearch(search_handler);

            if (index >= 0) return effected_creatures[index];

            effected_creatures.Insert(~index, search_handler);
            return search_handler;

        }






    }
}
