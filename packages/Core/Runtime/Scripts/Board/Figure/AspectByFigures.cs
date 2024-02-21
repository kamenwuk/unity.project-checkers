using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;

namespace Core.Board.Figure
{
    [System.Serializable]
    public sealed class AspectByFigures : ProtoAspectInject
    {
        public Transform StorageLocation => _storageLocation;
        public Sprite SpriteFigureBelongingToWhite => _spriteFigureBelongingToWhite;
        public Sprite SpriteFigureBelongingToBlack => _spriteFigureBelongingToBlack;

        public readonly ProtoPool<DataByFigureOnBoard> Pool = null;
        public readonly ProtoIt ItFigureLocatedOnCell = new(It.Inc<DataByFigureOnBoard>());

        [SerializeField] private Sprite _spriteFigureBelongingToWhite = null;
        [SerializeField] private Sprite _spriteFigureBelongingToBlack = null;
        [SerializeField] private Transform _storageLocation = null;
    }
}