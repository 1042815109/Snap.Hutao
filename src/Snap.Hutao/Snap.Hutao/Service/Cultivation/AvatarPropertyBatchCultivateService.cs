// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Factory.ContentDialog;
using Snap.Hutao.Model.Calculable;
using Snap.Hutao.Model.Cultivation;
using Snap.Hutao.Model.Entity.Primitive;
using Snap.Hutao.Service.AvatarInfo.Factory;
using Snap.Hutao.Service.Cultivation.Consumption;
using Snap.Hutao.Service.Cultivation.Offline;
using Snap.Hutao.UI.Xaml.View.Dialog;
using Snap.Hutao.ViewModel.AvatarProperty;
using Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate;
using System.Collections.Immutable;
using CalculatorAvatarPromotionDelta = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.AvatarPromotionDelta;
using CalculatorBatchConsumption = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.BatchConsumption;
using CalculatorConsumption = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.Consumption;
using CalculatorItemHelper = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.ItemHelper;

namespace Snap.Hutao.Service.Cultivation;

[Service(ServiceLifetime.Singleton, typeof(IAvatarPropertyBatchCultivateService))]
internal sealed partial class AvatarPropertyBatchCultivateService : IAvatarPropertyBatchCultivateService
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly ICultivationService cultivationService;
    private readonly IServiceProvider serviceProvider;

    [GeneratedConstructor]
    public partial AvatarPropertyBatchCultivateService(IServiceProvider serviceProvider);

    public async ValueTask<BatchCultivateResult?> ExecuteAsync(SummaryFactoryMetadataContext metadataContext, ImmutableArray<AvatarView> targetAvatars, CancellationToken cancellationToken)
    {
        CultivateProjectAvatarPropertyBatchPreferences? batchPrefs = await cultivationService
            .GetAvatarPropertyBatchCultivatePreferencesForCurrentProjectAsync()
            .ConfigureAwait(false);

        CultivatePromotionDeltaBatchDialog dialog = batchPrefs is null
            ? await contentDialogFactory
                .CreateInstanceAsync<CultivatePromotionDeltaBatchDialog>(serviceProvider)
                .ConfigureAwait(false)
            : await contentDialogFactory
                .CreateInstanceAsync<CultivatePromotionDeltaBatchDialog>(serviceProvider, batchPrefs)
                .ConfigureAwait(false);

        if (await dialog.GetPromotionDeltaBaselineAsync().ConfigureAwait(false) is not (true, { } baseline))
        {
            return null;
        }

        await cultivationService
            .SaveAvatarPropertyBatchCultivatePreferencesForCurrentProjectAsync(ToAvatarPropertyBatchPreferences(baseline))
            .ConfigureAwait(false);

        ArgumentNullException.ThrowIfNull(baseline.Delta.Weapon);

        ContentDialog progressDialog = await contentDialogFactory
            .CreateForIndeterminateProgressAsync(SH.ViewModelAvatarPropertyBatchCultivateProgressTitle)
            .ConfigureAwait(false);

        BatchCultivateResult result = default;
        using (await contentDialogFactory.BlockAsync(progressDialog).ConfigureAwait(false))
        {
            if (baseline.ClearAvatarAndWeaponEntriesBeforeSync)
            {
                await cultivationService.RemoveAvatarAndWeaponEntriesForCurrentProjectAsync().ConfigureAwait(false);
            }

            ImmutableArray<CalculatorAvatarPromotionDelta>.Builder deltasBuilder = ImmutableArray.CreateBuilder<CalculatorAvatarPromotionDelta>();
            foreach (AvatarView avatar in targetAvatars)
            {
                if (!baseline.Delta.TryGetNonErrorCopy(avatar, out CalculatorAvatarPromotionDelta? copy))
                {
                    ++result.SkippedCount;
                    continue;
                }

                deltasBuilder.Add(copy);
            }

            ImmutableArray<CalculatorAvatarPromotionDelta> deltas = deltasBuilder.ToImmutable();

            CalculatorBatchConsumption batchConsumption = OfflineCalculator.CalculateBatchConsumption(deltas, metadataContext);

            foreach ((CalculatorConsumption consumption, CalculatorAvatarPromotionDelta delta) in batchConsumption.Items.Zip(deltas))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!await SaveCultivationAsync(consumption, new CultivatePromotionDeltaOptions(delta, baseline.Strategy)).ConfigureAwait(false))
                {
                    break;
                }

                ++result.SucceedCount;
            }
        }

        return result;
    }

    private static CultivateProjectAvatarPropertyBatchPreferences ToAvatarPropertyBatchPreferences(CultivatePromotionDeltaOptions baseline)
    {
        CalculatorAvatarPromotionDelta d = baseline.Delta;
        uint sa = 10;
        uint se = 10;
        uint sq = 10;
        if (d.SkillList is [{ } a, { } e, { } q, ..])
        {
            sa = a.LevelTarget;
            se = e.LevelTarget;
            sq = q.LevelTarget;
        }

        return new CultivateProjectAvatarPropertyBatchPreferences
        {
            AvatarLevelTarget = d.AvatarLevelTarget,
            SkillATarget = sa,
            SkillETarget = se,
            SkillQTarget = sq,
            WeaponLevelTarget = d.Weapon?.LevelTarget ?? 90U,
            ConsumptionSaveStrategyIndex = (int)baseline.Strategy,
            ClearAvatarAndWeaponEntriesBeforeSync = baseline.ClearAvatarAndWeaponEntriesBeforeSync,
        };
    }

    private async ValueTask<bool> SaveCultivationAsync(CalculatorConsumption consumption, CultivatePromotionDeltaOptions options)
    {
        LevelInformation levelInformation = LevelInformation.From(options.Delta);

        InputConsumption avatarInput = new()
        {
            Type = CultivateType.AvatarAndSkill,
            ItemId = options.Delta.AvatarId,
            Items = CalculatorItemHelper.Merge(consumption.AvatarConsume, consumption.AvatarSkillConsume),
            LevelInformation = levelInformation,
            Strategy = options.Strategy,
        };

        ConsumptionSaveResult avatarSave = await cultivationService.SaveConsumptionAsync(avatarInput).ConfigureAwait(false);

        if (avatarSave.Kind is ConsumptionSaveResultKind.NoProject)
        {
            return false;
        }

        ArgumentNullException.ThrowIfNull(options.Delta.Weapon);

        Guid? relatedAvatarEntryId = avatarSave.CreatedEntryInnerId;
        if (relatedAvatarEntryId is null && avatarSave.Kind is ConsumptionSaveResultKind.Skipped)
        {
            relatedAvatarEntryId = await cultivationService.TryGetAvatarCultivateEntryInnerIdAsync(options.Delta.AvatarId).ConfigureAwait(false);
        }

        if (relatedAvatarEntryId is null && avatarSave.Kind is ConsumptionSaveResultKind.NoItem)
        {
            relatedAvatarEntryId = await cultivationService.TryGetAvatarCultivateEntryInnerIdAsync(options.Delta.AvatarId).ConfigureAwait(false);
        }

        if (relatedAvatarEntryId is null && !consumption.WeaponConsume.IsEmpty)
        {
            relatedAvatarEntryId = await cultivationService.EnsureAvatarAssociationStubAsync(options.Delta.AvatarId, levelInformation).ConfigureAwait(false);
        }

        InputConsumption weaponInput = new()
        {
            Type = CultivateType.Weapon,
            ItemId = options.Delta.Weapon.Id,
            Items = consumption.WeaponConsume,
            LevelInformation = levelInformation,
            Strategy = options.Strategy,
            RelatedEntryId = relatedAvatarEntryId,
        };

        ConsumptionSaveResult weaponSave = await cultivationService.SaveConsumptionAsync(weaponInput).ConfigureAwait(false);

        return weaponSave.Kind is not ConsumptionSaveResultKind.NoProject;
    }
}
