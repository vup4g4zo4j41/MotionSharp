using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;
using SharpDX;
using LeagueSharp;

namespace e.Motion_Gangplank
{
    public static class Helper
    {
        private const int QDELAY = 300;
        public static Vector2 PredPos;
        public static int GetQTime(Vector3 position)
        {
            return (int)(Program.Player.Distance(position) / 2.6f + QDELAY + Game.Ping/2);
        }

        public static bool GetPredPos(Obj_AI_Hero enemy, bool additionalReactionTime = false, bool additionalBarrelTime = false)
        {
            PredPos = SPrediction.Prediction.GetFastUnitPosition(enemy, Config.Menu.Item("misc.reactionTime").GetValue<Slider>().Value);
            float reactionDistance = Config.Menu.Item("misc.reactionTime").GetValue<Slider>().Value  +  (additionalReactionTime? Config.Menu.Item("misc.additionalReactionTime").GetValue<Slider>().Value : 0) * enemy.MoveSpeed*0.001f;
            if (PredPos.Distance(enemy) > reactionDistance)
            {
                PredPos = enemy.Position.Extend(PredPos.To3D(), reactionDistance).To2D();
            }
            return true;

        }
        public static bool CannotEscape(this Vector3 kegPosition, Vector3 distCalcPosition, Obj_AI_Hero enemy, bool additionalReactionTime = false, bool additionalBarrelTime = false)
        {
            GetPredPos(enemy, additionalReactionTime);
            if (PredPos.Distance(kegPosition) < 400 - enemy.MoveSpeed*(GetQTime(kegPosition)+(additionalBarrelTime ? 400 : 0) - (additionalReactionTime ? Config.Item("misc.additionalReactionTime").GetValue<Slider>().Value : 0) - Config.Item("misc.reactionTime").GetValue<Slider>().Value) * 0.00095f)
            {
                //Game.PrintChat("Distance:" + PredPos.Distance(kegPosition));
                //Game.PrintChat("Max Distance:" + (400 - enemy.MoveSpeed * (GetQTime(kegPosition) + (additionalBarrelTime ? 400 : 0) - (additionalReactionTime ? Config.Item("misc.additionalReactionTime").GetValue<Slider>().Value : 0) - Config.Item("misc.reactionTime").GetValue<Slider>().Value) * 0.00095f));
                return true;
            }
            return false;
        }
        
    }
}
