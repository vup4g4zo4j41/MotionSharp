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
        static Obj_AI_Hero[] allEnemy = HeroManager.Enemies.ToArray();
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
            Game.PrintChat("Summoner F Hash Code");
            Game.PrintChat(SpellSlot.Summoner2.GetHashCode().ToString());

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
            Menu ComboMenu = new Menu("Combo", "motion.katarina.Combo");
            {
                ComboMenu.AddItem(new MenuItem("motion.katarina.Combo.useq", "Use Q").SetValue(true));
                ComboMenu.AddItem(new MenuItem("motion.katarina.Combo.usew", "Use W").SetValue(true));
                ComboMenu.AddItem(new MenuItem("motion.katarina.Combo.usee", "Use E").SetValue(true));
                ComboMenu.AddItem(new MenuItem("motion.katarina.Combo.user", "Use R").SetValue(true));
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
            _Menu.AddSubMenu(KSMenu);

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
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                //Combo
                Combo();
            }
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
            if (HasRBuff())
                return;
            var useq = _Menu.Item("motion.katarina.Combo.useq").GetValue<bool>();
            var usew = _Menu.Item("motion.katarina.Combo.usew").GetValue<bool>();
            var usee = _Menu.Item("motion.katarina.Combo.usee").GetValue<bool>();
            var user = _Menu.Item("motion.katarina.Combo.user").GetValue<bool>();
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if(target != null && !target.IsZombie)
            {
                if(useq && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                }
                if (usew && W.IsReady() && target.IsValidTarget(W.Range - 10) && CanCastW(target))
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
            if (qTarget.HasBuff("katarinaqmark") || Q.Cooldown < 3)
            {
                qTarget = null;
            }
        }

        static bool CanCastW(Obj_AI_Hero enemy)
        {
            if(R.IsReady() || enemy!=qTarget)
            return true;
        return false;
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
                if (Player.Distance(enemy) > 1375)
                {
                    return;
                }
                if (CanKill(enemy, false, _Menu.Item("motion.katarina.killsteal.usew").GetValue<bool>(), false) && Player.Distance(enemy) < W.Range - 10)
                {
                    W.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, false , false, _Menu.Item("motion.katarina.killsteal.usee").GetValue<bool>()) && Player.Distance(enemy) < 675)
                {
                    E.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, _Menu.Item("motion.katarina.killsteal.useq").GetValue<bool>(), false, false) && Player.Distance(enemy) < 675)
                {
                    Q.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, _Menu.Item("motion.katarina.killsteal.useq").GetValue<bool>(), _Menu.Item("motion.katarina.killsteal.usew").GetValue<bool>(), _Menu.Item("motion.katarina.killsteal.usee").GetValue<bool>()) && Player.Distance(enemy) < 675)
                {
                    if(Q.IsReady())
                        Q.Cast(enemy);
                    if(E.IsReady())
                        E.Cast(enemy);
                    if (W.IsReady() && Player.Distance(enemy) < W.Range - 10)
                        W.Cast();
                    return; 
                }
                //KS with Wardjump
                if (_Menu.Item("motion.katarina.killsteal.wardjump").GetValue<bool>() && CanKill(enemy, true, false, false) && Player.Distance(enemy) < 1300 && E.IsReady())
                {
                    WardJump(enemy.Position, false);
                    if (Player.Distance(enemy) < 675)
                        Q.Cast(enemy);
                    return;
                }
            }
        }
        #endregion

    }
}