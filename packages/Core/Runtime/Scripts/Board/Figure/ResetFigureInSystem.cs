using Leopotam.EcsProto;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ResetFigureInSystem : IProtoInitSystem, IProtoDestroySystem
{
    private readonly InputSchemeOnBoard _inputScheme = null;

    public ResetFigureInSystem(InputSchemeOnBoard inputScheme)
    {
        _inputScheme = inputScheme;
    }
    public void Init(IProtoSystems systems)
    {

    }
    public void Destroy()
    {
    
    }
}
