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
            //Game.PrintChat("Channeling for" + Program.Q.Instance.SData.ChannelDuration);
        }

        public static void GetPredPos(Obj_AI_Hero enemy)
        {
            PredPos = SPrediction.Prediction.GetFastUnitPosition(enemy, Config.Menu.Item("misc.reactionTime").GetValue<Slider>().Value);
        }
        public static bool CanEscape(this Vector3 kegPosition, Obj_AI_Hero enemy, bool additionalReactionTime = false)
        {
            if (kegPosition.Distance(enemy.Position) < 400 - enemy.MoveSpeed*GetQTime(kegPosition))
                return false;
            int reactionTicks = Config.Menu.Item("misc.reactionTime").GetValue<Slider>().Value +
                                (additionalReactionTime
                                    ? Config.Menu.Item("misc.additionalReactionTime").GetValue<Slider>().Value
                                    : 0);
            PredPos = SPrediction.Prediction.GetFastUnitPosition(enemy,reactionTicks);
            if(PredPos.Distance(enemy.Position) < 400 - enemy.MoveSpeed*GetQTime(kegPosition)*0.00095)
                return false;
            return true;
        }
        
    }
}
