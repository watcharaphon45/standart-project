import { Component, inject } from '@angular/core';
import { NotificationService } from '../../../core/notification.service';

@Component({
  selector: 'app-notification',
  templateUrl: './notification.component.html',
  styleUrl: './notification.component.scss',
})
export class NotificationComponent {
  readonly notifications = inject(NotificationService);
}

