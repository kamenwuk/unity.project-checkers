using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;
using Core.Board;

namespace Core
{
    [System.Serializable]
    public sealed class AspectByWorld : IProtoAspect
    {
        public AspectByBoard Board => _board;
        [SerializeField] private AspectByBoard _board = null;

        public void Init(ProtoWorld world)
        {
            world.AddAspect(this);
            _board.Init(world);
        }
        public void PostInit() 
        {
            _board.PostInit();
        }
    }
}