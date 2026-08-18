# نقشه راه آموزشی استقرار ۰ تا ۱۰۰ EmpPortal (آفلاین / عملیاتی)

این سند برای تیم عملیات، امنیت، AD و DBA نوشته شده است. هدف: از صفر تا اولین اجرای
موفق روی محیط عملیاتی **بدون اینترنت روی سرور Production** و بدون نصب نرم‌افزار اضافه
روی سرور، جز مواردی که سازمان از قبل دارد (IIS، .NET Hosting Bundle، SQL Server، دامنه، SSL).

> اصل طلایی: روی سرور عملیاتی **Build نمی‌کنیم**. فقط Artifact آماده‌شده را کپی، تنظیم،
> Migration و Smoke Test می‌کنیم.

---

## فهرست سریع مراحل

| مرحله | عنوان | کجا انجام می‌شود |
|---:|---|---|
| ۰ | آماده‌سازی نیازمندی‌ها و جمع‌آوری اطلاعات | جلسات سازمانی |
| ۱ | ساخت بسته انتشار (Artifact) | ماشین توسعه / Build Agent با دسترسی feed |
| ۲ | انتقال آفلاین Artifact به سرور | رسانه امن / share داخلی |
| ۳ | آماده‌سازی SQL Server و Backup | سرور SQL |
| ۴ | گواهی‌ها و مسیرهای پایدار روی وب‌سرور | سرور IIS |
| ۵ | هویت App Pool، gMSA، SPN، ACL | AD + وب‌سرور |
| ۶ | استقرار فایل‌ها و `appsettings.Production.json` | سرور IIS |
| ۷ | اجرای Migration و مجوز Runtime | سرور SQL |
| ۸ | پیکربندی IIS با اسکریپت | سرور IIS |
| ۹ | Smoke Test کامل | کلاینت Domain-joined |
| ۱۰ | تثبیت، پایش و Rollback rehearsed | عملیات |

---

## مرحله ۰ ـ همه نیازمندی‌هایی که باید از قبل آماده شوند

قبل از هر دستور، این چک‌لیست را کامل کنید. اگر موردی خالی است، استقرار را شروع نکنید.

### ۰ـ۱. زیرساخت و نرم‌افزار (سرور وب)

| مورد | چیست؟ | از کجا؟ / چه کسی؟ | وضعیت |
|---|---|---|---|
| Windows Server 2022 (یا نسخه مصوب) | سیستم‌عامل وب‌سرور | عملیات زیرساخت | ☐ |
| عضویت در Domain | برای SSO و gMSA | AD Team | ☐ |
| IIS + Role Serviceها | `Web Server`، `Static Content`، `Windows Authentication`، `WebSocket Protocol` | Server Manager → Add Roles | ☐ |
| .NET 10 Hosting Bundle | Runtime لازم برای اجرای اپ ASP.NET Core روی IIS (Framework-dependent) | Microsoft Download / مخزن داخلی سازمان؛ روی سرور نصب شود | ☐ |
| ASPNETCORE_ENVIRONMENT | در IIS باید `Production` باشد (نه Development) | App Pool / web.config / Environment Variables | ☐ |
| مسیر سایت پیشنهادی | مثلاً `D:\Sites\EmpPortal\current` | عملیات | ☐ |
| مسیر داده پایدار | مثلاً `D:\EmpPortalData\DataProtectionKeys` (خارج از پوشه Artifact) | عملیات | ☐ |

**Hosting Bundle چیست؟**  
چون Artifact به‌صورت `--self-contained false` ساخته می‌شود، سرور باید Runtime .NET 10 را
داشته باشد. Hosting Bundle همان بسته رسمی مایکروسافت است که ASP.NET Core Module برای IIS
را هم نصب می‌کند. بدون آن سایت بالا نمی‌آید.

**چرا WebSocket؟**  
ناحیه داخلی پرتال با Blazor Interactive Server کار می‌کند و به WebSocket نیاز دارد.

### ۰ـ۲. دیتابیس

