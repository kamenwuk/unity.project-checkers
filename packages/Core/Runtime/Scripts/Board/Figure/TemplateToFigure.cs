using UnityEngine;

namespace Core.Board.Figure
{
    [CreateAssetMenu(fileName = "TemplateToFigure", menuName = "Tools/Template/Figure")]
    public sealed class TemplateToFigure : ScriptableObject
    {
        public Sprite View => _view;
        [SerializeField] private Sprite _view;
    }
}