import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GameService, GameState } from './game.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private readonly gameApi = inject(GameService);

  readonly boardSizes = [4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14];
  selectedSize = 8;
  game = signal<GameState | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  readonly statusText = computed(() => {
    const g = this.game();
    if (!g) {
      return 'Start a game to place queens. You cannot place on attacked squares.';
    }
    if (g.solved) {
      return `Solved! You placed ${g.n} non-attacking queens.`;
    }
    return `${g.queensPlaced} / ${g.n} queens placed.`;
  });

  readonly solutionHint = computed(() => {
    const g = this.game();
    if (!g?.solutionCount) {
      return null;
    }
    return `There are ${g.solutionCount.toLocaleString()} distinct solutions for n = ${g.n}.`;
  });

  startGame(): void {
    this.error.set(null);
    this.loading.set(true);
    this.gameApi.startGame(this.selectedSize).subscribe({
      next: (state) => {
        this.game.set(state);
        this.loading.set(false);
      },
      error: (e: Error) => {
        this.error.set(e.message);
        this.loading.set(false);
      }
    });
  }

  onCellClick(row: number, col: number): void {
    const g = this.game();
    if (!g || this.loading()) {
      return;
    }
    this.error.set(null);
    this.loading.set(true);
    this.gameApi.toggleCell(g.id, row, col).subscribe({
      next: (state) => {
        this.game.set(state);
        this.loading.set(false);
      },
      error: (e: Error) => {
        this.error.set(e.message);
        this.loading.set(false);
      }
    });
  }

  cellLabel(row: number, col: number): string {
    const g = this.game();
    if (!g) {
      return '';
    }
    if (g.grid[row][col] === 1) {
      return '♛';
    }
    return '';
  }

  isQueen(row: number, col: number): boolean {
    const g = this.game();
    return !!g && g.grid[row][col] === 1;
  }
}
