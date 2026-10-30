namespace FamilyConnect.Application.Abstractions;

public interface IOtpSender
{
    Task SendAsync(string destination, string code, CancellationToken cancellationToken);
}
