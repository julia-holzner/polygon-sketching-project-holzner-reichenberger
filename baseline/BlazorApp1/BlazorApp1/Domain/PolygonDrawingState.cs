using System.Collections.Generic;
using System.Linq;

namespace BlazorApp1.Domain
{
    // datatype for coordinates
    public readonly record struct Coord(double X, double Y);

    /// <summary>
    /// polygon state for ui, handels undo, redo, addpoint and finishpolygon
    /// provides finishedPolygons and currentpolygon
    /// </summary>
    public sealed class PolygonDrawingState
    {
        private readonly List<List<Coord>> _finishedPolygons = new();
        private List<Coord>? _currentPolygon;

        private readonly Stack<PolygonSnapshot> _past = new();
        private readonly Stack<PolygonSnapshot> _future = new();

        /// <summary>
        /// all finished polygons, api for ui 
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Coord>> FinishedPolygons => _finishedPolygons;

        /// <summary>
        /// currently drawing polygon, api for ui
        /// </summary>
        public IReadOnlyList<Coord>? CurrentPolygon => _currentPolygon;

        /// <summary>
        /// logic for adding a point to current polygon (for when a single click happens)
        /// </summary>
        public void AddPoint(Coord coord)
        {
            SaveForUndo();

            _currentPolygon ??= new List<Coord>(); // init if currentPolygon in empty
            _currentPolygon.Add(coord); // add coordinates to currentpolygon
        }

        /// <summary>
        /// logic for finishing a polygon (when a double click happens)
        /// </summary>
        public void FinishPolygon()
        {
            if (_currentPolygon is null || _currentPolygon.Count == 0)
            {
                return; // current polygon need to exist and have at least one point to finish
            }

            SaveForUndo();

            // add new polygon to finished polygons and reset current polygon
            _finishedPolygons.Insert(0, new List<Coord>(_currentPolygon));
            _currentPolygon = null;
        }

        /// <summary>
        /// goes back one step -> called when undo button is pressed
        /// </summary>
        public void Undo()
        {
            if (_past.Count == 0)
            {
                // checks if past exists
                return;
            }

            _future.Push(CreateSnapshot()); // pushes current state as snapshot on future for redo
            var previous = _past.Pop(); // get latest past and renders it
            RestoreSnapshot(previous);
        }

        /// <summary>
        /// goes one step further in the "future" -> called when redo button is pressed
        /// </summary>
        public void Redo()
        {
            if (_future.Count == 0)
            {
                return; // checks if furture exists
            }

            _past.Push(CreateSnapshot()); // pushes current state as snapshot on past for undo
            var next = _future.Pop(); // gets and renders latest furture snapshot
            RestoreSnapshot(next);
        }

        /// <summary>
        /// clears current values of state
        /// </summary>
        public void Clear()
        {
            if (_finishedPolygons.Count == 0 && _currentPolygon is null)
            {
                return;
            }

            SaveForUndo();
            _finishedPolygons.Clear();
            _currentPolygon = null;
        }

        /// <summary>
        /// internal neasted Snapshot class, holds a state at a specific time 
        /// </summary>
        private sealed class PolygonSnapshot
        {
            public IReadOnlyList<IReadOnlyList<Coord>> FinishedPolygons { get; }
            public IReadOnlyList<Coord>? CurrentPolygon { get; }

            public PolygonSnapshot(
                IReadOnlyList<IReadOnlyList<Coord>> finishedPolygons,
                IReadOnlyList<Coord>? currentPolygon)
            {
                FinishedPolygons = finishedPolygons;
                CurrentPolygon = currentPolygon;
            }
        }

        // creates a snapshot of the current state
        private PolygonSnapshot CreateSnapshot()
        {
            var finishedCopy = _finishedPolygons
                .Select(poly => (IReadOnlyList<Coord>)poly.ToList())
                .ToList();

            IReadOnlyList<Coord>? currentCopy = _currentPolygon is null
                ? null
                : _currentPolygon.ToList();

            return new PolygonSnapshot(finishedCopy, currentCopy);
        }

        // loads state from snapshot into current
        private void RestoreSnapshot(PolygonSnapshot snapshot)
        {
            _finishedPolygons.Clear();

            foreach (var poly in snapshot.FinishedPolygons)
            {
                _finishedPolygons.Add(poly.ToList());
            }

            _currentPolygon = snapshot.CurrentPolygon is null
                ? null
                : snapshot.CurrentPolygon.ToList();
        }

        // save current state whenever addpoint, finishedpolygons or clear change state -> for undo
        private void SaveForUndo()
        {
            _past.Push(CreateSnapshot());
            _future.Clear();
        }
    }
}
