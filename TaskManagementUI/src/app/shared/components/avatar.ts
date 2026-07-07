import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-avatar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="avatar-container" [style.width.px]="size" [style.height.px]="size" [style.font-size.px]="fontSize">
      @if (avatarUrl && !imgError) {
        <img [src]="avatarUrl" (error)="onImgError()" alt="User Avatar" class="avatar-img"/>
      } @else {
        <div class="avatar-initials" [style.background-color]="bgHex">
          {{ initials }}
        </div>
      }
    </div>
  `,
  styles: [`
    .avatar-container {
      border-radius: 50%;
      overflow: hidden;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      user-select: none;
      flex-shrink: 0;
    }
    .avatar-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
    .avatar-initials {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #ffffff;
      font-weight: 600;
      letter-spacing: 0.05em;
    }
  `]
})
export class Avatar implements OnChanges {
  @Input() name: string = '';
  @Input() avatarUrl: string | null = null;
  @Input() size: number = 32;

  protected initials: string = '';
  protected fontSize: number = 12;
  protected bgHex: string = '#6366f1';
  protected imgError: boolean = false;

  ngOnChanges(): void {
    this.initials = this.getInitials(this.name);
    this.fontSize = Math.max(10, Math.floor(this.size * 0.4));
    this.bgHex = this.getColorHex(this.name);
    this.imgError = false;
  }

  onImgError() {
    this.imgError = true;
  }

  private getInitials(name: string): string {
    if (!name) return 'U';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 0) return 'U';
    if (parts.length === 1) {
      return parts[0].substring(0, 2).toUpperCase();
    }
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  private getColorHex(name: string): string {
    if (!name) return '#6366f1';
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    const colors = [
      '#6366f1', // Indigo
      '#8b5cf6', // Violet
      '#ec4899', // Pink
      '#f43f5e', // Rose
      '#10b981', // Emerald
      '#06b6d4', // Cyan
      '#3b82f6', // Blue
      '#f59e0b'  // Amber
    ];
    const index = Math.abs(hash) % colors.length;
    return colors[index];
  }
}
