namespace Game.Domain.Battle
{
    public interface IBattleEntityFactory
    {
        Player CreatePlayer();
        Robot CreateRobot();
        Boss CreateBoss();
        Enemy CreateEnemy();
    }
}
