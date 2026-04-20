namespace NQueens.Api.Services;

/// <summary>Counts distinct solutions to the n-queens problem (n ≤ 14 is practical).</summary>
public static class NQueensSolver
{
    public static int CountSolutions(int n)
    {
        if (n is < 1 or > 14)
            throw new ArgumentOutOfRangeException(nameof(n), "N must be between 1 and 14.");

        return Solve(0, 0, 0, n);
    }

    static int Solve(int rowMask, int diag1Mask, int diag2Mask, int n)
    {
        if (rowMask == (1 << n) - 1)
            return 1;

        var count = 0;
        var available = ((1 << n) - 1) & ~(rowMask | diag1Mask | diag2Mask);
        while (available != 0)
        {
            var bit = available & -available;
            available -= bit;
            count += Solve(rowMask | bit, (diag1Mask | bit) << 1, (diag2Mask | bit) >> 1, n);
        }

        return count;
    }
}
