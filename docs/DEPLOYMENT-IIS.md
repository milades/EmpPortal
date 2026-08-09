# راهنمای استقرار آفلاین روی IIS

## پیش‌نیاز سرور

- Windows Server 2022 به‌روز و عضو Domain.
- IIS با Role Serviceهای `Windows Authentication`، WebSocket و Static Content.
- .NET 10 Hosting Bundle با Patch امنیتی مصوب سازمان.
- SQL Server 2022 و Database ایجادشده با Collation مورد تأیید DBA.
- Certificate معتبر HTTPS، Certificate امضای JWT و Certificate رمزنگاری Data Protection.
- LDAPS روی حداقل دو DC با Chain مورد اعتماد Local Computer.
- ترجیحاً gMSA مستقل برای App Pool و SPNهای `HTTP/<fqdn>` و `HTTP/<short-name>`.

## ساخت Artifact

روی Build Agent دارای feed داخلی NuGet/npm:

```powershell
pnpm install
pnpm css:build
.\deploy\Publish-Intranet.ps1
```

نبود `pnpm-lock.yaml` یک Gate انتشار است: اولین restore از feed داخلی باید lockfile را تولید
و پس از بازبینی Commit کند؛ از آن پس Build Agent فقط با `--frozen-lockfile` اجرا می‌شود.

فایل `sha256-manifest.json` داخل Artifact برای کنترل تمامیت ساخته می‌شود. Artifact همان
باینری تأییدشده UAT است و در Production دوباره Build نمی‌شود.

## دیتابیس

Migration در Startup اجرا نمی‌شود. حساب Deployment ابتدا
`deploy/sql/EmpPortal.Migrations.sql` را اجرا می‌کند. سپس اسکریپت
`deploy/sql/EmpPortal.RuntimePermissions.sql` با مقدار صحیح SQLCMD برای gMSA اجرا می‌شود.
حساب Runtime نباید `db_owner` یا مجوز DDL داشته باشد.

پیش از Migration باید Full Backup گرفته شود. سیاست Full/Differential/Log، RPO، RTO و
تست Restore هنوز باید توسط DBA سازمان تصویب شود.

## تنظیمات Production

از `deploy/appsettings.Production.example.json` یک فایل خارج از Git بسازید و آن را فقط
هنگام استقرار کنار Artifact قرار دهید. Thumbprintها، FQDN، DCها، Base DN و UPN مدیر اولیه
باید جایگزین شوند. ACL فایل تنظیمات فقط برای Administrators و هویت App Pool Read باشد.

مسیر پیشنهادی روی سرور `D:\Sites\EmpPortal\current\appsettings.Production.json` است؛ یعنی فایل باید
دقیقاً کنار `EmpPortal.Web.dll` و `web.config` نسخه منتشرشده قرار گیرد. فایل Development را به سرور
منتقل یا ویرایش نکنید. نمونه را پیش از اولین اجرای Application Pool کپی کنید:

```powershell
Copy-Item `
  -LiteralPath '.\deploy\appsettings.Production.example.json' `
  -Destination 'D:\Sites\EmpPortal\current\appsettings.Production.json'
```

در فایل Production این مقادیر حتماً با اطلاعات واقعی سازمان جایگزین شوند:

- `ConnectionStrings:PortalDatabase`: نام SQL Server/Instance و Database عملیاتی؛ در حالت
  `Trusted_Connection=True` اتصال با هویت gMSA مربوط به App Pool انجام می‌شود.
- `AllowedHosts`: فقط FQDN نهایی پورتال، بدون `https://` و بدون مسیر.
- `BootstrapAdministrator:Upn`: UPN واقعی حساب AD مدیر اولیه. هیچ Passwordی در تنظیمات ذخیره
  نمی‌شود و این کاربر در اولین ورود موفق نقش مدیر می‌گیرد.
- `ActiveDirectory`: نام DNS دامنه، Base DN و فهرست DCهای دارای LDAPS؛ ترتیب DCها ترتیب Failover است.
- `DataProtection`: مسیر پایدار Key Ring و Thumbprint گواهی رمزنگاری دارای Private Key در
  `LocalMachine\My`.
- `Jwt`: آدرس HTTPS پورتال به‌عنوان Issuer، شناسه API به‌عنوان Audience و Thumbprint گواهی مستقل
  امضای JWT در `LocalMachine\My`.
