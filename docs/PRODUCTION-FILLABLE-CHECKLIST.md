# فرم تکمیل‌شونده استقرار عملیاتی EmpPortal

**هدف این فرم:** با پر کردن کامل همه خانه‌ها و تیک زدن Gateها، دیگر ابهامی برای اجرای
بدون مشکل برنامه روی محیط عملیاتی آفلاین باقی نماند.

**قانون:** تا وقتی بخش «Gate نهایی Go / No-Go» همه تیک‌ها را نگرفته، App Pool را Start نکنید.

**راهنمای آموزشی همراه:** `docs/PRODUCTION-OFFLINE-ROLLOUT.md`

**نسخه Artifact هدف:** ______________________  
**تاریخ تکمیل فرم:** __________ / __________ / __________  
**تکمیل‌کننده:** ______________________  
**تأییدکننده نهایی (عملیات/امنیت):** ______________________

---

## نحوه استفاده

1. این فایل را کپی کنید (مثلاً خارج از Git در Share امن عملیات).
2. همه خانه‌های `________________` را با مقدار واقعی سازمان پر کنید.
3. هر ردیف «تأیید» را فقط وقتی تیک بزنید که واقعاً در محیط تست/سرور اثبات شده باشد.
4. در پایان، بخش «خروجی‌های آماده کپی» را عیناً در سرور استفاده کنید.
5. سپس Smoke Test انتهای فرم را یکی‌یکی پاس کنید.

علامت‌ها:
- `☐` = هنوز انجام نشده
- `☑` = انجام و تأیید شد (دستی عوض کنید)

---

# بخش A ـ هویت محیط و نام‌گذاری

> این بخش مبنای همه نام‌های بعدی است. اول این را کامل کنید.

| # | فیلد | مقدار واقعی شما | مثال فقط برای فهم |
|---|---|---|---|
| A1 | نام کوتاه سازمان / NetBIOS دامنه | ______________________________ | `CORP` یا `MILAD` |
| A2 | Domain FQDN | ______________________________ | `corp.example` |
| A3 | Base DN | ______________________________ | `DC=corp,DC=example` |
| A4 | FQDN نهایی پورتال (بدون https) | ______________________________ | `portal.corp.example` |
| A5 | URL کامل HTTPS | `https://`________________________ | `https://portal.corp.example` |
| A6 | نام کوتاه DNS (اگر دارید) | ______________________________ | `portal` |
| A7 | نام Site در IIS | `EmpPortal` (پیشنهادی) / ________ | `EmpPortal` |
| A8 | نام App Pool | `EmpPortal` (پیشنهادی) / ________ | `EmpPortal` |
| A9 | نام Database | `EmpPortal` (پیشنهادی) / ________ | `EmpPortal` |
| A10 | نام gMSA (بدون دامنه) | ______________________________ | `EmpPortalGmsa` |
| A11 | gMSA کامل (DOMAIN\Name$) | `\`______\`\`________________$` | `CORP\EmpPortalGmsa$` |
| A12 | UPN مدیر Bootstrap | ______________________________ | `portal.admin@corp.example` |
| A13 | نام نمایشی همان مدیر (اختیاری) | ______________________________ | علی رضایی |

**فرمول نام gMSA:**  
`{A1}\{A10}$`  
مثال: اگر A1=`MILAD` و A10=`EmpPortalGmsa` → `MILAD\EmpPortalGmsa$`

**تأیید A**
- ☐ A1 تا A12 پر شده‌اند و با تیم AD/عملیات هماهنگ شده‌اند
- ☐ FQDN پورتال (A4) در Certificate TLS هم آمده یا با SAN پوشش داده می‌شود
- ☐ UPN مدیر Bootstrap (A12) حساب واقعی AD است و Password آن را می‌دانید (در این فرم نوشته نشود)

---

# بخش B ـ سرور وب (IIS)

