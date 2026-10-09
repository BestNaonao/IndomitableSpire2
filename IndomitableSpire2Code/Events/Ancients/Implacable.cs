using BaseLib.Abstracts;
using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Events.Ancients;

/// <summary>
/// 已接入立绘场景与图标的占位先古，目前不参与自然遭遇。
/// 后续完成玩法、对话和奖励设计后再开放 IsValidForAct。
/// </summary>
public sealed class Implacable : CustomAncientModel
{
    private static readonly string[] ScenePaths =
    [
        "res://IndomitableSpire2/scenes/events/implacable.tscn",
        "res://IndomitableSpire2/scenes/events/implacable_ol.tscn"
    ];
    
    public override bool IsValidForAct(ActModel act) => 
        act.Id == ModelDb.Act<Hive>().Id || act.Id == ModelDb.Act<Glory>().Id;
    
    protected override OptionPools MakeOptionPools => 
        new(MakePool(ModelDb.RelicPool<ImplacableRelicPool>().AllRelics.ToArray()));
    
    // 单池构造器会将同一个池用于三次不放回抽取；当前只有一件遗物，不能调用默认 Roll。
    public override IEnumerable<EventOption> AllPossibleOptions => 
        ModelDb.RelicPool<ImplacableRelicPool>().AllRelics.Select(relic => RelicOption(relic.ToMutable()));
    
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => AllPossibleOptions.ToList();
    
    // TODO:占位阶段无需音频文件；完整对话未来再设计。
    protected override AncientDialogueSet DefineDialogues() => new()
    {
        FirstVisitEverDialogue = new AncientDialogue(""),
        AgnosticDialogues = [new AncientDialogue("")],
        CharacterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>()
    };
    
    // 预载发生在 canonical 模型上，此时尚无 Owner；提前预载全部候选，不在 getter 中抽取。
    public override IEnumerable<string> GetAssetPaths(IRunState runState) => ScenePaths;
    
    public override string CustomScenePath => ScenePaths[new Random().Next(0, ScenePaths.Length)];
    public override string CustomMapIconPath => "res://IndomitableSpire2/images/ancients/ancient_node_implacable.png";
    public override string CustomMapIconOutlinePath => "res://IndomitableSpire2/images/ancients/ancient_node_implacable_outline.png";
    public override string CustomRunHistoryIconPath => "res://IndomitableSpire2/images/ancients/run_history_implacable_icon.png";
    public override string CustomRunHistoryIconOutlinePath => "res://IndomitableSpire2/images/ancients/run_history_implacable_outline.png";
}