- `Forms:Pdf`: نوع مجوز واجدشرایط QuestPDF (`Community`، `Professional` یا `Enterprise`) با تأیید
  حقوقی/تدارکات و مسیر دو فونت محلی. Production با مقدار خالی یا `Evaluation` عمداً Fail-fast می‌شود.

در Production کاربر آزمایشی `admin@empportal.test` و پذیرش Password دلخواه اصلاً ثبت نمی‌شوند.
ورود دستی فقط با UPN و Password واقعی Active Directory و از طریق LDAPS اعتبارسنجی می‌شود.

### تقدم و روش تغییر تنظیمات

در اولین اجرا، جدول Runtime Settings خالی است و مقادیر پایه از `appsettings.Production.json` خوانده
می‌شوند. پس از ورود مدیر، مقادیر مجاز از مسیر `/admin/settings` قابل تغییرند و در SQL ثبت و Audit
می‌شوند. مقادیر ذخیره‌شده در SQL در راه‌اندازی بعدی بر فایل JSON تقدم دارند؛ بنابراین پس از اولین
ذخیره در پنل، تغییر همان کلید در فایل JSON اثری ندارد و باید از خود پنل تغییر داده شود.

تغییرات پنل شامل عنوان پورتال، Domain/Base DN/LDAPS، فعال‌بودن SSO یا ورود دستی، سیاست Session و
عمر JWT است و پس از ذخیره به Restart کنترل‌شده Application Pool نیاز دارد. Connection String،
`AllowedHosts`، مدیر Bootstrap، فهرست DCها، مسیر Key Ring و Thumbprint گواهی‌ها فقط در فایل
Production یا Secret Store مورد تأیید سازمان نگهداری می‌شوند. پس از تغییر فایل یا پنل:

```powershell
Restart-WebAppPool -Name 'EmpPortal'
```

برنامه در نبود Connection String، AD، Key Ring یا Certificateهای لازم عمداً Fail-fast
می‌شود. `TrustServerCertificate=True` در Production مجاز نیست.

## نصب IIS

در PowerShell با دسترسی Administrator و ابتدا با `-WhatIf`:

```powershell
.\deploy\Configure-Iis.ps1 `
  -PhysicalPath 'D:\Sites\EmpPortal\current' `
  -HostName 'portal.corp.example' `
  -CertificateThumbprint '<TLS thumbprint>' `
  -GmsaUserName 'CORP\EmpPortalGmsa$' `
  -WhatIf
```

پس از بازبینی، فرمان بدون `-WhatIf` اجرا می‌شود. Anonymous و Windows Authentication هر دو
برای Site فعال می‌شوند: صفحات عمومی با Anonymous باز هستند و فقط `/auth/sso` از scheme
`WindowsSso` Challenge می‌گیرد. Logout برنامه هیچ تغییری در Session ویندوز نمی‌دهد.

برای Chrome باید URL پرتال در AuthServerAllowlist سازمان و Intranet Zone قرار گیرد. Kerberos
باید با `klist` و لاگ IIS تأیید شود؛ فعال‌بودن NTLM fallback یک تصمیم صریح Security Team است.

## Smoke Test و Rollback

1. `/health/live` و `/health/ready` هر دو `200` بدهند.
2. SSO، ورود دستی UPN/Password، داشبورد و Logout آزموده شوند.
3. صدور JWT از endpoint محافظت‌شده و فراخوانی `/api/me` آزموده شود.
4. Disabled کردن حساب آزمایشی، رد Cookie/JWT و Revoke نشست‌ها تأیید شود.
5. Audit ورود موفق/ناموفق، خروج و تغییر تنظیمات در SQL کنترل شود.
6. یک فرم آزمایشی ساخته و منتشر شود؛ ثبت پاسخ، گزارش، Excel و PDF آن با نقش‌های مجاز کنترل شوند.

برای Rollback، binding IIS به پوشه Artifact قبلی برمی‌گردد. Rollback دیتابیس فقط مطابق
اسکریپت و Backup تأییدشده DBA انجام می‌شود؛ Downgrade خودکار Migration وجود ندارد.

راهنمای آموزشی گام‌به‌گام ۰ تا ۱۰۰ (نیازمندی‌ها، بسته انتشار، Cutover و عیب‌یابی) در
`docs/PRODUCTION-OFFLINE-ROLLOUT.md` آمده است.

فرم تکمیل‌شونده عملیاتی (با پر کردن کامل آن ابهام‌ها رفع و Go/No-Go صادر می‌شود) در
`docs/PRODUCTION-FILLABLE-CHECKLIST.md` است.
