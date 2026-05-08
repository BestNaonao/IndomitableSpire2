import json


def filter_dict_by_key_substring(dictionary: dict, substring):
    return {k: v for k, v in dictionary.items() if substring in k}

def filter_dict_by_value_substring(dictionary: dict, substring):
    filtered_names = [key.split(".")[0] for key in {k: v for k, v in dictionary.items() if substring in v}.keys()]
    return {k: v for k, v in dictionary.items() if k.split(".")[0] in filtered_names}

if __name__ == '__main__':
    dic = {
        "CALAMITY.description": "每当你打出一张攻击牌时，将一张随机攻击牌添加到你的[gold]手牌[/gold]。",
        "CALAMITY.title": "灾祸",
        "CREATIVE_AI.description": "在你的回合开始时，将一张随机能力牌加入你的[gold]手牌[/gold]。",
        "CREATIVE_AI.title": "创造性AI",
        "DISTRACTION.description": "将一张随机技能牌添加到你的[gold]手牌[/gold]中。这张牌在本回合内可以免费打出。",
        "DISTRACTION.title": "声东击西",
        "HELLO_WORLD.description": "在你的回合开始时，将一张随机普通牌加入你的[gold]手牌[/gold]。",
        "HELLO_WORLD.title": "你好世界",
        "INFERNAL_BLADE.description": "将一张随机攻击牌加入你的[gold]手牌[/gold]。那张牌在本回合内可以免费打出。",
        "INFERNAL_BLADE.title": "地狱之刃",
        "LARGESSE.description": "将一张随机的{IfUpgraded:show:[gold]升级过的[/gold]}无色牌添加至一位其他玩家的[gold]手牌[/gold]中。",
        "LARGESSE.title": "慷慨捐助",
        "MANIFEST_AUTHORITY.description": "获得{Block:diff()}点[gold]格挡[/gold]。\n将一张随机{IfUpgraded:show:[gold]升级过的[/gold]}无色牌加入你的[gold]手牌[/gold]。",
        "MANIFEST_AUTHORITY.title": "君权自授",
        "WHITE_NOISE.description": "将一张随机能力牌加入你的[gold]手牌[/gold]。这张牌在本回合内免费打出。",
        "WHITE_NOISE.title": "白噪声"
    }
    
    print(json.dumps(filter_dict_by_value_substring(dic, "将一张随机"), ensure_ascii=False, indent=4))