| مورد | چیست؟ | از کجا؟ | وضعیت |
|---|---|---|---|
| SQL Server 2022 | موتور دیتابیس | DBA | ☐ |
| Database خالی `EmpPortal` | پایگاه داده اختصاصی پرتال | DBA ایجاد می‌کند | ☐ |
| Collation مصوب سازمان | قواعد مرتب‌سازی/مقایسه متن | DBA | ☐ |
| حساب Migration | حساب با مجوز DDL برای اجرای `EmpPortal.Migrations.sql` | DBA (جدا از Runtime) | ☐ |
| حساب/لاگین Runtime | معمولاً همان gMSA وب‌سرور؛ فقط DML روی schemaها | DBA + AD | ☐ |
| Certificate اعتماد SQL | برای `Encrypt=True;TrustServerCertificate=False` | PKI / DBA | ☐ |
| سیاست Backup | Full / Differential / Log + محل نگهداری | DBA | ☐ |
| RPO / RTO مصوب | حداکثر داده قابل از دست رفتن و زمان برگشت | مدیریت + DBA | ☐ |

**چرا دو حساب؟**  
Migration فقط هنگام استقرار اجرا می‌شود و مجوز ساخت جدول می‌خواهد. حساب Runtime که App Pool
با آن به SQL وصل می‌شود نباید بتواند جدول بسازد/حذف کند (`db_owner` ممنوع).

### ۰ـ۳. Active Directory و احراز هویت

| مورد | چیست؟ | مثال | وضعیت |
|---|---|---|---|
| Domain FQDN | نام کامل DNS دامنه | `corp.example` | ☐ |
| Base DN | ریشه LDAP دامنه | `DC=corp,DC=example` | ☐ |
| حداقل دو DC با LDAPS | کنترل‌کننده دامنه با پورت ۶۳۶ و گواهی معتبر | `dc01.corp.example` | ☐ |
| Chain اعتماد LDAPS | گواهی DC در Trusted Root/Intermediate سرور وب | PKI | ☐ |
| UPN مدیر Bootstrap | اولین مدیر سیستم پرتال | `portal.admin@corp.example` | ☐ |
| gMSA برای App Pool | حساب ماشینی بدون Password دستی | `CORP\EmpPortalGmsa$` | ☐ |
| SPNها | برای Kerberos SSO | `HTTP/portal.corp.example` و `HTTP/portal` | ☐ |
| DNS A/CNAME | نام نهایی پورتال به IP وب‌سرور | `portal.corp.example` | ☐ |
| Chrome/Edge Intranet Zone + AuthServerAllowlist | برای SSO بدون Prompt مکرر | GPO امنیت | ☐ |
| تصمیم NTLM fallback | آیا اجازه Fallback به NTLM هست یا فقط Kerberos | Security Team | ☐ |

**LDAPS چیست؟**  
ورود دستی UPN/Password از طریق اتصال رمزشده به Active Directory روی پورت ۶۳۶ انجام می‌شود.
اگر گواهی DC نامعتبر باشد یا Chain روی وب‌سرور trusted نباشد، ورود دستی Fail می‌شود.

**gMSA چیست؟**  
Group Managed Service Account: هویت دامنه برای اجرای App Pool بدون ذخیره Password در سرور.
SQL با `Trusted_Connection=True` با همین هویت وصل می‌شود.

**SPN چیست؟**  
Service Principal Name؛ برای اینکه مرورگر Domain-joined بتواند با Kerberos به IIS SSO بدهد
باید روی حساب سرویس (معمولاً gMSA) ثبت شود.

### ۰ـ۴. گواهی‌ها (سه نقش جدا)

| نقش | کاربرد | کجا نصب شود | در تنظیمات |
|---|---|---|---|
| ۱) TLS / HTTPS سایت | رمزنگاری ترافیک مرورگر ↔ IIS | `LocalMachine\My` روی وب‌سرور + Binding IIS | پارامتر `-CertificateThumbprint` اسکریپت IIS |
| ۲) Data Protection | رمزنگاری Cookie/Keyهای ASP.NET Core بین Recycleها | `LocalMachine\My` + Private Key قابل خواندن برای App Pool | `DataProtection:CertificateThumbprint` |
| ۳) امضای JWT | امضای توکن API کوتاه‌عمر | `LocalMachine\My` + Private Key برای App Pool | `Jwt:SigningCertificateThumbprint` |

**چطور Thumbprint بگیریم؟**

