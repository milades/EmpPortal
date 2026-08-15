# استقرار سریع Production

این سند مسیر اصلی و کوتاه استقرار است. اسناد دیگر فقط برای جزئیات و عیب‌یابی نگه‌داری می‌شوند.

## اصل طراحی تنظیمات

- تنها تنظیمات Bootstrap امنیتی در `appsettings.Production.json` می‌مانند: اتصال دیتابیس اصلی،
  `AllowedHosts`، مدیر اولیه، مسیر/گواهی Data Protection، Issuer/Audience و گواهی امضای JWT،
  Connection String منابع خارجی و لایسنس موجود Stimulsoft.
- تنظیمات عملیاتی پس از اولین ورود از `/admin/settings` در SQL ذخیره می‌شوند.
- مقادیر SQL در Startup بر JSON تقدم دارند.
- ذخیره تنظیماتی که به Startup وابسته‌اند، Restart کنترل‌شده برنامه را زمان‌بندی می‌کند؛ IIS
  برنامه را با مقادیر جدید بالا می‌آورد.

## مرحله ۱ ـ ساخت Artifact

روی Build Agent:

```powershell
.\deploy\Publish-Intranet.ps1
```

Artifact خروجی را بدون Build مجدد به سرور منتقل کنید.

## مرحله ۲ ـ ساخت تنها فایل تنظیمات

روی سرور و پس از نصب Certificateها:

```powershell
.\deploy\New-ProductionSettings.ps1 `
  -DestinationPath 'D:\Sites\EmpPortal\current\appsettings.Production.json' `
  -PortalDatabaseConnectionString 'Server=SQL01;Database=EmpPortal;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True' `
  -HostName 'portal.corp.example' `
  -BootstrapAdministratorUpn 'portal.admin@corp.example' `
  -DomainFqdn 'corp.example' `
  -BaseDn 'DC=corp,DC=example' `
  -DomainControllers 'dc01.corp.example','dc02.corp.example' `
  -DataProtectionCertificateThumbprint 'DATA_PROTECTION_CERTIFICATE_THUMBPRINT' `
  -JwtSigningCertificateThumbprint 'JWT_SIGNING_CERTIFICATE_THUMBPRINT'
```

لایسنس Stimulsoft توسط این اسکریپت خوانده، بازنویسی یا حذف نمی‌شود و مقدار فعلی پروژه حفظ می‌شود.

## مرحله ۳ ـ دیتابیس و IIS

با حساب Migration:

```powershell
sqlcmd -S SQL01 -d EmpPortal -E -b -i .\deploy\sql\EmpPortal.Migrations.sql
sqlcmd -S SQL01 -d EmpPortal -E -b -v ApplicationLogin="CORP\EmpPortalGmsa$" -i .\deploy\sql\EmpPortal.RuntimePermissions.sql
```

سپس IIS:

```powershell
.\deploy\Configure-Iis.ps1 `
  -PhysicalPath 'D:\Sites\EmpPortal\current' `
  -HostName 'portal.corp.example' `
  -CertificateThumbprint 'TLS_CERTIFICATE_THUMBPRINT' `
  -GmsaUserName 'CORP\EmpPortalGmsa$'
```

## مرحله ۴ ـ مدیریت از پرتال

1. با UPN مدیر اولیه وارد شوید.
2. صفحه `/admin/settings` را باز کنید.
3. تنظیمات گروه‌بندی‌شده را اصلاح و ذخیره کنید.
4. برای تنظیمات وابسته به Startup، برنامه خودکار Restart می‌شود.
5. `/health/live` و `/health/ready` را کنترل کنید.

برای عیب‌یابی و جزئیات امنیتی به `docs/PRODUCTION-OFFLINE-ROLLOUT.md` مراجعه کنید.
