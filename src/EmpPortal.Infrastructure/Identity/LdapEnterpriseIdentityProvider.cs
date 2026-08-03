using System.Buffers.Binary;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Net;
using System.Text;
using EmpPortal.Application.Identity;
using Microsoft.Extensions.Options;

namespace EmpPortal.Infrastructure.Identity;

public sealed class LdapEnterpriseIdentityProvider(
    IOptionsMonitor<ActiveDirectoryOptions> optionsMonitor,
    TimeProvider timeProvider) : IEnterpriseIdentityProvider
{
    private const int AccountDisabledFlag = 0x0002;
    private const int PasswordExpiredFlag = 0x800000;
    private static readonly string[] IdentityAttributes =
    [
        "objectGUID",
        "objectSid",
        "userPrincipalName",
        "displayName",
        "mail",
        "userAccountControl",
        "lockoutTime",
        "accountExpires",
        "pwdLastSet"
    ];

    public async Task<PasswordAuthenticationResult> AuthenticatePasswordAsync(
        string upn,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(upn) || string.IsNullOrWhiteSpace(password))
        {
            return PasswordAuthenticationResult.Failed(
                PasswordAuthenticationFailure.InvalidCredentials);
        }

        ActiveDirectoryOptions options = optionsMonitor.CurrentValue;
        foreach (string server in GetServers(options))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                EnterpriseIdentity? identity = await Task.Run(() =>
                {
                    using LdapConnection connection = CreateConnection(
                        server,
                        options,
                        new NetworkCredential(upn.Trim(), password),
                        AuthType.Basic);
                    connection.Bind();
                    return SearchSingleIdentity(connection, options.BaseDn, BuildUpnFilter(upn));
                }, cancellationToken);

                return identity is null
                    ? PasswordAuthenticationResult.Failed(
                        PasswordAuthenticationFailure.InvalidCredentials)
                    : identity.State == DirectoryAccountState.Enabled
                        ? PasswordAuthenticationResult.Success(identity)
                        : PasswordAuthenticationResult.Failed(MapAccountState(identity.State));
            }
            catch (LdapException exception) when (IsInvalidCredentialError(exception))
            {
                return PasswordAuthenticationResult.Failed(MapBindFailure(exception));
            }
            catch (LdapException exception) when (IsServerUnavailable(exception))
            {
                continue;
            }
        }

        return PasswordAuthenticationResult.Failed(
            PasswordAuthenticationFailure.DirectoryUnavailable);
    }

    public async Task<EnterpriseIdentity?> FindByLoginNameAsync(
        string loginName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginName))
        {
            return null;
        }

        string filter = BuildLoginNameFilter(loginName);
        return await QueryWithIntegratedAuthenticationAsync(filter, cancellationToken);
    }

    public async Task<DirectoryAccountState> GetAccountStateAsync(
        Guid objectGuid,
        CancellationToken cancellationToken = default)
    {
        if (objectGuid == Guid.Empty)
        {
            return DirectoryAccountState.Unavailable;
        }

        EnterpriseIdentity? identity = await QueryWithIntegratedAuthenticationAsync(
            BuildObjectGuidFilter(objectGuid),
            cancellationToken);
        return identity?.State ?? DirectoryAccountState.Unavailable;
    }

    private async Task<EnterpriseIdentity?> QueryWithIntegratedAuthenticationAsync(
        string filter,
        CancellationToken cancellationToken)
    {
        ActiveDirectoryOptions options = optionsMonitor.CurrentValue;
        foreach (string server in GetServers(options))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await Task.Run(() =>
                {
                    using LdapConnection connection = CreateConnection(
                        server,
                        options,
                        credential: null,
                        AuthType.Negotiate);
                    connection.Bind();
                    return SearchSingleIdentity(connection, options.BaseDn, filter);
                }, cancellationToken);
            }
            catch (LdapException exception) when (IsServerUnavailable(exception))
            {
                continue;
            }
        }

        return null;
    }

    private EnterpriseIdentity? SearchSingleIdentity(
        LdapConnection connection,
        string baseDn,
        string filter)
    {
        SearchRequest request = new(
            baseDn,
            $"(&(objectCategory=person)(objectClass=user){filter})",
            SearchScope.Subtree,
            IdentityAttributes);
        SearchResponse response = (SearchResponse)connection.SendRequest(request);

        return response.Entries.Count == 1
            ? MapIdentity(response.Entries[0])
            : null;
    }

    private EnterpriseIdentity MapIdentity(SearchResultEntry entry)
    {
        byte[] objectGuidBytes = GetRequiredBytes(entry, "objectGUID");
        byte[] sidBytes = GetRequiredBytes(entry, "objectSid");
        string upn = GetRequiredString(entry, "userPrincipalName").Trim().ToLowerInvariant();
        string displayName = GetOptionalString(entry, "displayName") ?? upn;
        string? email = GetOptionalString(entry, "mail");

        return new EnterpriseIdentity(
            new Guid(objectGuidBytes),
            ConvertSidToString(sidBytes),
            upn,
            displayName,
            email,
            DetermineAccountState(entry));
    }

    private DirectoryAccountState DetermineAccountState(SearchResultEntry entry)
    {
        long userAccountControl = GetInt64(entry, "userAccountControl");
        if ((userAccountControl & AccountDisabledFlag) != 0)
        {
            return DirectoryAccountState.Disabled;
        }

        if (GetInt64(entry, "lockoutTime") > 0)
        {
            return DirectoryAccountState.Locked;
        }

        if ((userAccountControl & PasswordExpiredFlag) != 0 ||
            GetInt64(entry, "pwdLastSet") == 0)
        {
            return DirectoryAccountState.PasswordExpired;
        }

        long accountExpires = GetInt64(entry, "accountExpires");
        if (accountExpires is > 0 and < long.MaxValue &&
            DateTimeOffset.FromFileTime(accountExpires) <= timeProvider.GetUtcNow())
        {
            return DirectoryAccountState.Expired;
        }

        return DirectoryAccountState.Enabled;
    }

    private static LdapConnection CreateConnection(
        string server,
        ActiveDirectoryOptions options,
        NetworkCredential? credential,
        AuthType authType)
    {
        LdapDirectoryIdentifier identifier = new(
            server,
            options.LdapsPort,
            fullyQualifiedDnsHostName: true,
            connectionless: false);
        LdapConnection connection = new(identifier, credential, authType)
        {
            Timeout = TimeSpan.FromSeconds(options.OperationTimeoutSeconds)
        };
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = true;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
        return connection;
    }

    private static IEnumerable<string> GetServers(ActiveDirectoryOptions options)
    {
        IEnumerable<string> configuredServers = options.DomainControllers
            .Where(server => !string.IsNullOrWhiteSpace(server))
            .Select(server => server.Trim());

        return configuredServers.Any()
            ? configuredServers.Distinct(StringComparer.OrdinalIgnoreCase)
            : [options.DomainFqdn.Trim()];
    }

    private static string BuildLoginNameFilter(string loginName)
    {
        string trimmedLoginName = loginName.Trim();
        int slashIndex = trimmedLoginName.LastIndexOf('\\');
        return slashIndex >= 0 && slashIndex < trimmedLoginName.Length - 1
            ? $"(sAMAccountName={EscapeFilterValue(trimmedLoginName[(slashIndex + 1)..])})"
            : BuildUpnFilter(trimmedLoginName);
    }

    private static string BuildUpnFilter(string upn) =>
        $"(userPrincipalName={EscapeFilterValue(upn.Trim())})";

    private static string BuildObjectGuidFilter(Guid objectGuid)
    {
        StringBuilder builder = new("(objectGUID=");
        foreach (byte value in objectGuid.ToByteArray())
        {
            builder.Append('\\');
            builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.Append(')').ToString();
    }

    private static string EscapeFilterValue(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            builder.Append(character switch
            {
                '\\' => "\\5c",
                '*' => "\\2a",
                '(' => "\\28",
                ')' => "\\29",
                '\0' => "\\00",
                _ => character.ToString()
            });
        }

        return builder.ToString();
    }

    private static byte[] GetRequiredBytes(SearchResultEntry entry, string attributeName) =>
        entry.Attributes[attributeName]?[0] as byte[] ??
        throw new InvalidOperationException($"Required AD attribute '{attributeName}' is missing.");

    private static string ConvertSidToString(byte[] sidBytes)
    {
        if (sidBytes.Length < 8)
        {
            throw new InvalidOperationException("The AD objectSid attribute is invalid.");
        }

        int subAuthorityCount = sidBytes[1];
        int requiredLength = 8 + (subAuthorityCount * sizeof(uint));
        if (sidBytes.Length < requiredLength)
        {
            throw new InvalidOperationException("The AD objectSid attribute is incomplete.");
        }

        ulong identifierAuthority = 0;
        for (int index = 2; index < 8; index++)
        {
            identifierAuthority = (identifierAuthority << 8) | sidBytes[index];
        }

        StringBuilder builder = new();
        builder.Append("S-");
        builder.Append(sidBytes[0].ToString(CultureInfo.InvariantCulture));
        builder.Append('-');
        builder.Append(identifierAuthority.ToString(CultureInfo.InvariantCulture));

        for (int index = 0; index < subAuthorityCount; index++)
        {
            uint subAuthority = BinaryPrimitives.ReadUInt32LittleEndian(
                sidBytes.AsSpan(8 + (index * sizeof(uint)), sizeof(uint)));
            builder.Append('-');
            builder.Append(subAuthority.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string GetRequiredString(SearchResultEntry entry, string attributeName) =>
        GetOptionalString(entry, attributeName) ??
        throw new InvalidOperationException($"Required AD attribute '{attributeName}' is missing.");

    private static string? GetOptionalString(SearchResultEntry entry, string attributeName) =>
        entry.Attributes[attributeName]?.Count > 0
            ? Convert.ToString(entry.Attributes[attributeName]![0], CultureInfo.InvariantCulture)
            : null;

    private static long GetInt64(SearchResultEntry entry, string attributeName)
    {
        string? value = GetOptionalString(entry, attributeName);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result)
            ? result
            : 0;
    }

    private static bool IsInvalidCredentialError(LdapException exception) =>
        exception.ErrorCode == 49;

    private static bool IsServerUnavailable(LdapException exception) =>
        exception.ErrorCode is 51 or 52 or 81 or 85 or 91;

    private static PasswordAuthenticationFailure MapBindFailure(LdapException exception)
    {
        string serverMessage = exception.ServerErrorMessage ?? string.Empty;
        return serverMessage.Contains("data 532", StringComparison.OrdinalIgnoreCase) ||
            serverMessage.Contains("data 773", StringComparison.OrdinalIgnoreCase)
                ? PasswordAuthenticationFailure.PasswordExpired
                : serverMessage.Contains("data 533", StringComparison.OrdinalIgnoreCase)
                    ? PasswordAuthenticationFailure.Disabled
                    : serverMessage.Contains("data 701", StringComparison.OrdinalIgnoreCase)
                        ? PasswordAuthenticationFailure.Expired
                        : serverMessage.Contains("data 775", StringComparison.OrdinalIgnoreCase)
                            ? PasswordAuthenticationFailure.Locked
                            : PasswordAuthenticationFailure.InvalidCredentials;
    }

    private static PasswordAuthenticationFailure MapAccountState(DirectoryAccountState state) =>
        state switch
        {
            DirectoryAccountState.Disabled => PasswordAuthenticationFailure.Disabled,
            DirectoryAccountState.Locked => PasswordAuthenticationFailure.Locked,
            DirectoryAccountState.PasswordExpired => PasswordAuthenticationFailure.PasswordExpired,
            DirectoryAccountState.Expired => PasswordAuthenticationFailure.Expired,
            _ => PasswordAuthenticationFailure.DirectoryUnavailable
        };
}
