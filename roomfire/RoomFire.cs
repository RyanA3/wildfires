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
using UnityEngine.Assertions.Must;

namespace RW_19_Modding
{
    public class RoomFire
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
        public bool shortcuts_loaded = false;

        private List<TileFireFadeInOperation> fading_operations;


        public RoomFire(string room_name)
        {

            this.room_name = room_name;

            //Initialize information about all the tiles in the room
            tile_statuses = new List<TileData>();

            //Initialize list for fading in new fires
            fading_operations = new List<TileFireFadeInOperation>();

        }

        public void load(Room room)
        {

            this.room_name = room.abstractRoom.name;
            this.tile_width = room.TileWidth;
            this.tile_height = room.TileHeight;
            this.water_level = room.floatWaterLevel;

            //Sort any tiles that were added to the room before loading it
            if(tile_statuses.Count > 0)
            {
                //Set their index since the tile width of their room was not known until now
                foreach (TileData data in tile_statuses)
                    data.index = data.tilex + data.tiley * tile_width;

                tile_statuses.Sort();
            }

            //All data sorted beyond this point
            loaded = true;

            //Loop all tiles in the room (excluding boundary tiles)
            for (int i = 1; i < tile_width - 1; i++)
            {
                for (int j = 1; j < tile_height - 1; j++)
                {

                    //Don't do anything if this tile can't be occupied by a fire
                    if (!fireCanOccupy(room, i, j))
                        continue;

                    //TileData data = new TileData(i + j * tile_width, i, j, false, false, 0);
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
                        data.direction_data = (byte)(data.direction_data & TileDirectionData.BASE_BITS);
                    else if (data.direction_data == TileDirectionData.FLAT_UP_CASE)
                        data.direction_data = (byte)(data.direction_data & TileDirectionData.BASE_BITS);
                    else if (data.direction_data == TileDirectionData.FLAT_LEFT_CASE)
                        data.direction_data = (byte)(data.direction_data & TileDirectionData.BASE_BITS);
                    else if (data.direction_data == TileDirectionData.FLAT_DOWN_CASE)
                        data.direction_data = (byte)(data.direction_data & TileDirectionData.BASE_BITS);



                    //If the tile has any direction data, it must be flammable
                    if ((TileDirectionData.BITS_IN_USE & data.direction_data) != 0)
                        data.flammable = true;

                    //Only set the data if it's flammable
                    if (!data.flammable) continue;

                    //Ensure this data isn't already set (preloaded shortcuts, etc)
                    TileData preset = getFireData(i, j);
                    if (preset != TileData.NONE)
                    {   //Load the preset data with information it couldn't have obtained before the room loaded
                        preset.index = data.index;
                        preset.direction_data = data.direction_data;
                    }
                    else
                    {   //Otherwise push the new data to the list
                        updateFireData(data);
                    }

                }
            }

            //Initialize the fire mask
            fire_mask = new Texture2D(tile_width * 20, tile_height * 20);
            for (int tx = 0; tx < tile_width; tx++)
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
            for (int ignite_y = 0; ignite_y < tile_height; ignite_y++)
                ignite(ignite_x, ignite_y, false);

            //Mask should now automatically be written to by ignite function
            //Make sure to apply changes to the fire mask after igniting/extinguishing tiles
            fire_mask.Apply();

        }




