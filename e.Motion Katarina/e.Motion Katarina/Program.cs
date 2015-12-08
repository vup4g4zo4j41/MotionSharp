﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace NoobJax
{
    class Program
    {
        public const string ChampionName = "Jax";
        public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public static Orbwalking.Orbwalker Orbwalker;
        //Menu
        public static Menu Menu;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        private static Items.Item tiamat;
        private static Items.Item hydra;
        private static Items.Item cutlass;
        private static Items.Item botrk;
        private static Items.Item hextech;
        private static Obj_AI_Base target;

        private static bool IsEUsed
        {
            get { return Player.HasBuff("JaxCounterStrike"); }
        }
        private static bool IsWUsed
        {
            get { return Player.HasBuff("JaxEmpowerTwo"); }
        }
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Jax") return;

            Q = new Spell(SpellSlot.Q, 680);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);


            Menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            Menu spellMenu = Menu.AddSubMenu(new Menu("Combo", "Combo"));
            spellMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            spellMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
            spellMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
            spellMenu.AddItem(new MenuItem("useR", "Use R").SetValue(true));

            var lc = new Menu("Laneclear", "Laneclear");
            Menu.AddSubMenu(lc);
            lc.AddItem(new MenuItem("laneclearQ", "Use Q to LaneClear").SetValue(true));
            lc.AddItem(new MenuItem("laneclearW", "Use W to LaneClear").SetValue(true));

            var harass = new Menu("Harass", "Harass");
            Menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("harassW", "Use W to Cancel AA to Harass").SetValue(true));

            var miscMenu = new Menu("Misc", "Misc");
            Menu.AddSubMenu(miscMenu);
            miscMenu.AddItem(new MenuItem("usejump", "Use Wardjump").SetValue(true));
            miscMenu.AddItem(new MenuItem("jumpkey", "Wardjump Key").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press))); //Standardkey für Wardjump

            hydra = new Items.Item(3074, 185);
            tiamat = new Items.Item(3077, 185);
            cutlass = new Items.Item(3144, 450);
            botrk = new Items.Item(3153, 450);
            hextech = new Items.Item(3146, 700);
            Menu.AddToMainMenu();

            Game.OnUpdate += OnUpdate;
            Orbwalking.OnAttack += OnAa;
            Orbwalking.AfterAttack += AfterAa;
            Game.PrintChat("NoobJax by 1Shinigamix321312312");
        }
        private static void OnUpdate(EventArgs args)
        {
            var m = TargetSelector.GetTarget(700, TargetSelector.DamageType.Physical);
            if (m.Health < Q.GetDamage(m))
            {
                Q.Cast(m);
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (Player.Distance(m) <= botrk.Range)
                {
                    botrk.Cast(m);
                }
                if (Player.Distance(m) <= cutlass.Range)
                {
                    cutlass.Cast(m);
                }
                if (Player.Distance(m) <= hextech.Range)
                {
                    hextech.Cast(m);
                }
                if (Menu.Item("useQ").GetValue<bool>())
                    if (Player.Distance(m.Position) > 125)
                        Q.CastOnBestTarget();

                if (Menu.Item("useR").GetValue<bool>())
                    R.Cast(m);
                if (hydra.IsOwned() && Player.Distance(m) < hydra.Range && hydra.IsReady() && !W.IsReady() && !IsWUsed)
                    hydra.Cast();
                if (tiamat.IsOwned() && Player.Distance(m) < tiamat.Range && tiamat.IsReady() && !W.IsReady() && !IsWUsed)
                    tiamat.Cast();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                var minion = MinionManager.GetMinions(Player.Position, 225).FirstOrDefault();
                if (hydra.IsOwned() && Player.Distance(minion) < hydra.Range && hydra.IsReady() && !W.IsReady())
                    hydra.Cast(minion);
                if (tiamat.IsOwned() && Player.Distance(minion) < tiamat.Range && tiamat.IsReady() && !W.IsReady())
                    tiamat.Cast(minion);
            }
        }
        private static void OnAa(AttackableUnit unit, AttackableUnit target)
        {
            Obj_AI_Hero y = TargetSelector.GetTarget(700, TargetSelector.DamageType.Physical);
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (E.IsReady())
                {
                    if (E.IsReady() && Q.IsReady() && y.IsValidTarget(Q.Range))
                    {
                        E.Cast();
                    }
                    if (IsEUsed && y.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
                    {
                        E.Cast();
                    }
                }
            }
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Mixed)
            {
                return;
            }
            if (hydra.IsOwned() && Player.Distance(target) < hydra.Range && hydra.IsReady() && !W.IsReady()) hydra.Cast();
            if (tiamat.IsOwned() && Player.Distance(target) < tiamat.Range && tiamat.IsReady() && !W.IsReady()) tiamat.Cast();
        }
        private static void AfterAa(AttackableUnit unit, AttackableUnit target)
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                var minion = MinionManager.GetMinions(Player.Position, 225).FirstOrDefault();
                if (Menu.Item("laneclearW").GetValue<bool>())
                    W.Cast(minion);
                if (Menu.Item("laneclearQ").GetValue<bool>() && !W.IsReady())
                    Q.Cast(minion);
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                if (Menu.Item("useW").GetValue<bool>())
                    W.Cast();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (Menu.Item("harassW").GetValue<bool>() && W.IsReady()) W.Cast();
            }
        }
        public static void WardJump()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (!Q.IsReady())
            {
                return;
            }
            Vector3 wardJumpPosition = (Player.Position.Distance(Game.CursorPos) < 600) ? Game.CursorPos : Player.Position.Extend(Game.CursorPos, 600);
            var lstGameObjects = ObjectManager.Get<Obj_AI_Base>().ToArray();
            Obj_AI_Base entityToWardJump = lstGameObjects.FirstOrDefault(obj =>
                obj.Position.Distance(wardJumpPosition) < 150
                && (obj is Obj_AI_Minion || obj is Obj_AI_Hero)
                && !obj.IsMe && !obj.IsDead
                && obj.Position.Distance(Player.Position) < Q.Range);

            if (entityToWardJump != null)
            {
                Q.Cast(entityToWardJump);
            }
            else
            {
                int wardId = GetWardItem();


                if (wardId != -1 && !wardJumpPosition.IsWall())
                {
                    PutWard(wardJumpPosition.To2D(), (ItemId)wardId);
                    lstGameObjects = ObjectManager.Get<Obj_AI_Base>().ToArray();
                    Q.Cast(
                        lstGameObjects.FirstOrDefault(obj =>
                        obj.Position.Distance(wardJumpPosition) < 150 &&
                        obj is Obj_AI_Minion && obj.Position.Distance(Player.Position) < Q.Range));
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
    }
}