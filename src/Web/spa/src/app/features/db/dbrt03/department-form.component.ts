import { Component, computed, signal } from '@angular/core';
import { ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  Department,
  DepartmentOptions,
  DivisionLookupOption,
  LookupOption,
} from '../../../core/models';
import {
  ButtonComponent,
  InputComponent,
  LoadingComponent,
  SelectComponent,
  SelectOption,
} from '../../../shared/ui';
import { OrganizationFormBase } from '../shared/organization-crud';

@Component({
  selector: 'app-department-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    LoadingComponent,
    InputComponent,
    SelectComponent,
    ButtonComponent,
  ],
  templateUrl: './department-form.component.html',
  styleUrl: './department-form.component.scss',
})
export class DepartmentFormComponent extends OrganizationFormBase<
  Department,
  { divisionId: string; code: string; name: string }
> {
  protected override readonly programCode = 'DBRT03';
  protected override readonly apiBase = 'db/dbrt03';
  protected override readonly routeBase = '/db/dbrt03';

  private readonly branches = signal<readonly LookupOption[]>([]);
  private readonly divisions = signal<readonly DivisionLookupOption[]>([]);
  readonly selectedBranchId = signal('');

  readonly branchOptions = computed<readonly SelectOption[]>(() => [
    { value: '', label: 'ทุกสาขา' },
    ...this.branches().map((branch) => ({
      value: branch.id,
      code: branch.code,
      label: branch.name,
      disabledBadge: branch.isActive ? undefined : 'ปิดใช้งาน',
    })),
  ]);

  readonly divisionOptions = computed<readonly SelectOption[]>(() => {
    const branchId = this.selectedBranchId();
    const branchNames = new Map(this.branches().map((b) => [b.id, `${b.code} · ${b.name}`]));
    const filtered = branchId
      ? this.divisions().filter((division) => division.branchId === branchId)
      : this.divisions();
    return [
      { value: '', label: '-- เลือกฝ่าย --' },
      ...filtered.map((division) => ({
        value: division.id,
        code: division.code,
        label: division.name,
        group: branchNames.get(division.branchId),
        disabled: !division.isActive,
        disabledBadge: division.isActive ? undefined : 'ปิดใช้งาน',
      })),
    ];
  });

  override readonly form = this.fb.nonNullable.group({
    divisionId: ['', Validators.required],
    code: OrganizationFormBase.createCodeControl(),
    name: OrganizationFormBase.createNameControl(),
  });

  constructor() {
    super();
    this.initForm();
  }

  onBranchChanged(branchId: string): void {
    this.selectedBranchId.set(branchId);
    const currentDivisionId = this.form.controls.divisionId.value;
    if (branchId && currentDivisionId) {
      const match = this.divisions().find((division) => division.id === currentDivisionId);
      if (match && match.branchId !== branchId) {
        this.form.controls.divisionId.setValue('');
      }
    }
  }

  protected override async loadOptions(): Promise<void> {
    const companyId = this.session.companyId();
    if (!companyId) return;
    const options = await firstValueFrom(
      this.api.post<DepartmentOptions>('db/dbrt03/options', { companyId }),
    );
    this.branches.set(options.branches);
    this.divisions.set(options.divisions);
  }

  protected override applyLoadedDto(dto: Department): void {
    this.selectedBranchId.set(dto.branchId);
    this.form.reset({ divisionId: dto.divisionId, code: dto.code, name: dto.name });
    if (dto.codeLocked) this.form.controls.divisionId.disable();
    else this.form.controls.divisionId.enable();
  }

  protected override buildSaveBody(): Record<string, unknown> {
    const value = this.form.getRawValue();
    return this.editing
      ? {
          id: this.original!.id,
          divisionId: value.divisionId,
          code: value.code,
          name: value.name,
          version: this.original!.version,
        }
      : {
          companyId: this.session.companyId(),
          divisionId: value.divisionId,
          code: value.code,
          name: value.name,
        };
  }

  protected override captureDraft(): { divisionId: string; code: string; name: string } {
    return this.form.getRawValue();
  }
}

