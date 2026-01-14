using System.Collections.Generic;
using System.Linq;

namespace BlazorApp1.Domain
{
    // Kleiner Value-Type für Koordinaten
    public readonly record struct Coord(double X, double Y);

    /// <summary>
    /// Reiner Domain-State: fertige und aktuelles Polygon
    /// inkl. Undo/Redo. Keine UI-Details (wie MousePos).
    /// </summary>
    public sealed class PolygonDrawingState
    {
        private readonly List<List<Coord>> _finishedPolygons = new();
        private List<Coord>? _currentPolygon;

        private readonly Stack<PolygonSnapshot> _past = new();
        private readonly Stack<PolygonSnapshot> _future = new();

        /// <summary>
        /// Fertig abgeschlossene Polygone (nur lesbar nach außen).
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Coord>> FinishedPolygons => _finishedPolygons;

        /// <summary>
        /// Aktuelles, noch nicht abgeschlossenes Polygon (nur lesbar).
        /// </summary>
        public IReadOnlyList<Coord>? CurrentPolygon => _currentPolygon;

        /// <summary>
        /// Fügt einen Punkt zum aktuellen Polygon hinzu
        /// und legt einen Undo-Snapshot an.
        /// </summary>
        public void AddPoint(Coord coord)
        {
            SaveForUndo();

            _currentPolygon ??= new List<Coord>();
            _currentPolygon.Add(coord);
        }

        /// <summary>
        /// Schließt das aktuelle Polygon ab (falls vorhanden)
        /// und verschiebt es in die Liste der fertigen Polygone.
        /// </summary>
        public void FinishPolygon()
        {
            if (_currentPolygon is null || _currentPolygon.Count == 0)
            {
                return;
            }

            SaveForUndo();

            // Kopie erzeugen, damit spätere Änderungen
            // die gespeicherten Polygone nicht verändern.
            _finishedPolygons.Insert(0, new List<Coord>(_currentPolygon));
            _currentPolygon = null;
        }

        /// <summary>
        /// Macht den letzten Zustand rückgängig.
        /// </summary>
        public void Undo()
        {
            if (_past.Count == 0)
            {
                return;
            }

            _future.Push(CreateSnapshot());
            var previous = _past.Pop();
            RestoreSnapshot(previous);
        }

        /// <summary>
        /// Stellt einen rückgängig gemachten Zustand wieder her.
        /// </summary>
        public void Redo()
        {
            if (_future.Count == 0)
            {
                return;
            }

            _past.Push(CreateSnapshot());
            var next = _future.Pop();
            RestoreSnapshot(next);
        }

        /// <summary>
        /// Löscht alle Polygone und den aktuellen Zustand.
        /// Optional, aber oft praktisch.
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

        // ===================== intern: Snapshot-Logik =====================

        /// <summary>
        /// Interner Snapshot-Typ (Memento), nicht von außen sichtbar.
        /// Enthält tiefe Kopien der Listen.
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

        private PolygonSnapshot CreateSnapshot()
        {
            // tiefe Kopie der fertigen Polygone
            var finishedCopy = _finishedPolygons
                .Select(poly => (IReadOnlyList<Coord>)poly.ToList())
                .ToList();

            // Kopie des aktuellen Polygons (falls vorhanden)
            IReadOnlyList<Coord>? currentCopy = _currentPolygon is null
                ? null
                : _currentPolygon.ToList();

            return new PolygonSnapshot(finishedCopy, currentCopy);
        }

        private void RestoreSnapshot(PolygonSnapshot snapshot)
        {
            _finishedPolygons.Clear();

            foreach (var poly in snapshot.FinishedPolygons)
            {
                // Jede Liste wieder in eine neue, veränderbare Liste kopieren
                _finishedPolygons.Add(poly.ToList());
            }

            _currentPolygon = snapshot.CurrentPolygon is null
                ? null
                : snapshot.CurrentPolygon.ToList();

            // _future wird bewusst nicht angefasst:
            // Undo/Redo-Logik regelt das auf höherer Ebene.
        }

        private void SaveForUndo()
        {
            _past.Push(CreateSnapshot());
            _future.Clear();
        }
    }
}
