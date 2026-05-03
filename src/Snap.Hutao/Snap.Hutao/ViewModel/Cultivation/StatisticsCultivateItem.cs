// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Metadata.Item;

namespace Snap.Hutao.ViewModel.Cultivation;

internal sealed class StatisticsCultivateItem
{
    private readonly TimeSpan offset;

    private StatisticsCultivateItem(Material inner, TimeSpan offset)
    {
        Inner = inner;
        this.offset = offset;
        ExcludedFromPresentation = true;
    }

    private StatisticsCultivateItem(Material inner, Model.Entity.CultivateItem entity, TimeSpan offset)
    {
        Inner = inner;
        Count = entity.Count;
        this.offset = offset;
    }

    public Material Inner { get; }

    public uint Count { get; set; }

    public uint Current { get; set; }

    /// <summary>
    /// 升级材料合并后的展示用持有量；未启用合并时为 <see langword="null"/>。
    /// </summary>
    public uint? MergeAdjustedCurrent { get; set; }

    public uint DisplayCurrent { get => MergeAdjustedCurrent ?? Current; }

    public bool IsFinished { get => DisplayCurrent >= Count; }

    public string FormattedCount { get => $"{DisplayCurrent}/{Count}"; }

    /// <summary>未启用合并展示链时，使用紧凑 <see cref="FormattedCount"/>。</summary>
    public bool ShowNonMergeCompactCount { get => !MergeAdjustedCurrent.HasValue; }

    /// <summary>已合并但合成前后有效持有量与背包数一致，不显示括号，仅「合并后 / 需求」加空格。</summary>
    public bool ShowMergeSpacedWithoutParen { get => MergeAdjustedCurrent.HasValue && DisplayCurrent == Current; }

    /// <summary>合并后有效持有与背包原数不同，显示「(背包原数)」并着色。</summary>
    public bool ShowMergeInventoryParen { get => MergeAdjustedCurrent.HasValue && DisplayCurrent != Current; }

    /// <summary>合并后 &gt; 背包原数：括号用强调色（蓝系）。</summary>
    public bool MergeInventoryParenUseBlue { get => MergeAdjustedCurrent.HasValue && DisplayCurrent > Current; }

    /// <summary>合并后 &lt; 背包原数：括号用警告色（红系，低级被向上消耗）。</summary>
    public bool MergeInventoryParenUseRed { get => MergeAdjustedCurrent.HasValue && DisplayCurrent < Current; }

    /// <summary>背包同步原数，带前导空格与括号。</summary>
    public string RawInventoryParenthetical { get => $" ({Current})"; }

    /// <summary>「 / 需求数」段，含前导空格。</summary>
    public string SlashCountSuffix { get => $" / {Count}"; }

    public bool IsToday { get => Inner.IsItemOfToday(offset, true); }

    internal bool ExcludedFromPresentation { get; set; }

    public static StatisticsCultivateItem Create(Material inner, TimeSpan offset)
    {
        return new(inner, offset);
    }

    public static StatisticsCultivateItem Create(Material inner, Model.Entity.CultivateItem entity, TimeSpan offset)
    {
        return new(inner, entity, offset);
    }
}