using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RW_19_Modding
{
    internal class Effect
    {

        public const int BURNING = 1;

        public int time = 0, total_time = 0;
        public bool should_remove = false;
        public int type = 0;

        public Effect(int type, int time)
        {
            this.type = type;
            this.time = time;
        }

        public virtual void Update(Creature creature)
        {

            time--;

            if (creature.dead)
            {
                should_remove = true;
                total_time = 0;
                return;
            }

            total_time++;

            if (time < 0)
                should_remove = true;

        }



        public static bool shouldRemoveThis(Effect effect)
        {
            return effect.should_remove;
        }

    }



    class BurningEffect : Effect
    {

        private static readonly int TYPE = Effect.BURNING;

        private static readonly int KINDLING_TIME = 50;
        private static readonly int KNOCKOUT_TIME = 200;
        private static readonly int DIE_TIME = 300;

        public BurningEffect(int time) : base(TYPE, time) { }

        public override void Update(Creature creature)
        {

            if (should_remove)
                return;

            base.Update(creature);

            if (creature == null || creature.inShortcut || creature.room == null)
                return;

            //Give the effect some time to kick in
            if (total_time < KINDLING_TIME) return;

            int rand_body_chunk = (int)(creature.bodyChunks.Length * UnityEngine.Random.value);
            creature.room.AddObject(new FireParticle(creature.room, creature.bodyChunks[rand_body_chunk].pos + (new Vector2(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * creature.bodyChunks[rand_body_chunk].rad * 1.25f)));

            //If the total amount of time the creature has been on fire is greater than knockout time, knock them out
            if (total_time > KNOCKOUT_TIME)
            {
                //creature.Violence(null, null, creature.bodyChunks[rand_body_chunk], null, Creature.DamageType.Blunt, 0, 0.25f);
            }

            // DIE TIME!!!!!
            if (total_time > DIE_TIME)
            {
                //creature.Die();
                total_time = 0;
                this.should_remove = true;
            }

        }


    }

}
