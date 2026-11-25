using System;
using System.Collections.Generic;

/// <summary>芒种簇架连线规则。无计时、无步数上限，少步目标只作自愿挑战。</summary>
public sealed class XiazhiPuzzle
{
    public const int North = 1, East = 2, South = 4, West = 8, CellCount = 16, LevelCount = 3;
    private static readonly int[] Sides = { North, East, South, West };
    private static readonly int[] X = { 0, 1, 0, -1 }, Y = { -1, 0, 1, 0 };
    private sealed class Board
    {
        internal string Title;
        internal int Start, End, Target;
        internal int[] Masks;
    }
    private struct Move { internal int Index, Mask; }
    private static readonly Board[] Boards =
    {
        Build("初试 · 接起第一缕丝", new[,] { {0,1},{1,1},{1,2},{2,2},{2,3},{3,3} },
            new[] {0,3,2,3,3,0}, new int[0,3], 6),
        Build("熟手 · 绕过木结", new[,] { {0,0},{1,0},{1,1},{0,1},{0,2},{1,2},{2,2},{2,1},{3,1},{3,2} },
            new[] {0,1,3,1,2,1,3,2,1,0}, new[,] {{2,0,North|East},{2,3,North|South},{1,3,East|South}}, 18),
        Build("巧匠 · 一线回环", new[,] { {0,2},{0,3},{1,3},{1,2},{1,1},{0,1},{0,0},{1,0},{2,0},{2,1},{2,2},{3,2} },
            new[] {0,1,2,3,1,2,1,1,3,1,2,0}, new[,] {{3,0,North|South},{3,1,North|East},{2,3,East|West}}, 22)
    };
    private readonly int[] masks = new int[CellCount];
    private readonly bool[] connected = new bool[CellCount];
    private readonly List<Move> history = new List<Move>();
    public int LevelIndex { get; private set; }
    public int Moves { get; private set; }
    public int Hints { get; private set; }
    public bool Solved { get; private set; }
    public int ConnectedCount { get; private set; }
    public bool CanUndo => history.Count > 0;
    public int Start => Boards[LevelIndex].Start;
    public int End => Boards[LevelIndex].End;
    public int Target => Boards[LevelIndex].Target;
    public string Title => Boards[LevelIndex].Title;
    public XiazhiPuzzle(int level = 0) { Load(level); }
    public int Mask(int index) => masks[index];
    public bool IsConnected(int index) => connected[index];
    public bool IsLocked(int index) => index == Start || index == End;
    public void Load(int level)
    {
        if (level < 0 || level >= LevelCount) throw new ArgumentOutOfRangeException(nameof(level));
        LevelIndex = level;
        Array.Copy(Boards[level].Masks, masks, CellCount);
        Moves = Hints = 0;
        history.Clear();
        Trace();
    }
    public static int RotateMask(int mask, int turns = 1)
    {
        for (int i = 0; i < (turns % 4 + 4) % 4; i++) mask = ((mask << 1) & 15) | (mask >> 3);
        return mask;
    }
    public bool Rotate(int index)
    {
        if (Solved || index < 0 || index >= CellCount || IsLocked(index) || masks[index] == 0) return false;
        history.Add(new Move { Index = index, Mask = masks[index] });
        masks[index] = RotateMask(masks[index]);
        Moves++;
        Trace();
        return true;
    }
    public bool Undo()
    {
        if (!CanUndo) return false;
        Move move = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);
        masks[move.Index] = move.Mask;
        Moves--;
        Trace();
        return true;
    }
    public bool Hint(out int index, out int turns)
    {
        index = -1; turns = 0;
        if (Solved) return false;
        int[] answer = Solve();
        if (answer == null) return false;
        // Prefer the first incorrect segment reached from the entrance.
        int cursor = Start, incoming = West, seen = 0;
        while ((seen & (1 << cursor)) == 0)
        {
            seen |= 1 << cursor;
            if (answer[cursor] > 0)
            {
                index = cursor; turns = answer[cursor]; Hints++; return true;
            }
            int outgoing = masks[cursor] & ~incoming;
            int side = Array.IndexOf(Sides, outgoing);
            if (side < 0 || cursor == End) break;
            int col = cursor % 4 + X[side], row = cursor / 4 + Y[side];
            if (col < 0 || col > 3 || row < 0 || row > 3) break;
            cursor = row * 4 + col; incoming = Sides[(side + 2) % 4];
        }
        return false;
    }
    public int[] Solve()
    {
        int bestCost = int.MaxValue;
        int[] best = null, candidate = new int[CellCount];
        Search(Start, West, 0, 0, candidate, ref bestCost, ref best);
        return best;
    }
    private void Search(int index, int incoming, int seen, int cost, int[] candidate, ref int bestCost, ref int[] best)
    {
        if ((seen & (1 << index)) != 0 || cost >= bestCost || masks[index] == 0) return;
        seen |= 1 << index;
        int mask = masks[index], tried = 0;
        for (int turns = 0; turns < (IsLocked(index) ? 1 : 4); turns++, mask = RotateMask(mask))
        {
            if ((tried & (1 << mask)) != 0) continue;
            tried |= 1 << mask;
            if ((mask & incoming) == 0) continue;
            int outgoing = mask & ~incoming, side = Array.IndexOf(Sides, outgoing);
            if (side < 0) continue;
            candidate[index] = turns;
            if (index == End && outgoing == East)
            {
                if (cost + turns < bestCost) { bestCost = cost + turns; best = (int[])candidate.Clone(); }
            }
            else
            {
                int col = index % 4 + X[side], row = index / 4 + Y[side];
                if (col >= 0 && col < 4 && row >= 0 && row < 4)
                    Search(row * 4 + col, Sides[(side + 2) % 4], seen, cost + turns, candidate, ref bestCost, ref best);
            }
            candidate[index] = 0;
        }
    }
    private void Trace()
    {
        Array.Clear(connected, 0, CellCount);
        ConnectedCount = 0; Solved = false;
        int index = Start, incoming = West, seen = 0;
        while ((seen & (1 << index)) == 0)
        {
            seen |= 1 << index;
            if ((masks[index] & incoming) == 0) return;
            connected[index] = true; ConnectedCount++;
            int outgoing = masks[index] & ~incoming, side = Array.IndexOf(Sides, outgoing);
            if (side < 0) return;
            if (index == End && outgoing == East) { Solved = true; return; }
            int col = index % 4 + X[side], row = index / 4 + Y[side];
            if (col < 0 || col > 3 || row < 0 || row > 3) return;
            index = row * 4 + col; incoming = Sides[(side + 2) % 4];
        }
    }
    private static Board Build(string title, int[,] path, int[] turns, int[,] decoys, int target)
    {
        int count = path.GetLength(0);
        var board = new Board { Title = title, Target = target, Masks = new int[CellCount],
            Start = path[0,1] * 4 + path[0,0], End = path[count-1,1] * 4 + path[count-1,0] };
        for (int i = 0; i < count; i++)
        {
            int incoming = i == 0 ? West : Direction(path[i-1,0] - path[i,0], path[i-1,1] - path[i,1]);
            int outgoing = i == count - 1 ? East : Direction(path[i+1,0] - path[i,0], path[i+1,1] - path[i,1]);
            board.Masks[path[i,1] * 4 + path[i,0]] = RotateMask(incoming | outgoing, turns[i]);
        }
        for (int i = 0; i < decoys.GetLength(0); i++) board.Masks[decoys[i,1] * 4 + decoys[i,0]] = decoys[i,2];
        return board;
    }
    private static int Direction(int x, int y) => x == 1 ? East : x == -1 ? West : y == 1 ? South : North;
}
