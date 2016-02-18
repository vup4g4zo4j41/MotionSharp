using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


namespace e.Motion_Katarina{
    class Program {

        #region Declaration

        private static bool ShallJumpNow;
        private static Vector3 JumpPosition;
        private static Spell Q, W, E, R;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _menu;
        private static int whenToCancelR;
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Obj_AI_Hero qTarget;
        private static readonly Obj_AI_Hero[] AllEnemy = HeroManager.Enemies.ToArray();
        private static bool WardJumpReady;
        private static SpellSlot IgniteSpellSlot;
        private static Dictionary<int,Obj_AI_Turret> AllEnemyTurret = new Dictionary<int,Obj_AI_Turret>();
        private static Dictionary<int, Obj_AI_Turret> AllAllyTurret = new Dictionary<int, Obj_AI_Turret>();
        private static Dictionary<int,bool> TurretHasAggro = new Dictionary<int, bool>();
        private static int lastLeeQTick;

        #endregion

        static bool IsTurretPosition(Vector3 pos)
        {
            float mindistance = 2000;
            foreach (KeyValuePair<int, Obj_AI_Turret> key in AllAllyTurret)
            {
                if (key.Value != null && !key.Value.IsDead && !TurretHasAggro[key.Value.NetworkId])
                {
                    float distance = pos.Distance(key.Value.Position);
                    if (mindistance >= distance)
                    {
                        mindistance = distance;
                        
                    }

                }
            }
            return mindistance <= 950;
        }

