using System.Collections.Generic;

namespace Game.Domain
{
    public class BattleContext
    {
        public BattlePhase Phase { get; private set; } = BattlePhase.BattleStart;
        public Player Player { get; private set; }
        public Robot Robot { get; private set; }
        public Boss Boss { get; private set; }
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
        public List<RobotBullet> RobotBullets { get; private set; } = new List<RobotBullet>();
        public List<EnemyBullet> EnemyBullets { get; private set; } = new List<EnemyBullet>();
        public List<ItemInstance> Items { get; private set; } = new List<ItemInstance>();

        public void SetPhase(BattlePhase phase)
        {
            Phase = phase;
        }

        public BattlePhase GetPhase()
        {
            return Phase;
        }

        public void Setup(StageId stageId, IBattleEntityFactory factory)
        {
            Phase = BattlePhase.BattleStart;
            Player = factory.CreatePlayer();
            Robot = factory.CreateRobot();
            Boss = factory.CreateBoss();
            Enemies = new List<Enemy>();
            RobotBullets = new List<RobotBullet>();
            EnemyBullets = new List<EnemyBullet>();
            Items = new List<ItemInstance>();
        }

        public void ResetForRetry()
        {
            // Requirement: Reset internal state and restart from BossBoot
            Phase = BattlePhase.BossBoot;
            
            Enemies.Clear();
            RobotBullets.Clear();
            EnemyBullets.Clear();
            Items.Clear();
            
            // TODO: Reset Player and Boss state
        }
    }
}
