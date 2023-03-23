using IL.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using System.Collections;
using System.Reflection;

namespace RW_19_Modding
{
    internal class RoomFire /*: UpdatableAndDeletable*/
    {

        public string room_name;

        private List<TileData> tile_statuses;

        private int tile_width, tile_height;
        private float water_level;

        private int num_fires = 0;

        public Texture2D fire_mask;
        public bool should_update_mask = false;
        public bool should_delay_ignite = false;
        public bool loaded = false;

        private List<TileFireFadeInOperation> fading_operations;


        public RoomFire(Room room)
        {
            this.room_name = room.abstractRoom.name;
            this.tile_width = room.TileWidth;
            this.tile_height = room.TileHeight;
            this.water_level = room.floatWaterLevel;

            //Initialize information about all the tiles in the room
            tile_statuses = new List<TileData>();

            //Initialize list for fading in new fires
            fading_operations = new List<TileFireFadeInOperation>();

            //Loop all tiles in the room (excluding boundary tiles)
            for (int i = 1; i < tile_width-1; i++)
            {
                for(int j = 1; j < tile_height-1; j++)
                {

                    //Don't do anything if this tile can't be occupied by a fire
                    if (!fireCanOccupy(room, i, j))
                        continue;

                    TileData data = new TileData(i + j * tile_width, i, j, false, false, 0);



                    //Floor tiles are always marked as down, and only support fires above them
                    if (isTile(room, i, j, Room.Tile.TerrainType.Floor))
                        data.direction_data |= TileDirectionData.UP;
                    else if (isTile(room, i, j - 1, Room.Tile.TerrainType.Floor))
                        data.direction_data |= TileDirectionData.DOWN;

                    //Mark the base bits (direction in which there is a solid/fire supporting tile)
                    if (canSupportFire(room, i, j - 1))
                        data.direction_data |= TileDirectionData.DOWN;
                    if (canSupportFire(room, i, j + 1))
                        data.direction_data |= TileDirectionData.UP;
                    if (canSupportFire(room, i + 1, j))
                        data.direction_data |= TileDirectionData.RIGHT;
                    if (canSupportFire(room, i - 1, j))
                        data.direction_data |= TileDirectionData.LEFT;


                    //Only check corners if the tile has more or less than one base, to make long flat sections look cleaner
                    //   -- Not Flat Tile Case --
                    if (canSupportFire(room, i - 1, j - 1))
                        data.direction_data |= TileDirectionData.DOWN_LEFT;
                    if (canSupportFire(room, i - 1, j + 1))
                        data.direction_data |= TileDirectionData.UP_LEFT;
                    if (canSupportFire(room, i + 1, j + 1))
                        data.direction_data |= TileDirectionData.UP_RIGHT;
                    if (canSupportFire(room, i + 1, j - 1))
                        data.direction_data |= TileDirectionData.DOWN_RIGHT;

                    //Check for special cases
                    //(Remove corner directions for flat segments of land)
                    if (data.direction_data == TileDirectionData.FLAT_RIGHT_CASE)
                        data.direction_data = (byte) (data.direction_data & TileDirectionData.BASE_BITS);
                    else if (data.direction_data == TileDirectionData.FLAT_UP_CASE)
                        data.direction_data = (byte) (data.direction_data & TileDirectionData.BASE_BITS);
                    else if (data.direction_data == TileDirectionData.FLAT_LEFT_CASE)
                        data.direction_data = (byte) (data.direction_data & TileDirectionData.BASE_BITS);
                    else if (data.direction_data == TileDirectionData.FLAT_DOWN_CASE)
                        data.direction_data = (byte) (data.direction_data & TileDirectionData.BASE_BITS);

                    //If the tile has any direction data, it must be flammable
                    if ((TileDirectionData.BITS_IN_USE & data.direction_data) != 0)
                        data.flammable = true;

                    //Only set the data if it's flammable
                    if(data.flammable)
                        updateFireData(data);

                }
            }

            //Initialize the fire mask
            fire_mask = new Texture2D(tile_width * 20, tile_height * 20);
            for(int tx = 0; tx < tile_width; tx++)
            {
                for (int ty = 0; ty < tile_height; ty++)
                {
                    TileData this_tile = getFireData(tx, ty);
                    if (this_tile.flammable)
                    {
                        for (int pox = 0; pox < 20; pox++)
                            for (int poy = 0; poy < 20; poy++)
                                fire_mask.SetPixel(tx * 20 + pox, ty * 20 + poy, Color.blue);
                    }
                    else
                    {
                        for (int pox = 0; pox < 20; pox++)
                            for (int poy = 0; poy < 20; poy++)
                                fire_mask.SetPixel(tx * 20 + pox, ty * 20 + poy, Color.black);
                    }
                }
            }
           
            //Ignite a column of tiles for fun
            int ignite_x = tile_width - 20;
            for(int ignite_y = 0; ignite_y < tile_height; ignite_y++)
                ignite(ignite_x, ignite_y, false);

            //Mask should now automatically be written to by ignite function
            //Make sure to apply changes to the fire mask after igniting/extinguishing tiles
            fire_mask.Apply();

            //loaded = true;

        }




        //Utility function for checking the type of a tile in a room
        private static bool isTile(Room room, int x, int y, Room.Tile.TerrainType terrain)
        {
            return room.GetTile(new RWCustom.IntVector2(x, y)).Terrain == terrain;
        }

        private static bool isTileOnScreen(RoomCamera camera, int x, int y)
        {
            return camera.IsVisibleAtCameraPosition(camera.currentCameraPosition, new Vector2(x, y) * 20);
        }

