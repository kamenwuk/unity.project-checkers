using Leopotam.EcsProto.Unity;
using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;

namespace Core
{
    public sealed class Startup : MonoBehaviour
    {
        private AspectByWorld _aspectByWorld = null;
        private ProtoSystems _systems = null;
        private ProtoWorld _world = null;

        private void Start()
        {
            _aspectByWorld = new();
            _world = new(_aspectByWorld);
            _systems
                .AddModule(new AutoInjectModule())
                .AddModule(new UnityModule())
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