| # | فیلد / سؤال | مقدار / پاسخ |
|---|---|---|
| B1 | نام یا IP سرور وب | ______________________________ |
| B2 | نسخه Windows Server | ______________________________ |
| B3 | سرور عضو Domain است؟ | ☐ بله / ☐ خیر (اگر خیر، Stop) |
| B4 | IIS نصب است؟ | ☐ بله |
| B5 | Static Content | ☐ فعال |
| B6 | Windows Authentication | ☐ فعال |
| B7 | WebSocket Protocol | ☐ فعال |
| B8 | .NET 10 Hosting Bundle نصب است؟ | ☐ بله ـ نسخه دقیق: ______________ |
| B9 | مسیر Artifact/سایت (`current`) | ______________________________ |
| B10 | مسیر releases (نسخه‌ها) | ______________________________ |
| B11 | مسیر Key Ring (خارج از سایت) | ______________________________ |
| B12 | پورت HTTPS | `443` / ________ |
| B13 | `ASPNETCORE_ENVIRONMENT` | باید `Production` باشد ☐ تأیید |

پیشنهاد آماده (در صورت توافق عملیات، عیناً می‌توانید استفاده کنید):

```text
B9  = D:\Sites\EmpPortal\current
B10 = D:\Sites\EmpPortal\releases
B11 = D:\EmpPortalData\DataProtectionKeys
```

**تأیید B**
- ☐ Hosting Bundle و WebSocket بدون آن‌ها سایت/Blazor پایدار بالا نمی‌آید
- ☐ مسیر B9 و B11 ساخته شده و روی دیسک مناسب است
- ☐ روی این سرور قرار نیست Node/pnpm/SDK برای Build نصب شود (فقط Runtime)

---

# بخش C ـ SQL Server

| # | فیلد / سؤال | مقدار / پاسخ |
|---|---|---|
| C1 | نام SQL Server / Instance | ______________________________ |
| C2 | Database Name | همان A9: ______________________ |
| C3 | Collation | ______________________________ |
| C4 | حساب Migration (چه کسی اجرا می‌کند) | ______________________________ |
| C5 | روش Auth حساب Migration | ☐ Windows / ☐ SQL Auth |
| C6 | Login Runtime = همان gMSA (A11)؟ | ☐ بله |
| C7 | Encrypt اتصال | باید `True` ☐ |
| C8 | TrustServerCertificate | باید `False` ☐ |
| C9 | گواهی SQL برای وب‌سرور trusted است؟ | ☐ بله |
| C10 | Full Backup قبل از Migration | ☐ برنامه دارد / مسیر: ____________ |
| C11 | RPO مصوب | ______ دقیقه/ساعت |
| C12 | RTO مصوب | ______ دقیقه/ساعت |
| C13 | محل نگهداری Backup | ______________________________ |

**Connection String نهایی (بعد از پر کردن C1/C2 بسازید):**

```text
Server={C1};Database={C2};Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True
```

مقدار نهایی شما:

```text
________________________________________________________________________________
```

**تأیید C**
- ☐ Database ایجاد شده
- ☐ Login ویندوزی برای gMSA (A11) در Instance ساخته شده
- ☐ حساب Runtime `db_owner` نیست
- ☐ Connection String بالا تست اتصال از وب‌سرور با هویت gMSA موفق بوده (یا در Cutover تست می‌شود)

---

# بخش D ـ Active Directory / SSO / LDAPS

