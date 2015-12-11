using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;
using SharpDX.Win32;


namespace e.Motion_Katarina{
    class Program {

        #region Declaration
        static Spell Q, W, E, R;
        static SpellSlot IgniteSlot;
        static Orbwalking.Orbwalker _orbwalker;
        static Menu _Menu;
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Obj_AI_Hero qTarget = null;
        static Obj_AI_Base[] _lstGameObjects;
        static readonly Obj_AI_Hero[] allEnemy = HeroManager.Enemies.ToArray();
        static int lastward;
        static bool WardJumpReady = false;
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
            Menu orbwalkerMenu = new Menu("Orbwalker", "motion.katarina.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _Menu.AddSubMenu(orbwalkerMenu);

            //Combo-Menü
            Menu comboMenu = new Menu("Combo", "motion.katarina.Combo");
            {
                comboMenu.AddItem(new MenuItem("motion.katarina.Combo.useq", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("motion.katarina.Combo.usew", "Use W").SetValue(true));
                comboMenu.AddItem(new MenuItem("motion.katarina.Combo.usee", "Use E").SetValue(true));
                comboMenu.AddItem(new MenuItem("motion.katarina.Combo.user", "Use R").SetValue(true));
            }
            _Menu.AddSubMenu(comboMenu);

            //Harrass-Menü
            Menu harassMenu = new Menu("Harass", "motion.katarina.harrass");
            {
                harassMenu.AddItem(new MenuItem("motion.katarina.harrass.useq", "Use Q").SetValue(true));
                harassMenu.AddItem(new MenuItem("motion.katarina.harrass.usew", "Use W").SetValue(true));
                harassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrass", "Automatic Harrass").SetValue(true));
                harassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrasskey","Toogle Harrass").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
            }
            _Menu.AddSubMenu(harassMenu);

            //KS-Menü
            Menu ksMenu = new Menu("KillSteal", "motion.katarina.killsteal");
            {
                ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.useq", "Use Q").SetValue(true));
                ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.usew", "Use W").SetValue(true));
                ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.usee", "Use E").SetValue(true));
                ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.wardjump", "KS with Wardjump").SetValue(true));
            }
            _Menu.AddSubMenu(ksMenu);

            //Misc-Menü
            Menu miscMenu = new Menu("Miscellanious","motion.katarina.misc");
            {
                miscMenu.AddItem(new MenuItem("motion.katarina.misc.wardjump", "Use Wardjump").SetValue(true));
                miscMenu.AddItem(new MenuItem("motion.katarina.misc.wardjumpkey", "Wardjump Key").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            }
            _Menu.AddSubMenu(miscMenu);

            //alles zum Hauptmenü hinzufügen
            _Menu.AddToMainMenu();

            #endregion
            Game.PrintChat("<font color='#bb0000'>e</font><font color='#0000cc'>Motion</font> Katarina loaded");

            #region Subscriptions
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;

            #endregion
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
                Killsteal();
            }
            Demark();
            if (true)
            {
                //Combo
                Combo();
            }

            Harass();
            if (_Menu.Item("motion.katarina.misc.wardjumpkey").GetValue<KeyBind>().Active && _Menu.Item("motion.katarina.misc.wardjump").GetValue<bool>())
            {
                WardJump(Game.CursorPos);
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
        


        static void Combo()
        {
            if (HasRBuff() || _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                return;
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if(target != null && !target.IsZombie)
            {
                if(_Menu.Item("motion.katarina.Combo.useq").GetValue<bool>() && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                    qTarget = target;
                }
                if (_Menu.Item("motion.katarina.Combo.usew").GetValue<bool>() && W.IsReady() && target.IsValidTarget(W.Range - 10) && target != qTarget)
                {
                    W.Cast(target);
                }
                if (_Menu.Item("motion.katarina.Combo.user").GetValue<bool>() && R.IsReady() && target.IsValidTarget(375))
                {
                    R.Cast();
                    _orbwalker.SetAttack(false);
                    _orbwalker.SetMovement(false);
                }
                if (_Menu.Item("motion.katarina.Combo.usee").GetValue<bool>() && E.IsReady() && target.IsValidTarget(E.Range) && (!R.IsReady() || !_Menu.Item("motion.katarina.Combo.user").GetValue<bool>() || !target.IsValidTarget(375)) && (W.IsReady() || R.IsReady() && target != qTarget ))
                {
                    E.Cast(target);
                }
            }
        }


        public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name == "KatarinaQ" && args.Target.GetType() == typeof(Obj_AI_Hero))
            {
                qTarget = (Obj_AI_Hero) args.Target;
            }
            if (sender.IsMe && WardJumpReady)
            {
                E.Cast((Obj_AI_Base)args.Target);
                WardJumpReady = false;
            }
        }


        static void Demark()
        {
            if (qTarget!=null && qTarget.HasBuff("katarinaqmark") || Q.Cooldown < 3)
            {
                qTarget = null;
            }
        }


        #region WardJumping
        static void WardJump(Vector3 where,bool move = true)
        {
            if(move)
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (!E.IsReady())
            {
                return;
            }
            Vector3 wardJumpPosition = Player.Position.Distance(where) < 600 ? where : Player.Position.Extend(Game.CursorPos, 600);
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
            else
            {
                int wardId = GetWardItem();


                if (wardId != -1 && !wardJumpPosition.IsWall())
                {
                    PutWard(wardJumpPosition.To2D(), (ItemId)wardId);
                    lstGameObjects = ObjectManager.Get<Obj_AI_Base>().ToArray();
                    E.Cast(
                        lstGameObjects.FirstOrDefault(obj =>
                        obj.Position.Distance(wardJumpPosition) < 150 &&
                        obj is Obj_AI_Minion && obj.Position.Distance(Player.Position) < E.Range));
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
            return (float)damage;
        }

        #region Killsteal
        static bool CanKill(Obj_AI_Hero target, bool useq, bool usew, bool usee)
        {
            double damage = 0;
            if (!useq && !usew && !usee)
                return false;
            if (Q.IsReady() && useq)
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
                if (W.IsReady() && usew || E.IsReady() && usee)
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
            return damage >= target.Health;
        }

        private static void Killsteal()
        {
            foreach (Obj_AI_Hero enemy in allEnemy)
            {
                if (enemy == null)
                    return;
                if (CanKill(enemy, false, _Menu.Item("motion.katarina.killsteal.usew").GetValue<bool>(), false) && enemy.IsValidTarget(390))
                {
                    W.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, false, false, _Menu.Item("motion.katarina.killsteal.usee").GetValue<bool>()) && enemy.IsValidTarget(700))
                {
                    E.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, _Menu.Item("motion.katarina.killsteal.useq").GetValue<bool>(), false, false) && enemy.IsValidTarget(675))
                {
                    Q.Cast(enemy);
                    qTarget = enemy;
                    return;
                }
                if (CanKill(enemy, _Menu.Item("motion.katarina.killsteal.useq").GetValue<bool>(), _Menu.Item("motion.katarina.killsteal.usew").GetValue<bool>(), _Menu.Item("motion.katarina.killsteal.usee").GetValue<bool>()) && enemy.IsValidTarget(675))
                {
                    if (Q.IsReady())
                        Q.Cast(enemy);
                    if (E.IsReady() && W.IsReady() || qTarget != enemy)
                        E.Cast(enemy);
                    if (W.IsReady() && enemy.IsValidTarget(390) && qTarget != enemy)
                        W.Cast();
                    return;
                }
                //KS with Wardjump
                if (_Menu.Item("motion.katarina.killsteal.wardjump").GetValue<bool>() && CanKill(enemy, true, false, false) && enemy.IsValidTarget(1300) && Q.IsReady() && E.IsReady())
                {
                    WardJump(enemy.Position, false);
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
            if (target != null && _Menu.Item("motion.katarina.harrass.autoharrass").GetValue<bool>() && _Menu.Item("motion.katarina.harrass.autoharrasskey").GetValue<bool>() || _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (Q.IsReady())
                    Q.Cast(target);
                if (W.IsReady() && null != TargetSelector.GetTarget(W.Range - 10, TargetSelector.DamageType.Magical))
                    W.Cast();
            }
        }

        #endregion
    }
}