        //Should be called after load, may access other room fires before their load function is called however
        public void linkShortcuts(Room room, GlobalFireManager fireman)
        {

            /*foreach (ShortcutData shortcut in room.shortcuts)
            {
                if (!shortcut.LeadingSomewhere) continue;
                TileData start_data_processing = getFireData(shortcut.StartTile.x, shortcut.StartTile.y);

                //Ignore this shortcut if it has already been initialized by another room
                if (start_data_processing is ShortcutTileData && (start_data_processing as ShortcutTileData).exit != null)
                {
                    Debug.Log("RoomFire(" + room_name + "): Shortcut already linked, skip");
                    continue;
                }

                ShortcutTileData start_data = new ShortcutTileData(start_data_processing.index, start_data_processing.tilex, start_data_processing.tiley, true, false, start_data_processing.direction_data, room_name, null);
                updateFireData(start_data);



                TileData end_data_processing;
                ShortcutTileData end_data;
                
                //Link shortcuts in the same room
                if (shortcut.shortCutType == ShortcutData.Type.Normal) 
                {

                    Debug.Log("RoomFire(" + room_name + "): Linking Shortcut in same room");

                    //Grab tile data for the destination of the shortcut
                    end_data_processing = getFireData(shortcut.DestTile.x, shortcut.DestTile.y);

                    //If the exit shortcut wasn't initialized in this room then initialize it
                    if (end_data_processing is ShortcutTileData)
                    {
                        (end_data_processing as ShortcutTileData).exit = start_data;
                        start_data.exit = (ShortcutTileData) end_data_processing;
                        continue;
                    }
                    else /*if (end_data_processing == TileData.NONE)*//*
                    {
                        end_data = new ShortcutTileData(shortcut.DestTile.x + shortcut.DestTile.y * tile_width, shortcut.DestTile.x, shortcut.DestTile.y,
                            true, false, TileDirectionData.BASE_BITS, room_name, null);
                        updateFireData(end_data);
                    }

                } else if (shortcut.shortCutType == ShortcutData.Type.RoomExit)
                {


                    //Get the room the shortcut leads to
                    AbstractRoom end_room = room.world.GetAbstractRoom(shortcut.destinationCoord.room);
                    AbstractRoom start_room = room.world.GetAbstractRoom(shortcut.startCoord.room);
                    Debug.Log("Shortcut: " + start_room.name + "->" + end_room.name + " (" + shortcut.startCoord.x + "," + shortcut.startCoord.y + ")" + "->(" + shortcut.destinationCoord.x + "," + shortcut.destinationCoord.y + ") Node: " + shortcut.startCoord.abstractNode + "->" + shortcut.destinationCoord.abstractNode);


                    //Find the exit room
                    //AbstractRoom end_room = room.WhichRoomDoesThisExitLeadTo(shortcut.DestTile);


                    //Ensure the end room isn't null ? and is loaded
                    if (end_room == null || end_room.realizedRoom == null)
                    {
                        Debug.Log("RoomFire(" + room_name + "): Failed to link shortcut: Destination room unspecified");
                        continue;
                    }

                    int other_shortcut_data_index = -1;
                    for(int i = 0; i < end_room.realizedRoom.shortcuts.Length; i++)
                    {
                        ShortcutData check = end_room.realizedRoom.shortcuts[i];
                        AbstractRoom check_room = end_room.realizedRoom.WhichRoomDoesThisExitLeadTo(check.DestTile);
                        if(check_room != null && check_room.name == room_name)
                        {
                            other_shortcut_data_index = i;
                            break;
                        }
                    }

                    //Ensure other shortcut is specified
                    if(other_shortcut_data_index == -1)
                    {
                        Debug.Log("RoomFire(" + room_name + "): Failed to link shortcut: Destination shortcut not found");
                        continue;
                    }
                    

                    ShortcutData other_shortcut = end_room.realizedRoom.shortcuts[other_shortcut_data_index];

                    Debug.Log("RoomFire(" + room_name + "): Linking Shortcut in other room '" + end_room.name + "'");
                    //Otherwise, grab the shortcut tile data for the exit from the other room fire
                    end_data_processing = fireman.getFireData(end_room.name, other_shortcut.StartTile.x, other_shortcut.StartTile.y);

                    //If the exit shortcut data wasn't initialized in the other room, then initialize it, and add it to the room's fire data
                    //The index of the tile wont be known until the roomfire is initialized, since the room's tile width and height isn't known
                    //The elements added before initialization will be sorted once the room's tile width and height is known
                    if (end_data_processing is ShortcutTileData)
                        end_data = end_data_processing as ShortcutTileData;
                    else /*if (end_data_processing == TileData.NONE)*//*
                    {
                        end_data = new ShortcutTileData(other_shortcut.StartTile.x + other_shortcut.StartTile.y * fireman.room_fires[end_room.name].tile_width,
                            other_shortcut.StartTile.x, other_shortcut.StartTile.y, true, false, TileDirectionData.BASE_BITS, other_shortcut.room.abstractRoom.name, null);
                        fireman.updateFireData(end_data, end_room.name);
                    }

                } else
                {
                    Debug.Log("RoomFire(" + room_name + "): Failed to link shortcut '" + shortcut.shortCutType.value + "'");
                    continue;
                }

                //Link the shortcuts
                start_data.exit = end_data;
                end_data.exit = start_data;

                

                Debug.Log("RoomFire(" + room_name + "): Linked Shortcuts: " + start_data.room_name + "->" + end_data.room_name);

            }
            */

            //Celebrate profusely
            shortcuts_loaded = true;

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
        //Internal use only!!!
        public void updateFireData(TileData status)
        {

            //Just add the fire data to the list if the room isn't loaded, since we don't know the tile width of the room uet
            //We will sort the list once the room is loaded
            if (!loaded)
            {

                //Ensure it hasn't already been added // Remove it if it has already been added and replace it
                TileData preset = getFireData(status.tilex, status.tiley);
                if (preset != TileData.NONE)
                {
                    status.index = preset.index;
                    status.direction_data = preset.direction_data;
                    tile_statuses.Remove(preset);
                }

                tile_statuses.Add(status);
                return;

            }

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

            //If the room hasn't been loaded yet, do a linear search on the list of data, since it hasn't been sorted yet
            if(!loaded)
            {
                foreach (TileData data in tile_statuses)
                    if (data.tilex == x && data.tiley == y)
                        return data;

                return TileData.NONE;
            }

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




        public void ignite(int x, int y, bool should_fade)
        {

            TileData data = getFireData(x, y);
            if (!data.flammable) return;       //Don't ignite non-flammable tiles (they won't be stored in memory anyways)
            if (data.burning) return;          //Ignore this request if the tile is already on fire
            if (y * 20 < water_level) return;  //Don't ignite tiles below the current water level

            data.burning = true;

            num_fires++;

            //Write to the mask
            if(should_fade) //If the current screen is being viewed, add a smooth fading operation
                fading_operations.Add(new TileFireFadeInOperation(data, x, y));
            else            //Otherwise write to the fire mask immediately 
                TileDirectionData.writeToFireMask(fire_mask, x, y, data.direction_data, 1.0f);

            should_update_mask = true;

        }

        //Works like ignite, but has special conditions based on where the fire is spreading from/to
        public void spread(TileData from, TileData to, bool should_fade)
        {

        }

        public void extinguish(int x, int y)
        {

            TileData data = getFireData(x, y);

            if (!data.burning) return;    //Do nothing if this tile isn't burning
            data.burning = false;
            data.flammable = false;
            //updateFireData(data);

            num_fires--;

            //Write to the mask
            TileDirectionData.clearTileFromMask(fire_mask, x, y);
            should_update_mask = true;

        }












        /*
         *   UPDATE HANDLING
         */
        public /*override*/ void Update(Room room, /*bool eu*/bool on_screen)
        {

            //Spread fire to adjacent tiles here
            foreach (TileData data in tile_statuses)
            {

                //if (data is ShortcutTileData)
                //    (data as ShortcutTileData).Update(room);
                //else
                    data.Update();


                if (!data.burning)
                    continue;

                if (!data.can_spread)
                    continue;

                //Only spread around every once in a while
                if (UnityEngine.Random.value < 0.95f) 
                    continue;

                //if (data is ShortcutTileData && !(data as ShortcutTileData).has_spread_to_exit)
                //    (data as ShortcutTileData).SpreadToExit();

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
            

            


            //Update all the fading operations for new fires
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
        private static Color FIRE_SHORTCUT_COLOR = new Color(0.8f, 0.2f, 0.2f);
        public void HandleCameraDrawUpdate(RoomCamera self, float timeStacker, float timeSpeed)
        {

            //If there are any fading operations, the mask should update this frame
            if (fading_operations.Count > 0) 
                should_update_mask = true;

            //Update all fade in/out operations
            foreach(TileFireFadeInOperation fading_op in fading_operations)
            {
                fading_op.WriteToMask(fire_mask);
            }

            //Upload fire mask data to the gpu if it has been changed
            if (should_update_mask)
            {
                fire_mask.Apply();
                should_update_mask = false;
            }


            //Only continue if shortcuts are ready
            if (self == null || self.shortcutGraphics == null) return;

            //Make all shortcut entrances with a fire on the other side light up red
            /*
            foreach (TileData data in tile_statuses)
            {

                //if (!(data is ShortcutTileData)) continue;
                //ShortcutTileData sdata = data as ShortcutTileData;

                if (sdata.shortcut == -1 || sdata.shortcut >= self.shortcutGraphics.entranceSpriteLocations.Length) continue;

                if(sdata.burning || (sdata.exit != null && sdata.exit.burning))
                    self.shortcutGraphics.ColorEntrance(sdata.shortcut, FIRE_SHORTCUT_COLOR);


            }*/

        }

    }
}
