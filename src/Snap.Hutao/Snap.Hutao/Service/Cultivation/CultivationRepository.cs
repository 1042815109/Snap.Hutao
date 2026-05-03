// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snap.Hutao.Core.Database;
using Snap.Hutao.Model.Entity;
using Snap.Hutao.Model.Entity.Database;
using Snap.Hutao.Service.Abstraction;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace Snap.Hutao.Service.Cultivation;

[Service(ServiceLifetime.Singleton, typeof(ICultivationRepository))]
internal sealed partial class CultivationRepository : ICultivationRepository
{
    [GeneratedConstructor]
    public partial CultivationRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public ImmutableArray<CultivateEntry> GetCultivateEntryImmutableArrayByProjectId(Guid projectId)
    {
        return this.ImmutableArray<CultivateEntry>(e => e.ProjectId == projectId);
    }

    public ImmutableArray<CultivateEntry> GetCultivateEntryImmutableArrayIncludingLevelInformationByProjectId(Guid projectId)
    {
        return this.ImmutableArray<CultivateEntry, CultivateEntry>(query => query.Where(e => e.ProjectId == projectId).Include(e => e.LevelInformation));
    }

    public ImmutableArray<CultivateItem> GetCultivateItemImmutableArrayByEntryId(Guid entryId)
    {
        return this.ImmutableArray<CultivateItem, CultivateItem>(query => query.Where(i => i.EntryId == entryId).OrderBy(i => i.ItemId));
    }

    public void RemoveCultivateEntryById(Guid entryId)
    {
        this.DeleteByInnerId<CultivateEntry>(entryId);
    }

    public void UpdateCultivateItem(CultivateItem item)
    {
        this.Update(item);
    }

    public ImmutableArray<CultivateEntry> GetCultivateEntryImmutableArrayByProjectIdAndItemId(Guid projectId, uint itemId)
    {
        return this.ImmutableArray<CultivateEntry>(e => e.ProjectId == projectId && e.Id == itemId);
    }

    public void AddCultivateEntry(CultivateEntry entry)
    {
        this.Add(entry);
    }

    public void RemoveCultivateItemRangeByEntryId(Guid entryId)
    {
        this.Delete<CultivateItem>(i => i.EntryId == entryId);
    }

    public void AddCultivateItemRange(IEnumerable<CultivateItem> toAdd)
    {
        this.AddRange(toAdd);
    }

    public void AddCultivateProject(CultivateProject project)
    {
        this.Add(project);
    }

    public void RemoveCultivateProjectById(Guid projectId)
    {
        this.DeleteByInnerId<CultivateProject>(projectId);
    }

    public ObservableCollection<CultivateProject> GetCultivateProjectCollection()
    {
        return this.ObservableCollection<CultivateProject>();
    }

    public void RemoveLevelInformationByEntryId(Guid entryId)
    {
        this.Delete<CultivateEntryLevelInformation>(l => l.EntryId == entryId);
    }

    public void AddLevelInformation(CultivateEntryLevelInformation levelInformation)
    {
        this.Add(levelInformation);
    }

    public CultivateProject? GetCultivateProjectById(Guid projectId)
    {
        return this.SingleOrDefault<CultivateProject>(p => p.InnerId == projectId);
    }

    public Guid GetCultivateProjectIdByEntryId(Guid entryId)
    {
        return this.Single<CultivateEntry, Guid>(query => query.Where(entry => entry.InnerId == entryId).Select(entry => entry.InnerId));
    }

    public ImmutableArray<(CultivateEntry Entry, CultivateItem Item)> GetCultivateEntryItemPairsByProjectId(Guid projectId)
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext db = scope.GetAppDbContext();
            IQueryable<CultivateEntry> entries = db.Set<CultivateEntry>().AsNoTracking().Where(e => e.ProjectId == projectId);
            return [.. db.Set<CultivateItem>().AsNoTracking()
                .Join(
                    entries,
                    item => item.EntryId,
                    entry => entry.InnerId,
                    (item, entry) => new { Entry = entry, Item = item })
                .OrderBy(t => t.Entry.InnerId)
                .ThenBy(t => t.Item.ItemId)
                .ToList()
                .Select(t => (t.Entry, t.Item))];
        }
    }
}