| # | فیلد / سؤال | مقدار / پاسخ |
|---|---|---|
| D1 | Domain FQDN | کپی از A2: ______________________ |
| D2 | Base DN | کپی از A3: ______________________ |
| D3 | DC1 (FQDN با LDAPS) | ______________________________ |
| D4 | DC2 (FQDN با LDAPS) | ______________________________ |
| D5 | پورت LDAPS | `636` / ________ |
| D6 | از وب‌سرور به DC1:636 باز است؟ | ☐ بله |
| D7 | از وب‌سرور به DC2:636 باز است؟ | ☐ بله |
| D8 | Chain اعتماد گواهی LDAPS روی وب‌سرور | ☐ تأیید PKI |
| D9 | gMSA ساخته شده و وب‌سرور اجازه Retrieve دارد؟ | ☐ بله |
| D10 | SPN کامل | `HTTP/`{A4} = `HTTP/`______________ |
| D11 | SPN کوتاه (اگر لازم) | `HTTP/`{A6} = `HTTP/`______________ |
| D12 | SPN روی کدام حساب ثبت شده؟ | باید gMSA/سرویس مرتبط: __________ |
| D13 | DNS A یا CNAME برای A4 | نوع: ____ مقدار: ________________ |
| D14 | Intranet Zone برای URL پورتال در GPO | ☐ تنظیم شده |
| D15 | Chrome/Edge AuthServerAllowlist شامل A4 | ☐ تنظیم شده |
| D16 | سیاست NTLM fallback | ☐ فقط Kerberos / ☐ Kerberos+NTLM مجاز |
| D17 | تصمیم D16 توسط Security امضا شده؟ | ☐ بله ـ نام: ____________________ |
| D18 | SSO در Cutover تست می‌شود؟ | ☐ بله / ☐ فعلاً فقط Manual Login |
| D19 | Manual Login حتماً فعال می‌ماند؟ | ☐ بله (توصیه برای روز اول) |

**تأیید D**
- ☐ حداقل یک مسیر ورود روز اول قطعی است (Manual یا SSO)
- ☐ اگر SSO روز اول اجباری است، D9 تا D15 همه سبز هستند
- ☐ LDAPS از وب‌سرور با ابزار سازمانی (LDP/تست Bind) یک‌بار موفق شده

---

# بخش E ـ گواهی‌ها (۳ نقش)

> Password/Private Key را در این فرم ننویسید. فقط Thumbprint و وضعیت.

## E1) گواهی TLS / HTTPS سایت

| فیلد | مقدار |
|---|---|
| Subject / CN یا SAN شامل A4؟ | ☐ بله ـ توضیح: ____________________ |
| Thumbprint (بدون فاصله) | ________________________________ |
| HasPrivateKey | ☐ بله |
| NotAfter (تاریخ انقضا) | __________ / __________ / __________ |
| نصب در `LocalMachine\My` وب‌سرور | ☐ بله |
| قرار است در Binding IIS استفاده شود | ☐ بله |

## E2) گواهی Data Protection

| فیلد | مقدار |
|---|---|
| Subject | ________________________________ |
| Thumbprint | ________________________________ |
| HasPrivateKey | ☐ بله |
| NotAfter | __________ / __________ / __________ |
| App Pool/gMSA روی Private Key = Read | ☐ بله |
| مقدار تنظیمات `DataProtection:CertificateThumbprint` | همان Thumbprint بالا ☐ |

## E3) گواهی امضای JWT

| فیلد | مقدار |
|---|---|
| Subject | ________________________________ |
| Thumbprint | ________________________________ |
| HasPrivateKey | ☐ بله |
| NotAfter | __________ / __________ / __________ |
| App Pool/gMSA روی Private Key = Read | ☐ بله |
| مقدار تنظیمات `Jwt:SigningCertificateThumbprint` | همان Thumbprint بالا ☐ |

**اگر فعلاً کمتر از ۳ گواهی دارید (موقتی):**

| سؤال | پاسخ |
|---|---|
| آیا Security استفاده موقت از گواهی مشترک را کتباً تأیید کرده؟ | ☐ بله / ☐ خیر (اگر خیر، Stop) |
| کدام Thumbprint برای کدام نقش؟ | TLS:____ DP:____ JWT:____ |

**تأیید E**
- ☐ هر سه نقش Thumbprint دارند
- ☐ هیچ گواهی منقضی نیست
- ☐ ACL Private Key برای gMSA ست شده

---

