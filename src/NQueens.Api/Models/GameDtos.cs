namespace NQueens.Api.Models;

public sealed record StartGameRequest(int N);

public sealed record ToggleCellRequest(int Row, int Col);

public sealed record GameStateResponse(Guid Id, int N, int[][] Grid, int QueensPlaced, bool Solved, int? SolutionCount);

public sealed record ErrorResponse(string Message);
