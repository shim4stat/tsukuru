namespace Game.Domain.Battle
{
    public class BattleEntityFactory : IBattleEntityFactory
    {
        public Player CreatePlayer()
        {
            return new Player();
        }

        public Robot CreateRobot()
        {
            return new Robot();
        }

        public Boss CreateBoss()
        {
            return new Boss();
        }

        public Enemy CreateEnemy()
        {
            return new Enemy();
        }
    }
}
