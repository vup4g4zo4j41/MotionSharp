using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;



namespace e.Motion_Katarina{
        class Program {

        #region Declaration
        static Spell Q, W, E, R;
        static SpellSlot IgniteSlot;
        static Orbwalking.Orbwalker _orbwalker;
        static Menu _Menu;
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Obj_AI_Hero qTarget = null;
        #endregion



        static void Game_OnGameLoad(EventArgs args) {
            //Wird aufgerufen, wenn LeagueSharp Injected
            if (Player.ChampionName != "Katarina")
            {
                return;
            }
            #region Spells
            Q = new Spell(SpellSlot.Q, 675);
            W = new Spell(SpellSlot.W, 375);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 550);
            #endregion

            Utility.HpBarDamageIndicator.Enabled = true;
            Utility.HpBarDamageIndicator.DamageToUnit = CalculateDamage;

            
            #region Menu
            _Menu = new Menu("e.Motion Katarina", "motion.katarina", true);

            //Orbwalker-Menü
            Menu OrbwalkerMenu = new Menu("Orbwalker", "motion.katarina.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(OrbwalkerMenu);
            _Menu.AddSubMenu(OrbwalkerMenu);

            //TargetSelector-Menü
            TargetSelector.AddToMenu(_Menu);

            //Combo-Menü
            Menu ComboMenu = new Menu("Combo", "motion.katarina.combo");
            {
                ComboMenu.AddItem(new MenuItem("motion.katarina.combo.useq", "Use Q").SetValue(true));
                ComboMenu.AddItem(new MenuItem("motion.katarina.combo.usew", "Use W").SetValue(true));
                ComboMenu.AddItem(new MenuItem("motion.katarina.combo.usee", "Use E").SetValue(true));
                ComboMenu.AddItem(new MenuItem("motion.katarina.combo.user", "Use R").SetValue(true));
            }
            _Menu.AddSubMenu(ComboMenu);

            //Harrass-Menü
            Menu HarrassMenu = new Menu("Harrass", "motion.katarina.harrass");
            {
                HarrassMenu.AddItem(new MenuItem("motion.katarina.harrass.useq", "Use Q").SetValue(true));
                HarrassMenu.AddItem(new MenuItem("motion.katarina.harrass.usew", "Use W").SetValue(true));
                HarrassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrass", "Automatic Harrass"));
                HarrassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrasskey","Toogle Harrass").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
            }
            _Menu.AddSubMenu(HarrassMenu);

            //KS-Menü
            Menu KSMenu = new Menu("KillSteal", "motion.katarina.killsteal");
            {
                KSMenu.AddItem(new MenuItem("motion.katarina.killsteal.q", "Use Q"));
                KSMenu.AddItem(new MenuItem("motion.katarina.killsteal.w", "Use W"));
                KSMenu.AddItem(new MenuItem("motion.katarina.killsteal.e", "Use E"));
                KSMenu.AddItem(new MenuItem("motion.katarina.killsteal.wardjump", "KS with Wardjump"));
            }


            //alles zum Hauptmenü hinzufügen
            _Menu.AddToMainMenu();

            #endregion
            Game.PrintChat("<font color='#bb0000'>e</font><font color='#0000cc'>Motion</font> Katarina loaded");

            /**EDIT SUBSCRIPTIONS BASED ON YOUR LIKING**/
            #region Subscriptions
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += onProcessSpellCast;

            #endregion
            /**EDIT SUBSCRIPTIONS BASED ON YOUR LIKING**/
        }



        static void Game_OnUpdate(EventArgs args) {
            if (Player.IsDead)
            {
                return;
            }
            if (HasRBuff())
            {
                _orbwalker.SetAttack(false);
                _orbwalker.SetMovement(false);
            }
            else
            {
                _orbwalker.SetAttack(true);
                _orbwalker.SetMovement(true);
            }
            demark();
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                //Combo
                combo();
            }
            
        }



        static void Drawing_OnDraw(EventArgs args) {
        }



        static bool HasRBuff()
        {
            return (Player.HasBuff("KatarinaR") || Player.IsChannelingImportantSpell() || Player.HasBuff("katarinarsound"));
        }



        static void Main(string[] args) {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        


        static void combo()
        {
            if (HasRBuff())
                return;
            var useq = _Menu.Item("motion.katarina.combo.useq").GetValue<bool>();
            var usew = _Menu.Item("motion.katarina.combo.usew").GetValue<bool>();
            var usee = _Menu.Item("motion.katarina.combo.usee").GetValue<bool>();
            var user = _Menu.Item("motion.katarina.combo.user").GetValue<bool>();
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if(target != null && !target.IsZombie)
            {
                if(useq && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                }
                if (usew && W.IsReady() && target.IsValidTarget(W.Range - 10) && canCastW(target))
                {
                    W.Cast(target);
                }
                if (user && R.IsReady() && target.IsValidTarget(375))
                {
                    R.Cast();
                    _orbwalker.SetAttack(false);
                    _orbwalker.SetMovement(false);
                }
                if (usee && E.IsReady() && target.IsValidTarget(E.Range) && (!R.IsReady() || !target.IsValidTarget(375)))
                {
                    E.Cast(target);
                }
                
            }
            
        }



        static void onProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name == "KatarinaQ" && args.Target.GetType() == typeof(Obj_AI_Hero))
            {
                qTarget = (Obj_AI_Hero) args.Target;
            }
        }


        static void demark()
        {
            if (qTarget.HasBuff("katarinaqmark") || Q.Cooldown < 3)
            {
                qTarget = null;
                
                
            }
        }


        static bool canCastW(Obj_AI_Hero enemy)
        {
            if(R.IsReady() || enemy!=qTarget)
            return true;
        return false;
        }



        static void wardJump()
        {
            if (!Q.IsReady())
            {
                return;
            }
        }


        //Calculating Damage
        static float CalculateDamage(Obj_AI_Hero target) {
            double damage = 0d;
            if (Q.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) + ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
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
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R, 0)*6;
            }
            return (float)damage;
        }

    }
}