namespace Game.Contracts.Battle
{
    public interface IStageMapRepository
    {
        StageMapDto LoadStageMap(string filepath);
    }
}
