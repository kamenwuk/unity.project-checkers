using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;
using System.Collections.Generic;
using Alchemy.Inspector;
using System;
using Alchemy.Serialization;

namespace Core.Board.Figure
{
    [System.Serializable]
    [AlchemySerialize]
    public partial class AspectByFigures : ProtoAspectInject
    {
        public Transform StorageLocation => _storageLocation;
        public IReadOnlyDictionary<DataByFigureOnBoard.Belongs, TemplateToFigure> Templates => _templates;
        
        public readonly ProtoPool<DataByFigureOnBoard> Pool = null;
        public readonly ProtoIt ItFigureLocatedOnCell = new(It.Inc<DataByFigureOnBoard>());

        [AlchemySerializeField, NonSerialized, ShowInInspector] private Dictionary<DataByFigureOnBoard.Belongs, TemplateToFigure> _templates = new();
        [SerializeField] private Transform _storageLocation = null;

        public override void Init(ProtoWorld world)
        {
            base.Init(world);
        }
    }
}