# بخش F ـ Key Ring و ACL فایل‌ها

| # | مورد | مقدار / تأیید |
|---|---|---|
| F1 | مسیر Key Ring | کپی از B11: ______________________ |
| F2 | پوشه ساخته شد | ☐ |
| F3 | Inheritance شکسته و محدود شد | ☐ |
| F4 | gMSA (A11) دسترسی Modify روی Key Ring | ☐ |
| F5 | Administrators دسترسی کامل | ☐ |
| F6 | مسیر سایت (B9) برای gMSA حداقل RX | ☐ (اسکریپت IIS هم می‌دهد) |
| F7 | ACL روی `appsettings.Production.json` فقط Admin + gMSA Read | ☐ |
| F8 | Key Ring داخل پوشه Artifact/current نیست | ☐ تأیید خارج بودن |

**تأیید F:** ☐ همه موارد سبز است

---

# بخش G ـ QuestPDF / خروجی PDF

| # | مورد | مقدار |
|---|---|---|
| G1 | نوع License قطعی برای استارت | ☐ Community / ☐ Professional / ☐ Enterprise |
| G2 | تأیید حقوقی/تدارکات (حتی اگر موقت) | ☐ انجام شد ـ توضیح کوتاه: __________ |
| G3 | RegularFontPath | `wwwroot/fonts/Vazirmatn-Regular.ttf` ☐ |
| G4 | BoldFontPath | `wwwroot/fonts/Vazirmatn-Bold.ttf` ☐ |
| G5 | فونت‌ها داخل Artifact هستند | ☐ |

> مقدار `Evaluation` یا خالی = برنامه در Production بالا نمی‌آید.

**تأیید G:** ☐ License غیر Evaluation انتخاب شده

---

# بخش H ـ Artifact و انتقال آفلاین

| # | مورد | مقدار / تأیید |
|---|---|---|
| H1 | ریپو / Commit یا Tag | ______________________________ |
| H2 | ماشین Build | ______________________________ |
| H3 | `pnpm css:build` انجام شد | ☐ |
| H4 | `.\deploy\Publish-Intranet.ps1` موفق | ☐ |
| H5 | مسیر Artifact روی Build | ______________________________ |
| H6 | وجود `EmpPortal.Web.dll` | ☐ |
| H7 | وجود `sha256-manifest.json` | ☐ |
| H8 | همراه بودن `EmpPortal.Migrations.sql` | ☐ |
| H9 | همراه بودن `EmpPortal.RuntimePermissions.sql` | ☐ |
| H10 | همراه بودن `Configure-Iis.ps1` | ☐ |
| H11 | روش انتقال آفلاین | ☐ USB امن / ☐ Share / ☐ دیگر: ____ |
| H12 | کپی روی سرور در B10/B9 انجام شد | ☐ |
| H13 | هش حداقل DLL اصلی با manifest یکی است | ☐ |

**تأیید H:** ☐ Artifact نهایی قفل و منتقل شده

---

# بخش I ـ خروجی آماده کپی ۱: `appsettings.Production.json`

فایل را بسازید در:

`{B9}\appsettings.Production.json`

مقادیر داخل `{}` را از بخش‌های بالا جایگزین کنید، بعد کل JSON را در فایل بگذارید:

```json
{
  "ConnectionStrings": {
    "PortalDatabase": "Server={C1};Database={C2};Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True"
  },
  "AllowedHosts": "{A4}",
  "BootstrapAdministrator": {
    "Upn": "{A12}"
  },
  "ActiveDirectory": {
    "DomainFqdn": "{A2}",
    "BaseDn": "{A3}",
    "DomainControllers": [
      "{D3}",
      "{D4}"
    ],
    "LdapsPort": 636,
    "OperationTimeoutSeconds": 10
  },
  "DataProtection": {
    "KeyRingPath": "{B11}",
    "CertificateThumbprint": "{E2 Thumbprint}"
  },
  "Jwt": {
    "Issuer": "{A5}",
    "Audience": "EmpPortal.Api",
    "AccessTokenMinutes": 5,
    "SigningCertificateThumbprint": "{E3 Thumbprint}"
  },
  "Forms": {
    "Pdf": {
      "License": "{G1}",
      "RegularFontPath": "wwwroot/fonts/Vazirmatn-Regular.ttf",
      "BoldFontPath": "wwwroot/fonts/Vazirmatn-Bold.ttf"
    }
  }
}
```

