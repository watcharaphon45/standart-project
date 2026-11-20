import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { AuthService } from '../core/auth.service';
import { NavigationItem } from '../core/models';
import { SessionState } from '../core/session-state.service';
import { ShellComponent } from './shell.component';

@Component({ template: '' })
class DummyRouteComponent {}

describe('ShellComponent', () => {
  let session: SessionState;
  let logoutSpy: ReturnType<typeof vi.fn>;

  function setUserNavigation(navigation: NavigationItem[]): void {
    session.accept({
      id: '1',
      userName: 'demo',
      mustChangePassword: false,
      companies: [{ id: '1', code: 'MAIN', name: 'บริษัทหลัก', isActive: true }],
      companyAccess: [],
      globalPermissions: [],
      navigation,
    });
  }

  beforeEach(async () => {
    sessionStorage.clear();
    logoutSpy = vi.fn().mockResolvedValue(undefined);
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideRouter([
          { path: 'db/dbrt01', component: DummyRouteComponent },
          { path: 'db/dbrt01/create', component: DummyRouteComponent },
          { path: 'su/surt02/edit', component: DummyRouteComponent },
          { path: 'login', component: DummyRouteComponent },
        ]),
        {
          provide: AuthService,
          useValue: {
            logout: logoutSpy,
          },
        },
      ],
    }).compileComponents();
    session = TestBed.inject(SessionState);
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('groups navigation items under DB and SU headers without showing program codes', () => {
    setUserNavigation([
      { code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' },
      { code: 'DBRT02', name: 'ข้อมูลสาขา', route: '/db/dbrt02' },
      { code: 'SURT01', name: 'ผู้ใช้งาน', route: '/su/surt01' },
      { code: 'SURT09', name: 'ประวัติการใช้งาน', route: '/su/surt09' },
    ]);

    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const nav = fixture.nativeElement.querySelector('nav[aria-label="เมนูหลัก"]') as HTMLElement;
    const labels = Array.from(nav.querySelectorAll('.nav-group-label')).map((el) =>
      el.textContent?.trim(),
    );
    expect(labels).toEqual(['ข้อมูลองค์กร', 'ระบบและการตั้งค่า']);

    const dbLinks = Array.from(nav.querySelectorAll('#nav-group-DB a')).map((el) =>
      el.textContent?.trim(),
    );
    const suLinks = Array.from(nav.querySelectorAll('#nav-group-SU a')).map((el) =>
      el.textContent?.trim(),
    );
    expect(dbLinks).toEqual(['ข้อมูลบริษัท', 'ข้อมูลสาขา']);
    expect(suLinks).toEqual(['ผู้ใช้งาน', 'ประวัติการใช้งาน']);

    const navText = nav.textContent ?? '';
    expect(navText).not.toContain('DBRT01');
    expect(navText).not.toContain('DBRT02');
    expect(navText).not.toContain('SURT01');
    expect(navText).not.toContain('SURT09');
  });

  it('omits empty module groups when user only has DB navigation items', () => {
    setUserNavigation([{ code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' }]);

    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const labels = Array.from<Element>(
      fixture.nativeElement.querySelectorAll('.nav-group-label'),
    ).map((el) => el.textContent?.trim());
    expect(labels).toEqual(['ข้อมูลองค์กร']);
    expect(fixture.nativeElement.querySelector('#nav-group-SU')).toBeNull();
  });

  it('collapses and expands module groups and persists state in sessionStorage', () => {
    setUserNavigation([
      { code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' },
      { code: 'SURT01', name: 'ผู้ใช้งาน', route: '/su/surt01' },
    ]);

    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const dbToggle = fixture.nativeElement.querySelector(
      '#nav-group-DB',
    )?.previousElementSibling?.querySelector('button') as HTMLButtonElement;
    const dbPanel = fixture.nativeElement.querySelector('#nav-group-DB') as HTMLElement;

    expect(dbToggle.getAttribute('aria-expanded')).toBe('true');
    expect(dbToggle.getAttribute('aria-controls')).toBe('nav-group-DB');
    expect(dbPanel.hidden).toBe(false);

    dbToggle.click();
    fixture.detectChanges();

    expect(dbToggle.getAttribute('aria-expanded')).toBe('false');
    expect(dbPanel.hidden).toBe(true);
    expect(JSON.parse(sessionStorage.getItem('clean.nav.collapsed') ?? '[]')).toEqual(['DB']);

    dbToggle.click();
    fixture.detectChanges();

    expect(dbToggle.getAttribute('aria-expanded')).toBe('true');
    expect(dbPanel.hidden).toBe(false);
    expect(JSON.parse(sessionStorage.getItem('clean.nav.collapsed') ?? '[]')).toEqual([]);
  });

  it('toggles desktop sidebar collapse, persists preference, and auto-expands on group click', () => {
    setUserNavigation([
      { code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' },
      { code: 'SURT01', name: 'ผู้ใช้งาน', route: '/su/surt01' },
    ]);

    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const workspace = fixture.nativeElement.querySelector('.workspace') as HTMLElement;
    const collapseBtn = fixture.nativeElement.querySelector(
      '.sidebar-collapse-btn button',
    ) as HTMLButtonElement;

    expect(workspace.classList.contains('sidebar-collapsed')).toBe(false);
    collapseBtn.click();
    fixture.detectChanges();

    expect(workspace.classList.contains('sidebar-collapsed')).toBe(true);
    expect(sessionStorage.getItem('clean.nav.sidebarCollapsed')).toBe('1');
    expect(collapseBtn.getAttribute('aria-label')).toBe('ขยายแถบเมนู');

    const dbToggle = fixture.nativeElement.querySelector(
      '#nav-group-DB',
    )?.previousElementSibling?.querySelector('button') as HTMLButtonElement;
    dbToggle.click();
    fixture.detectChanges();

    expect(workspace.classList.contains('sidebar-collapsed')).toBe(false);
    expect(sessionStorage.getItem('clean.nav.sidebarCollapsed')).toBe('0');
  });

  it('updates header breadcrumbs across list, create, and edit routes', async () => {
    setUserNavigation([
      { code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' },
      { code: 'SURT02', name: 'บทบาท', route: '/su/surt02' },
    ]);

    const router = TestBed.inject(Router);
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    await router.navigateByUrl('/db/dbrt01');
    fixture.detectChanges();
    let texts = Array.from<Element>(
      fixture.nativeElement.querySelectorAll('.breadcrumb li'),
    ).map((el) => el.textContent?.trim());
    expect(texts).toEqual(['ข้อมูลองค์กร', 'ข้อมูลบริษัท']);

    await router.navigateByUrl('/db/dbrt01/create');
    fixture.detectChanges();
    texts = Array.from<Element>(
      fixture.nativeElement.querySelectorAll('.breadcrumb li'),
    ).map((el) => el.textContent?.trim());
    expect(texts).toEqual(['ข้อมูลองค์กร', 'ข้อมูลบริษัท', 'เพิ่มข้อมูล']);
    expect(fixture.nativeElement.querySelector('.breadcrumb li a')?.textContent?.trim()).toBe(
      'ข้อมูลบริษัท',
    );

    await router.navigateByUrl('/su/surt02/edit');
    fixture.detectChanges();
    texts = Array.from<Element>(
      fixture.nativeElement.querySelectorAll('.breadcrumb li'),
    ).map((el) => el.textContent?.trim());
    expect(texts).toEqual(['ระบบและการตั้งค่า', 'บทบาท', 'แก้ไขข้อมูล']);
  });

  it('resets main scroll position when navigating to a different route path', async () => {
    setUserNavigation([
      { code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' },
      { code: 'SURT02', name: 'บทบาท', route: '/su/surt02' },
    ]);

    const router = TestBed.inject(Router);
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const mainEl = fixture.nativeElement.querySelector('main#content') as HTMLElement;
    await router.navigateByUrl('/db/dbrt01');
    fixture.detectChanges();

    mainEl.scrollTop = 240;
    expect(mainEl.scrollTop).toBe(240);

    await router.navigateByUrl('/su/surt02/edit');
    fixture.detectChanges();
    expect(mainEl.scrollTop).toBe(0);

    mainEl.scrollTop = 180;
    await router.navigateByUrl('/su/surt02/edit?q=1');
    fixture.detectChanges();
    expect(mainEl.scrollTop).toBe(180);
  });

  it('opens mobile drawer as modal dialog and restores focus on Escape', async () => {
    setUserNavigation([{ code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' }]);

    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const menuBtn = fixture.nativeElement.querySelector('app-button.menu button') as HTMLButtonElement;
    const aside = fixture.nativeElement.querySelector('aside') as HTMLElement;
    const focusSpy = vi.spyOn(menuBtn, 'focus');

    menuBtn.click();
    fixture.detectChanges();
    await Promise.resolve();

    expect(aside.classList.contains('open')).toBe(true);
    expect(aside.getAttribute('role')).toBe('dialog');
    expect(aside.getAttribute('aria-modal')).toBe('true');

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    await Promise.resolve();

    expect(aside.classList.contains('open')).toBe(false);
    expect(aside.getAttribute('role')).toBeNull();
    expect(focusSpy).toHaveBeenCalled();
  });

  it('opens user menu, closes cleanly on Escape, and triggers logout', async () => {
    setUserNavigation([{ code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' }]);

    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const triggerBtn = fixture.nativeElement.querySelector(
      '.user-menu-trigger button',
    ) as HTMLButtonElement;
    const dropdown = fixture.nativeElement.querySelector('#header-user-menu') as HTMLElement;
    const focusSpy = vi.spyOn(triggerBtn, 'focus');

    expect(dropdown.hidden).toBe(true);

    triggerBtn.click();
    fixture.detectChanges();
    expect(dropdown.hidden).toBe(false);
    expect(dropdown.querySelector('.user-menu-link')?.textContent?.trim()).toBe('เปลี่ยนรหัสผ่าน');

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    await Promise.resolve();

    expect(dropdown.hidden).toBe(true);
    expect(focusSpy).toHaveBeenCalled();

    triggerBtn.click();
    fixture.detectChanges();
    const logoutBtn = dropdown.querySelector('.user-menu-logout button') as HTMLButtonElement;
    logoutBtn.click();
    await fixture.whenStable();

    expect(logoutSpy).toHaveBeenCalledTimes(1);
  });

  it('switches active company via the Editable Combobox company switcher', async () => {
    session.accept({
      id: '1',
      userName: 'demo',
      mustChangePassword: false,
      companies: [
        { id: '1', code: 'MAIN', name: 'บริษัทหลัก', isActive: true },
        { id: '2', code: 'BRANCH2', name: 'บริษัทที่สอง', isActive: true },
      ],
      companyAccess: [],
      globalPermissions: [],
      navigation: [{ code: 'DBRT01', name: 'ข้อมูลบริษัท', route: '/db/dbrt01' }],
    });

    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const companyInput = fixture.nativeElement.querySelector('#company') as HTMLInputElement;
    expect(companyInput.getAttribute('role')).toBe('combobox');
    expect(companyInput.value).toBe('MAIN · บริษัทหลัก');

    companyInput.value = 'BRANCH2';
    companyInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    companyInput.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', cancelable: true }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(session.companyId()).toBe('2');
    expect(companyInput.value).toBe('BRANCH2 · บริษัทที่สอง');
  });
});
