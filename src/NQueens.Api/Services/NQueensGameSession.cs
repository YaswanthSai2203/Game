using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using NQueens.Api.Models;

namespace NQueens.Api.Services;

public sealed class NQueensGameSession
{
    readonly int[,] _grid;
    int _queenCount;

    public Guid Id { get; }
    public int N { get; }
    public int? SolutionCount { get; }

    public NQueensGameSession(Guid id, int n)
    {
        Id = id;
        N = n;
        _grid = new int[n, n];
        SolutionCount = n is >= 1 and <= 14 ? NQueensSolver.CountSolutions(n) : null;
    }

    public GameStateResponse Toggle(int row, int col)
    {
        if ((uint)row >= (uint)N || (uint)col >= (uint)N)
            throw new ArgumentOutOfRangeException(nameof(row), "Row and column must be inside the board.");

        if (_grid[row, col] == 1)
        {
            _grid[row, col] = 0;
            _queenCount--;
            return GetState();
        }

        if (IsUnderAttack(row, col))
            throw new InvalidOperationException("That square is attacked by another queen.");

        _grid[row, col] = 1;
        _queenCount++;
        return GetState();
    }

    bool IsUnderAttack(int row, int col)
    {
        for (var r = 0; r < N; r++)
        {
            for (var c = 0; c < N; c++)
            {
                if (_grid[r, c] == 0)
                    continue;
                if (r == row && c == col)
                    continue;
                if (r == row || c == col || Math.Abs(r - row) == Math.Abs(c - col))
                    return true;
            }
        }

        return false;
    }

    int[][] SnapshotGrid()
    {
        var copy = new int[N][];
        for (var r = 0; r < N; r++)
        {
            copy[r] = new int[N];
            for (var c = 0; c < N; c++)
                copy[r][c] = _grid[r, c];
        }

        return copy;
    }

    public GameStateResponse GetState() =>
        new(Id, N, SnapshotGrid(), _queenCount, _queenCount == N, SolutionCount);
}

public sealed class GameSessionStore
{
    readonly ConcurrentDictionary<Guid, NQueensGameSession> _sessions = new();

    public NQueensGameSession Start(int n)
    {
        if (n is < 4 or > 14)
            throw new ArgumentOutOfRangeException(nameof(n), "N must be between 4 and 14.");

        var id = Guid.NewGuid();
        var session = new NQueensGameSession(id, n);
        _sessions[id] = session;
        return session;
    }

    public bool TryGet(Guid id, [NotNullWhen(true)] out NQueensGameSession? session) =>
        _sessions.TryGetValue(id, out session);

    public void Remove(Guid id) => _sessions.TryRemove(id, out _);
}