**نسخه نهایی پرشده شما (اینجا Paste کنید تا مبهم نماند):**

```json




```

**تأیید I**
- ☐ هیچ `corp.example` / `REPLACE_` / مقدار نمونه باقی نمانده
- ☐ فایل کنار `EmpPortal.Web.dll` است
- ☐ ACL فایل محدود شده

---

# بخش J ـ خروجی آماده کپی ۲: دستورات SQL

### J1) Login gMSA (سطح Instance — DBA)

```sql
CREATE LOGIN [{A11}] FROM WINDOWS;
-- مثال: CREATE LOGIN [CORP\EmpPortalGmsa$] FROM WINDOWS;
```

دستور نهایی شما:

```sql
____________________________________________________________
```

### J2) Migration

```powershell
sqlcmd -S {C1} -d {C2} -E -i .\EmpPortal.Migrations.sql
```

دستور نهایی شما:

```powershell
____________________________________________________________
```

### J3) Runtime Permissions

```powershell
sqlcmd -S {C1} -d {C2} -E -v ApplicationLogin="{A11}" -i .\EmpPortal.RuntimePermissions.sql
```

توجه: در مقدار `ApplicationLogin` بک‌اسلش را مطابق SQLCMD escape کنید  
(معمولاً `DOMAIN\\Name$`).

دستور نهایی شما:

```powershell
____________________________________________________________
```

**تأیید J**
- ☐ Backup قبل از Migration گرفته شد
- ☐ Migration بدون خطا تمام شد
- ☐ RuntimePermissions بدون خطا تمام شد

---

# بخش K ـ خروجی آماده کپی ۳: IIS

ابتدا Dry-Run:

```powershell
.\Configure-Iis.ps1 `
  -PhysicalPath '{B9}' `
  -HostName '{A4}' `
  -CertificateThumbprint '{E1 Thumbprint}' `
  -GmsaUserName '{A11}' `
  -WhatIf
```

سپس اجرای واقعی (همان بدون `-WhatIf`).

دستور نهایی شما:

```powershell




```

همچنین:

```powershell
# پس از تغییر تنظیمات
Restart-WebAppPool -Name '{A8}'
```

**تأیید K**
- ☐ WhatIf بازبینی شد
- ☐ اجرا بدون خطا بود
- ☐ Site و App Pool Started هستند
- ☐ `ASPNETCORE_ENVIRONMENT=Production`

---

# بخش L ـ Smoke Test اجباری (بدون این‌ها Go-Live نکنید)

از یک کلاینت داخلی Domain-joined (ترجیحاً غیر خود سرور):

| # | تست | نتیجه | شواهد / زمان |
|---|---|---|---|
| L1 | `https://{A4}/health/live` = 200 | ☐ Pass / ☐ Fail | __________ |
| L2 | `https://{A4}/health/ready` = 200 | ☐ Pass / ☐ Fail | __________ |
| L3 | صفحه Login باز می‌شود | ☐ Pass / ☐ Fail | __________ |
| L4 | ورود دستی با UPN واقعی AD | ☐ Pass / ☐ Fail | __________ |
| L5 | ورود با UPN مدیر Bootstrap نقش ادمین گرفت | ☐ Pass / ☐ Fail | __________ |
| L6 | داشبورد و منوها لود می‌شوند | ☐ Pass / ☐ Fail | __________ |
| L7 | Logout فقط پرتال را می‌بندد (ویندوز می‌ماند) | ☐ Pass / ☐ Fail | __________ |
| L8 | SSO (اگر در D18 فعال است) | ☐ Pass / ☐ Skip | __________ |
| L9 | ساخت فرم آزمایشی + انتشار | ☐ Pass / ☐ Fail | __________ |
| L10 | ثبت پاسخ کاربر عادی | ☐ Pass / ☐ Fail | __________ |
| L11 | Excel خروجی | ☐ Pass / ☐ Fail | __________ |
| L12 | PDF خروجی | ☐ Pass / ☐ Fail | __________ |
| L13 | `/admin/settings` ذخیره + Restart + اعمال | ☐ Pass / ☐ Fail | __________ |
| L14 | Audit ورود در SQL دیده می‌شود | ☐ Pass / ☐ Fail | __________ |

