using FamilyConnect.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyConnect.Infrastructure.Services;

public sealed class DevelopmentOtpSender(IHostEnvironment environment, ILogger<DevelopmentOtpSender> logger) : IOtpSender
{
    public Task SendAsync(string destination, string code, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Development OTP sender is disabled outside Development. Configure an SMS/email provider.");
        logger.LogWarning("DEVELOPMENT OTP for {Destination}: {Code}. Do not use in production.", destination, code);
        return Task.CompletedTask;
    }
}
