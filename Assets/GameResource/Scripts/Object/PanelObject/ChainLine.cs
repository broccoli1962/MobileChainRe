using System.Collections.Generic;
using Backend.Util;
using UnityEngine;

namespace Backend.Object.PanelObject
{
    public class ChainLine : CachedMonobehaviour
    {
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private float _lineWidth = 0.1f;
        [SerializeField] private Color _previewColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color _breakColor = Color.white;
        [SerializeField] private int _sortingOrder = 10;

        private readonly List<LineRenderer> _segments = new List<LineRenderer>();
        private int _usedCount;

        public void ShowPreview(IReadOnlyList<IReadOnlyList<(Panel from, Panel to)>> edgesByLayer)
        {
            Clear();
            for (int i = 0; i < edgesByLayer.Count; i++)
            {
                var layer = edgesByLayer[i];
                for (int j = 0; j < layer.Count; j++)
                    AddSegment(layer[j].from, layer[j].to, _previewColor);
            }
        }

        public void ShowLayer(IReadOnlyList<(Panel from, Panel to)> layerEdges)
        {
            for (int i = 0; i < layerEdges.Count; i++)
                AddSegment(layerEdges[i].from, layerEdges[i].to, _breakColor);
        }

        public void Hide()
        {
            Clear();
        }

        private void AddSegment(Panel from, Panel to, Color color)
        {
            LineRenderer lr;
            if (_usedCount < _segments.Count)
            {
                lr = _segments[_usedCount];
                lr.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject($"ChainSeg_{_segments.Count}");
                go.transform.SetParent(CachedTransform, false);
                lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.numCapVertices = 2;
                lr.numCornerVertices = 0;
                lr.sortingOrder = _sortingOrder;
                lr.alignment = LineAlignment.View;
                if (_lineMaterial != null)
                    lr.material = _lineMaterial;
                _segments.Add(lr);
            }

            lr.startWidth = _lineWidth;
            lr.endWidth = _lineWidth;
            lr.startColor = color;
            lr.endColor = color;
            lr.positionCount = 2;
            lr.SetPosition(0, from.SpriteBoundsCenter);
            lr.SetPosition(1, to.SpriteBoundsCenter);
            _usedCount++;
        }

        private void Clear()
        {
            for (int i = 0; i < _usedCount; i++)
                _segments[i].gameObject.SetActive(false);
            _usedCount = 0;
        }
    }
}
