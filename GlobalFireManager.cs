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
    internal class GlobalFireManager
    {

        private Dictionary<string, RoomFire> room_fires;
        public RoomFire current_room_fire;

        public GlobalFireManager()
        {
            room_fires = new Dictionary<string, RoomFire>();
            
        }





        public void HandleRoomLoad(Room room)
        {

            //Create a room fire object for this room if it doesn't have one
            if(!room_fires.ContainsKey(room.abstractRoom.name))
            {
                RoomFire new_room_fire = new RoomFire(room);
                room_fires.Add(room.abstractRoom.name, new_room_fire);

                //Add fire effect to the room
                room.roomSettings.effects.Add(new RoomSettings.RoomEffect(TileDirectionData.FIRE_SETTING, 1.0f, false));

            }

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

            if (room_fires.ContainsKey(room.abstractRoom.name))
                room_fires[room.abstractRoom.name].Update(current_room_fire != null && room.abstractRoom.name == current_room_fire.room_name);

        }


    }
}
