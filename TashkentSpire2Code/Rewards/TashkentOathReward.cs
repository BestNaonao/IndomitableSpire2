using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using TashkentSpire2.TashkentSpire2Code.Enchantment;

namespace TashkentSpire2.TashkentSpire2Code.Rewards;

public sealed class TashkentOathReward : CustomReward
{
    private readonly Player _player;
    public int IntelAmount { get; }

    public TashkentOathReward(Player player, int intelAmount = 1)
        : base(player)
    {
        _player = player;
        IntelAmount = intelAmount;
    }

    protected override RewardType RewardType =>
        TashkentRewardTypes.Oath;

    public override int RewardsSetIndex => 9;

    public override bool IsPopulated => true;

    public override LocString Description =>
        new("gameplay_ui", "COMBAT_REWARD_CARD_ENCHANT_OATH");

    protected override string IconPath =>
        "res://TashkentSpire2/images/packed/rewards/eternaloath_power.png";

    public override Task Populate() =>
        Task.CompletedTask;

    protected override async Task<bool> OnSelect()
    {
        var selectedCards = await CardSelectCmd.FromDeckForEnchantment(
            prefs: new CardSelectorPrefs(
                CardSelectorPrefs.EnchantSelectionPrompt,
                1
            ),
            player: _player,
            enchantment: ModelDb.Enchantment<OathEnchantment>(),
            amount: 1
        );

        foreach (CardModel card in selectedCards)
        {
            CardCmd.Enchant<OathEnchantment>(card, 1m);

            var vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
        
        return true;
    }

    public override void MarkContentAsSeen()
    {
    }

    public override SerializableReward ToSerializable()
    {
        SerializableReward save = base.ToSerializable();

        save.GoldAmount = IntelAmount;

        return save;
    }

    public static CustomReward CreateFromSerializable(
        SerializableReward save,
        Player player)
    {
        return new TashkentOathReward(
            player,
            save.GoldAmount);
    }

    public override CreateRewardFromSave<CustomReward>
        DeserializeMethod =>
        CreateFromSerializable;
}

public static class TashkentRewardTypes
{
    public static readonly RewardType Oath =
        (RewardType)83079;
}