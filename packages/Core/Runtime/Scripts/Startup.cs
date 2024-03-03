using Leopotam.EcsProto.Unity;
using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;
using Core.Board;
using Core.Board.Figure;
using UnityEditor;

namespace Core
{
    public sealed class Startup : MonoBehaviour
    {
        [SerializeField] private AspectByWorld _aspectByWorld = null;

        private InputSchemeOnBoard _inputSchemeOnBoard = null;
        private ProtoSystems _systems = null;
        private ProtoWorld _world = null;

        private void Awake()
        {
            _inputSchemeOnBoard = new();
        }
        private void Start()
        {
            _world = new(_aspectByWorld);
            _systems = new(_world);
            _systems
                .AddModule(new AutoInjectModule())
                .AddModule(new UnityModule())
                .AddSystem(new InitBoardInSystem())
                .AddSystem(new DrawBoardInSystem())
                .AddSystem(new SelectFigureInSystem(_inputSchemeOnBoard))
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