        private static bool canSupportFire(Room room, int x, int y)
        {
            Room.Tile.TerrainType check = room.GetTile(new RWCustom.IntVector2(x, y)).Terrain;
            return check == Room.Tile.TerrainType.Solid || check == Room.Tile.TerrainType.ShortcutEntrance;
        }

        private static bool fireCanOccupy(Room room, int x, int y)
        {
            Room.Tile.TerrainType check = room.GetTile(new RWCustom.IntVector2(x, y)).Terrain;
            return check == Room.Tile.TerrainType.Air || check == Room.Tile.TerrainType.Slope || check == Room.Tile.TerrainType.Floor;
        }



        //Function for setting fire data on certain tiles
        private void updateFireData(TileData status)
        {

            //Perform binary search on the list
            //Documentation: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.binarysearch?view=net-8.0
            //If the index is negative, it is not in the list, and the complement of the index is where the newly inserted item should go
            int index = tile_statuses.BinarySearch(status);
            if (index < 0) tile_statuses.Insert(~index, status);
            else tile_statuses[index] = status;

        }


        //Function for getting fire data on certain tiles
        public TileData getFireData(int x, int y)
        {

            //Create an object to compare to when searching
            TileData get_index = TileData.NONE;
            get_index.index = x + y * tile_width;

            int data_index = tile_statuses.BinarySearch(get_index);
            if (data_index >= 0) return tile_statuses[data_index];

            //If the search found nothing, return the comparable
            get_index.tilex = x;
            get_index.tiley = y;
            return get_index;

        }




        List<TileData> delayed_ignitions = new List<TileData>();
        public void ignite(int x, int y, bool should_fade)
        {

            TileData data = getFireData(x, y);
            if (!data.flammable) return;       //Don't ignite non-flammable tiles
            if (data.burning) return;          //Ignore this request if the tile is already on fire
            if (y * 20 < water_level) return;  //Don't ignite tiles below the current water level

            data.burning = true;

            //Don't update the main data if we're currently accessing it
            //if (should_delay_ignite) delayed_ignitions.Add(data);
            //else updateFireData(data);

            num_fires++;

            //Write to the mask
            if(should_fade)
                fading_operations.Add(new TileFireFadeInOperation(data.direction_data, x, y));
            else
                TileDirectionData.writeToFireMask(fire_mask, x, y, data.direction_data, 1.0f);

            should_update_mask = true;

        }

        public void uploadDelayedIgnites()
        {
            foreach (TileData data in delayed_ignitions)
                updateFireData(data);
            delayed_ignitions.Clear();
        }

        public void extinguish(int x, int y)
        {

            TileData data = getFireData(x, y);

            if (!data.burning) return;    //Do nothing if this tile isn't burning
            data.burning = false;
            data.flammable = false;
            //updateFireData(data);

            /*
            int index = x + y * tile_width;

            //Don't do anything if the tile isn't on fire
            if ((tile_data[index] & TileDirData.BURNING) == 0)
                return;

            //Mark the tile as burnt by removing the flammable property
            tile_data[index] &= TileDirData.NOT_FLAMMABLE;
            tile_data[index] &= TileDirData.NOT_BURNING;
            */

            num_fires--;

            //Write to the mask
            TileDirectionData.clearTileFromMask(fire_mask, x, y);
            should_update_mask = true;

        }












        /*
         *   UPDATE HANDLING
         */
        public /*override*/ void Update(/*bool eu*/bool on_screen)
        {

            should_delay_ignite = true;
            foreach (TileData data in tile_statuses)
            {
                if (!data.burning)
                    continue;

                if (!data.can_spread)
                    continue;

                //Only spread around every once in a while
                if (UnityEngine.Random.value < 0.95f) 
                    continue;

                ignite(data.tilex + 1, data.tiley, on_screen);
                ignite(data.tilex, data.tiley + 1, on_screen);
                ignite(data.tilex - 1, data.tiley, on_screen);
                ignite(data.tilex, data.tiley - 1, on_screen);
                ignite(data.tilex + 1, data.tiley + 1, on_screen);
                ignite(data.tilex - 1, data.tiley - 1, on_screen);
                ignite(data.tilex + 1, data.tiley - 1, on_screen);
                ignite(data.tilex - 1, data.tiley + 1, on_screen);

                data.can_spread = false;

            }
            should_delay_ignite = false;
            uploadDelayedIgnites();
            

            



            foreach (TileFireFadeInOperation fading_op in fading_operations)
            {
                if (!on_screen) //Forcefully complete all fading operations if this room is no longer on screen (room switch during fading operation)
                    fading_op.ForceComplete(fire_mask);
                else
                    fading_op.Update();

                this.should_update_mask = true;
            }

            fading_operations.RemoveAll(TileFireFadeInOperation.shouldRemove);
            

        }



        //TODO: See https://forum.unity.com/threads/async-texture2d-creation-with-jobs.530474/ about creating procedural textures (reading/writing to textures on
        // a seperate thread) to stop the main game rendering loop from halting when a lot of updates need to be applied to the fire mask texture
        public void HandleCameraDrawUpdate(RoomCamera self, float timeStacker, float timeSpeed)
        {

            if (fading_operations.Count > 0) 
                should_update_mask = true;

            foreach(TileFireFadeInOperation fading_op in fading_operations)
            {
                fading_op.WriteToMask(fire_mask);
            }

            if (should_update_mask)
            {
                fire_mask.Apply();
                should_update_mask = false;
            }

        }


        /*
        public override void PausedUpdate()
        {
            base.PausedUpdate();
        }

        //TODO: Is destroy called when room is no longer on screen or when region is unloaded?
        public override void Destroy()
        {
            base.Destroy();
        }*/

    }
}
