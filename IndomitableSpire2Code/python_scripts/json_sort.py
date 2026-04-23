import json


def method(json_path: str = "", offered_dict=None):
    if not offered_dict:
        offered_dict = {}
    if json_path:
        offered_dict = json.load(open(json_path, encoding='utf-8'))
    tuples = sorted(offered_dict.items(), key=lambda k: k[0])
    print(json.dumps({k: v for k, v in tuples}, ensure_ascii=False, indent=4))


if __name__ == '__main__':
    path = "../../IndomitableSpire2/localization/zhs/cards.json"

    dic = {
        "INDOMITABLESPIRE2-AVIATION_POWER.title": "航空",
        "INDOMITABLESPIRE2-AVIATION_POWER.description": "航空は[gold]艦載機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]を増加させる。",
        "INDOMITABLESPIRE2-AVIATION_POWER.smartDescription": "自身の[gold]艦載機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",
        "INDOMITABLESPIRE2-AVIATION_POWER.remoteDescription": "[gold]{OwnerName}[/gold]の[gold]艦載機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",

        "INDOMITABLESPIRE2-AIR_COMBAT_ELITE_POWER.title": "空戦エリート",
        "INDOMITABLESPIRE2-AIR_COMBAT_ELITE_POWER.description": "空戦エリートは[gold]戦闘攻撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]を増加させる。",
        "INDOMITABLESPIRE2-AIR_COMBAT_ELITE_POWER.smartDescription": "自身の[gold]戦闘攻撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",
        "INDOMITABLESPIRE2-AIR_COMBAT_ELITE_POWER.remoteDescription": "[gold]{OwnerName}[/gold]の[gold]戦闘攻撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",

        "INDOMITABLESPIRE2-TORPEDO_MASTERY_POWER.title": "雷撃精通",
        "INDOMITABLESPIRE2-TORPEDO_MASTERY_POWER.description": "雷撃精通は[gold]雷撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]を増加させる。",
        "INDOMITABLESPIRE2-TORPEDO_MASTERY_POWER.smartDescription": "自身の[gold]雷撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",
        "INDOMITABLESPIRE2-TORPEDO_MASTERY_POWER.remoteDescription": "[gold]{OwnerName}[/gold]の[gold]雷撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",

        "INDOMITABLESPIRE2-LETHAL_DIVE_POWER.title": "致命的な急降下",
        "INDOMITABLESPIRE2-LETHAL_DIVE_POWER.description": "致命的な急降下は[gold]急降下爆撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]を増加させる。",
        "INDOMITABLESPIRE2-LETHAL_DIVE_POWER.smartDescription": "自身の[gold]急降下爆撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",
        "INDOMITABLESPIRE2-LETHAL_DIVE_POWER.remoteDescription": "[gold]{OwnerName}[/gold]の[gold]急降下爆撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",

        "INDOMITABLESPIRE2-SCORCHED_EARTH_POWER.title": "焦土爆撃",
        "INDOMITABLESPIRE2-SCORCHED_EARTH_POWER.description": "焦土爆撃は[gold]水平爆撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]を増加させる。",
        "INDOMITABLESPIRE2-SCORCHED_EARTH_POWER.smartDescription": "自身の[gold]水平爆撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。",
        "INDOMITABLESPIRE2-SCORCHED_EARTH_POWER.remoteDescription": "[gold]{OwnerName}[/gold]の[gold]水平爆撃機[/gold]カードのアタックダメージと得る[gold]ブロック[/gold]が[blue]{Amount:abs()}%[/blue]{Amount:cond:<0?減少|増加}する。"
    }

    method(path, dic)