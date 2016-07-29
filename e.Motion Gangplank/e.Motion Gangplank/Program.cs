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
using Color = System.Drawing.Color;


namespace e.Motion_Gangplank
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        #region Declaration

        private static int BarrelTime;
        public static Spell Q, W, E, R;
        public static Obj_AI_Hero Player => ObjectManager.Player;
        public static List<Barrel> AllBarrel = new List<Barrel>();
        public static Vector3 EnemyPosition;
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
            SetBarrelTime();
            //Game.PrintChat("<font color='#bb0000'>e</font>.<font color='#0000cc'>Motion</font> Gangplank loaded");
             

            #region Subscriptions

            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += GameOnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreate;
            Obj_AI_Base.OnDoCast += CheckForBarrel;
            Obj_AI_Base.OnNewPath += OnNewPath;
            Obj_AI_Base.OnLevelUp += OnLevelUp;
            //Obj_AI_Base.OnDelete += OnDelete;

            #endregion

        }

        private static void OnLevelUp(Obj_AI_Base sender, EventArgs args)
        {
            if(sender.IsMe)
                SetBarrelTime();
        }

        private static void OnDraw(EventArgs args)
        {
            Vector2 predPosOnScreen = Drawing.WorldToScreen(Helper.PredPos.To3D());
            Drawing.DrawLine(predPosOnScreen.X-10,predPosOnScreen.Y-10, predPosOnScreen.X + 10, predPosOnScreen.Y + 10,3,Color.Red);
            Drawing.DrawLine(predPosOnScreen.X + 10, predPosOnScreen.Y - 10, predPosOnScreen.X - 10, predPosOnScreen.Y + 10, 3, Color.Red);
            Drawing.DrawLine(predPosOnScreen,Drawing.WorldToScreen(EnemyPosition),1,Color.Blue);
            Warning();
        }

        private static void SetBarrelTime()
        {
            if (Player.Level < 7)
            {
                BarrelTime = 4;
            }
            else if (Player.Level < 13)
            {
                BarrelTime = 2;
            }
            else
            {
                BarrelTime = 1;
            }
        }
        private static void OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if(sender.IsEnemy && sender is Obj_AI_Hero)
                Combo(true,(Obj_AI_Hero) sender);
        }

        private static void KillSteal()
        {
            //if(Config.Item("killsteal.q").GetValue<bool>())
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
                            AllBarrel.ElementAt(i).ReduceBarrelAttackTick();
                        }
                        else
                        {
                            int i1 = i;
                            Utility.DelayAction.Add((int)(args.Start.Distance(args.End)/args.SData.MissileSpeed), () => { AllBarrel.ElementAt(i1).ReduceBarrelAttackTick(); });
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
            CleanBarrel();
            Combo();
            Lasthit();
            //Game.PrintChat(E.IsReady(1).ToString());
            //QEDebug();
            //Game.PrintChat("Delay on Q: "+Q.Delay);
            
        }

        private static void Warning()
        {
            if ((Player.Position.Distance(new Vector3(394, 461, 171)) <= 1000 ||
                 Player.Position.Distance(new Vector3(14340, 14391, 170)) <= 1000) &&
                Player.GetBuffCount("gangplankbilgewatertoken") >= 500 && Config.Item("drawings.warning").GetValue<bool>())
            {
                Drawing.DrawText(200,200,Color.Red,"Don't forget to buy Ultimate Upgrade with Silver Serpents");
            }
        }
        //private static void QEDebug()
        //{
        //    if (Q.IsReady())
        //    {
        //        for (int i = 0; i < AllBarrel.Count; i++)
        //        {
        //            if (AllBarrel.ElementAt(i).CanQNow() && Player.Position.Distance(AllBarrel.ElementAt(i).GetBarrel().Position) <= Q.Range)
        //            {
        //                Q.Cast(AllBarrel.ElementAt(i).GetBarrel());
        //            }
        //        }
        //    }
        //}

        private static void TryE(Barrel barrel, Obj_AI_Hero toIgnore)
        {
            if (!E.IsReady() || !Config.Item("misc.trye").GetValue<bool>())
                return;
            Vector3 castPos = new Vector3();
            Vector3 barrelPos = barrel.GetBarrel().Position;
            if (HeroManager.Enemies.FirstOrDefault(
                e =>
                    e != toIgnore && !e.IsZombie && !e.IsDead && e.Distance(barrelPos) < 1200 &&
                    !barrelPos.CannotEscape(e) && !GetBarrelsInRange(barrel).Any(b => b.GetBarrel().Position.CannotEscape(e,false,true)) && Helper.GetPredPos(e) &&
                    (castPos =
                        barrelPos.Extend(Helper.PredPos.To3D(), Math.Min(650, Player.Distance(Helper.PredPos.To3D()))))
                        .CannotEscape(e,false,true)) != null)
                E.Cast(castPos);
        }

        private static void Combo(bool extended = false,Obj_AI_Hero sender = null)
        {
            if (Config.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }
            
            if (Config.Menu.Item("combo.qe").GetValue<bool>() /* && Q.IsReady() */)
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(1200, TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    EnemyPosition = target.Position;
                    Helper.GetPredPos(target);
                    if (extended && target != sender)
                    {
                        extended = false;
                    }
                    if (!Q.IsReady()) return;
                    foreach (var b in AllBarrel)
                    {
                        
                        if (b.CanQNow() && (b.GetBarrel().Position.CannotEscape(target, extended) || GetBarrelsInRange(b).Any(bb => bb.GetBarrel().Position.CannotEscape(target, extended,true))))
                        {
                            TryE(b, target);
                            Q.Cast(b.GetBarrel());
                            Game.PrintChat("Casted on Extended basis: "+(extended?"yes":"no"));
                            return;
                        }
                    }
                    if (E.IsReady() && Config.Item("combo.ex").GetValue<bool>())
                    {
                        foreach (var b in AllBarrel)
                        {
                            Vector3 castPos;
                            if (b.CanQNow() &&
                                (castPos =
                                    b.GetBarrel()
                                        .Position.Extend(Helper.PredPos.To3D(),
                                            Math.Min(650, Helper.PredPos.To3D().Distance(b.GetBarrel().Position))))
                                    .Distance(Player.Position) < 1000 && castPos.CannotEscape(target, extended, true))
                            {
                                E.Cast(castPos);
                                Q.Cast(b.GetBarrel());
                                Game.PrintChat("Casted on Extended basis: " + (extended ? "yes" : "no"));
                                return;
                            }
                        }
                    }
                }
            }

            if (Config.Item("combo.q").GetValue<bool>() && Q.IsReady())
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (target != null && (E.Cooldown <= Q.Instance.Cooldown - BarrelTime || Config.Item("key.q").GetValue<KeyBind>().Active) && !AllBarrel.Any(b => b.GetBarrel().Distance(Player) < 1200))
                {
                    Q.Cast(target);
                }
            }
        }
        
        private static IEnumerable<Barrel> GetBarrelsInRange (Barrel initalBarrel)
        {
            return (AllBarrel.Where(b => b.GetBarrel().Position.Distance(initalBarrel.GetBarrel().Position) < 650 && b != initalBarrel));
        }
        private static void Lasthit()
        {
            if (Config.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
            {
                return;
            }
            if (Q.IsReady())
            {
                foreach (Barrel barrel in AllBarrel)
                {
                    if (barrel.CanQNow() && MinionManager.GetMinions(barrel.GetBarrel().Position,650).Any(m => m.Health < Q.GetDamage(m)))
                    {
                        Q.Cast(barrel.GetBarrel());
                    }
                }
                
                if (Config.Item("lasthit.q").GetValue<bool>() && (!AllBarrel.Any(b => b.GetBarrel().Position.Distance(Player.Position) < 1200) || Config.Item("key.q").GetValue<KeyBind>().Active))
                {
                    var lowHealthMinion = MinionManager.GetMinions(Player.Position, Q.Range).FirstOrDefault();
                    if (lowHealthMinion != null && lowHealthMinion.Health <= Q.GetDamage(lowHealthMinion))
                        Q.Cast(lowHealthMinion);
                }
            }
        }
    }
}
