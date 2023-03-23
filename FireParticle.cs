using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RW_19_Modding
{
    internal class FireParticle : CosmeticSprite
    {

        private static readonly int MIN_LIFETIME = 30;
        private static readonly int MAX_LIFESPAN = 30;

        private float life;
        private float life_decrement;

        public FireParticle(Room room, Vector2 location)
        {
            this.room = room;
            this.pos = location;
            this.lastPos = location;

            this.vel.x = 0;
            this.vel.y = 0;

            this.life_decrement = 1f / (UnityEngine.Random.value * MAX_LIFESPAN + MIN_LIFETIME);
        }

        public override void Update(bool eu)
        {

            base.Update(eu);

            this.lastPos = pos;
            this.vel.x += WeatherData.wind_x * WeatherData.wind_intensity;
            this.vel.y += WeatherData.wind_y * WeatherData.wind_intensity;

            this.life -= life_decrement;

            if (life < 0) 
                this.Destroy();

        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("deerEyeB", true);
            this.AddToContainer(sLeaser, rCam, null);
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            sLeaser.sprites[0].x = Mathf.Lerp(lastPos.x, pos.x, timeStacker);
            sLeaser.sprites[0].y = Mathf.Lerp(lastPos.y, pos.y, timeStacker);
            sLeaser.sprites[0].scale = life;
            sLeaser.sprites[0].color = RWCustom.Custom.HSL2RGB(Mathf.Lerp(0.01f, 0.08f, life), 1f, Mathf.Lerp(0.5f, 1f, Mathf.Pow(life, 3f)));

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

    }
}
