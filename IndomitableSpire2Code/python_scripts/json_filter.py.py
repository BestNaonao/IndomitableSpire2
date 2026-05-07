import json


def filter_dict_by_substring(dictionary: dict, substring):
    return {k: v for k, v in dictionary.items() if substring in k}


if __name__ == '__main__':
    dic = {
        "ACROBATICS.description": "カードを{Cards:diff()}枚引く。\nカードを1枚捨てる。",
        "ACROBATICS.title": "アクロバット",
        "ADAPTIVE_STRIKE.description": "{Damage:diff()}ダメージを与える。\nこのカードの0{energyPrefix:energyIcons(1)}のコピーを1枚[gold]捨て札[/gold]に加える。",
        "ADAPTIVE_STRIKE.title": "アダプティブストライク",
        "ADRENALINE.description": "{Energy:energyIcons()}を得る。\nカードを2枚引く。",
        "ADRENALINE.title": "アドレナリン",
    }
    
    print(json.dumps(filter_dict_by_substring(dic, "STRIKE"), ensure_ascii=False, indent=4))