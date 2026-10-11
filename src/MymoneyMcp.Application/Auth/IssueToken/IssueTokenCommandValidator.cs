using FluentValidation;

namespace MymoneyMcp.Application.Auth.IssueToken;

public sealed class IssueTokenCommandValidator : AbstractValidator<IssueTokenCommand>
{
    public IssueTokenCommandValidator()
    {
        RuleFor(c => c.ClientId).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ClientSecret).NotEmpty();
    }
}
