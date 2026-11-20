import { Component, signal } from '@angular/core';
import { ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Division, LookupOption } from '../../../core/models';
import {
  ButtonComponent,
  InputComponent,
  LoadingComponent,
  SelectComponent,
  SelectOption,
} from '../../../shared/ui';
import { OrganizationFormBase } from '../shared/organization-crud';

@Component({
  selector: 'app-division-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    LoadingComponent,
    InputComponent,
    SelectComponent,
    ButtonComponent,
  ],
  templateUrl: './division-form.component.html',
  styleUrl: './division-form.component.scss',
})
export class DivisionFormComponent extends OrganizationFormBase<
  Division,
  { branchId: string; code: string; name: string }
> {
  protected override readonly programCode = 'DBRT04';
  protected override readonly apiBase = 'db/dbrt04';
  protected override readonly routeBase = '/db/dbrt04';

  readonly branchOptions = signal<readonly SelectOption[]>([
    { value: '', label: '-- เลือกสาขา --' },
  ]);

  override readonly form = this.fb.nonNullable.group({
    branchId: ['', Validators.required],
    code: OrganizationFormBase.createCodeControl(),
    name: OrganizationFormBase.createNameControl(),
  });

  constructor() {
    super();
    this.initForm();
  }

  protected override async loadOptions(): Promise<void> {
    const companyId = this.session.companyId();
    if (!companyId) return;
    const branches = await firstValueFrom(
      this.api.post<LookupOption[]>('db/dbrt04/options', { companyId }),
    );
    this.branchOptions.set([
      { value: '', label: '-- เลือกสาขา --' },
      ...branches.map((branch) => ({
        value: branch.id,
        code: branch.code,
        label: branch.name,
        disabled: !branch.isActive,
        disabledBadge: branch.isActive ? undefined : 'ปิดใช้งาน',
      })),
    ]);
  }

  protected override applyLoadedDto(dto: Division): void {
    this.form.reset({ branchId: dto.branchId, code: dto.code, name: dto.name });
    if (dto.codeLocked) this.form.controls.branchId.disable();
    else this.form.controls.branchId.enable();
  }

  protected override buildSaveBody(): Record<string, unknown> {
    const value = this.form.getRawValue();
    return this.editing
      ? {
          id: this.original!.id,
          branchId: value.branchId,
          code: value.code,
          name: value.name,
          version: this.original!.version,
        }
      : {
          companyId: this.session.companyId(),
          branchId: value.branchId,
          code: value.code,
          name: value.name,
        };
  }

  protected override captureDraft(): { branchId: string; code: string; name: string } {
    return this.form.getRawValue();
  }
}

