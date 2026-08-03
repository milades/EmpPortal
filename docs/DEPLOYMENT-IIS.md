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

برای Rollback، binding IIS به پوشه Artifact قبلی برمی‌گردد. Rollback دیتابیس فقط مطابق
اسکریپت و Backup تأییدشده DBA انجام می‌شود؛ Downgrade خودکار Migration وجود ندارد.