        static void Game_OnGameLoad(EventArgs args) {
            //Wird aufgerufen, wenn LeagueSharp Injected
            if (Player.ChampionName != "Katarina")
            {
                return;
            }
            #region Spells
            Q = new Spell(SpellSlot.Q, 675, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 375, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 700, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 550, TargetSelector.DamageType.Magical);
            //Get Ignite
            if (Player.Spellbook.GetSpell(SpellSlot.Summoner1).Name.Contains("summonerdot"))
            {
                IgniteSpellSlot = SpellSlot.Summoner1;
            }
            if (Player.Spellbook.GetSpell(SpellSlot.Summoner2).Name.Contains("summonerdot"))
            {
                IgniteSpellSlot = SpellSlot.Summoner2;
            }
            #endregion

            foreach (Obj_AI_Turret turret in ObjectManager.Get<Obj_AI_Turret>())
            {
                if (turret.IsEnemy)
                {
                    AllEnemyTurret.Add(turret.NetworkId,turret);
                    TurretHasAggro[turret.NetworkId] = false;
                }
                if (turret.IsAlly)
                {
                    AllAllyTurret.Add(turret.NetworkId, turret);
                    TurretHasAggro[turret.NetworkId] = false;
                }
            }

            Utility.HpBarDamageIndicator.Enabled = true;
            Utility.HpBarDamageIndicator.DamageToUnit = CalculateDamage;

            
            #region Menu
            _menu = new Menu("e.Motion Katarina", "motion.katarina", true);

            //Orbwalker-Menü
            Menu orbwalkerMenu = new Menu("Orbwalker", "motion.katarina.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _menu.AddSubMenu(orbwalkerMenu);

            //Combo-Menü
            Menu comboMenu = new Menu("Combo", "motion.katarina.Combo");
            comboMenu.AddItem(new MenuItem("motion.katarina.Combo.useq", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("motion.katarina.Combo.usew", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("motion.katarina.Combo.usee", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("motion.katarina.Combo.user", "Use R").SetValue(true));
            _menu.AddSubMenu(comboMenu);

            //Harrass-Menü
            Menu harassMenu = new Menu("Harass", "motion.katarina.harrass");
            harassMenu.AddItem(new MenuItem("motion.katarina.harrass.useq", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("motion.katarina.harrass.usew", "Use W").SetValue(true));
            Menu autoHarassMenu = new Menu("Autoharass","motion.katarina.autoharrass");
            autoHarassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrass.toggle", "Automatic Harrass").SetValue(true));
            autoHarassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrass.key","Toogle Harrass").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
            autoHarassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrass.usew", "Use W").SetValue(true));
            autoHarassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrass.useq", "Use Q").SetValue(false));
            harassMenu.AddSubMenu(autoHarassMenu);
            _menu.AddSubMenu(harassMenu);
            
            //Laneclear-Menü
            Menu laneclear = new Menu("Laneclear", "motion.katarina.laneclear");
            laneclear.AddItem(new MenuItem("motion.katarina.laneclear.useq", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("motion.katarina.laneclear.usew", "Use W").SetValue(true));
            laneclear.AddItem(new MenuItem("motion.katarina.laneclear.minw", "Minimum Minions to use W").SetValue(new Slider(3,1,6)));
            laneclear.AddItem(new MenuItem("motion.katarina.laneclear.minwlasthit", "Minimum Minions to Lasthit with W").SetValue(new Slider(2, 0, 6)));
            _menu.AddSubMenu(laneclear);

            //Jungleclear-Menü
            Menu jungleclear = new Menu("Jungleclear", "motion.katarina.jungleclear");
            jungleclear.AddItem(new MenuItem("motion.katarina.jungleclear.useq", "Use Q").SetValue(true));
            jungleclear.AddItem(new MenuItem("motion.katarina.jungleclear.usew", "Use W").SetValue(true));
            jungleclear.AddItem(new MenuItem("motion.katarina.jungleclear.usee", "Use E").SetValue(true));
            _menu.AddSubMenu(jungleclear);

            //Lasthit-Menü
            Menu lasthit = new Menu("Lasthit", "motion.katarina.lasthit");
            lasthit.AddItem(new MenuItem("motion.katarina.lasthit.useq", "Use Q").SetValue(true));
            lasthit.AddItem(new MenuItem("motion.katarina.lasthit.usew", "Use W").SetValue(true).SetTooltip("Advanced Calculation may cause FPS-Drops"));
            lasthit.AddItem(new MenuItem("motion.katarina.lasthit.usee", "Use E").SetValue(false).SetTooltip("Advanced Calculation may cause FPS-Drops"));
            lasthit.AddItem(new MenuItem("motion.katarina.lasthit.type", "Lasthit Algorithm").SetValue(new StringList(new[] {"Advanced", "Lightweight", "Lightweight and Advanced"})));
            _menu.AddSubMenu(lasthit);

            //KS-Menü
            Menu ksMenu = new Menu("Killsteal", "motion.katarina.killsteal");
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.useq", "Use Q").SetValue(true));
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.usew", "Use W").SetValue(true));
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.usee", "Use E").SetValue(true));
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.usef", "Use Ignite").SetValue(true));
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.wardjump", "KS with Wardjump").SetValue(true));
            _menu.AddSubMenu(ksMenu);

            //Drawings-Menü
            Menu drawingsMenu = new Menu("Drawings","motion.katarina.drawings");
            drawingsMenu.AddItem(new MenuItem("motion.katarina.drawings.drawq", "Draw Q").SetValue(false));
            drawingsMenu.AddItem(new MenuItem("motion.katarina.drawings.draww", "Draw W").SetValue(false));
            drawingsMenu.AddItem(new MenuItem("motion.katarina.drawings.drawe", "Draw E").SetValue(false));
            drawingsMenu.AddItem(new MenuItem("motion.katarina.drawings.drawr", "Draw R").SetValue(false));
            drawingsMenu.AddItem(new MenuItem("motion.katarina.drawings.drawalways", "Draw Always").SetValue(false).SetTooltip("Enable this if you want Drawings while your Skills are on Cooldown"));
            _menu.AddSubMenu(drawingsMenu);

            //Misc-Menü
            Menu miscMenu = new Menu("Miscellanious", "motion.katarina.misc");
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.wardjump", "Use Wardjump").SetValue(true));
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.wardjumpkey", "Wardjump Key").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.noRCancel", "Prevent R Cancel").SetValue(true).SetTooltip("This is preventing you from cancelling R accidentally within the first 0.4 seconds of cast"));
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.kswhileult", "Do Killsteal while Ulting").SetValue(true));
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.allyTurret", "Jump unter Turret for Gapcloser").SetTooltip("Try to Jump under Ally Turret when enemy tries to Gapclose you").SetValue(true));
            _menu.AddSubMenu(miscMenu);

            //Dev-Menü
            Menu devMenu = new Menu("Dev", "motion.katarina.dev");
            devMenu.AddItem(new MenuItem("motion.katarina.dev.enable", "Enable Dev-Tools").SetValue(false));
            devMenu.AddItem(new MenuItem("motion.katarina.dev.targetdistance", "Distance to Target").SetValue(new KeyBind("L".ToCharArray()[0], KeyBindType.Press)));
            _menu.AddSubMenu(devMenu);

            //alles zum Hauptmenü hinzufügen
            _menu.AddToMainMenu();

            #endregion
            Game.PrintChat("<font color='#bb0000'>e</font>.<font color='#0000cc'>Motion</font> Katarina loaded");
            #region Subscriptions
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnIssueOrder += Obj_AI_Base_OnIssueOrder;
            Obj_AI_Base.OnTarget += Turret_OnTarget;
            Obj_AI_Base.OnBuffRemove += BuffRemove;
            

            #endregion
        }

       

        private static void OnDraw(EventArgs args)
        {
            if(_menu.Item("motion.katarina.drawings.drawq").GetValue<bool>() && (Q.IsReady() || _menu.Item("motion.katarina.drawings.drawalways").GetValue<bool>()))
                Render.Circle.DrawCircle(Player.Position,Q.Range,Color.IndianRed);
            if (_menu.Item("motion.katarina.drawings.draww").GetValue<bool>() && (W.IsReady() || _menu.Item("motion.katarina.drawings.drawalways").GetValue<bool>()))
                Render.Circle.DrawCircle(Player.Position, W.Range, Color.IndianRed);
            if (_menu.Item("motion.katarina.drawings.drawe").GetValue<bool>() && (E.IsReady() || _menu.Item("motion.katarina.drawings.drawalways").GetValue<bool>()))
                Render.Circle.DrawCircle(Player.Position, E.Range, Color.IndianRed);
            if (_menu.Item("motion.katarina.drawings.drawr").GetValue<bool>() && (R.IsReady() || _menu.Item("motion.katarina.drawings.drawalways").GetValue<bool>()))
                Render.Circle.DrawCircle(Player.Position, R.Range, Color.IndianRed);
        }


        private static void BuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (sender.IsMe && args.Buff.Name == "BlindMonkQOne")
            {

                //Game.PrintChat("Player lost Lee Sin Q Buff");
                lastLeeQTick = Utils.TickCount;
            }
        }


        static void Game_OnUpdate(EventArgs args) {
            Demark();
            
            if (Player.IsDead || Player.IsRecalling())
            {
                return;
            }
            if (HasRBuff())
            {
                _orbwalker.SetAttack(false);
                _orbwalker.SetMovement(false);
                if(_menu.Item("motion.katarina.misc.kswhileult").GetValue<bool>())
                    Killsteal();
                return;
            }
            if (ShallJumpNow)
            {
                WardJump(JumpPosition,false,false);
                if (!E.IsReady())
                {
                    ShallJumpNow = false;
                }
            }
            _orbwalker.SetAttack(true);
            _orbwalker.SetMovement(true);
            Dev();
            Killsteal();
            Combo();
            Lasthit();
            Harass();
            LaneClear();
            JungleClear();
            if (_menu.Item("motion.katarina.misc.wardjumpkey").GetValue<KeyBind>().Active && _menu.Item("motion.katarina.misc.wardjump").GetValue<bool>())
            {
                WardJump(Game.CursorPos);
            }
        }

        private static void Dev()
        {
            if(_menu.Item("motion.katarina.dev.enable").GetValue<bool>() && _menu.Item("motion.katarina.dev.targetdistance").GetValue<KeyBind>().Active)
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    Game.PrintChat("Distance to Target:" + Player.Distance(target));
                }
            }
        }


        static bool HasRBuff()
        {
            return Player.HasBuff("KatarinaR") || Player.IsChannelingImportantSpell() || Player.HasBuff("katarinarsound");
        }



        static void Main(string[] args) {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        


        static void Combo()
        {
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                return;
            Obj_AI_Hero target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
           
            if(target != null && !target.IsZombie)
            {
                if(_menu.Item("motion.katarina.Combo.useq").GetValue<bool>() && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                    qTarget = target;
                }
                if (_menu.Item("motion.katarina.Combo.usew").GetValue<bool>() && W.IsReady() && target.IsValidTarget(W.Range - 10) && (target != qTarget || (R.IsReady() && _menu.Item("motion.katarina.Combo.user").GetValue<bool>())))
                {
                    W.Cast(target);
                }
                if (_menu.Item("motion.katarina.Combo.usee").GetValue<bool>() 
                    && E.IsReady() 
                    && target.IsValidTarget(E.Range) 
                    && (W.IsReady() || R.IsReady() || target != qTarget))
                {
                    E.Cast(target);
                }
                if (_menu.Item("motion.katarina.Combo.user").GetValue<bool>() && R.IsReady() && target.IsValidTarget(375))
                {
                    R.Cast();
                    _orbwalker.SetAttack(false);
                    _orbwalker.SetMovement(false);
                    whenToCancelR = Utils.GameTimeTickCount + 400;
                }

            }
        }

        private static void Turret_OnTarget(Obj_AI_Base sender, Obj_AI_BaseTargetEventArgs args)
        {
            if (sender.GetType() == typeof (Obj_AI_Turret))
            {
                TurretHasAggro[sender.NetworkId] = !(args.Target == null || args.Target is Obj_AI_Minion);
                //Game.PrintChat("Turret with Index[" + sender.Index + "] has Aggro: " + (TurretHasAggro[sender.Index]? "yes" : "no"));
            }
        }

        public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name == "KatarinaQ" && args.Target.GetType() == typeof(Obj_AI_Hero))
            {
                qTarget = (Obj_AI_Hero) args.Target;
            }
            if (args.SData.Name == "katarinaE")
            {
                WardJumpReady = false;
            }
            if (sender.IsMe && WardJumpReady)
            {
                E.Cast((Obj_AI_Base)args.Target);
                WardJumpReady = false;
            }
            //Todo Check for Lee Q
            if (args.SData.Name == "blindmonkqtwo")
            {

                if (lastLeeQTick - Utils.TickCount <= 10)
                {
                    //Game.PrintChat("Trying to Jump undeder Ally Turret - OnProcessSpellCast");
                    JumpUnderTurret(-100,sender.Position);
                }
                lastLeeQTick = Utils.TickCount;
            }
            // Todo Test
            if (args.Target != null && args.Target.IsMe)
            {
                switch (args.SData.Name)
                {
                    case "ZedR":
                        JumpUnderTurret(-100,sender.Position);
                        break;
                    case "ViR":
                        JumpUnderTurret(100,sender.Position);
                        break;
                    case "NocturneParanoia":
                        JumpUnderTurret(100,sender.Position);
                        break;
                    case "MaokaiUnstableGrowth":
                        JumpUnderTurret(0,sender.Position);
                        break;
                }

            }

        }

        

