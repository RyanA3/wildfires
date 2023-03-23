using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using AssetBundles;
using BepInEx;
using IL;
using On;
using RW_19_Modding;
using UnityEngine;
using UnityEngine.Rendering;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace WildfiresMod
{
    [BepInEx.BepInPlugin("felns.wildfires", "Wildfires", "0.0.1")]
    public class WildfiresMain : BepInEx.BaseUnityPlugin
    {


        bool asset_bundles_loaded = false;
        bool requested_asset_bundles_load = false;
        bool added_shader = false;
        bool loaded_mask_atlas = false;
        private LoadedAssetBundle asset_bundle;

        private GlobalFireManager fireman;

        private GlobalEffectHandler effection;





        public void OnEnable()
        {

            On.Room.Loaded += RoomLoaded;
            On.Room.Update += RoomUpdate;
            On.RainWorld.Update += RainWorldUpdate;
            On.RoomCamera.ApplyPalette += RoomCameraApplyPalette;
            On.RoomCamera.DrawUpdate += RoomCameraDrawUpdate;
            On.RoomCamera.ChangeRoom += RoomCameraChangeRoom;

        }





        /*------------------------
        |     UTIL FUNCTIONS     |
        -------------------------*/




        /*----------------
         |     HOOKS     |
         ----------------*/

        //Create a fire object for all rooms once they're loaded
        void RoomLoaded(On.Room.orig_Loaded orig, Room self)
        {

            orig(self);

            if (fireman != null)
                fireman.HandleRoomLoad(self);

        }


        void RoomUpdate(On.Room.orig_Update orig, Room self)
        {

            orig(self);

            if (fireman != null)
                fireman.HandleRoomUpdate(self);

        }


        //Apply global shader uniforms that may change on each frame
        void RoomCameraDrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {

            orig(self, timeStacker, timeSpeed);

            if (fireman != null)
                fireman.HandleCameraDrawUdate(self, timeStacker, timeSpeed);

        }


        //Apply the fire screen effect if the current room on camera has the fire effect
        void RoomCameraApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
        {

            orig(self);

            if (fireman != null)
                fireman.HandleCameraApplyPalette(self);

        }

        void RoomCameraChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room new_room, int position)
        {

            orig(self, new_room, position);

            if (new_room == null)
                return;

            if (fireman != null)
                fireman.HandleRoomChange(new_room);

        }



        //Main update loop
        void GameUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            
            orig(self);

            if (effection != null)
                effection.Update();
            
        }

        






        /*
         *  Load Shaders and Assets
         */
        void RainWorldUpdate(On.RainWorld.orig_Update orig, RainWorld self)
        {
            orig(self);


            //Load the asset bundle!! Yay!!! BUNDLE!!!!!!! WOWWWW!!!!!!!11!
            if (!asset_bundles_loaded && self.assetBundlesInitialized)
            {

                if (!requested_asset_bundles_load)
                {
                    AssetBundleManager.LoadAssetBundle("testbundle");
                    this.requested_asset_bundles_load = true;
                }
                else
                {
                    string err;
                    LoadedAssetBundle bundle_load_attempt = AssetBundleManager.GetLoadedAssetBundle("testbundle", out err);

                    if(bundle_load_attempt != null)
                    {
                        this.asset_bundle = bundle_load_attempt;
                        this.asset_bundles_loaded = true;
                    }
                }

            }



            //Load the fire shader
            if(asset_bundles_loaded && !added_shader && self.Shaders != null)
            {
                Shader fire_shader = asset_bundle.m_AssetBundle.LoadAsset<Shader>("Fire.shader");
                self.Shaders.Add("Fire", FShader.CreateShader("Fire", fire_shader));
                added_shader = true;
            }

            

            //Load the texture atlas for fire masks
            if(asset_bundles_loaded && !loaded_mask_atlas)
            {
                Texture2D simplified_mask_atlas = asset_bundle.m_AssetBundle.LoadAsset<Texture2D>("simplified_tilefiremask_atlas.png");
                TileDirectionData.generateAtlasFromSimplified(simplified_mask_atlas);
                loaded_mask_atlas = true;
            }



            //Load the fire manager
            if(fireman == null)
            {
                fireman = new GlobalFireManager();
            }

            //Load the effect manager
            if(effection == null)
            {
                effection = new GlobalEffectHandler();
            }
 
        }


    }

}