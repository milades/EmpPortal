# وضعیت پیاده‌سازی EmpPortal

## خط مبنای رابط کاربری

- پوسته ناحیه احراز هویت‌شده مطابق الگوی TailAdmin Community و با کامپوننت‌های بومی Blazor
  برای اجرای کاملاً آفلاین در اینترانت پیاده شده است.
- داشبورد RTL به سه شاخص اصلی، فرم‌های اخیر و اطلاعات ضروری نشست محدود شده است؛ مدل ذخیره و
  شخصی‌سازی ویجت‌ها عمداً برای فاز بعد در نظر گرفته شده است.
- صفحه ورود در عرض و ارتفاع‌های متداول کاملاً وسط‌چین است؛ ارسال تکراری Static SSR مهار شده و
  تأییدهای خروج، بایگانی و حذف با SweetAlert2 محلی و بدون وابستگی اینترنت انجام می‌شوند.
- پنجره قطع/بازیابی ارتباط Blazor به‌طور کامل فارسی، راست‌چین، Responsive و دارای شمارنده با ارقام
  فارسی است و همه حالت‌های اتصال مجدد، توقف و ادامه نشست را پوشش می‌دهد.
- همه ورودی‌های تاریخ و ساعت کسب‌وکاری در UI با تقویم شمسی نمایش داده می‌شوند و در مرز
  Application/Database همچنان به‌شکل ISO و UTC ذخیره می‌شوند.

## فاز ۲ ـ فرم‌ساز داخلی

پیاده‌سازی نرم‌افزاری فاز ۲ تکمیل و در محیط توسعه به‌صورت end-to-end آزموده شده است:

- فرم‌ساز Interactive Server با Drag & Drop، جابه‌جایی جایگزین، صفحه/بخش، ۲۹ نوع المنت،
  اعتبارسنجی، شرط نمایش، محاسبه امن، پیش‌نمایش و طراحی Responsive RTL.
- چرخه Draft/Published/Paused/Archived، زمان‌بندی خودکار باز و بسته‌شدن و نسخه‌بندی immutable.
- حذف فیزیکی فقط برای Draft منتشرنشده، بدون پاسخ/پیش‌نویس و بدون سابقه نسخه منتشرشده؛ شرط‌ها در
  UI و سرویس سمت سرور دوباره بررسی و عملیات با `rowversion` و Audit محافظت می‌شود.
- فهرست مدیریت فرم‌ها با Query سمت سرور، مرتب‌سازی قطعی و صفحه‌بندی ۲۰تایی ارائه می‌شود؛ فرم
  بایگانی‌شده در Builder فقط‌خواندنی است و هیچ کنترل ذخیره یا ویرایش نمایش نمی‌دهد.
- ACL برای همه کاربران، کاربر یا نقش SQL و نقش‌های تفکیک‌شده طراحی، انتشار، گزارش و خروجی.
- تکمیل فرم، ذخیره پیش‌نویس، ثبت نهایی، کنترل هم‌زمانی و ثبت Audit.
- گزارش جدولی داخل پرتال، جزئیات/چاپ، Excel امن و PDF فارسی با فونت محلی.
- مدل Hybrid روی SQL Server شامل JSON نسخه/پاسخ و Answer Indexهای typed برای گزارش آینده.
- endpointهای JWT برای فهرست/دریافت فرم و ذخیره/ثبت پاسخ؛ دانلودهای مرورگر با Cookie امن.
- Migration و مجوزهای Runtime در بسته استقرار آفلاین به‌روزرسانی شده‌اند.

Workflow تأیید، فایل/امضا و نمودارهای آماری برای فازهای بعد ثبت شده‌اند. راهنمای کامل و Smoke Test در
`docs/PHASE2-FORM-BUILDER.md` قرار دارد.

## فاز ۱ ـ تکمیل‌شده و قابل آزمون محلی

- Clean Architecture روی .NET 10 با Domain، Application، Infrastructure، Web و چهار پروژه تست.
- Blazor Web App با Static SSR برای Account و Interactive Server برای ناحیه داخلی.
- ASP.NET Core Identity/EF Core/SQL Server برای User، Role، Security Stamp و Sign-in Cookie.
- Fake AD توسعه، SSO توسعه و Manual Login با UPN فرضی.
- Windows SSO endpoint و Provider واقعی LDAPS با Certificate Validation و DC Failover.
- نشست SQL با عمر مطلق ۱۸۰ دقیقه، Idle سی دقیقه و سقف سه نشست هم‌زمان.
- JWT کوتاه‌عمر برای API با `sid`، Authorization Version و ابطال سمت سرور؛ Token مرورگر در
  `localStorage` ذخیره، در Logout پاک و فقط با Header نوع Bearer به API ارسال می‌شود.
- خروج مستقل از Windows، ابطال Cookie/JWT، Rate Limit، Antiforgery و Security Headers.
- Roleهای SQL، Bootstrap مدیر اولیه، صفحه تنظیمات Runtime و Audit امنیتی.
- Bootstrap RTL، Tailwind pipeline، فونت Vazirmatn و تمام دارایی‌های Runtime به‌صورت محلی.
- Health endpoint، Migration idempotent، Publish Profile و اسکریپت IIS/ACL/HTTPS.

## Gateهای وابسته به زیرساخت سازمان

موارد زیر کدنویسی ناقص نیستند و فقط روی زیرساخت واقعی قابل نهایی‌شدن‌اند:

- FQDN نهایی، دو یا چند DC، Base DN، وضعیت Forest/Trust و UPN suffixها.
- gMSA، SPN و Chrome GPO برای تأیید Kerberos و سیاست NTLM.
- Certificateهای Production و محل پایدار Data Protection Key Ring.
- SQL HA/Backup، RPO/RTO، حساب Migration و حساب Runtime.
- تعداد کاربر و Peak Concurrent Circuit برای Load/Capacity Test.
- feed داخلی npm برای Restore کنترل‌شده وابستگی‌های قفل‌شده؛ `pnpm-lock.yaml` و همه دارایی‌های
  Runtime موردنیاز اجرای آفلاین در Repository وجود دارند.
- UAT سناریوهای Disabled/Locked/Expired و قطع دسترسی DC روی IIS Domain-joined.

تا زمان عبور این Gateها، Artifact برای Production «Release Approved» محسوب نمی‌شود؛ با این
حال مسیر توسعه محلی و اجرای کامل جریان فاز اول مستقل از AD واقعی فراهم است.
