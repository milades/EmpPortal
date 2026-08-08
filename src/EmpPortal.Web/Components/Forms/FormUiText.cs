using EmpPortal.Application.Forms.Schema;
using EmpPortal.Domain.Forms;

namespace EmpPortal.Web.Components.Forms;

internal static class FormUiText
{
    public static string ElementType(FormElementType type) => type switch
    {
        FormElementType.Text => "متن کوتاه",
        FormElementType.TextArea => "متن چندخطی",
        FormElementType.RichText => "متن توضیحی بلند",
        FormElementType.Number => "عدد",
        FormElementType.Currency => "مبلغ",
        FormElementType.Percentage => "درصد",
        FormElementType.Email => "ایمیل",
        FormElementType.Phone => "شماره تماس",
        FormElementType.Url => "نشانی وب",
        FormElementType.NationalId => "کد ملی",
        FormElementType.Date => "تاریخ",
        FormElementType.DateTime => "تاریخ و زمان",
        FormElementType.Time => "زمان",
        FormElementType.DateRange => "بازه تاریخ",
        FormElementType.Select => "فهرست انتخابی",
        FormElementType.MultiSelect => "انتخاب چندگانه",
        FormElementType.Radio => "گزینه‌های رادیویی",
        FormElementType.Checkbox => "تأیید/عدم تأیید",
        FormElementType.Switch => "کلید روشن/خاموش",
        FormElementType.Slider => "نوار عددی",
        FormElementType.Hidden => "مقدار مخفی",
        FormElementType.CurrentUser => "کاربر جاری",
        FormElementType.Calculated => "مقدار محاسباتی",
        FormElementType.Heading => "عنوان نمایشی",
        FormElementType.Paragraph => "متن نمایشی",
        FormElementType.Divider => "جداکننده",
        FormElementType.Alert => "پیام برجسته",
        FormElementType.Repeater => "ردیف تکرارشونده",
        FormElementType.Table => "جدول ورود داده",
        _ => type.ToString()
    };

    public static string Lifecycle(FormLifecycleStatus status) => status switch
    {
        FormLifecycleStatus.Draft => "پیش‌نویس",
        FormLifecycleStatus.Published => "منتشرشده",
        FormLifecycleStatus.Paused => "متوقف",
        FormLifecycleStatus.Archived => "بایگانی",
        _ => status.ToString()
    };

    public static string Submission(FormSubmissionStatus status) => status switch
    {
        FormSubmissionStatus.Draft => "پیش‌نویس",
        FormSubmissionStatus.Submitted => "ثبت نهایی",
        FormSubmissionStatus.Withdrawn => "پس‌گرفته‌شده",
        _ => status.ToString()
    };

    public static string ConditionOperator(FormConditionOperator conditionOperator) => conditionOperator switch
    {
        FormConditionOperator.Equals => "برابر باشد با",
        FormConditionOperator.NotEquals => "برابر نباشد با",
        FormConditionOperator.Contains => "شامل باشد",
        FormConditionOperator.NotContains => "شامل نباشد",
        FormConditionOperator.GreaterThan => "بزرگ‌تر از",
        FormConditionOperator.GreaterThanOrEqual => "بزرگ‌تر یا برابر",
        FormConditionOperator.LessThan => "کوچک‌تر از",
        FormConditionOperator.LessThanOrEqual => "کوچک‌تر یا برابر",
        FormConditionOperator.IsEmpty => "خالی باشد",
        FormConditionOperator.IsNotEmpty => "خالی نباشد",
        _ => conditionOperator.ToString()
    };
}
