# ADR-0003: یکپارچه‌سازی Active Directory

- وضعیت: پذیرفته‌شده مشروط به اطلاعات زیرساخت
- تاریخ: 2026-08-03

## تصمیم

- Production از IIS Windows Authentication برای SSO و LDAPS برای ورود دستی استفاده می‌کند.
- Username ورودی UPN است.
- هویت پایدار کاربر با SID و objectGUID ذخیره می‌شود.
- Fake Provider فقط در Development/Test مجاز است و در Production باعث Fail-fast می‌شود.
- DC در کد ثابت نیست؛ Discovery و Override کنترل‌شده پشتیبانی می‌شوند.

## موارد باز پیش از Production

- Domain FQDN، UPN suffixها، Forest/Trust و فهرست DCها.
- SPN، Chrome GPO و حساب سرویس برنامه.
- نتیجه تست Failover و LDAPS Certificate روی همه DCها.

