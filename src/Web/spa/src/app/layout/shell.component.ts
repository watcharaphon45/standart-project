import { A11yModule } from '@angular/cdk/a11y';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { SessionState } from '../core/session-state.service';
import { AuthService } from '../core/auth.service';
import { PendingChanges } from '../core/pending-changes.service';
import { NavigationItem } from '../core/models';
import { ButtonComponent, SelectComponent, SelectOption } from '../shared/ui';

export interface NavModuleGroup {
  code: string;
  label: string;
  items: NavigationItem[];
}

export interface BreadcrumbItem {
  label: string;
  route?: string;
}

const NAV_COLLAPSED_KEY = 'clean.nav.collapsed';
const SIDEBAR_COLLAPSED_KEY = 'clean.nav.sidebarCollapsed';
const MODULE_ORDER = ['DB', 'SU'];
const MODULE_LABELS: Record<string, string> = {
  DB: 'ข้อมูลองค์กร',
  SU: 'ระบบและการตั้งค่า',
};
const PROGRAM_FALLBACK_LABELS: Record<string, string> = {
  DBRT01: 'ข้อมูลบริษัท',
  DBRT02: 'ข้อมูลสาขา',
  DBRT03: 'ข้อมูลแผนก',
  DBRT04: 'ข้อมูลฝ่าย',
  DBRT05: 'ตำแหน่งงาน',
  DBRT06: 'ศูนย์ต้นทุน',
  DBRT07: 'หน่วยธุรกิจ',
  SURT01: 'ผู้ใช้งาน',
  SURT02: 'บทบาท',
  SURT03: 'สิทธิ์',
  SURT04: 'ขอบเขตผู้ใช้งาน',
  SURT05: 'เมนู',
  SURT06: 'เลขที่เอกสาร',
  SURT07: 'ขั้นตอนอนุมัติ',
  SURT08: 'ตั้งค่าระบบ',
  SURT09: 'ประวัติการใช้งาน',
};

function loadCollapsedGroups(): ReadonlySet<string> {
  try {
    const raw = sessionStorage.getItem(NAV_COLLAPSED_KEY);
    if (!raw) return new Set();
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed)
      ? new Set(parsed.filter((x): x is string => typeof x === 'string'))
      : new Set();
  } catch {
    return new Set();
  }
}

function saveCollapsedGroups(codes: ReadonlySet<string>): void {
  try {
    sessionStorage.setItem(NAV_COLLAPSED_KEY, JSON.stringify([...codes]));
  } catch {
    /* ignore storage unavailability */
  }
}

function loadSidebarCollapsed(): boolean {
  try {
    return sessionStorage.getItem(SIDEBAR_COLLAPSED_KEY) === '1';
  } catch {
    return false;
  }
}

function saveSidebarCollapsed(collapsed: boolean): void {
  try {
    sessionStorage.setItem(SIDEBAR_COLLAPSED_KEY, collapsed ? '1' : '0');
  } catch {
    /* ignore storage unavailability */
  }
}

