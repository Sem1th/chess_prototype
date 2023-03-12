using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Checkers
{
    public class ClickHandler : MonoBehaviour
    {
        [SerializeField] private PlayableSide _playableSide;
        [SerializeField] private CameraMover _cameraMover;

        [SerializeField] private Material _chipHighlightMaterial;

        [SerializeField] private CellComponent[,] _cells;
        [SerializeField] private PathCreator _pathCreator;
        [SerializeField] private List<CellComponent> _pairs;
        private Vector3 _previousPosition;

        private bool _isReadyToMove;
        private BaseClickComponent _cachedCell;

        public List<ChipComponent> Chips { get; private set; }

        public event Action ObjectsMoved;

        public event Action<ColorType> GameEnded;

        public event Action<BaseClickComponent> ChipDestroyed;

        public void Init(CellComponent[,] cells, List<ChipComponent> chipComponents)
        {
            Chips = chipComponents;
            _pathCreator = new PathCreator(cells, _playableSide);

            _cells = cells;

            foreach (var cell in cells)
            {
                cell.Clicked += OnCellClicked;
            }
        }

        private void OnDisable()
        {
            foreach (var cell in _cells)
            {
                cell.Clicked -= OnCellClicked;
            }
        }

        private void OnCellClicked(BaseClickComponent cell)
        {
            if (_isReadyToMove)
            {
                StartCoroutine(Move(cell));
            }

            ClearDesk();

            if (cell.Pair == null)
            {
                return;
            }

            if (_playableSide.CurrentSide != cell.Pair.Color)
            {
                Debug.LogError("Ходит другой игрок!!!");
                return;
            }

            HighlightBaseObjects(cell);
        }

        private IEnumerator Move(BaseClickComponent cell)
        {
            if (!_pairs.Contains(cell))
            {
                yield break;
            }

            var eventSystem = EventSystem.current;
            eventSystem.gameObject.SetActive(false);

            yield return StartCoroutine(_cachedCell.Pair.Move(cell));
            _previousPosition = cell.transform.position;

            var destroyCandidate = _pathCreator.DestroyCandidate;
            if (destroyCandidate.Count != 0)
            {
                DestroyCandidateChip(destroyCandidate);
            }

            switch (_playableSide.CurrentSide)
            {
                case ColorType.Black when cell.Coordinate.Y == 7:
                    GameEnded?.Invoke(ColorType.Black);
                    yield break;

                case ColorType.White when cell.Coordinate.Y == 0:
                    GameEnded?.Invoke(ColorType.White);
                    yield break;
            }

            cell.Pair = _cachedCell.Pair;
            _cachedCell.Pair = null;

            yield return StartCoroutine(_cameraMover.Move());

            eventSystem.gameObject.SetActive(true);
            ObjectsMoved?.Invoke();
        }

        private void DestroyCandidateChip(List<BaseClickComponent> destroyCandidate)
        {
            foreach (var chip in destroyCandidate.Where(chip =>
                         Vector3.Distance(chip.transform.position, _previousPosition) < 1.5f))
            {
                ChipDestroyed?.Invoke(chip);
                Destroy(chip.gameObject);
                return;
            }
        }

        private void HighlightBaseObjects(BaseClickComponent cell)
        {
            cell.Pair.SetMaterial(_chipHighlightMaterial);

            _pairs = _pathCreator.FindFreeCells(cell);
            _pairs = _pairs.Where(pair => pair != null).ToList();

            if (_pairs != null)
            {
                _isReadyToMove = true;
                _cachedCell = cell;
            }

            foreach (var pair in _pairs)
            {
                pair.IsFreeCellToMove = true;
                pair.HighLightFreeCellToMove();
            }
        }

        private void ClearDesk()
        {
            _isReadyToMove = false;

            if (_pairs != null)
            {
                foreach (var pair in _pairs)
                {
                    pair.IsFreeCellToMove = false;
                }
            }

            foreach (var cell in _cells)
            {
                cell.SetMaterial();

                if (cell.Pair == null)
                {
                    continue;
                }

                cell.Pair.SetMaterial();
            }
        }
    }
}