```powershell
Get-ChildItem Cert:\LocalMachine\My |
  Select-Object Subject, Thumbprint, NotAfter, HasPrivateKey |
  Format-Table -AutoSize
```

Thumbprint را بدون فاصله و ترجیحاً Uppercase در تنظیمات بگذارید.

**نکته امنیتی:** ترجیحاً سه گواهی جدا؛ حداقل TLS از دو نقش دیگر جدا باشد. گواهی‌ها باید
Private Key داشته باشند و برای هویت App Pool در ACL Private Key دسترسی Read داده شود.

### ۰ـ۵. مجوز حقوقی PDF (QuestPDF)

| مورد | توضیح | وضعیت |
|---|---|---|
| نوع مجوز | `Community` فقط در صورت احراز شرایط رسمی QuestPDF؛ وگرنه `Professional` یا `Enterprise` | ☐ |
| تأیید حقوقی/تدارکات | Production با `Evaluation` یا خالی **عمداً استارت نمی‌شود** | ☐ |

### ۰ـ۶. ماشین ساخت Artifact (نه سرور عملیاتی)

| مورد | توضیح | وضعیت |
|---|---|---|
| Clone ریپو | `https://github.com/milades/EmpPortal` | ☐ |
| .NET SDK 10 | برای restore/build/test/publish | ☐ |
| دسترسی NuGet داخلی | restore آفلاین/داخلی با `packages.lock.json` | ☐ |
| Node.js LTS + pnpm | فقط برای `pnpm css:build` هنگام ساخت | ☐ |
| feed npm داخلی (در صورت نیاز) | چون سرور عملیاتی اینترنت ندارد؛ CSS از قبل در Artifact است | ☐ |

**روی سرور عملیاتی Node/pnpm لازم نیست.**

### ۰ـ۷. اطلاعات نهایی که باید روی کاغذ/Secure Note آماده باشد

قبل از نوشتن `appsettings.Production.json` این جدول را پر کنید:

```text
FQDN پورتال:                 ______________________________
URL کامل:                    https://_______________________
SQL Server/Instance:         ______________________________
Database Name:               EmpPortal
Domain FQDN:                 ______________________________
Base DN:                     ______________________________
DC1 (LDAPS):                 ______________________________
DC2 (LDAPS):                 ______________________________
Bootstrap Admin UPN:         ______________________________
gMSA:                        DOMAIN\Name$
TLS Thumbprint:              ______________________________
DataProtection Thumbprint:   ______________________________
JWT Signing Thumbprint:      ______________________________
Key Ring Path:               D:\EmpPortalData\DataProtectionKeys
QuestPDF License:            Community / Professional / Enterprise
Site Physical Path:          D:\Sites\EmpPortal\current
```

---

## مرحله ۱ ـ ساخت بسته انتشار (روی ماشین Build)

### ۱ـ۱. دریافت کد

```powershell
git clone https://github.com/milades/EmpPortal.git
cd EmpPortal
git checkout main
git pull
```

### ۱ـ۲. ساخت CSS (یک‌بار روی Build)

```powershell
pnpm install --frozen-lockfile
pnpm css:build
```

اگر feed داخلی جدا دارید، registry را طبق سیاست سازمان تنظیم کنید. خروجی CSS داخل
`wwwroot` قرار می‌گیرد و همراه Artifact به سرور می‌رود.

### ۱ـ۳. Publish رسمی

```powershell
.\deploy\Publish-Intranet.ps1
```

اسکریپت به‌ترتیب انجام می‌دهد:

1. `dotnet restore --locked-mode`
2. `dotnet build -c Release`
3. `dotnet test -c Release`
4. `dotnet publish` برای `win-x64` و Framework-dependent
5. ساخت `sha256-manifest.json` برای کنترل تمامیت فایل‌ها

خروجی پیش‌فرض:

```text
artifacts\publish\win-x64-YYYYMMDD-HHMMSS\
```

### ۱ـ۴. همراه Artifact چه چیزی ببرید؟

از همین پوشه Publish:

- همه فایل‌های منتشرشده (`EmpPortal.Web.dll`, `web.config`, `wwwroot`, …)
- `sha256-manifest.json`

از ریپو (کنار Artifact روی رسانه انتقال):