        private static void JumpUnderTurret(float extrarange, Vector3 objectPosition)
        {
            float mindistance = 100000;
            //Getting next Turret
            Obj_AI_Turret turretToJump = null;

            foreach (KeyValuePair<int, Obj_AI_Turret> key in AllAllyTurret)
            {
                if (key.Value != null && !key.Value.IsDead )
                {
                    float distance = Player.Position.Distance(key.Value.Position);
                    if (mindistance >= distance)
                    {
                        mindistance = distance;
                        turretToJump = key.Value;
                    }
                    
                }
            }
            if (turretToJump != null && !TurretHasAggro[turretToJump.NetworkId] && Player.Position.Distance(turretToJump.Position) < 1500)
            {
                int i = 0;
                
                do
                {
                    Vector3 extPos = Player.Position.Extend(turretToJump.Position, 685 - i);
                    float dist = objectPosition.Distance(extPos + extrarange);
                    Vector3 predictedPosition = objectPosition.Extend(extPos, dist);
                    if (predictedPosition.Distance(turretToJump.Position) <= 890 && !predictedPosition.IsWall())
                    {
                        WardJump(Player.Position.Extend(turretToJump.Position, 650 - i), false);
                        JumpPosition = Player.Position.Extend(turretToJump.Position, 650 - i);
                        ShallJumpNow = true;
                        break;
                    }

                    i += 50;
                } while (i <= 300 || !Player.Position.Extend(turretToJump.Position, 650 - i).IsWall());
            }
            
        }


