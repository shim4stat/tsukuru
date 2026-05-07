using System;
using NumericsVector3 = System.Numerics.Vector3;
using UnityEngine;

namespace Game.Presentation.Game.Boss.Runtime
{
    internal interface IBossBattleEntityView : IDisposable
    {
        float HitboxRadius { get; }

        void SetPosition(NumericsVector3 position);

        void SetVisible(bool visible);

        void SetHitboxVisible(bool visible);
    }

    internal interface IBossBattleTintableView : IBossBattleEntityView
    {
        void SetBodyColor(Color color);
    }

    internal interface IBossBattleAnimatableView : IBossBattleEntityView
    {
        void PlayAnimation(string stateName, float crossFadeSeconds);
    }
}