- `deploy\sql\EmpPortal.Migrations.sql`
- `deploy\sql\EmpPortal.RuntimePermissions.sql`
- `deploy\Configure-Iis.ps1`
- `deploy\appsettings.Production.example.json` (فقط به‌عنوان الگو؛ مقادیر واقعی جداگانه)

**هرگز** فایل‌های Development (`appsettings.Development.json`) و Secret واقعی را داخل Git نگذارید.

### ۱ـ۵. تأیید تمامیت (اختیاری ولی توصیه‌شده)

روی ماشین Build یا پس از کپی روی سرور:

```powershell
# نمونه: مقایسه هش یک فایل با manifest
Get-FileHash .\EmpPortal.Web.dll -Algorithm SHA256
```

با مقدار داخل `sha256-manifest.json` تطبیق دهید.

---

## مرحله ۲ ـ انتقال آفلاین به محیط عملیاتی

1. Artifact را Zip کنید یا روی share داخلی کپی کنید.
2. رسانه را با روش مصوب سازمان به سرور/اتاق عملیات ببرید.
3. روی سرور وب، پوشه‌ای مثل این بسازید:

```powershell
New-Item -ItemType Directory -Force -Path 'D:\Sites\EmpPortal\releases\2026-08-08'
New-Item -ItemType Directory -Force -Path 'D:\Sites\EmpPortal\current'
New-Item -ItemType Directory -Force -Path 'D:\EmpPortalData\DataProtectionKeys'
```

4. محتویات Artifact را در `releases\...` بریزید، سپس محتوای تأییدشده را به `current` کپی کنید
   (یا junction/symlink طبق استاندارد سازمان).

الگوی نسخه‌گذاری پوشه‌ها Rollback را ساده می‌کند: IIS فقط به `current` اشاره می‌کند.

---

## مرحله ۳ ـ آماده‌سازی SQL

### ۳ـ۱. ایجاد Database

توسط DBA (نمونه مفهومی):

```sql
CREATE DATABASE [EmpPortal];
-- Collation طبق استاندارد سازمان
```

### ۳ـ۲. Full Backup قبل از Migration

حتی برای Database خالی، عادت عملیاتی را رعایت کنید؛ برای استقرارهای بعدی الزامی است.

### ۳ـ۳. اجرای Migration با حساب Deployment

با SQLCMD یا SSMS و حساب Migration:

```powershell
sqlcmd -S SQL01 -d EmpPortal -E -i .\deploy\sql\EmpPortal.Migrations.sql
```

(`-E` یعنی Windows Auth؛ اگر حساب جدا دارید مطابق روش سازمان جایگزین کنید.)

این اسکریپت جداول Identity، Session، Audit، Settings و schema `forms` را می‌سازد.
**Migration داخل Startup برنامه اجرا نمی‌شود.**

### ۳ـ۴. ایجاد Login برای gMSA و مجوز Runtime

ابتدا در SQL Server سطح Instance، Login برای gMSA (توسط DBA):

```sql
CREATE LOGIN [CORP\EmpPortalGmsa$] FROM WINDOWS;
```

سپس اسکریپت مجوز Runtime را با نام صحیح اجرا کنید:

```powershell
sqlcmd -S SQL01 -d EmpPortal -E -v ApplicationLogin="CORP\EmpPortalGmsa$" -i .\deploy\sql\EmpPortal.RuntimePermissions.sql
```

این اسکریپت فقط این مجوزها را می‌دهد:

- `SELECT/INSERT/UPDATE/DELETE` روی `identity`, `security`, `portal`, `forms`
- `INSERT` روی `audit`

بدون `db_owner` و بدون DDL.

---

## مرحله ۴ ـ گواهی‌ها و مسیر Key Ring روی وب‌سرور

1. سه گواهی را در `LocalMachine\My` وارد کنید (یا تأیید کنید موجودند).
2. برای گواهی‌های Data Protection و JWT، به هویت App Pool/gMSA روی Private Key دسترسی Read بدهید
   (Certificates MMC → All Tasks → Manage Private Keys).
3. پوشه Key Ring را بسازید و ACL را محدود کنید:

