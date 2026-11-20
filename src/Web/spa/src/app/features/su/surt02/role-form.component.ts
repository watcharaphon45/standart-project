import { Component, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  ButtonComponent,
  InputComponent,
  LoadingComponent,
  SelectComponent,
  SelectOption,
} from '../../../shared/ui';
import { OrganizationFormBase, OrganizationRow } from '../../db/shared/organization-crud';

interface RoleFormDto extends OrganizationRow {
  companyId: string;
}

const CREATE_SCOPE_OPTIONS: readonly SelectOption[] = [
  { value: 'company', label: 'บทบาทประจำบริษัทที่เลือก' },
  { value: 'global', label: 'บทบาทส่วนกลาง (Global)' },
];

@Component({
  selector: 'app-role-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    LoadingComponent,
    InputComponent,
    SelectComponent,
    ButtonComponent,
  ],
  templateUrl: './role-form.component.html',
  styleUrl: './role-form.component.scss',
})
export class RoleFormComponent extends OrganizationFormBase<
  RoleFormDto,
  { code: string; name: string }
> {
  protected override readonly programCode = 'SURT02';
  protected override readonly apiBase = 'su/surt02';
  protected override readonly routeBase = '/su/surt02';

  readonly scopeOptions = CREATE_SCOPE_OPTIONS;
  readonly targetScopeLabel = signal('');

  override readonly form = this.fb.nonNullable.group({
    scope: [this.session.companyId() ? 'company' : 'global'],
    code: OrganizationFormBase.createCodeControl(),
    name: OrganizationFormBase.createNameControl(),
  });

  constructor() {
    super();
    this.initForm();
  }

  protected override applyLoadedDto(dto: RoleFormDto): void {
    this.targetScopeLabel.set(dto.companyId ? 'บทบาทประจำบริษัท' : 'บทบาทส่วนกลาง (Global)');
    this.form.reset({
      scope: dto.companyId ? 'company' : 'global',
      code: dto.code,
      name: dto.name,
    });
  }

  protected override buildSaveBody(): Record<string, unknown> {
    const value = this.form.getRawValue();
    return this.editing
      ? { id: this.original!.id, code: value.code, name: value.name, version: this.original!.version }
      : {
          companyId: value.scope === 'global' ? null : this.session.companyId(),
          code: value.code,
          name: value.name,
        };
  }

  protected override captureDraft(): { code: string; name: string } {
    const value = this.form.getRawValue();
    return { code: value.code, name: value.name };
  }
}

