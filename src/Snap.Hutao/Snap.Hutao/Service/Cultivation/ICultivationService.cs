// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Database;
using Snap.Hutao.Model.Entity;
using Snap.Hutao.Service.Cultivation.Consumption;
using Snap.Hutao.ViewModel.Cultivation;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Service.Cultivation;

internal interface ICultivationService
{
    ValueTask<IAdvancedDbCollectionView<CultivateProject>> GetProjectCollectionAsync();

    ValueTask<bool> EnsureCurrentProjectAsync(IAdvancedDbCollectionView<CultivateProject> projects);

    ValueTask<ObservableCollection<CultivateEntryView>> GetCultivateEntryCollectionAsync(CultivateProject cultivateProject, ICultivationMetadataContext context);

    ValueTask<StatisticsCultivateItemCollection> GetStatisticsCultivateItemCollectionAsync(CultivateProject cultivateProject, ICultivationMetadataContext context, CultivationStatisticsMergeOptions mergeOptions, CancellationToken token);

    ValueTask<ResinStatistics> GetResinStatisticsAsync(StatisticsCultivateItemCollection statisticsCultivateItems, CancellationToken token);

    ValueTask RemoveCultivateEntryAsync(Guid entryId);

    ValueTask RemoveProjectAsync(CultivateProject project);

    ValueTask<ConsumptionSaveResult> SaveConsumptionAsync(InputConsumption inputConsumption);

    ValueTask<Guid?> TryGetAvatarCultivateEntryInnerIdAsync(uint avatarId);

    /// <summary>
    /// 当前计划中尚无该角色的养成条目时，插入一条无材料行的角色占位条目（等级信息与 delta 一致），供武器 RelatedEntryId 关联。
    /// 若已存在角色条目则返回其 InnerId。
    /// </summary>
    ValueTask<Guid?> EnsureAvatarAssociationStubAsync(uint avatarId, LevelInformation levelInformation);

    void SaveCultivateItem(CultivateItemView item);

    ValueTask<ProjectAddResultKind> TryAddProjectAsync(CultivateProject project);
}