```powershell
New-Item -ItemType Directory -Force -Path 'D:\EmpPortalData\DataProtectionKeys'
icacls 'D:\EmpPortalData\DataProtectionKeys' /inheritance:r
icacls 'D:\EmpPortalData\DataProtectionKeys' /grant 'CORP\EmpPortalGmsa$:(OI)(CI)(M)'
icacls 'D:\EmpPortalData\DataProtectionKeys' /grant 'Administrators:(OI)(CI)(F)'
```

**چرا خارج از پوشه سایت؟**  
با تعویض Artifact، Key Ring نباید پاک شود؛ وگرنه Cookieهای قبلی باطل/ناخوانا می‌شوند.

---

## مرحله ۵ ـ gMSA، SPN و DNS

### ۵ـ۱. gMSA (AD Team)

حساب gMSA را از قبل بسازید و وب‌سرور را مجاز به بازیابی Password آن کنید. نام نهایی را در
اسکریپت IIS و SQL استفاده کنید (مثلاً `CORP\EmpPortalGmsa$`).

### ۵ـ۲. SPN

روی حساب سرویس مرتبط با Kerberos (معمولاً همان gMSA):

```text
HTTP/portal.corp.example
HTTP/portal
```

(دستور دقیق `setspn` را AD Team طبق استاندارد سازمان اجرا می‌کند.)

### ۵ـ۳. DNS

رکورد `portal.corp.example` باید به IP وب‌سرور عملیاتی اشاره کند. کلاینت‌ها باید همین نام را
در نوار آدرس بزنند (نه IP خام)، مخصوصاً برای Kerberos و Certificate.

---

## مرحله ۶ ـ فایل تنظیمات Production

### ۶ـ۱. ساخت فایل خارج از Git

```powershell
Copy-Item `
  -LiteralPath '.\deploy\appsettings.Production.example.json' `
  -Destination 'D:\Sites\EmpPortal\current\appsettings.Production.json'
```

فایل باید **کنار** `EmpPortal.Web.dll` و `web.config` باشد.

### ۶ـ۲. پر کردن مقادیر واقعی (خط‌به‌خط)

```json
{
  "ConnectionStrings": {
    "PortalDatabase": "Server=SQL01;Database=EmpPortal;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True"
  },
  "AllowedHosts": "portal.corp.example",
  "BootstrapAdministrator": {
    "Upn": "portal.admin@corp.example"
  },
  "ActiveDirectory": {
    "DomainFqdn": "corp.example",
    "BaseDn": "DC=corp,DC=example",
    "DomainControllers": [ "dc01.corp.example", "dc02.corp.example" ],
    "LdapsPort": 636,
    "OperationTimeoutSeconds": 10
  },
  "DataProtection": {
    "KeyRingPath": "D:\\EmpPortalData\\DataProtectionKeys",
    "CertificateThumbprint": "THUMBPRINT_DATA_PROTECTION"
  },
  "Jwt": {
    "Issuer": "https://portal.corp.example",
    "Audience": "EmpPortal.Api",
    "AccessTokenMinutes": 5,
    "SigningCertificateThumbprint": "THUMBPRINT_JWT"
  },
  "Forms": {
    "Pdf": {
      "License": "Professional",
      "RegularFontPath": "wwwroot/fonts/Vazirmatn-Regular.ttf",
      "BoldFontPath": "wwwroot/fonts/Vazirmatn-Bold.ttf"
    }
  }
}
```

| کلید | معنی |
|---|---|
| `ConnectionStrings:PortalDatabase` | اتصال SQL با Windows Auth هویت App Pool؛ `TrustServerCertificate=True` در Production ممنوع |
| `AllowedHosts` | فقط Hostname بدون `https://` |
| `BootstrapAdministrator:Upn` | UPN مدیر اول؛ Password اینجا ذخیره نمی‌شود |
| `ActiveDirectory:*` | دامنه، Base DN، DCهای LDAPS و timeout |
| `DataProtection:*` | مسیر پایدار Key Ring + Thumbprint گواهی رمزنگاری |
| `Jwt:*` | Issuer=URL نهایی، Audience=شناسه API، Thumbprint امضا |
| `Forms:Pdf:License` | مجوز حقوقی QuestPDF |

### ۶ـ۳. ACL فایل تنظیمات

فقط Administrators و هویت App Pool باید Read داشته باشند؛ از Share عمومی خارج باشد.

