using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WildfiresMod;

namespace RW_19_Modding
{
    public class GlobalFireManager
    {

        //For managing actual tile fires
        public readonly Dictionary<string, RoomFire> room_fires;
        public RoomFire current_room_fire;

        public World world;


        public GlobalFireManager(World world)
        {
            room_fires = new Dictionary<string, RoomFire>();
            this.world = world;
        }




        public bool ignite(string room_name, int tilex, int tiley)
        {

            if(!room_fires.ContainsKey(room_name) || !room_fires[room_name].loaded)
            {
                return false;
            }

            room_fires[room_name].ignite(tilex, tiley, room_name == current_room_fire.room_name);
            return true;

        }

        public void updateFireData(TileData data, string room_name)
        {

            if (!room_fires.ContainsKey(room_name)) room_fires.Add(room_name, new RoomFire(room_name));
            room_fires[room_name].updateFireData(data);

        }

        public TileData getFireData(string room_name, int tilex, int tiley)
        {

            if (!room_fires.ContainsKey(room_name)) room_fires.Add(room_name, new RoomFire(room_name));
            return room_fires[room_name].getFireData(tilex, tiley);

        }

        public TileData getFireData(WorldCoordinate wco)
        {

            AbstractRoom room = world.GetAbstractRoom(wco);
            return getFireData(room.name, wco.x, wco.y);

        }

        




        /**
         *  UPDATE HANDLING
         */
        public void HandleRoomLoad(Room room)
        {

            //Create a room fire object for this room if it doesn't have one
            if(!room_fires.ContainsKey(room.abstractRoom.name))
            {
                RoomFire new_room_fire = new RoomFire(room.abstractRoom.name);
                new_room_fire.load(room);
                room_fires.Add(room.abstractRoom.name, new_room_fire);

                //Add fire effect to the room
                room.roomSettings.effects.Add(new RoomSettings.RoomEffect(TileDirectionData.FIRE_SETTING, 1.0f, false));

            } else //Load the room if it hasn't been fully loaded yet
            {
                RoomFire room_fire = room_fires[room.abstractRoom.name];
                if (!room_fire.loaded) room_fire.load(room);
            }

        }

        public void HandleShortcutsLoad(Room room)
        {

            //Get the current room fire, load it if it hasn't been already
            RoomFire room_fire;
            if (!room_fires.ContainsKey(room.abstractRoom.name))
            {
                room_fire = new RoomFire(room.abstractRoom.name);
                room_fire.load(room);
                room_fires.Add(room.abstractRoom.name, room_fire);
            }
            else
            {
                room_fire = room_fires[room.abstractRoom.name];
                if (!room_fire.loaded) room_fire.load(room);
            }

            //Do nothing if the shortcuts are already loaded (somehow?)
            if (room_fire.shortcuts_loaded)
                return;

            //Link shortcuts for the room
            room_fire.linkShortcuts(room, this);

        }


        public void HandleRoomChange(Room new_room)
        {

            if (room_fires.ContainsKey(new_room.abstractRoom.name))
                current_room_fire = room_fires[new_room.abstractRoom.name];
            else
                return;

        }

        public void HandleCameraDrawUdate(RoomCamera camera, float timeStacker, float timeSpeed)
        {

            //Don't do anything if this room has no room fire object
            if (camera.room == null || current_room_fire == null)
                return;

            current_room_fire.HandleCameraDrawUpdate(camera, timeStacker, timeSpeed);

        }

        public void HandleCameraApplyPalette(RoomCamera camera)
        {

            //Nothing to do here if the camera isn't in any room
            if (camera.room == null)
                return;

            //Nothing to do here if this room has no fire
            if (current_room_fire == null || camera.room.abstractRoom.name != current_room_fire.room_name)
            {
                if (room_fires.ContainsKey(camera.room.abstractRoom.name))
                    current_room_fire = room_fires[camera.room.abstractRoom.name];
                else
                    return;
            }



            //Apply the fire shader to the screen
            camera.SetUpFullScreenEffect("Foreground");
            camera.fullScreenEffect.shader = camera.game.rainWorld.Shaders["Fire"];

            Shader.SetGlobalInt("_fireDepthCutoff", 5);
            Shader.SetGlobalVector("_maskStart", new Vector2(camera.pos.x / current_room_fire.fire_mask.width, camera.pos.y / current_room_fire.fire_mask.height));
            Shader.SetGlobalVector("_maskToScreenRatio", new Vector2(camera.sSize.x / current_room_fire.fire_mask.width, camera.sSize.y / current_room_fire.fire_mask.height));
            Shader.SetGlobalFloat("_fireEffectAmount", camera.room.roomSettings.GetEffectAmount(TileDirectionData.FIRE_SETTING));

            Shader.SetGlobalTexture("_FireMaskTex", current_room_fire.fire_mask);

        }

        public void HandleRoomUpdate(Room room)
        {

            //Pass on room update to the fire manager
            if (room_fires.ContainsKey(room.abstractRoom.name))
                room_fires[room.abstractRoom.name].Update(current_room_fire != null && room.abstractRoom.name == current_room_fire.room_name);

        }


    }
}
