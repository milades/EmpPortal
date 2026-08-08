namespace EmpPortal.Application.Forms;

public sealed class FormConcurrencyException(string message, Exception innerException)
    : Exception(message, innerException);
