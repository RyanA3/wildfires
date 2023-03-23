using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RW_19_Modding
{
    internal interface Effect
    {

        void Update(Creature creature);
        bool shouldRemove();
        bool isType(int type);

    }

    class EffectConstants
    {

        public static readonly int BURNING = 0;

        public static bool shouldRemoveThis(Effect effect)
        {
            return effect.shouldRemove();
        }

    }


    class Burning : Effect
    {

        private int time = 0;
        private int total_burn_time = 0;
        private static readonly int TYPE = EffectConstants.BURNING;

        public void Update(Creature creature)
        {
            time--;
            total_burn_time++;

            if (time < 0)
                return;

            if (creature.inShortcut)
                return;

            creature.room.AddObject(new FireParticle(creature.room, new UnityEngine.Vector2(creature.coord.x, creature.coord.y)));
        }

        public bool shouldRemove()
        {
            return time <= 0;
        }

        public bool isType(int type)
        {
            return TYPE == type;
        }

        public void addTime(int time)
        {
            this.time += time;
        }


    }

}
