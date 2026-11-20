import { Component } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BusinessUnit } from '../../../core/models';
import { ButtonComponent, InputComponent, LoadingComponent } from '../../../shared/ui';
import { OrganizationFormBase } from '../shared/organization-crud';

@Component({
  selector: 'app-business-unit-form',
  imports: [ReactiveFormsModule, RouterLink, LoadingComponent, InputComponent, ButtonComponent],
  templateUrl: './business-unit-form.component.html',
  styleUrl: './business-unit-form.component.scss',
})
export class BusinessUnitFormComponent extends OrganizationFormBase<
  BusinessUnit,
  { code: string; name: string }
> {
  protected override readonly programCode = 'DBRT07';
  protected override readonly apiBase = 'db/dbrt07';
  protected override readonly routeBase = '/db/dbrt07';

  override readonly form = this.fb.nonNullable.group({
    code: OrganizationFormBase.createCodeControl(),
    name: OrganizationFormBase.createNameControl(),
  });

  constructor() {
    super();
    this.initForm();
  }

  protected override applyLoadedDto(dto: BusinessUnit): void {
    this.form.reset({ code: dto.code, name: dto.name });
  }

  protected override buildSaveBody(): Record<string, unknown> {
    const value = this.form.getRawValue();
    return this.editing
      ? { id: this.original!.id, code: value.code, name: value.name, version: this.original!.version }
      : { code: value.code, name: value.name };
  }

  protected override captureDraft(): { code: string; name: string } {
    return this.form.getRawValue();
  }
}

