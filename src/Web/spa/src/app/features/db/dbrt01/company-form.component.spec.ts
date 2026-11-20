import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { throwError, of } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { CompanyFormComponent } from './company-form.component';
import { ApiService } from '../../../core/api.service';
import { PendingChanges } from '../../../core/pending-changes.service';
describe('Company conflict recovery', () => {
  it('preserves the draft for reference while explicitly reloading the latest version', async () => {
    const api = {
      post: vi.fn(() =>
        of({
          id: '1',
          code: 'MAIN',
          name: 'ล่าสุด',
          isActive: true,
          codeLocked: false,
          version: '4',
        }),
      ),
      put: vi.fn(() =>
        throwError(
          () => new HttpErrorResponse({ status: 409, error: { code: 'CONCURRENCY_CONFLICT' } }),
        ),
      ),
    };
    await TestBed.configureTestingModule({
      imports: [CompanyFormComponent],
      providers: [
        provideRouter([]),
        { provide: ApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { editing: true } } } },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(CompanyFormComponent);
    const component = fixture.componentInstance;
    await fixture.whenStable();
    component.form.controls.name.setValue('ค่าที่กรอกไว้');
    await component.save();
    expect(component.conflict()).toBe(true);
    expect(component.draft()?.name).toBe('ค่าที่กรอกไว้');
    await component.load();
    expect(component.form.controls.name.value).toBe('ล่าสุด');
    expect(component.draft()?.name).toBe('ค่าที่กรอกไว้');
    expect(TestBed.inject(PendingChanges).dirty()).toBe(false);
  });
});
