using System.Collections.Generic;
using Alchemy.Serialization;
using Alchemy.Inspector;
using UnityEngine;

namespace Core.Board.Figure
{
    [CreateAssetMenu(fileName = "TemplateToFigure", menuName = "Tools/Template/Figure")]
    [System.Serializable]
    [AlchemySerialize]
    public partial class TemplateToFigure : ScriptableObject
    {
        public IReadOnlyDictionary<DataByFigureOnBoard.Types, Sprite> Views => _views;
        [AlchemySerializeField, System.NonSerialized, ShowInInspector] private Dictionary<DataByFigureOnBoard.Types, Sprite> _views = new();
    }
}