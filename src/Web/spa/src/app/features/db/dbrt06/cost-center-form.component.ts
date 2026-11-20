import { Component } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CostCenter } from '../../../core/models';
import { ButtonComponent, InputComponent, LoadingComponent } from '../../../shared/ui';
import { OrganizationFormBase } from '../shared/organization-crud';

@Component({
  selector: 'app-cost-center-form',
  imports: [ReactiveFormsModule, RouterLink, LoadingComponent, InputComponent, ButtonComponent],
  templateUrl: './cost-center-form.component.html',
  styleUrl: './cost-center-form.component.scss',
})
export class CostCenterFormComponent extends OrganizationFormBase<
  CostCenter,
  { code: string; name: string }
> {
  protected override readonly programCode = 'DBRT06';
  protected override readonly apiBase = 'db/dbrt06';
  protected override readonly routeBase = '/db/dbrt06';

  override readonly form = this.fb.nonNullable.group({
    code: OrganizationFormBase.createCodeControl(),
    name: OrganizationFormBase.createNameControl(),
  });

  constructor() {
    super();
    this.initForm();
  }

  protected override applyLoadedDto(dto: CostCenter): void {
    this.form.reset({ code: dto.code, name: dto.name });
  }

  protected override buildSaveBody(): Record<string, unknown> {
    const value = this.form.getRawValue();
    return this.editing
      ? { id: this.original!.id, code: value.code, name: value.name, version: this.original!.version }
      : { companyId: this.session.companyId(), code: value.code, name: value.name };
  }

  protected override captureDraft(): { code: string; name: string } {
    return this.form.getRawValue();
  }
}