@Component({
  selector: 'app-shell',
  imports: [
    A11yModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ButtonComponent,
    SelectComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  host: {
    '(document:keydown.escape)': 'onEscape()',
    '(document:click)': 'onDocumentClick($event)',
  },
})
export class ShellComponent {
  readonly session = inject(SessionState);
  private readonly auth = inject(AuthService);
  private readonly pending = inject(PendingChanges);
  private readonly router = inject(Router);

  readonly drawerOpen = signal(false);
  readonly sidebarCollapsed = signal<boolean>(loadSidebarCollapsed());
  readonly userMenuOpen = signal(false);
  readonly currentUrl = signal(this.router.url.split('?')[0].split('#')[0]);
  private readonly collapsedGroups = signal<ReadonlySet<string>>(loadCollapsedGroups());

  private readonly mobileMenuBtn = viewChild<ButtonComponent>('mobileMenuBtn');
  private readonly drawerCloseBtn = viewChild<ButtonComponent>('drawerCloseBtn');
  private readonly userMenuBtn = viewChild<ButtonComponent>('userMenuBtn');
  private readonly userMenuContainer = viewChild<ElementRef<HTMLElement>>('userMenuContainer');
  private readonly mainContent = viewChild<ElementRef<HTMLElement>>('mainContent');

  readonly userInitial = computed(() =>
    (this.session.user()?.userName?.trim().charAt(0) || 'U').toUpperCase(),
  );

  readonly navGroups = computed<readonly NavModuleGroup[]>(() => {
    const items = this.session.user()?.navigation ?? [];
    const grouped = new Map<string, NavigationItem[]>();
    for (const item of items) {
      const code = item.code.slice(0, 2).toUpperCase();
      const bucket = grouped.get(code);
      if (bucket) bucket.push(item);
      else grouped.set(code, [item]);
    }
    const orderedCodes = [
      ...MODULE_ORDER.filter((code) => grouped.has(code)),
      ...[...grouped.keys()].filter((code) => !MODULE_ORDER.includes(code)),
    ];
    return orderedCodes.map((code) => ({
      code,
      label: MODULE_LABELS[code] ?? code,
      items: grouped.get(code)!,
    }));
  });

  readonly breadcrumbs = computed<readonly BreadcrumbItem[]>(() => {
    const url = this.currentUrl();
    const navigation = this.session.user()?.navigation ?? [];
    const matchedNav = navigation.find(
      (item) => url === item.route || url.startsWith(item.route + '/'),
    );

    const routeMatch = /^\/(db|su)\/([a-z]{4}\d{2})(?:\/(create|edit))?$/i.exec(url);
    const moduleCode = matchedNav
      ? matchedNav.code.slice(0, 2).toUpperCase()
      : routeMatch?.[1].toUpperCase();
    const programCode = matchedNav ? matchedNav.code : routeMatch?.[2].toUpperCase();
    const programRoute = matchedNav
      ? matchedNav.route
      : routeMatch
        ? `/${routeMatch[1].toLowerCase()}/${routeMatch[2].toLowerCase()}`
        : undefined;
    const programName =
      matchedNav?.name ?? (programCode ? PROGRAM_FALLBACK_LABELS[programCode] : undefined);

    if (!moduleCode || !programName || !programRoute) return [];

    const suffix = routeMatch?.[3]?.toLowerCase();
    const isSubpage = Boolean(suffix) || url !== programRoute;
    const crumbs: BreadcrumbItem[] = [
      { label: MODULE_LABELS[moduleCode] ?? moduleCode },
      { label: programName, route: isSubpage ? programRoute : undefined },
    ];
    if (suffix === 'create') crumbs.push({ label: 'เพิ่มข้อมูล' });
    else if (suffix === 'edit') crumbs.push({ label: 'แก้ไขข้อมูล' });
    return crumbs;
  });

  readonly companyOptions = computed<readonly SelectOption[]>(() => {
    const companies = this.session.user()?.companies ?? [];
    if (companies.length === 0) {
      return [{ value: '', label: 'ยังไม่มีบริษัทที่เข้าถึงได้' }];
    }
    return companies.map((company) => ({
      value: company.id,
      code: company.code,
      label: company.name,
      disabledBadge: company.isActive ? undefined : 'ปิดใช้งาน',
    }));
  });

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((event) => {
        const nextUrl = event.urlAfterRedirects.split('?')[0].split('#')[0];
        const pathChanged = nextUrl !== this.currentUrl();
        this.currentUrl.set(nextUrl);
        if (pathChanged) {
          const mainEl = this.mainContent()?.nativeElement;
          if (mainEl) mainEl.scrollTop = 0;
        }
      });
  }

  isGroupCollapsed(code: string): boolean {
    return this.collapsedGroups().has(code);
  }

  toggleGroup(code: string): void {
    const next = new Set(this.collapsedGroups());
    if (next.has(code)) next.delete(code);
    else next.add(code);
    this.collapsedGroups.set(next);
    saveCollapsedGroups(next);
  }

  toggleSidebarCollapsed(): void {
    const next = !this.sidebarCollapsed();
    this.sidebarCollapsed.set(next);
    saveSidebarCollapsed(next);
  }

  onGroupHeaderClick(code: string): void {
    if (this.sidebarCollapsed() && !this.drawerOpen()) {
      this.sidebarCollapsed.set(false);
      saveSidebarCollapsed(false);
      if (this.collapsedGroups().has(code)) {
        const next = new Set(this.collapsedGroups());
        next.delete(code);
        this.collapsedGroups.set(next);
        saveCollapsedGroups(next);
      }
      return;
    }
    this.toggleGroup(code);
  }

  isGroupActive(group: NavModuleGroup): boolean {
    const url = this.currentUrl();
    return group.items.some((item) => url === item.route || url.startsWith(item.route + '/'));
  }

  toggleDrawer(): void {
    if (this.drawerOpen()) this.closeDrawer(true);
    else this.openDrawer();
  }

  openDrawer(): void {
    this.userMenuOpen.set(false);
    this.drawerOpen.set(true);
    queueMicrotask(() => this.drawerCloseBtn()?.focus());
  }

  closeDrawer(restoreFocus = true): void {
    if (!this.drawerOpen()) return;
    this.drawerOpen.set(false);
    if (restoreFocus) {
      queueMicrotask(() => this.mobileMenuBtn()?.focus());
    }
  }

  toggleUserMenu(): void {
    if (this.userMenuOpen()) {
      this.closeUserMenu(false);
      return;
    }
    this.drawerOpen.set(false);
    this.userMenuOpen.set(true);
  }

  closeUserMenu(restoreFocus = true): void {
    if (!this.userMenuOpen()) return;
    this.userMenuOpen.set(false);
    if (restoreFocus) {
      queueMicrotask(() => this.userMenuBtn()?.focus());
    }
  }

  onEscape(): void {
    if (this.userMenuOpen()) {
      this.closeUserMenu(true);
      return;
    }
    if (this.drawerOpen()) {
      this.closeDrawer(true);
    }
  }

  onDocumentClick(event: MouseEvent): void {
    if (!this.userMenuOpen()) return;
    const container = this.userMenuContainer()?.nativeElement;
    if (container && event.target instanceof Node && !container.contains(event.target)) {
      this.closeUserMenu(false);
    }
  }

  async switchCompany(id: string): Promise<void> {
    if (!id || id === this.session.companyId()) return;
    if (!(await this.pending.leave())) return;
    this.session.selectCompany(id);
    const url = this.currentUrl();
    if (url.endsWith('/create') || url.endsWith('/edit')) {
      const base = url.replace(/\/(create|edit)$/, '');
      await this.router.navigateByUrl(base || '/db/dbrt01');
    }
  }

  async logout(): Promise<void> {
    this.closeUserMenu(false);
    if (!(await this.pending.leave())) return;
    try {
      await this.auth.logout();
      await this.router.navigateByUrl('/login');
    } catch {
      /* interceptor presents error */
    }
  }
}
