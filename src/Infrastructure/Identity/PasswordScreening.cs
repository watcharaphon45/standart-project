using System.Net;
using System.Security.Cryptography;
using System.Text;
using clean.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace clean.Infrastructure.Identity;

public interface IPasswordScreening
{
    Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken);
}

public sealed class PwnedPasswordScreening(HttpClient http) : IPasswordScreening
{
    // Additional contextual words are checked in the Identity validator.
    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "passwordpassword", "password123456789", "123456789012345", "1234567890123456",
        "qwertyuiopasdfgh", "letmeinletmeinletmein", "adminadminadmin", "Password123456!"
    };

    public async Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken)
    {
        if (Common.Contains(password)) return true;
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "range/" + hash[..5]);
            request.Headers.Add("Add-Padding", "true");
            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK) throw Unavailable();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length == 0) throw Unavailable();
            var compromised = false;
            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length != 2 || parts[0].Length != 35 || !parts[0].All(Uri.IsHexDigit)
                    || !long.TryParse(parts[1], out var count) || count < 0) throw Unavailable();
                if (count > 0 && parts[0].Equals(hash[5..], StringComparison.OrdinalIgnoreCase)) compromised = true;
            }
            return compromised;
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { throw Unavailable(); }
    }

    private static RuleException Unavailable() => new("PASSWORD_SCREENING_UNAVAILABLE",
        "ยังตรวจสอบรหัสผ่านไม่ได้ในขณะนี้ กรุณาลองใหม่อีกครั้ง", 503);
}

public sealed class ScreenedPasswordValidator(IPasswordScreening screening) : IPasswordValidator<ApplicationUser>
{
    private readonly Dictionary<string, bool> screened = [];

    private async Task<bool> CompromisedAsync(string password)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        if (screened.TryGetValue(key, out var result)) return result;
        result = await screening.IsCompromisedAsync(password, CancellationToken.None);
        screened.Add(key, result);
        return result;
    }
    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        var errors = new List<IdentityError>();
        if (password is null || password.Length is < 15 or > 128)
            errors.Add(new() { Code = "PasswordLength", Description = "รหัสผ่านต้องยาว 15–128 ตัวอักษร" });
        else if (password.Equals(user.UserName, StringComparison.OrdinalIgnoreCase)
            || password.Equals(user.Email, StringComparison.OrdinalIgnoreCase)
            || await CompromisedAsync(password))
            errors.Add(new() { Code = "PasswordCompromised", Description = "รหัสผ่านนี้พบบ่อยหรือเคยรั่วไหล กรุณาเลือกรหัสใหม่" });
        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
    }
}
