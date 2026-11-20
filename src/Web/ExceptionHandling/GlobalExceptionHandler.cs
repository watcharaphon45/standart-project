using clean.Domain.Exceptions;
using clean.Web.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace clean.Web.ExceptionHandling;

public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = RequestObservabilityMiddleware.ResolveTraceId(context);
        var postgres = exception as PostgresException ?? exception.InnerException as PostgresException;
        var (status, code, detail) = exception switch
        {
            ValidationException => (400, "VALIDATION_ERROR", "กรุณาตรวจสอบข้อมูลที่ระบุ"),
            RuleException rule => (rule.Status, rule.Code, rule.Message),
            DbUpdateConcurrencyException => (409, "CONCURRENCY_CONFLICT", "ข้อมูลถูกแก้ไขแล้ว กรุณาโหลดข้อมูลล่าสุดและแก้ใหม่"),
            _ when postgres?.SqlState == PostgresErrorCodes.UniqueViolation => (409, "DUPLICATE_CODE", "รหัสหรือข้อมูลนี้มีอยู่แล้ว กรุณาตรวจสอบ"),
            _ when postgres?.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected =>
                (409, "CONCURRENCY_CONFLICT", "ข้อมูลกำลังถูกเปลี่ยนแปลง กรุณาโหลดข้อมูลล่าสุดและลองใหม่"),
            _ => (500, "INTERNAL_ERROR", "ไม่สามารถดำเนินการได้ในขณะนี้ กรุณาลองใหม่อีกครั้ง")
        };
        if (status >= 500 && exception is not RuleException)
            LogUnhandledFailure(logger, exception, traceId);
        var problem = new ProblemDetails { Status = status, Title = detail, Detail = detail, Type = "about:blank" };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;
        if (exception is ValidationException validation)
            problem.Extensions["errors"] = validation.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).Distinct().ToArray());
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Unhandled request failure. TraceId={TraceId}")]
    private static partial void LogUnhandledFailure(ILogger logger, Exception exception, string traceId);
}
