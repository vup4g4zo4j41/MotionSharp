using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace e.Motion_Gangplank
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        #region Declaration
        public static Spell Q, W, E, R;
        public static Obj_AI_Hero Player => ObjectManager.Player;
        public static List<Barrel> AllBarrel = new List<Barrel>();
        public static List<Waypoints> AllWaypoints = new List<Waypoints>(); 
        #endregion


        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Gangplank")
            {
                return;
            }
            Q = new Spell(SpellSlot.Q, 625);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R);
            #region Menu

            Config Menu = new Config();
            Menu.Initialize();
            #endregion
            Game.PrintChat("e.Motion Gangplank loaded");
            Game.PrintChat("Projectile Speed: "+ Q.Instance.SData.MissileSpeed);
            //Game.PrintChat("<font color='#bb0000'>e</font>.<font color='#0000cc'>Motion</font> Gangplank loaded");


            #region Subscriptions
            Game.OnUpdate += GameOnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreate;
            Obj_AI_Base.OnDoCast += CheckForBarrel;
            Obj_AI_Base.OnNewPath += OnNewPath;
            //Obj_AI_Base.OnDelete += OnDelete;

            #endregion

        }

        private static void OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if(sender.IsEnemy && sender is Obj_AI_Hero)
                Combo(true,(Obj_AI_Hero) sender);
        }

        private static void CheckForBarrel(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.Target != null && args.Target.Name == "Barrel")
            {
                for (int i = 0; i < AllBarrel.Count; i++)
                {
                    
                    if (AllBarrel.ElementAt(i).GetBarrel().NetworkId == args.Target.NetworkId)
                    {
                        Game.PrintChat("Barrel Time reduced");
                        if (sender.IsMelee)
                        {
                            AllBarrel.ElementAt(i).reduceBarrelAttackTick();
                        }
                        else
                        {
                            int i1 = i;
                            Utility.DelayAction.Add((int)(args.Start.Distance(args.End)/args.SData.MissileSpeed), () => { AllBarrel.ElementAt(i1).reduceBarrelAttackTick(); });
                        }
                    }
                }
            }
        }

        private static void CleanBarrel()
        {
            for (int i = AllBarrel.Count - 1; i >= 0; i--)
            {
                //Console.WriteLine("Looped");
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (AllBarrel.ElementAt(i).GetBarrel() == null || AllBarrel.ElementAt(i).GetBarrel().Health == 0)
                {
                    AllBarrel.RemoveAt(i);

                    Console.WriteLine("Removed a Barrel while Cleaning");
                    break;
                }
            }
        }
        

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Barrel")
            {
                Console.WriteLine("Some Barrel was removed");
                for (int i = AllBarrel.Count-1; i >= 0; i--)
                {
                    if (AllBarrel.ElementAt(i).GetBarrel().NetworkId == sender.NetworkId)
                    {
                        AllBarrel.RemoveAt(i);
                        
                        Console.WriteLine("Removed a Barrel");
                        break;
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Barrel")
            {
                AllBarrel.Add(new Barrel((Obj_AI_Minion)sender));
                Console.WriteLine("We got " + AllBarrel.Count + " Barrels");
            }
            

        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            
        }

        private static void GameOnUpdate(EventArgs args)
        {
            Combo();
            Lasthit();
            CleanBarrel();
            //QEDebug();
            //Game.PrintChat("Delay on Q: "+Q.Delay);
            
        }

        private static void QEDebug()
        {
            if (Q.IsReady())
            {
                for (int i = 0; i < AllBarrel.Count; i++)
                {
                    if (AllBarrel.ElementAt(i).CanQNow() && Player.Position.Distance(AllBarrel.ElementAt(i).GetBarrel().Position) <= Q.Range)
                    {
                        Q.Cast(AllBarrel.ElementAt(i).GetBarrel());
                    }
                }
            }
        }

        private static void Combo(bool extended = false,Obj_AI_Hero sender = null)
        {
            if (Config.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }
            
            if (Config.Menu.Item("combo.qe").GetValue<bool>() && Q.IsReady())
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    if (extended && target != sender)
                    {
                        extended = false;
                    }
                    foreach (var b in AllBarrel)
                    {
                        if (b.CanQNow() && !b.GetBarrel().Position.CanEscape(target, extended))
                        {
                            Q.Cast(b.GetBarrel());
                            Game.PrintChat("Casted on Extended basis: "+(extended?"yes":"no"));
                        }
                    }
                }
                //if (target != null)
                //{
                //    Q.Cast(target);
                //}
            }
        }
        

        private static void Lasthit()
        {
            if (Config.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
            {
                return;
            }

            //Todo Implement Barrel-check
            if (Config.Menu.Item("lasthit.q").GetValue<bool>() && Q.IsReady() && (AllBarrel.Count == 0))
            {
                var sourroundingMinions = MinionManager.GetMinions(Player.Position, Q.Range).ToArray();
                foreach(Obj_AI_Base minion in sourroundingMinions)
                {
                    float predictedHealth = HealthPrediction.GetHealthPrediction(minion,
                        (int) Helper.GetQTime(minion.Position) + Game.Ping/2, (int) Q.Delay*1000);
                    if (predictedHealth > 0 && predictedHealth <= Q.GetDamage(minion))
                    {
                        Q.Cast(minion);
                    }
                    
                }
            }
        }
         
    }
}
