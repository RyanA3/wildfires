using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RW_19_Modding
{
    internal class GlobalEffectHandler
    {

        private List<CreatureEffectHandler> effected_creatures;

        public GlobalEffectHandler()
        {
            this.effected_creatures = new List<CreatureEffectHandler>();
        }

        public void Update()
        {

            foreach (CreatureEffectHandler eh in effected_creatures)
                eh.Update();

        }







    }
}
