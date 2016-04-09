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
        private const int QDELAY = 250;
        public static float GetQTime(Vector3 position)
        {
            return Program.Player.Distance(position) / 2.6f + QDELAY + Game.Ping/2 + Config.Menu.Item("misc.additionalServerTick").GetValue<Slider>().Value;
            //Game.PrintChat("Channeling for" + Program.Q.Instance.SData.ChannelDuration);
        }
        
        public static bool CanEscape(this Vector3 kegPosition, Obj_AI_Hero enemy, bool additionalReactionTime = false)
        {
            if (kegPosition.Distance(enemy.ServerPosition) > 400 - enemy.MoveSpeed*GetQTime(kegPosition))
                return false;
            
            return true;
        }
        
    }
}
