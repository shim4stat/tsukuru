namespace Game.Domain.Battle
{
    public class Robot
    {
        public StageMap StageMap { get; }

        public Robot(StageMap stageMap)
        {
            StageMap = stageMap ?? throw new System.ArgumentNullException(nameof(stageMap));
        }
    }
}
