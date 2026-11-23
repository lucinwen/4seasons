using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiXu.CocoonMatch
{
    public enum CocoonGameState { Playing, Won, Lost }

    public sealed class CocoonMove
    {
        public int tileId;
        public int[] trayBeforeMatch;
        public int[] matched;
        public int[] trayAfterMatch;
    }

    // Rules have no scene references, animation state, input calls, or save data.
    public sealed class CocoonMatchModel
    {
        public const int Capacity = 7;
        private readonly CocoonMatchLevel level;
        private readonly bool[] onBoard;
        private readonly List<int>[] blockers;
        private readonly List<int> tray = new List<int>();
        public CocoonGameState State { get; private set; }
        public int BoardCount { get; private set; }
        public int ClearedCount { get; private set; }
        public IReadOnlyList<int> Tray => tray.AsReadOnly();

        public CocoonMatchModel(CocoonMatchLevel data)
        {
            level = data ?? throw new ArgumentNullException(nameof(data));
            onBoard = Enumerable.Repeat(true, data.placements.Count).ToArray();
            blockers = new List<int>[onBoard.Length];
            BoardCount = onBoard.Length;
            for (int i = 0; i < onBoard.Length; i++)
            {
                blockers[i] = new List<int>();
                for (int j = 0; j < onBoard.Length; j++)
                    if (data.placements[j].layer > data.placements[i].layer && Overlaps(data, i, j))
                        blockers[i].Add(j);
            }
        }

        public static bool Overlaps(CocoonMatchLevel data, int a, int b)
        {
            var offset = data.placements[a].position - data.placements[b].position;
            return Mathf.Abs(offset.x) < data.tileSize - .5f && Mathf.Abs(offset.y) < data.tileSize - .5f;
        }

        public bool IsOnBoard(int id) => id >= 0 && id < onBoard.Length && onBoard[id];
        public bool IsExposed(int id) => IsOnBoard(id) && !blockers[id].Any(other => onBoard[other]);
        public bool CanCollect(int id) => State == CocoonGameState.Playing && tray.Count < Capacity && IsExposed(id);

        public bool TryCollect(int id, out CocoonMove move)
        {
            move = null;
            if (!CanCollect(id)) return false;
            int pattern = level.placements[id].pattern;
            int lastSame = tray.FindLastIndex(other => level.placements[other].pattern == pattern);
            int insertion = lastSame >= 0 ? lastSame + 1 : tray.Count;
            onBoard[id] = false;
            BoardCount--;
            tray.Insert(insertion, id);
            var before = tray.ToArray();
            var matches = tray.Where(other => level.placements[other].pattern == pattern).Take(3).ToArray();
            if (matches.Length == 3)
            {
                foreach (int match in matches) tray.Remove(match);
                ClearedCount += 3;
            }
            else matches = Array.Empty<int>();

            // Resolve a triple BEFORE evaluating a full tray, including the seventh tile.
            if (BoardCount == 0 && tray.Count == 0) State = CocoonGameState.Won;
            else if (tray.Count >= Capacity) State = CocoonGameState.Lost;
            move = new CocoonMove { tileId = id, trayBeforeMatch = before, matched = matches, trayAfterMatch = tray.ToArray() };
            return true;
        }
    }
}
