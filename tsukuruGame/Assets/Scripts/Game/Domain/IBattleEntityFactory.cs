namespace Game.Domain
{
    public interface IBattleEntityFactory
    {
        Player CreatePlayer();
        Robot CreateRobot();
        Boss CreateBoss();
        Enemy CreateEnemy();
    }
}
