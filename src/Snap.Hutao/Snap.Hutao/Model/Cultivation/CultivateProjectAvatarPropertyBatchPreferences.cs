// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Model.Cultivation;

/// <summary>
/// 「我的角色」批量同步到当前养成计划时，在批量对话框中使用的目标等级、保存策略等；按 <see cref="Entity.CultivateProject"/> 持久化。
/// </summary>
internal sealed class CultivateProjectAvatarPropertyBatchPreferences
{
    public uint AvatarLevelTarget { get; set; }

    public uint SkillATarget { get; set; }

    public uint SkillETarget { get; set; }

    public uint SkillQTarget { get; set; }

    public uint WeaponLevelTarget { get; set; }

    public int ConsumptionSaveStrategyIndex { get; set; }

    public bool ClearAvatarAndWeaponEntriesBeforeSync { get; set; }
}
