using LeagueSharp;
using SharpDX;



namespace e.Motion_Gangplank
{
    public class Waypoints
    {
        public Vector3 Position;
        public Obj_AI_Hero Enemy;
        public bool Winding;
        public Waypoints(Obj_AI_Hero hero)
        {
            Position = Vector3.Zero;
            Enemy = hero;
        }

        public void UpdatePositions(Vector3 pos)
        {
            Position = pos;
        }
    }
}