        static void Demark()
        {
            if ((qTarget!=null && qTarget.HasBuff("katarinaqmark")) || Q.Cooldown < 3)
            {
                qTarget = null;
            }
        }


        #region WardJumping
        private static void WardJump(Vector3 where,bool move = true,bool placeward = true)
        {
            if (move)
                Player.IssueOrder(GameObjectOrder.MoveTo, where);
            if (!E.IsReady())
            {
                return;
            }
            Vector3 wardJumpPosition = Player.Position.Distance(where) < 600 ? where : Player.Position.Extend(where, 600);
            var lstGameObjects = ObjectManager.Get<Obj_AI_Base>().ToArray();
            Obj_AI_Base entityToWardJump = lstGameObjects.FirstOrDefault(obj =>
                obj.Position.Distance(wardJumpPosition) < 150
                && (obj is Obj_AI_Minion || obj is Obj_AI_Hero)
                && !obj.IsMe && !obj.IsDead
                && obj.Position.Distance(Player.Position) < E.Range);

            if (entityToWardJump != null)
            {
                E.Cast(entityToWardJump);
            }
            else if(placeward)
            {
                int wardId = GetWardItem();
                if (wardId != -1 && !wardJumpPosition.IsWall())
                {
                    WardJumpReady = true;
                    PutWard(wardJumpPosition.To2D(), (ItemId)wardId);
                }
            }

        }

