import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
})
export class EmptyStateComponent {
  readonly message = input<string>('ไม่พบข้อมูล หรือยังไม่มีสิทธิ์ดูข้อมูลที่เลือก');
}

