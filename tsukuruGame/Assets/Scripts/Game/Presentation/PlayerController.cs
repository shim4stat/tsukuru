using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Game.Domain.Battle.Player _domainPlayer; // Domain層のPlayerへの参照

    // 初期化時にDomainのPlayerを受け取る
    public void Initialize(Game.Domain.Battle.Player player)
    {
        _domainPlayer = player;
    }

    void Update()
    {
        if (_domainPlayer != null)
        {
            transform.position = new Vector3(_domainPlayer.Position.X, _domainPlayer.Position.Y, 0);
        }
    }
}
