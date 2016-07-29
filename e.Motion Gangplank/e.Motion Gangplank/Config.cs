using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;

namespace e.Motion_Gangplank
{
    class Config
    {
        public static Menu Menu;
        public static Orbwalking.Orbwalker Orbwalker;
        public void Initialize()
        {
            Menu = new Menu("e.Motion Gangplank", "mainMenu", true);

            //Orbwalker
            Menu orbwalkerMenu = new Menu("Orbwalker", "orbwalkerMain");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);

            //Key
            Menu keyMenu = new Menu("Key","key");
            keyMenu.AddItem(new MenuItem("key.q", "Semi-Automatic Q").SetValue(new KeyBind(81,KeyBindType.Press)));
            Menu.AddSubMenu(keyMenu);
           
            //Combo
            Menu comboMenu = new Menu("Combo", "combo");
            comboMenu.AddItem(new MenuItem("combo.q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("combo.qe", "Use Q on Barrel").SetValue(true));
            comboMenu.AddItem(new MenuItem("combo.e", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("combo.ex", "Use E to Extend").SetValue(true));
            comboMenu.AddItem(new MenuItem("combo.r", "Use R").SetValue(true));
            Menu.AddSubMenu(comboMenu);

            //Lasthit
            Menu lasthitMenu = new Menu("Lasthit", "lasthit");
            lasthitMenu.AddItem(new MenuItem("lasthit.q", "Use Q").SetValue(true));
            lasthitMenu.AddItem(new MenuItem("lasthit.qe", "Use Q on Barrels").SetValue(true));
            Menu.AddSubMenu(lasthitMenu);

            //Killsteal
            Menu killstealMenu = new Menu("Killsteal","killsteal");
            killstealMenu.AddItem(new MenuItem("killsteal.q ", "Use Q").SetValue(true));
            killstealMenu.AddItem(new MenuItem("killsteal.qe", "Use Q on Barrel").SetValue(true));

            //Drawings
            Menu drawingMenu = new Menu("Drawings","drawings");
            drawingMenu.AddItem(new MenuItem("drawings.warning", "Remember me to Upgrade Ult").SetValue(true));
            Menu.AddSubMenu(drawingMenu);

            //Misc
            Menu miscMenu = new Menu("Miscellanious", "misc");
            miscMenu.AddItem(new MenuItem("misc.additionalServerTick", "Additional Server Tick").SetTooltip("Don't change that if you don't know what it is").SetValue(new Slider(30)));
            miscMenu.AddItem(new MenuItem("misc.reactionTime", "enemy Reaction Time").SetTooltip("Higher = Possible not to hit enemy with Barrel, Lower = Possible to use additional Barrels").SetValue(new Slider(100,0,500)));
            miscMenu.AddItem(new MenuItem("misc.additionalReactionTime", "Additional Reaction Time on Direction Change").SetTooltip("For Calculation when enemy will change direction").SetValue(new Slider(50, 0, 200)));
            miscMenu.AddItem(new MenuItem("misc.trye", "Always extend with E").SetValue(true).SetTooltip("Will always try to Extend with E to get additional Champions"));
            Menu.AddSubMenu(miscMenu);

            Menu.AddToMainMenu();
        }

        public static MenuItem Item(string itemname)
        {
            return Menu.Item(itemname);
        }
    }
}
