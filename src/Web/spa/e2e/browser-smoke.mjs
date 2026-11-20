import { createServer } from 'node:https';
import { readFile } from 'node:fs/promises';
import { resolve, extname, sep } from 'node:path';
import { once } from 'node:events';
import { chromium, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const api = process.env.CLEAN_BROWSER_API.replace('127.0.0.1', 'localhost');
const spaPort = Number(process.env.CLEAN_BROWSER_SPA_PORT || '4200');
const spaOrigin = `https://localhost:${spaPort}`;
const root = resolve('dist/clean/browser');
const types = {
  '.html': 'text/html',
  '.js': 'application/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.ico': 'image/x-icon',
};
const server = createServer(
  { pfx: Buffer.from(process.env.CLEAN_BROWSER_CERT, 'base64') },
  async (request, response) => {
    try {
      const pathname = new URL(request.url, spaOrigin).pathname;
      if (pathname === '/config.json') {
        response.writeHead(200, { 'Content-Type': 'application/json' });
        response.end(JSON.stringify({ apiBaseUrl: api }));
        return;
      }
      const candidate = resolve(root, '.' + decodeURIComponent(pathname));
      if (candidate !== root && !candidate.startsWith(root + sep)) {
        response.writeHead(403);
        response.end();
        return;
      }
      let file = candidate;
      let body;
      try {
        body = await readFile(file);
      } catch {
        file = resolve(root, 'index.html');
        body = await readFile(file);
      }
      response.writeHead(200, {
        'Content-Type': types[extname(file)] ?? 'application/octet-stream',
      });
      response.end(body);
    } catch {
      response.writeHead(500);
      response.end();
    }
  },
);
server.listen(spaPort, 'localhost');
await once(server, 'listening');

async function selectOptionContaining(comboboxLocator, text) {
  await comboboxLocator.click();
  await comboboxLocator.fill(text);
  const listbox = comboboxLocator.page().getByRole('listbox').last();
  await expect(listbox).toBeVisible();
  const option = listbox.getByRole('option', { name: new RegExp(text, 'i') }).first();
  await expect(option).toBeVisible();
  await option.click();
  await expect(listbox).toBeHidden();
  await expect(comboboxLocator).toHaveAttribute('aria-expanded', 'false');
  await expect(comboboxLocator).toHaveValue(new RegExp(text, 'i'));
}

let browser;
try {
  browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    ignoreHTTPSErrors: true,
    viewport: { width: 1440, height: 1000 },
  });
  const page = await context.newPage();
  const errors = [];
  page.on('requestfailed', (request) =>
    console.error('Request failed:', request.method(), request.url(), request.failure()?.errorText),
  );
  page.on('response', (response) => {
    if (response.status() >= 400) console.error('HTTP failure:', response.status(), response.url());
  });
  page.on('console', (message) => {
    if (message.type() === 'error') console.error('Browser:', message.text());
  });
  page.on('pageerror', (error) => errors.push(error.message));

  await page.goto(`${spaOrigin}/login`);
  await expect(page.getByRole('heading', { name: 'เข้าสู่ระบบ' })).toBeVisible();
  await page.keyboard.press('Tab');
  await expect(page.getByLabel('Username หรือ Email')).toBeFocused();
  const loginAxe = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(loginAxe.violations).toEqual([]);

  await page.getByLabel('Username หรือ Email').fill('tester');
  await page.getByLabel('รหัสผ่าน', { exact: true }).fill(process.env.CLEAN_BROWSER_PASSWORD);
  await page.getByRole('button', { name: 'เข้าสู่ระบบ', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'ข้อมูลบริษัท', exact: true })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'บริษัทหนึ่ง', exact: true })).toBeVisible();

  // 1) Sidebar module grouping (no program codes), breadcrumb & desktop collapse/expand
  const nav = page.getByRole('navigation', { name: 'เมนูหลัก' });
  await expect(nav.getByText('ข้อมูลองค์กร', { exact: true })).toBeVisible();
  await expect(nav.getByText('ระบบและการตั้งค่า', { exact: true })).toBeVisible();
  const navText = (await nav.textContent()) ?? '';
  expect(navText).not.toContain('DBRT01');
  expect(navText).not.toContain('SURT01');

  const breadcrumb = page.getByRole('navigation', { name: 'ตำแหน่งหน้าปัจจุบัน' });
  await expect(breadcrumb).toContainText('ข้อมูลองค์กร');
  await expect(breadcrumb).toContainText('ข้อมูลบริษัท');

  const sidebar = page.locator('aside');
  await page.getByRole('button', { name: 'ย่อแถบเมนู', exact: true }).click();
  await expect
    .poll(async () => (await sidebar.boundingBox())?.width ?? 999)
    .toBeLessThanOrEqual(80);
  await page.getByRole('button', { name: 'ขยายแถบเมนู', exact: true }).click();
  await expect
    .poll(async () => (await sidebar.boundingBox())?.width ?? 0)
    .toBeGreaterThanOrEqual(240);

  // 2) DBRT01 create, concurrent edit conflict recovery, update, deactivate Escape dialog, confirm & activate
  await page.getByRole('link', { name: '+ เพิ่มบริษัท' }).click();
  await page.getByLabel('รหัสบริษัท').fill('web');
  await page.getByLabel('ชื่อบริษัท').fill('บริษัทผ่าน Browser');
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('cell', { name: 'WEB', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'แก้ไข', exact: true }).click();
  await expect(page.getByLabel('รหัสบริษัท')).toHaveValue('WEB');
  await page.getByLabel('ชื่อบริษัท').fill('ค่าที่กรอกไว้จาก Browser');
  // A second editor writes through the real API before this form is saved.
  const mutation = await page.evaluate(async (apiUrl) => {
    const token = (await (await fetch(apiUrl + '/api/auth/csrf', { credentials: 'include' })).json())
      .token;
    const headers = { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token };
    const me = await (await fetch(apiUrl + '/api/auth/me', { credentials: 'include' })).json();
    const company = me.companies.find((item) => item.code === 'WEB');
    const current = await (
      await fetch(apiUrl + '/api/db/dbrt01/detail', {
        method: 'POST',
        credentials: 'include',
        headers,
        body: JSON.stringify({ id: company.id }),
      })
    ).json();
    return (
      await fetch(apiUrl + '/api/db/dbrt01/update', {
        method: 'PUT',
        credentials: 'include',
        headers,
        body: JSON.stringify({
          id: current.id,
          code: current.code,
          name: 'ชื่อจากผู้แก้ไขอีกคน',
          version: current.version,
        }),
      })
    ).status;
  }, api);
  expect(mutation).toBe(200);
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('heading', { name: 'ข้อมูลถูกแก้ไขแล้ว' })).toBeVisible();
  await page.getByRole('button', { name: 'โหลดข้อมูลล่าสุด', exact: true }).click();
  await expect(page.getByLabel('ชื่อบริษัท')).toHaveValue('ชื่อจากผู้แก้ไขอีกคน');
  await expect(page.getByText('ชื่อ: ค่าที่กรอกไว้จาก Browser', { exact: true })).toBeVisible();
  await page.getByLabel('ชื่อบริษัท').fill('ชื่อที่บันทึกสำเร็จ');
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('cell', { name: 'ชื่อที่บันทึกสำเร็จ', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'ปิดใช้งาน', exact: true }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).not.toBeVisible();
  await expect(page.getByRole('button', { name: 'ปิดใช้งาน', exact: true })).toBeFocused();
  await page.getByRole('button', { name: 'ปิดใช้งาน', exact: true }).click();
  await page.getByRole('button', { name: 'ยืนยัน', exact: true }).click();
  await expect(page.getByRole('cell', { name: 'ปิดใช้งาน', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'เปิดใช้งาน', exact: true }).click();
  await page.getByRole('button', { name: 'ยืนยัน', exact: true }).click();
  await expect(page.getByRole('cell', { name: 'เปิดใช้งาน', exact: true })).toBeVisible();
  const companyAxe = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(companyAxe.violations).toEqual([]);
  await page.screenshot({ path: 'test-results/company-desktop.png', fullPage: true });

  // 3) Organization Hierarchy: DBRT02 (Branch) -> DBRT04 (Division) -> DBRT03 (Department)
  await nav.getByRole('link', { name: 'ข้อมูลสาขา', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'ข้อมูลสาขา', exact: true })).toBeVisible();
  await page.getByRole('link', { name: '+ เพิ่มสาขา' }).click();
  await page.getByLabel('รหัสสาขา').fill('BKK');
  await page.getByLabel('ชื่อสาขา').fill('สาขากรุงเทพ');
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('cell', { name: 'BKK', exact: true })).toBeVisible();

  await nav.getByRole('link', { name: 'ข้อมูลฝ่าย', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'ข้อมูลฝ่าย', exact: true })).toBeVisible();
  await page.getByRole('link', { name: '+ เพิ่มฝ่าย' }).click();
  await selectOptionContaining(page.getByRole('combobox', { name: 'สาขา', exact: true }), 'BKK');
  await page.getByLabel('รหัสฝ่าย').fill('OPS');
  await page.getByLabel('ชื่อฝ่าย').fill('ฝ่ายปฏิบัติการ');
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('cell', { name: 'OPS', exact: true })).toBeVisible();

  await nav.getByRole('link', { name: 'ข้อมูลแผนก', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'ข้อมูลแผนก', exact: true })).toBeVisible();
  await page.getByRole('link', { name: '+ เพิ่มแผนก' }).click();
  await selectOptionContaining(page.getByRole('combobox', { name: 'ฝ่าย', exact: true }), 'OPS');
  await page.getByLabel('รหัสแผนก').fill('ACC');
  await page.getByLabel('ชื่อแผนก').fill('แผนกบัญชี');
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('cell', { name: 'ACC', exact: true })).toBeVisible();

  // 4) System Admin & Operations: SURT02 -> SURT06 -> SURT08 -> SURT09
  await nav.getByRole('link', { name: 'บทบาท', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'ข้อมูลบทบาท', exact: true })).toBeVisible();
  await page.getByRole('link', { name: '+ เพิ่มบทบาท' }).click();
  await page.getByLabel('รหัสบทบาท').fill('APPROVER');
  await page.getByLabel('ชื่อบทบาท').fill('ผู้อนุมัติ');
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('cell', { name: 'APPROVER', exact: true })).toBeVisible();

  await nav.getByRole('link', { name: 'เลขที่เอกสาร', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'เลขที่เอกสาร', exact: true })).toBeVisible();
  await page.getByRole('link', { name: '+ เพิ่มเลขที่เอกสาร' }).click();
  await page.getByLabel('รหัสชุดเลขที่').fill('INV');
  await page.getByLabel('ชื่อชุดเลขที่เอกสาร').fill('ใบแจ้งหนี้');
  await page.getByLabel('Prefix เอกสาร').fill('INV');
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('cell', { name: 'INV', exact: true }).first()).toBeVisible();
  await page.getByRole('button', { name: 'ออกเลขถัดไป', exact: true }).first().click();
  await page.getByRole('dialog').getByRole('button', { name: 'ออกเลขถัดไป', exact: true }).click();
  await expect(page.locator('td', { hasText: '-00002' }).first()).toBeVisible();

  await nav.getByRole('link', { name: 'ตั้งค่าระบบ', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'ตั้งค่าระบบ', exact: true })).toBeVisible();
  await page.getByRole('link', { name: '+ เพิ่มค่าตั้งค่า' }).click();
  await page.getByLabel('คีย์ตั้งค่า').fill('API_TOKEN');
  await page.getByLabel('ชื่อแสดงผล').fill('โทเค็นระบบ');
  await page.getByLabel('เป็นข้อมูลลับ (ซ่อนค่าใน API/หน้าจอ และปกปิดใน Audit Log)').check();
  await page.getByLabel('ค่าตั้งค่า').fill('secret-browser-token-99');
  await page.getByRole('button', { name: 'บันทึกข้อมูล' }).click();
  await expect(page.getByRole('cell', { name: 'API_TOKEN', exact: true })).toBeVisible();
  expect((await page.locator('body').textContent()) ?? '').not.toContain('secret-browser-token-99');

  await nav.getByRole('link', { name: 'ประวัติการใช้งาน', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'ประวัติการใช้งาน (Audit Log)', exact: true })).toBeVisible();
  const filterToggle = page.getByRole('button', { name: /^ตัวกรอง/ });
  await filterToggle.click();
  await expect(filterToggle).toHaveAttribute('aria-expanded', 'true');
  await expect(page.getByLabel('โปรแกรม')).toBeVisible();
  await page.getByRole('button', { name: 'ดูรายละเอียด', exact: true }).first().click();
  await expect(page.getByRole('heading', { name: /รายละเอียดรายการ #/ })).toBeVisible();
  const auditAxe = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(auditAxe.violations).toEqual([]);
  await page.screenshot({ path: 'test-results/audit-desktop.png', fullPage: true });

  // 5) Tablet 1024x768 & Mobile 390x844 Drawer + Accessibility + User Menu Logout
  await page.setViewportSize({ width: 1024, height: 768 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const scrollMetrics = await page.evaluate(() => {
    const docNoScroll =
      document.documentElement.scrollHeight <= window.innerHeight && window.scrollY === 0;
    const aside = document.querySelector('aside');
    const navEl = document.querySelector('.main-nav');
    const headerEl = document.querySelector('.main > header');
    const mainEl = document.querySelector('main#content');

    const navOverflow = navEl.scrollHeight > navEl.clientHeight;
    navEl.scrollTop = 120;
    const navTopAfterNav = navEl.scrollTop;
    const mainTopAfterNav = mainEl.scrollTop;
    const headerTopAfterNav = Math.round(headerEl.getBoundingClientRect().top);
    const asideTopAfterNav = Math.round(aside.getBoundingClientRect().top);

    const mainOverflow = mainEl.scrollHeight > mainEl.clientHeight;
    let mainTopAfterMain = mainEl.scrollTop;
    let windowYAfterMain = window.scrollY;
    let headerTopAfterMain = headerTopAfterNav;
    let asideTopAfterMain = asideTopAfterNav;
    let navTopAfterMain = navTopAfterNav;
    if (mainOverflow) {
      mainEl.scrollTop = 120;
      mainTopAfterMain = mainEl.scrollTop;
      windowYAfterMain = window.scrollY;
      headerTopAfterMain = Math.round(headerEl.getBoundingClientRect().top);
      asideTopAfterMain = Math.round(aside.getBoundingClientRect().top);
      navTopAfterMain = navEl.scrollTop;
    }

    navEl.scrollTop = 0;
    mainEl.scrollTop = 0;
    return {
      docNoScroll,
      navOverflow,
      navTopAfterNav,
      mainTopAfterNav,
      headerTopAfterNav,
      asideTopAfterNav,
      mainOverflow,
      mainTopAfterMain,
      windowYAfterMain,
      headerTopAfterMain,
      asideTopAfterMain,
      navTopAfterMain,
    };
  });
  expect(scrollMetrics.docNoScroll).toBe(true);
  expect(scrollMetrics.navOverflow).toBe(true);
  expect(scrollMetrics.navTopAfterNav).toBeGreaterThan(0);
  expect(scrollMetrics.mainTopAfterNav).toBe(0);
  expect(scrollMetrics.headerTopAfterNav).toBe(0);
  expect(scrollMetrics.asideTopAfterNav).toBe(0);
  expect(scrollMetrics.mainOverflow).toBe(true);
  expect(scrollMetrics.mainTopAfterMain).toBeGreaterThan(0);
  expect(scrollMetrics.windowYAfterMain).toBe(0);
  expect(scrollMetrics.headerTopAfterMain).toBe(0);
  expect(scrollMetrics.asideTopAfterMain).toBe(0);
  expect(scrollMetrics.navTopAfterMain).toBe(scrollMetrics.navTopAfterNav);

  const tabletAxe = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(tabletAxe.violations).toEqual([]);
  await page.screenshot({ path: 'test-results/tablet.png', fullPage: true });

  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const mobileMenuBtn = page.getByRole('button', { name: 'เปิดหรือปิดเมนู', exact: true });
  await mobileMenuBtn.click();
  await expect(page.getByRole('dialog', { name: 'เมนูหลัก' })).toBeVisible();
  await expect(nav).toBeVisible();
  const drawerAxe = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(drawerAxe.violations).toEqual([]);
  await page.screenshot({ path: 'test-results/company-mobile.png', fullPage: true });
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog', { name: 'เมนูหลัก' })).not.toBeVisible();
  await expect(mobileMenuBtn).toBeFocused();

  const userMenuBtn = page.getByRole('button', { name: 'เมนูผู้ใช้ tester', exact: true });
  await userMenuBtn.click();
  const changePasswordLink = page.getByRole('link', { name: 'เปลี่ยนรหัสผ่าน', exact: true });
  await expect(changePasswordLink).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(changePasswordLink).not.toBeVisible();
  await expect(userMenuBtn).toBeFocused();

  await userMenuBtn.click();
  await page.getByRole('button', { name: 'ออกจากระบบ', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'เข้าสู่ระบบ' })).toBeVisible();
  expect(errors).toEqual([]);
  console.log(
    'Browser passed: Login, Shell collapse/drawer/user-menu/breadcrumbs, DBRT01/02/04/03, SURT02/06/08/09, multi-viewport and axe WCAG 2.1 AA checks.',
  );
} finally {
  await browser?.close();
  await new Promise((resolve) => server.close(resolve));
}
