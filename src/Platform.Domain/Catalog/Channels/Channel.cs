using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Channels;

public sealed class Channel
{
    private readonly List<ChannelMarketAssignment> _marketAssignments = [];

    private Channel()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        Name = string.Empty;
        HostName = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public Channel(
        Guid id,
        string code,
        string name,
        string? hostName,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        HostName = hostName;
        Status = "Active";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? HostName { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<ChannelMarketAssignment> MarketAssignments => _marketAssignments;

    public void Update(string code, string name, string? hostName, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Code = code;
        Name = name;
        HostName = hostName;
        Touch();
    }

    public void Archive()
    {
        Status = "Archived";
        Touch();
    }

    public void UpsertMarketAssignment(Guid marketId, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        if (_marketAssignments.All(x => x.MarketId != marketId))
        {
            _marketAssignments.Add(new ChannelMarketAssignment(Guid.NewGuid(), marketId));
            SortMarkets();
            Touch();
        }
    }

    public void RemoveMarketAssignment(Guid marketId, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        var existing = _marketAssignments.FirstOrDefault(x => x.MarketId == marketId);
        if (existing is null)
        {
            return;
        }

        _marketAssignments.Remove(existing);
        Touch();
    }

    private void SortMarkets()
    {
        var ordered = _marketAssignments.OrderBy(x => x.MarketId).ToList();
        _marketAssignments.Clear();
        _marketAssignments.AddRange(ordered);
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The channel has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
