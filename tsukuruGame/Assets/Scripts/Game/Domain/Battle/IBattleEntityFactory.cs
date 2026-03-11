namespace Game.Domain.Battle
{
    public interface IBattleEntityFactory
    {
        Player CreatePlayer(PlayerStaticParams staticParams);
        Robot CreateRobot(StageId stageId);
        Boss CreateBoss();
        Enemy CreateEnemy();
    }
}
