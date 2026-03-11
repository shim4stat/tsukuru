namespace Game.Domain.Battle
{
    public interface IBattleEntityFactory
    {
        Player CreatePlayer(PlayerStaticParams staticParams, BattleContext battleContext);
        Robot CreateRobot(StageId stageId);
        Boss CreateBoss();
        Enemy CreateEnemy();
    }
}
