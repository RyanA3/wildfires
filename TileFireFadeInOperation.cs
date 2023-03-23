using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RW_19_Modding
{

    internal class TileFireFadeInOperation
    {

        bool complete = false;
        byte this_tile_data;
        int tx, ty;

        private static float LIFETIME_DEC = (1f / 25.0f);
        private float life;

        public TileFireFadeInOperation(byte this_tile_data, int tx, int ty)
        {
            this.this_tile_data = this_tile_data;
            this.tx = tx;
            this.ty = ty;

            life = 1;

        }




        public bool isComplete()
        {
            return complete;
        }

        public static bool shouldRemove(TileFireFadeInOperation operation)
        {
            return operation.isComplete();
        }




        public void Update()
        {

            this.life -= LIFETIME_DEC;
            if(this.life < 0)
            {
                complete = true;
            }

        }

        public void WriteToMask(Texture2D fire_mask)
        {

            TileDirectionData.writeToFireMask(fire_mask, tx, ty, this_tile_data, 1 - life);

        }

        public void ForceComplete(Texture2D fire_mask)
        {

            this.life = 0;
            this.complete = true;
            WriteToMask(fire_mask);

        }






    }
}