**تأیید L:** ☐ همه موارد غیر Skip برابر Pass هستند

---

# بخش M ـ Rollback از قبل rehearsed

| # | مورد | مقدار / تأیید |
|---|---|---|
| M1 | مسیر Artifact قبلی (یا برنامه اگر اولین بار است) | ______________________________ |
| M2 | روش برگشت IIS به Artifact قبلی مکتوب است | ☐ |
| M3 | Backup DB مربوط به قبل از Migration موجود است | ☐ |
| M4 | مالک تماس اضطراری | نام: ________ تلفن: ________ |
| M5 | مالک AD/PKI اضطراری | نام: ________ تلفن: ________ |
| M6 | مالک DBA اضطراری | نام: ________ تلفن: ________ |

**تأیید M:** ☐ اگر شکست بخورد می‌دانیم چه کنیم

---

# Gate نهایی Go / No-Go

فقط وقتی همه موارد زیر ☑ شد، اجازه Go-Live بدهید:

- ☐ بخش A کامل
- ☐ بخش B کامل
- ☐ بخش C کامل
- ☐ بخش D کامل (حداقل یک مسیر ورود قطعی)
- ☐ بخش E کامل
- ☐ بخش F کامل
- ☐ بخش G کامل
- ☐ بخش H کامل
- ☐ بخش I JSON نهایی Paste و تأیید شده
- ☐ بخش J SQL اجرا و سبز
- ☐ بخش K IIS اجرا و سبز
- ☐ بخش L Smoke Test سبز
- ☐ بخش M Rollback آماده

**تصمیم:** ☐ GO  ☐ NO-GO  

**امضا / تاریخ تأییدکننده:** ______________________  __________ / __________ / __________

---

# پیوست ۱ ـ ابهام‌زدایی سریع اگر گیر کردید

| مشکل | اولین چیزی که در این فرم چک کنید |
|---|---|
| سایت اصلاً بالا نمی‌آید | B8 Hosting Bundle، K محیط Production، I JSON |
| ready قرمز | C Connection String، C6 gMSA Login، C9 Trust SQL |
| ورود دستی Fail | D3/D4/D5/D8 LDAPS و Base DN |
| SSO Fail | D10–D15 SPN/DNS/GPO |
| بعد از Recycle Logout می‌شوید | F Key Ring + E2 گواهی Data Protection |
| PDF نمی‌دهد / استارت نمی‌شود | G1 License |
| مدیر نیستید | A12 دقیقاً همان UPN ورود باشد |
| دسترسی فایل/کلید | F4/F7 و Private Key ACL در E2/E3 |

---

# پیوست ۲ ـ چیزهایی که هرگز در این فرم نوشته نشوند

- Password کاربران
- Private Key / فایل `.pfx` password
- Connection String حاوی SQL Password (در این معماری نباید لازم باشد)
- Tokenها و Secretهای متفرقه

این‌ها فقط در Secret Store / فایل ACL‌دار سرور بمانند.

---

**پایان فرم.**  
پس از GO، یک کپی از همین فرم تکمیل‌شده را با تاریخ Artifact در بایگانی Change Record عملیات نگه دارید.
