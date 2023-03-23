using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RW_19_Modding
{
    class TileData : IComparable<TileData>
    {

        public int index;
        public int tilex, tiley;
        public bool flammable, burning;
        public bool can_spread;
        public byte direction_data;

        public TileData(int index, int tilex, int tiley, bool flammable, bool burning, byte direction_data)
        {
            can_spread = true;
            this.index = index;
            this.tilex = tilex;
            this.tiley = tiley;
            this.flammable = flammable;
            this.burning = burning;
            this.direction_data = direction_data;
        }

        public static TileData NONE = new TileData(-1, -1, -1, false, false, 0);

        public int CompareTo(TileData other)
        {
            return other.index - this.index;
        }

    }

}