        public static int GetWardItem()
        {
            int[] wardItems = { 3340, 3350, 3205, 3207, 2049, 2045, 2044, 3361, 3154, 3362, 3160, 2043 };
            foreach (var id in wardItems.Where(id => Items.HasItem(id) && Items.CanUseItem(id)))
                return id;
            return -1;
        }

        public static void PutWard(Vector2 pos, ItemId warditem)
        {

            foreach (var slot in Player.InventoryItems.Where(slot => slot.Id == warditem))
            {
                ObjectManager.Player.Spellbook.CastSpell(slot.SpellSlot, pos.To3D());
                return;
            }
        }
        #endregion
        //Calculating Damage
        static float CalculateDamage(Obj_AI_Hero target)
        {
            double damage = 0d;
            if (Q.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) + ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
            }
            if (target.HasBuff("katarinaqmark"))
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
            }
            if (W.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            }
            if (E.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            }
            if (R.IsReady() || (ObjectManager.Player.GetSpell(R.Slot).State == SpellState.Surpressed && R.Level > 0))
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
            }
            if (Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > 0)
            {
                damage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
                damage -= target.HPRegenRate*2.5;
            }
            return (float)damage;
        }

        #region Killsteal
        static int CanKill(Obj_AI_Hero target, bool useq, bool usew, bool usee, bool usef)
        {
            double damage = 0;
            if (!useq && !usew && !usee &&!usef)
                return 0;
            if (Q.IsReady() && useq)
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
                if ((W.IsReady() && usew) || (E.IsReady() && usee))
                {
                    damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
                }

            }
            if (target.HasBuff("katarinaqmark"))
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
            }
            if (W.IsReady() && usew)
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            }
            if (E.IsReady() && usee)
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            }
            if (damage >= target.Health)
            {
                return 1;
            }
            if (Player.GetSummonerSpellDamage(target,Damage.SummonerSpell.Ignite) > 0 && !target.HasBuff("summonerdot") && !HasRBuff())
            {
                damage += Player.GetSummonerSpellDamage(target,Damage.SummonerSpell.Ignite);
                damage -= target.HPRegenRate*2.5;
            }
            return damage >= target.Health? 2 : 0;

        }

        private static void Killsteal()
        {
            foreach (Obj_AI_Hero enemy in AllEnemy)
            {
                if (enemy == null)
                    return;
                if (CanKill(enemy, false, _menu.Item("motion.katarina.killsteal.usew").GetValue<bool>(), false, false)==1 && enemy.IsValidTarget(390))
                {
                    W.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, false, false, _menu.Item("motion.katarina.killsteal.usee").GetValue<bool>(), false)==1 && enemy.IsValidTarget(700))
                {
                    E.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, _menu.Item("motion.katarina.killsteal.useq").GetValue<bool>(), false, false, false)==1 && enemy.IsValidTarget(675))
                {
                    Q.Cast(enemy);
                    qTarget = enemy;
                    return;
                }
                int cankill = CanKill(enemy, _menu.Item("motion.katarina.killsteal.useq").GetValue<bool>(),_menu.Item("motion.katarina.killsteal.usew").GetValue<bool>(),_menu.Item("motion.katarina.killsteal.usee").GetValue<bool>(),_menu.Item("motion.katarina.killsteal.usef").GetValue<bool>());
                if (( cankill==1 || cankill == 2) && enemy.IsValidTarget(Q.Range))
                {
                    if (cankill == 2 && enemy.IsValidTarget(600))
                        Player.Spellbook.CastSpell(IgniteSpellSlot,enemy);
                    if (Q.IsReady())
                        Q.Cast(enemy);
                    if (E.IsReady() && (W.IsReady() || qTarget != enemy))
                        E.Cast(enemy);
                    if (W.IsReady() && enemy.IsValidTarget(390) && qTarget != enemy)
                        W.Cast();
                    return;
                }
                //KS with Wardjump
                cankill = CanKill(enemy, true, false, false,_menu.Item("motion.katarina.killsteal.usef").GetValue<bool>());
                if (_menu.Item("motion.katarina.killsteal.wardjump").GetValue<bool>() && (cankill ==1 || cankill ==2) && enemy.IsValidTarget(1300) && Q.IsReady() && E.IsReady())
                {
                    WardJump(enemy.Position, false);
                    if (cankill == 2 && enemy.IsValidTarget(600))
                        Player.Spellbook.CastSpell(IgniteSpellSlot, enemy);
                    if (enemy.IsValidTarget(675))
                        Q.Cast(enemy);
                    return;
                }
            }
        }
        #endregion

        #region Harrass

        private static void Harass()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            //Q-Logic
            if ((_menu.Item("motion.katarina.harrass.autoharrass.toggle").GetValue<bool>() && _menu.Item("motion.katarina.harrass.autoharrass.key").GetValue<KeyBind>().Active
                && _menu.Item("motion.katarina.harrass.autoharrass.useq").GetValue<bool>()
                || _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && _menu.Item("motion.katarina.harrass.useq").GetValue<bool>())
                && target != null && Q.IsReady())
            {
                Q.Cast(target);
                qTarget = target; 
            }
            //Q-Logic
            target = HeroManager.Enemies.FirstOrDefault(hero => !hero.IsDead && hero.Distance(Player) <390);
                
            if ((_menu.Item("motion.katarina.harrass.autoharrass.toggle").GetValue<bool>() && _menu.Item("motion.katarina.harrass.autoharrass.key").GetValue<KeyBind>().Active
                && _menu.Item("motion.katarina.harrass.autoharrass.usew").GetValue<bool>()
                || _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && _menu.Item("motion.katarina.harrass.usew").GetValue<bool>())
                && target != null && Player.Distance(target) < 390 && target != qTarget)
            {
                W.Cast();
            }
        }

        #endregion

        #region Lasthit

        private static void Lasthit()
        {
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
                return;
            Obj_AI_Base[] sourroundingMinions;
            if (_menu.Item("motion.katarina.lasthit.usew").GetValue<bool>() && W.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, 390).ToArray();
                if (_menu.Item("motion.katarina.lasthit.type").GetValue<StringList>().SelectedIndex == 0)
                {
                    
                    //Only Cast W when minion is not killable with Autoattacks
                    if (
                        sourroundingMinions.Any(
                            minion =>
                                !minion.IsDead && _orbwalker.GetTarget() != minion &&
                                W.GetDamage(minion) > minion.Health &&
                                HealthPrediction.GetHealthPrediction(minion,
                                    (Player.CanAttack
                                        ? Game.Ping/2
                                        : Orbwalking.LastAATick - Utils.GameTimeTickCount +
                                          (int) Player.AttackDelay*1000) + 300 + (int) Player.AttackCastDelay*1000) <= 0))
                    {
                        W.Cast();
                    }
                }
                else if (_menu.Item("motion.katarina.lasthit.type").GetValue<StringList>().SelectedIndex == 1)
                {
                    if (sourroundingMinions.Any(minion => !minion.IsDead && W.GetDamage(minion) > minion.Health))
                        W.Cast();
                }
                else
                {
                    if(sourroundingMinions[0] != null && !sourroundingMinions[0].IsDead && W.GetDamage(sourroundingMinions[0]) > sourroundingMinions[0].Health && _orbwalker.GetTarget() != sourroundingMinions[0] && HealthPrediction.GetHealthPrediction(sourroundingMinions[0],
                                    (Player.CanAttack
                                        ? Game.Ping / 2
                                        : Orbwalking.LastAATick - Utils.GameTimeTickCount +
                                          (int)Player.AttackDelay * 1000) + 300 + (int)Player.AttackCastDelay * 1000) <= 0)
                    {
                        W.Cast();
                    }
                }

            }
            if (_menu.Item("motion.katarina.lasthit.useq").GetValue<bool>() && Q.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, Q.Range).ToArray();
                foreach (var minion in sourroundingMinions.Where(minion => !minion.IsDead && Q.GetDamage(minion) > minion.Health))
                {

                    Q.Cast(minion);
                    break;
                }
            }
            if (_menu.Item("motion.katarina.lasthit.usee").GetValue<bool>() && E.IsReady())
            {
                //Same Logic with W + not killable with W
                sourroundingMinions = MinionManager.GetMinions(Player.Position, E.Range).ToArray();
                if (_menu.Item("motion.katarina.lasthit.type").GetValue<StringList>().SelectedIndex == 0)
                {
                    foreach (var minions in sourroundingMinions.Where(
                        minion =>
                            !minion.IsDead && _orbwalker.GetTarget() != minion &&
                            E.GetDamage(minion) >= minion.Health &&
                            (!W.IsReady() || !_menu.Item("motion.katarina.lasthit.usew").GetValue<bool>() || Player.Position.Distance(minion.Position) > 390)
                            &&
                            HealthPrediction.GetHealthPrediction(minion,
                                (Player.CanAttack
                                    ? Game.Ping/2
                                    : Orbwalking.LastAATick - Utils.GameTimeTickCount + (int) Player.AttackDelay*1000) +
                                300 + (int) Player.AttackCastDelay*1000) <= 0
                            &&
                            !IsTurretPosition(Player.Position.Extend(minion.Position,
                                Player.Position.Distance(minion.Position) + 35))))
                    {
                        E.Cast(minions);
                        break;
                    }
                }
                else if (_menu.Item("motion.katarina.lasthit.type").GetValue<StringList>().SelectedIndex == 1)
                {
                    foreach (
                        var minions in
                            sourroundingMinions.Where(
                                minion =>
                                    !minion.IsDead && E.GetDamage(minion) >= minion.Health &&
                                    !IsTurretPosition(Player.Position.Extend(minion.Position,
                                        Player.Position.Distance(minion.Position) + 35)) &&
                                    (W.IsReady() || _menu.Item("motion.katarina.lasthit.usew").GetValue<bool>() ||
                                     Player.Position.Distance(minion.Position) > 390)))
                    {
                        E.Cast(minions);
                        break;
                    }
                }
                else
                {
                    if (sourroundingMinions[0] != null && !sourroundingMinions[0].IsDead &&
                        E.GetDamage(sourroundingMinions[0]) >= sourroundingMinions[0].Health &&
                        !IsTurretPosition(Player.Position.Extend(sourroundingMinions[0].Position,
                            Player.Position.Distance(sourroundingMinions[0].Position) + 35)) &&
                        (W.IsReady() || _menu.Item("motion.katarina.lasthit.usew").GetValue<bool>() ||
                         Player.Position.Distance(sourroundingMinions[0].Position) > 390))
                    {
                        E.Cast(sourroundingMinions[0]);
                    }
                }
            }
        }
        #endregion

        #region LaneClear
        private static void LaneClear()
        {
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
                return;
            Obj_AI_Base[] sourroundingMinions;
            if (_menu.Item("motion.katarina.laneclear.usew").GetValue<bool>() && W.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, W.Range - 5).ToArray();
                if (sourroundingMinions.GetLength(0) >= _menu.Item("motion.katarina.laneclear.minw").GetValue<Slider>().Value)
                {
                    int lasthittable = sourroundingMinions.Count(minion => W.GetDamage(minion) + (minion.HasBuff("katarinaqmark")? Q.GetDamage(minion,1) : 0) > minion.Health);
                    if (lasthittable >= _menu.Item("motion.katarina.laneclear.minwlasthit").GetValue<Slider>().Value)
                    {
                        W.Cast();
                    }
                }
            }
            if (_menu.Item("motion.katarina.laneclear.useq").GetValue<bool>() && Q.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, Q.Range - 5).ToArray();
                foreach (var minion in sourroundingMinions.Where(minion => !minion.IsDead))
                {
                    Q.Cast(minion);
                    break;
                }
            }
        }
        #endregion

        #region Jungleclear

        private static void JungleClear()
        {
            Obj_AI_Base[] sourroundingMinions;
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
                return;
            if (_menu.Item("motion.katarina.jungleclear.useq").GetValue<bool>() && Q.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral).ToArray();
                float maxhealth = 0;
                int chosenminion = 0;
                if (sourroundingMinions.GetLength(0) >= 1)
                {
                    for(int i = 0;i < sourroundingMinions.Length; i++)
                    {
                        if (maxhealth < sourroundingMinions[i].MaxHealth)
                        {
                            maxhealth = sourroundingMinions[i].MaxHealth;
                            chosenminion = i;
                        }
                    }
                    Q.Cast(sourroundingMinions[chosenminion]);
                }
            }
            if (_menu.Item("motion.katarina.jungleclear.usew").GetValue<bool>() && W.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, W.Range - 5, MinionTypes.All,MinionTeam.Neutral).ToArray();
                if (sourroundingMinions.GetLength(0) >= 1)
                {
                    W.Cast();
                }
            }
            if (_menu.Item("motion.katarina.jungleclear.usee").GetValue<bool>() && E.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral).ToArray();
                float maxhealth = 0;
                int chosenminion = 0;
                if (sourroundingMinions.GetLength(0) >= 1)
                {
                    for (int i = 0; i < sourroundingMinions.Length; i++)
                    {
                        if (maxhealth < sourroundingMinions[i].MaxHealth)
                        {
                            maxhealth = sourroundingMinions[i].MaxHealth;
                            chosenminion = i;
                        }
                    }
                    E.Cast(sourroundingMinions[chosenminion]);
                }
            }
        }
        #endregion
        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender.IsMe && HasRBuff() && Utils.GameTimeTickCount <= whenToCancelR && _menu.Item("motion.katarina.misc.noRCancel").GetValue<bool>())
                args.Process = false;
        }

    }
}