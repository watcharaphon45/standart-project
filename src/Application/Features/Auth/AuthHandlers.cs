using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.Auth;

public sealed record LoginCommand(LoginRequest Input) : IRequest<LoginResult>;
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Input.Identifier).NotEmpty().MaximumLength(256).WithMessage("กรุณาระบุ Username หรือ Email");
        RuleFor(x => x.Input.Password).NotEmpty().MaximumLength(128).WithMessage("กรุณาระบุรหัสผ่าน");
    }
}
public sealed class LoginHandler(IIdentityService identity) : IRequestHandler<LoginCommand, LoginResult>
{
    public Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken) =>
        identity.LoginAsync(command.Input, cancellationToken);
}
public sealed record ChangePasswordCommand(ChangePasswordRequest Input) : IRequest;
public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.Input.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Input.NewPassword).NotEmpty().Length(15, 128).WithMessage("รหัสผ่านต้องยาว 15–128 ตัวอักษร");
    }
}
public sealed class ChangePasswordHandler(IIdentityService identity, ICurrentActor actor) : IRequestHandler<ChangePasswordCommand>
{
    public Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        identity.ChangePasswordAsync(actor.UserId, command.Input, cancellationToken);
}
