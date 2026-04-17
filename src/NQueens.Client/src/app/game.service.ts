import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface GameState {
  id: string;
  n: number;
  grid: number[][];
  queensPlaced: number;
  solved: boolean;
  solutionCount: number | null;
}

export interface ApiError {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly http = inject(HttpClient);

  startGame(n: number): Observable<GameState> {
    return this.http.post<GameState>('/api/games', { n }).pipe(catchError(this.mapError));
  }

  toggleCell(id: string, row: number, col: number): Observable<GameState> {
    return this.http.post<GameState>(`/api/games/${id}/toggle`, { row, col }).pipe(catchError(this.mapError));
  }

  private mapError(err: HttpErrorResponse): Observable<never> {
    const body = err.error as ApiError | undefined;
    const message = body?.message ?? err.message ?? 'Request failed';
    return throwError(() => new Error(message));
  }
}
