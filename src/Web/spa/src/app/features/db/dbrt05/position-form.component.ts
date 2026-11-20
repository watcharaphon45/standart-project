import { Component } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Position } from '../../../core/models';
import { ButtonComponent, InputComponent, LoadingComponent } from '../../../shared/ui';
import { OrganizationFormBase } from '../shared/organization-crud';

@Component({
  selector: 'app-position-form',
  imports: [ReactiveFormsModule, RouterLink, LoadingComponent, InputComponent, ButtonComponent],
  templateUrl: './position-form.component.html',
  styleUrl: './position-form.component.scss',
})
export class PositionFormComponent extends OrganizationFormBase<
  Position,
  { code: string; name: string }
> {
  protected override readonly programCode = 'DBRT05';
  protected override readonly apiBase = 'db/dbrt05';
  protected override readonly routeBase = '/db/dbrt05';

  override readonly form = this.fb.nonNullable.group({
    code: OrganizationFormBase.createCodeControl(),
    name: OrganizationFormBase.createNameControl(),
  });

  constructor() {
    super();
    this.initForm();
  }

  protected override applyLoadedDto(dto: Position): void {
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

