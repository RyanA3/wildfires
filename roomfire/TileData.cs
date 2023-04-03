using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WildfiresMod;

namespace RW_19_Modding
{
    public class TileData : IComparable<TileData>
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



        public void CopyStatus(TileData other)
        {
            this.flammable = other.flammable;
            this.burning = other.burning;
            this.direction_data = other.direction_data;
            this.can_spread = other.can_spread;
        }


        public void LinkShortcut(GlobalFireManager fireman, ShortcutTileData other, string this_room_name)
        {

            ShortcutTileData this_shortcut = new ShortcutTileData(index, tilex, tiley, flammable, burning, direction_data, this_room_name, other);

            fireman.room_fires[this_room_name].updateFireData(this_shortcut);

        }



        public int CompareTo(TileData other)
        {
            return other.index - this.index;
        }

    }

    public class ShortcutTileData : TileData
    {

        public string room_name;
        public ShortcutTileData exit;
        public bool has_spread_to_exit = false;

        public ShortcutTileData(int index, int tilex, int tiley, bool flammable, bool burning, byte direction_data,
            string room_name, ShortcutTileData exit) : base(index, tilex, tiley, flammable, burning, direction_data)
        {
            this.room_name = room_name;
            this.exit = exit;
        }

        public void SpreadToExit()
        {
            if (exit == null) return;
            has_spread_to_exit = WildfiresMain.fireman.ignite(exit.room_name, exit.tilex, exit.tiley);
        }

    }

}
