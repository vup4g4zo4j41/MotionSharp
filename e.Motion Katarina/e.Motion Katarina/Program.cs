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
            Menu orbwalkerMenu = new Menu("Orbwalker", "motion.katarina.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _Menu.AddSubMenu(orbwalkerMenu);

            //TargetSelector-Menü
            TargetSelector.AddToMenu(_Menu);

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
            Menu harrassMenu = new Menu("Harrass", "motion.katarina.harrass");
            {
                harrassMenu.AddItem(new MenuItem("motion.katarina.harrass.useq", "Use Q").SetValue(true));
                harrassMenu.AddItem(new MenuItem("motion.katarina.harrass.usew", "Use W").SetValue(true));
                harrassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrass", "Automatic Harrass"));
                harrassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrasskey","Toogle Harrass").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
            }
            _Menu.AddSubMenu(harrassMenu);

            //KS-Menü
            Menu ksMenu = new Menu("KillSteal", "motion.katarina.killsteal");
            {
                ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.q", "Use Q"));
                ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.w", "Use W"));
                ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.e", "Use E"));
                ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.wardjump", "KS with Wardjump"));
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
            Game.OnUpdate += OnUpdate;
            
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;

            #endregion
        }



        private static void OnUpdate(EventArgs args) {
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

            //Demark, important for W Usage
            if (qTarget != null && (qTarget.HasBuff("katarinaqmark") || Q.Cooldown < 3))
            {
                qTarget = null;
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                //Combo
                Combo();
            }

            if (_Menu.Item("motion.katarina.misc.wardjumpkey").GetValue<KeyBind>().Active && _Menu.Item("motion.katarina.misc.wardjump").GetValue<bool>())
            {
                WardJump();
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
            bool useq = _Menu.Item("motion.katarina.Combo.useq").GetValue<bool>();
            bool usew = _Menu.Item("motion.katarina.Combo.usew").GetValue<bool>();
            bool usee = _Menu.Item("motion.katarina.Combo.usee").GetValue<bool>();
            bool user = _Menu.Item("motion.katarina.Combo.user").GetValue<bool>();
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if(target != null && !target.IsZombie)
            {
                if(useq && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                    qTarget = target;
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
        }


        private static bool CanCastW(Obj_AI_Hero enemy)
        {
            return R.IsReady() || (enemy != null && enemy!=qTarget);
        }

        #region WardJumping
        //alles zum Wardjump
        static void WardJump()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (!E.IsReady())
            {
                return;
            }
            var lstGameObjects = ObjectManager.Get<Obj_AI_Base>().ToArray();
            Obj_AI_Base entityToWardJump = lstGameObjects.FirstOrDefault(obj => 
                obj.Position.Distance(MaxPosition(Player.Position.To2D(), Game.CursorPos.To2D(), E.Range).To3D()) < 150
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

                if (wardId != -1)
                {
                    PutWard(MaxPosition(Player.Position.To2D(), Game.CursorPos.To2D(), 600), (ItemId) wardId);
                    lstGameObjects = ObjectManager.Get<Obj_AI_Base>().ToArray();
                    E.Cast(
                        lstGameObjects.FirstOrDefault(obj =>
                        obj.Position.Distance(Player.Position.Extend(Game.CursorPos, 600)) < 150 &&
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

        public static bool PutWard(Vector2 pos, ItemId warditem)
        {
            foreach (var slot in Player.InventoryItems.Where(slot => slot.Id == warditem))
            {
                ObjectManager.Player.Spellbook.CastSpell(slot.SpellSlot, pos.To3D());
                return true;
            }
            return false;
        }
        
        public static bool InDistance(Vector2 pos1, Vector2 pos2, float distance)
        {
            float dist2 = Vector2.DistanceSquared(pos1, pos2);
            return dist2 <= distance * distance;
        }

        public static Vector2 MaxPosition(Vector2 init, Vector2 pos, float distance)
        {
            if (InDistance(init, pos, distance))
            {
                return pos;
            }
            return distance*(pos - init).Normalized()+init;
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
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R, 0) * 6;
            }
            return (float)damage;
        }
    }
}