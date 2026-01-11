using System.Collections.Generic;
using System.Linq;

namespace BlazorApp1.Domain
{
    public record Coord(double X, double Y);

    public class PolygonSnapshot
    {
        public List<List<Coord>> FinishedPolygons { get; init; } = new();
        public List<Coord>? CurrentPolygon { get; init; }
    }

    public class PolygonDrawingState
    {
        public List<List<Coord>> FinishedPolygons { get; } = new();
        public List<Coord>? CurrentPolygon { get; private set; }
        public Coord? MousePos { get; private set; }

        private readonly Stack<PolygonSnapshot> _past = new();
        private readonly Stack<PolygonSnapshot> _future = new();

        private PolygonSnapshot CreateSnapshot()
        {
            return new PolygonSnapshot
            {
                FinishedPolygons = FinishedPolygons
                    .Select(poly => new List<Coord>(poly))
                    .ToList(),
                CurrentPolygon = CurrentPolygon != null
                    ? new List<Coord>(CurrentPolygon)
                    : null
            };
        }

        private void RestoreSnapshot(PolygonSnapshot snapshot)
        {
            FinishedPolygons.Clear();
            foreach (var poly in snapshot.FinishedPolygons)
            {
                FinishedPolygons.Add(new List<Coord>(poly));
            }

            CurrentPolygon = snapshot.CurrentPolygon != null
                ? new List<Coord>(snapshot.CurrentPolygon)
                : null;
        }

        private void SaveForUndo()
        {
            _past.Push(CreateSnapshot());
            _future.Clear();
        }

        public void SetCursorPos(Coord? coord)
        {
            MousePos = coord;
        }

        public void AddPoint(Coord coord)
        {
            SaveForUndo();

            if (CurrentPolygon == null)
                CurrentPolygon = new List<Coord>();

            CurrentPolygon.Add(coord);
        }

        public void FinishPolygon()
        {
            if (CurrentPolygon == null || CurrentPolygon.Count == 0)
                return;

            SaveForUndo();

            FinishedPolygons.Insert(0, new List<Coord>(CurrentPolygon));
            CurrentPolygon = null;
        }

        public void Undo()
        {
            if (_past.Count == 0)
            {
                MousePos = null;
                return;
            }

            MousePos = null;
            _future.Push(CreateSnapshot());

            var previous = _past.Pop();
            RestoreSnapshot(previous);
        }

        public void Redo()
        {
            if (_future.Count == 0)
            {
                MousePos = null;
                return;
            }

            MousePos = null;
            _past.Push(CreateSnapshot());

            var next = _future.Pop();
            RestoreSnapshot(next);
        }
    }
}
