using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace e.Motion_Gangplank
{
    class DelayManager
    {
        private Spell _spellToUse;
        private bool _blocked;
        private int _expireTime;
        private int _lastuse;
        private Obj_AI_Base target;

        public void UnblockDelay()
        {
            _blocked = false;
            _lastuse = Utils.TickCount;
        }

        public DelayManager(Spell spell, int expireTicks)
        {
            _spellToUse = spell;
            _expireTime = expireTicks;
        }

        public void Delay(Obj_AI_Base enemy, bool blocked = false)
        {
            _lastuse = Utils.TickCount;
            target = enemy;
            _blocked = blocked;
        }

        public bool Active()
        {
            return (target != null && _lastuse + _expireTime >= Utils.TickCount);
        }

        public void CheckEachTick()
        {
            if (!_blocked && target != null 
                && _lastuse + _expireTime >= Utils.TickCount 
                && _spellToUse.IsReady() 
                && _spellToUse.Range >= Program.Player.Distance(target))
            {
                _spellToUse.Cast(target);
                target = null;
                //Game.PrintChat("Casted with DelayManager(TM)");
            }
        }
    }
}
