using System;
using LeagueSharp.Common;

namespace e.Motion_Katarina
{
    static class Program
    {
        static void Main(string[] args)
        {
            if(HeroManager.Player.ChampionName != "Katarina")
            {
                return;
            }
            CustomEvents.Game.OnGameLoad += Initialize;
        }
        static void Initialize(EventArgs args)
        {
            //Various Entry Points
            Config.InitializeMenu();
            Logic.startLogic();
            DaggerManager.startTracking();
            BlockIssueOrder.InitializeBlockIssueOrder();
        }
    }
}
