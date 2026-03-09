using System;
using NumericsVector3 = System.Numerics.Vector3;
using UnityEngine;

namespace Game.Presentation.TestBoss
{
    internal interface ITestBossEntityView : IDisposable
    {
        float HitboxRadius { get; }

        void SetPosition(NumericsVector3 position);

        void SetVisible(bool visible);

        void SetHitboxVisible(bool visible);
    }

    internal interface ITestBossTintableView : ITestBossEntityView
    {
        void SetBodyColor(Color color);
    }
}
