using Leopotam.EcsProto.Unity;
using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;
using Core.Board;
using Alchemy.Serialization;
using System.Collections.Generic;
using System;

namespace Core
{
    public sealed class Startup : MonoBehaviour
    {
        [SerializeField] private AspectByWorld _aspectByWorld = null;

        private ProtoSystems _systems = null;
        private ProtoWorld _world = null;

        private void Start()
        {
            _world = new(_aspectByWorld);
            _systems = new(_world);
            _systems
                .AddModule(new AutoInjectModule())
                .AddModule(new UnityModule())
                .AddSystem(new InitBoardToSystem())
                .AddSystem(new DrawBoardToSystem())
                .Init();
        }
        private void Update()
        {
            _systems.Run();
        }
        private void OnDestroy()
        {
            if(_systems != null)
            {
                _systems.Destroy();
                _systems = null;
            }
            if(_world != null)
            {
                _world.Destroy();
                _world = null;
            }
        }
    }
}