### ۶ـ۴. تقدم تنظیمات بعد از Go-Live

- بار اول: از JSON خوانده می‌شود.
- پس از ذخیره در `/admin/settings`: مقادیر SQL مقدم‌اند.
- Connection String، Thumbprintها، DC list، Key Ring، Bootstrap UPN فقط از فایل/Secret Store.

بعد از هر تغییر فایل یا پنل:

```powershell
Restart-WebAppPool -Name 'EmpPortal'
```

---

## مرحله ۷ ـ پیکربندی IIS

با PowerShell **Administrator** روی وب‌سرور، اول Dry-Run:

```powershell
cd D:\Sites\EmpPortal\current   # یا جایی که Configure-Iis.ps1 را کپی کرده‌اید

.\Configure-Iis.ps1 `
  -PhysicalPath 'D:\Sites\EmpPortal\current' `
  -HostName 'portal.corp.example' `
  -CertificateThumbprint 'TLS_THUMBPRINT_HERE' `
  -GmsaUserName 'CORP\EmpPortalGmsa$' `
  -WhatIf
```

پس از بازبینی، بدون `-WhatIf` اجرا کنید.

اسکریپت چه کار می‌کند؟

- App Pool بدون Managed CLR (برای ASP.NET Core) و `AlwaysRunning`
- هویت gMSA
- Site با Host Header
- Binding HTTPS + SNI
- فعال‌سازی Anonymous + Windows Authentication در سطح Site
- ACL خواندن/اجرا روی پوشه سایت برای هویت App Pool
- Start کردن Site و Pool

**رفتار Auth:** صفحات عمومی با Anonymous باز می‌شوند؛ فقط مسیر `/auth/sso` Challenge ویندوزی
می‌گیرد. Logout پرتال Session ویندوز را قطع نمی‌کند.

Environment را Production کنید (اگر در `web.config`/IIS تنظیم نشده):

```powershell
# نمونه مفهومی در Configuration Editor / Environment Variables اپ‌پول:
# ASPNETCORE_ENVIRONMENT = Production
```

---

## مرحله ۸ ـ اولین روشن‌کردن و Smoke Test

### ۸ـ۱. Health

از سرور یا کلاینت داخلی:

```text
https://portal.corp.example/health/live   → 200
https://portal.corp.example/health/ready  → 200 (وابسته به SQL)
```

اگر Site بالا نمی‌آید: Event Viewer، لاگ stdout ASP.NET Core، و Fail-fast تنظیمات
(Connection String / AD / Certificate / PDF License) را بررسی کنید.

### ۸ـ۲. ورود

1. صفحه Login را باز کنید.
2. **SSO** (اگر Kerberos/GPO آماده است) را از کلاینت Domain-joined تست کنید.
3. **ورود دستی** با UPN واقعی و Password AD را تست کنید.
4. با `BootstrapAdministrator:Upn` اولین ورود موفق باید نقش `SystemAdministrator` بدهد.
5. داشبورد، منوها و Logout را تست کنید (Logout نباید لاگین ویندوز را ببندد).

### ۸ـ۳. امنیت نشست

- چند نشست هم‌زمان طبق سقف تنظیمات
- غیرفعال کردن یک حساب آزمایشی در AD و اطمینان از قطع دسترسی در بازه SLA
- در DevTools مرورگر تأیید شود JWT با کلید `empportal.auth.access-token` در `localStorage` است،
  Cookie جداگانه‌ای برای JWT وجود ندارد و Logout آن را حذف می‌کند

### ۸ـ۴. فرم‌ساز / ثبت‌نام‌ها

1. ساخت یک فرم آزمایشی (جشنواره/رویداد)
2. انتشار و ACL
3. ثبت پیش‌نویس و ثبت نهایی با کاربر عادی
4. گزارش جدولی، Excel، PDF، چاپ
5. نقش‌های محدود (`SubmissionViewer` نباید بتواند Publish کند)

### ۸ـ۵. تنظیمات پنل

`/admin/settings` را باز کنید، یک مقدار غیرحساس (مثلاً عنوان پرتال) را ذخیره و پس از Restart
تأیید کنید اعمال شده است.

### ۸ـ۶. Audit

در SQL، رخدادهای Login موفق/ناموفق، Logout و تغییر تنظیمات را کنترل کنید.

---

## مرحله ۹ ـ تثبیت عملیاتی

| کار | توضیح |
|---|---|
| پایش فشرده ۲۴ساعته | Health، Event Log، خطاهای LDAPS/SQL |
| Backup Job | Full/Diff/Log طبق RPO |
| تست Restore | حداقل یک‌بار روی محیط آزمایش |
| Runbook Rollback | پوشه Artifact قبلی + Backup DB |
| آموزش مدیر سیستم | Bootstrap، نقش‌ها، انتشار فرم، تنظیمات |
| ثبت نسخه Artifact | نام پوشه + هش manifest در Change Record |

### Rollback سریع برنامه (بدون برگشت DB)

1. Stop App Pool
2. اشاره IIS/پوشه `current` به Artifact قبلی تأییدشده
3. Start App Pool
4. Smoke Test کوتاه

### Rollback دیتابیس

فقط با Backup/اسکریپت DBA. Downgrade خودکار Migration وجود ندارد.

---

## مرحله ۱۰ ـ چیزهایی که عمداً روی Production نیست / نباید باشد

| مورد | دلیل |
|---|---|
| Fake AD / `admin@empportal.test` | فقط Development |
| `TrustServerCertificate=True` | تضعیف TLS به SQL |
| اجرای Migration در Startup | کنترل‌نشده و خطرناک |
| نصب Node/pnpm روی سرور عملیاتی | لازم نیست |
| Build مجدد روی Production | Artifact باید همان UAT باشد |
| گذاشتن Secret در Git | ممنوع |
| QuestPDF = `Evaluation` | Fail-fast عمدی |

---

## پیوست الف ـ ترتیب پیشنهادی یک روز Cutover

1. Freeze تغییر کد؛ Artifact نهایی را قفل کنید  
2. Backup SQL  
3. کپی Artifact به `releases\...` و آماده‌سازی `current`  
4. Migration + RuntimePermissions  
5. تأیید گواهی‌ها، Key Ring، ACL، gMSA، SPN، DNS  
6. نوشتن `appsettings.Production.json`  
7. `Configure-Iis.ps1`  
8. Health + Login Bootstrap Admin  
9. Smoke Test فرم و Audit  
10. اعلام Go-Live محدود / پایش  

---

## پیوست ب ـ عیب‌یابی سریع

| نشانه | احتمال علت |
|---|---|
| سایت ۵۰۰ بلافاصله | تنظیمات ناقص، Thumbprint غلط، PDF License، نبود Connection String |
| `/health/ready` قرمز | SQL در دسترس نیست / Login gMSA / Encrypt/Trust |
| SSO Prompt مکرر یا Fail | SPN، DNS نام، Intranet Zone، gMSA، Windows Auth |
| ورود دستی Fail | LDAPS، گواهی DC، Base DN، Firewall 636 |
| Cookie بعد از Recycle می‌پرد | Key Ring ناپایدار یا گواهی Data Protection/ACL |
| PDF ساخته نمی‌شود | License نامعتبر یا مسیر فونت |
| کاربر مدیر نیست | UPN Bootstrap با حساب ورود یکی نیست / هنوز اولین ورود موفق نشده |

---

## پیوست ج ـ مالکیت‌ها (RACI خلاصه)

| حوزه | مالک پیشنهادی |
|---|---|
| Artifact و نسخه | توسعه / Release Manager |
| IIS و Hosting Bundle | عملیات وب |
| SQL / Backup / Migration | DBA |
| gMSA / SPN / LDAPS / GPO | AD / Security |
| گواهی TLS و داخلی | PKI / Security |
| QuestPDF License | حقوقی / تدارکات |
| Smoke Test پذیرش | مالک محصول + عملیات |

---

پس از تکمیل چک‌لیست مرحله ۰، می‌توانید مرحله ۱ (ساخت Artifact) را شروع کنید.
برای جلسه Kickoff و رفع قطعی ابهام‌ها، فرم تکمیل‌شونده
`docs/PRODUCTION-FILLABLE-CHECKLIST.md` را پر کنید؛ تا Gate نهایی GO نشود استقرار را
شروع نکنید.
