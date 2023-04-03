using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RW_19_Modding
{



    public class TileDirectionData
    {
        public const byte

        BITS_IN_USE    = 0b11111111,
        BASE_BITS      = 0b00001111,
        CORNER_BITS    = 0b11110000,

        RIGHT          = 0b00000001,
        UP             = 0b00000010,
        LEFT           = 0b00000100,
        DOWN           = 0b00001000,
        UP_RIGHT       = 0b00010000,
        UP_LEFT        = 0b00100000,
        DOWN_LEFT      = 0b01000000,
        DOWN_RIGHT     = 0b10000000,


        NOT_RIGHT      = RIGHT      ^ BITS_IN_USE,
        NOT_UP         = UP         ^ BITS_IN_USE,
        NOT_LEFT       = LEFT       ^ BITS_IN_USE,
        NOT_DOWN       = DOWN       ^ BITS_IN_USE,
        NOT_UP_RIGHT   = UP_RIGHT   ^ BITS_IN_USE,
        NOT_UP_LEFT    = UP_LEFT    ^ BITS_IN_USE,
        NOT_DOWN_LEFT  = DOWN_LEFT  ^ BITS_IN_USE,
        NOT_DOWN_RIGHT = DOWN_RIGHT ^ BITS_IN_USE,

        FLAT_RIGHT_CASE = DOWN_RIGHT | RIGHT | UP_RIGHT,
        FLAT_UP_CASE    = UP_LEFT    | UP    | UP_RIGHT,
        FLAT_LEFT_CASE  = DOWN_LEFT  | LEFT  | UP_LEFT,
        FLAT_DOWN_CASE  = DOWN_LEFT  | DOWN  | DOWN_RIGHT,

        FLAT_RIGHT_CASE1 = FLAT_RIGHT_CASE ^ DOWN_RIGHT,
        FLAT_RIGHT_CASE2 = FLAT_RIGHT_CASE ^ UP_RIGHT,
        FLAT_UP_CASE1    = FLAT_UP_CASE    ^ UP_RIGHT,
        FLAT_UP_CASE2    = FLAT_UP_CASE    ^ UP_LEFT,
        FLAT_LEFT_CASE1  = FLAT_LEFT_CASE  ^ UP_LEFT,
        FLAT_LEFT_CASE2  = FLAT_LEFT_CASE  ^ DOWN_LEFT,
        FLAT_DOWN_CASE1  = FLAT_DOWN_CASE  ^ DOWN_LEFT,
        FLAT_DOWN_CASE2  = FLAT_DOWN_CASE  ^ DOWN_RIGHT;

        //Checks whether or not the given direction byte has the specified direction(s)
        public static bool hasDirection(byte data, byte check)
        {
            return ((data & check) == check);
        }



        //A texture containing the masks for individual types of tile fires, used to generate fire masks for rooms
        /*
         * FIRE MASK ENCODING:
         * Red Channel:    Opacity of Fire (255 = Fully Visible)
         * BELOW NOT IMPLEMENTED / Couldn't Get it to work / not worth the effort :cry:
         * Green Channel:  Direction of Fire; The green value provided by the atlas is the angle in radians, The sine of this angle is computed and then sent to the shader
         * Blue Channel:   The cosine of the angle is sent to the shader thru the blue channel
         */
        public static Texture2D tile_fire_mask_atlas;

        //Writes the pixels from the fire mask atlas to the given input fire mask, depending on the type of fire that is provided
        //Changes should be applied by the user with fire_mask#Apply() once all tiles have been copied into the mask
        public static void writeToFireMask(Texture2D fire_mask, int tile_x, int tile_y, byte fire_data, float fade) 
        {

            //The x offset of the texture to pull is based off of the base bits
            //The y offset of the texture to pull is based off of the corner bits
            int atlas_off_x = (fire_data & BASE_BITS) * 20;
            int atlas_off_y = ((fire_data & CORNER_BITS) >> 4) * 20;

            Color[] colors_to_copy = tile_fire_mask_atlas.GetPixels(atlas_off_x, atlas_off_y, 20, 20);

            //Apply fade
            if (fade != 1)
                for (int i = 0; i < colors_to_copy.Length; i++)
                    colors_to_copy[i].r *= fade;

            fire_mask.SetPixels(tile_x * 20, tile_y * 20, 20, 20, colors_to_copy);

            //for (int px = 0; px < 20; px++)
            //    for (int py = 0; py < 20; py++)
            //        fire_mask.SetPixel(tile_x * 20 + px, tile_y * 20 + py, tile_fire_mask_atlas.GetPixel(atlas_off_x + px, atlas_off_y + py));

        }

        //Writes an empty tile to the mask at the specified location
        public static void clearTileFromMask(Texture2D fire_mask, int tile_x, int tile_y)
        {
            for (int ox = 0; ox < 20; ox++)
                for (int oy = 0; oy < 20; oy++)
                    fire_mask.SetPixel(tile_x * 20 + ox, tile_y * 20 + oy, Color.black);
        }

        //Generates the tile fire mask atlas from a simplified atlas
        public static void generateAtlasFromSimplified(Texture2D simplified)
        {

            tile_fire_mask_atlas = new Texture2D(16 * 20, 16 * 20);
            Color[] this_tile_colors = new Color[20 * 20];
            for (int z = 0; z < this_tile_colors.Length; z++)
                this_tile_colors[z] = Color.black;


            //Generate sub combinations for bases
            //Loop all sub combinations with i
            for (byte i = 0; i < 16; i++)
            {
                //Loop all base bits in i
                for(int b = 0; b < 4; b++)
                {

                    //Don't copy pixels if this base is not supposed to be included in the current tile
                    byte this_bit = (byte) (1 << b);
                    if ((i & this_bit) == 0)
                        continue;

                    //Loop all pixels in the current base, and copy them to the proper location
                    for (int px = 0; px < 20; px++)
                    {
                        for (int py = 0; py < 20; py++)
                        {
                            this_tile_colors[px + py * 20] += simplified.GetPixel(b * 20 + px, py);
                        }
                    }

                }

                //Set the pixels to the current base
                tile_fire_mask_atlas.SetPixels(i * 20, 0, 20, 20, this_tile_colors);

                //Clear the color array for the next run
                for (int z = 0; z < this_tile_colors.Length; z++)
                    this_tile_colors[z] = Color.black;

            }



            //Now do it again but for corners
            //Loop all sub combinations with i
            for (byte i = 0; i < 16; i++)
            {
                //Loop all base bits in i
                for (int b = 0; b < 4; b++)
                {

                    //Don't copy pixels if this base is not supposed to be included in the current tile
                    byte this_bit = (byte)(1 << b);
                    if ((i & this_bit) == 0)
                        continue;

                    //Loop all pixels in the current corner, and copy them to the proper location
                    for (int px = 0; px < 20; px++)
                    {
                        for (int py = 0; py < 20; py++)
                        {
                            this_tile_colors[px + py * 20] += simplified.GetPixel(b * 20 + 80 + px, py); //Corners tiles are offset by 80 in the xdir
                        }
                    }

                }

                //Set the pixels to the current corner(s)
                tile_fire_mask_atlas.SetPixels(0, i * 20, 20, 20, this_tile_colors);

                //Clear the color array for the next run
                for (int z = 0; z < this_tile_colors.Length; z++)
                    this_tile_colors[z] = Color.black;

            }


            //Apply to the mask atlas so the next stage can mix ?
            //tile_fire_mask_atlas.Apply();



            //Now combine all combinations of both corners and bases
            //:skull:
            for(int b = 1; b < 16; b++)
            {
                for(int c = 1; c < 16; c++)
                {

                    //The x offset of the texture to pull is based off of the base bits
                    //The y offset of the texture to pull is based off of the corner bits
                    int atlas_off_x = (b & BASE_BITS) * 20;
                    int atlas_off_y = (c & (CORNER_BITS >> 4)) * 20;

                    //Loop all pixels for this base and corner combo
                    for (int px = 0; px < 20; px++)
                    {
                        for (int py = 0; py < 20; py++)
                        {

                            //Combine the color from the base and corner
                            Color base_color = tile_fire_mask_atlas.GetPixel(atlas_off_x + px, py);
                            Color corner_color = tile_fire_mask_atlas.GetPixel(px, atlas_off_y + py);
                            Color combined = new Color(base_color.r + corner_color.r, 0, 0, 1);
                            this_tile_colors[px + py * 20] = combined;

                        }
                    }

                    //Apply this tile to the mask and clear the color array
                    tile_fire_mask_atlas.SetPixels(b * 20, c * 20, 20, 20, this_tile_colors);
                    for (int z = 0; z < this_tile_colors.Length; z++)
                        this_tile_colors[z] = Color.black;

                }
            }


            //Fin
            tile_fire_mask_atlas.Apply();

        }



        //Public room setting enum for fire
        public static readonly RoomSettings.RoomEffect.Type FIRE_SETTING = new RoomSettings.RoomEffect.Type("fire", true);

    }



    class WeatherData
    {
        public static float wind_x = 1.0f;
        public static float wind_y = 0.0f;
        public static float wind_intensity = 0.05f;

        public static Vector2 getWind()
        {
            return new Vector2(wind_x, wind_y) * wind_intensity;
        }
    }

}
