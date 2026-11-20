import { Component } from '@angular/core';
import { ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DocumentSequenceDto } from '../../../core/models';
import {
  ButtonComponent,
  InputComponent,
  LoadingComponent,
  SelectComponent,
  SelectOption,
} from '../../../shared/ui';
import { OrganizationFormBase } from '../../db/shared/organization-crud';

const RESET_POLICY_OPTIONS: readonly SelectOption[] = [
  { value: 'YEAR_MONTH', label: 'รีเซ็ตทุกเดือน (YYYYMM)' },
  { value: 'YEAR', label: 'รีเซ็ตทุกปี (YYYY)' },
  { value: 'NONE', label: 'ต่อเนื่องไม่รีเซ็ต' },
];

@Component({
  selector: 'app-doc-sequence-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    LoadingComponent,
    InputComponent,
    SelectComponent,
    ButtonComponent,
  ],
  templateUrl: './doc-sequence-form.component.html',
  styleUrl: './doc-sequence-form.component.scss',
})
export class DocSequenceFormComponent extends OrganizationFormBase<
  DocumentSequenceDto,
  { code: string; name: string; prefix: string; resetPolicy: string; paddingLength: number }
> {
  protected override readonly programCode = 'SURT06';
  protected override readonly apiBase = 'su/surt06';
  protected override readonly routeBase = '/su/surt06';

  readonly resetPolicyOptions = RESET_POLICY_OPTIONS;

  override readonly form = this.fb.nonNullable.group({
    code: OrganizationFormBase.createCodeControl(),
    name: OrganizationFormBase.createNameControl(),
    prefix: [
      '',
      [Validators.required, Validators.maxLength(20), Validators.pattern(/^[A-Za-z0-9_-]+$/)],
    ],
    resetPolicy: ['YEAR_MONTH' as 'NONE' | 'YEAR' | 'YEAR_MONTH', Validators.required],
    paddingLength: [5, [Validators.required, Validators.min(3), Validators.max(10)]],
  });

  constructor() {
    super();
    this.initForm();
  }

  protected override applyLoadedDto(dto: DocumentSequenceDto): void {
    this.form.reset({
      code: dto.code,
      name: dto.name,
      prefix: dto.prefix,
      resetPolicy: dto.resetPolicy,
      paddingLength: dto.paddingLength,
    });
    if (dto.codeLocked) {
      this.form.controls.prefix.disable();
      this.form.controls.resetPolicy.disable();
    } else {
      this.form.controls.prefix.enable();
      this.form.controls.resetPolicy.enable();
    }
  }

  protected override buildSaveBody(): Record<string, unknown> {
    const value = this.form.getRawValue();
    return {
      ...(this.editing
        ? { id: this.original!.id, version: this.original!.version }
        : { companyId: this.session.companyId() }),
      code: value.code,
      name: value.name,
      prefix: value.prefix.trim().toUpperCase(),
      resetPolicy: value.resetPolicy,
      paddingLength: Number(value.paddingLength),
    };
  }

  protected override captureDraft() {
    return this.form.getRawValue();
  }
}

