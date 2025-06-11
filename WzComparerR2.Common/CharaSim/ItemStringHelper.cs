﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public static class ItemStringHelper
    {
        /// <summary>
        /// 获取怪物category属性对应的类型说明。
        /// </summary>
        /// <param Name="category">怪物的category属性的值。</param>
        /// <returns></returns>
        public static string GetMobCategoryName(int category)
        {
            switch (category)
            {
                case 0: return "No type";
                case 1: return "Mammal type";
                case 2: return "Plant type";
                case 3: return "Fish type";
                case 4: return "Reptile type";
                case 5: return "Spirit type";
                case 6: return "Devil type";
                case 7: return "Undead type";
                case 8: return "Enchanted type";
                default: return null;
            }
        }

        public static string GetGearPropString(GearPropType propType, long value)
        {
            return GetGearPropString(propType, value, 0);
        }

        public static string[] GetGearPropString22(GearPropType propType, long value)
        {
            return GetGearPropString22(propType, value, 0);
        }

        /// <summary>
        /// 获取GearPropType所对应的文字说明。
        /// </summary>
        /// <param Name="propType">表示装备属性枚举GearPropType。</param>
        /// <param Name="Value">表示propType属性所对应的值。</param>
        /// <returns></returns>
        public static string GetGearPropString(GearPropType propType, long value, int signFlag)
        {

            string sign;
            switch (signFlag)
            {
                default:
                case 0: //默认处理符号
                    sign = value > 0 ? "+" : null;
                    break;

                case 1: //固定加号
                    sign = "+";
                    break;

                case 2: //无特别符号
                    sign = "";
                    break;
            }
            switch (propType)
            {
                case GearPropType.incSTR: return "STR : " + sign + value;
                case GearPropType.incSTRr: return "STR : " + sign + value + "%";
                case GearPropType.incDEX: return "DEX : " + sign + value;
                case GearPropType.incDEXr: return "DEX : " + sign + value + "%";
                case GearPropType.incINT: return "INT : " + sign + value;
                case GearPropType.incINTr: return "INT : " + sign + value + "%";
                case GearPropType.incLUK: return "LUK : " + sign + value;
                case GearPropType.incLUKr: return "LUK : " + sign + value + "%";
                case GearPropType.incAllStat: return "All Stats: " + sign + value;
                case GearPropType.statR: return "All Stats: " + sign + value + "%";
                case GearPropType.incMHP: return "MaxHP : " + sign + value;
                case GearPropType.incMHPr: return "MaxHP : " + sign + value + "%";
                case GearPropType.incMMP: return "MaxMP : " + sign + value;
                case GearPropType.incMMPr: return "MaxMP : " + sign + value + "%";
                case GearPropType.incMDF: return "MaxDF : " + sign + value;
                case GearPropType.incPAD: return "Attack Power: " + sign + value;
                case GearPropType.incPADr: return "Attack Power: " + sign + value + "%";
                case GearPropType.incMAD: return "Magic Attack: " + sign + value;
                case GearPropType.incMADr: return "Magic Attack: " + sign + value + "%";
                case GearPropType.incPDD: return "Defense: " + sign + value;
                case GearPropType.incPDDr: return "Defense: " + sign + value + "%";
                //case GearPropType.incMDD: return "MAGIC DEF. : " + sign + value;
                //case GearPropType.incMDDr: return "MAGIC DEF. : " + sign + value + "%";
                //case GearPropType.incACC: return "ACCURACY : " + sign + value;
                //case GearPropType.incACCr: return "ACCURACY : " + sign + value + "%";
                //case GearPropType.incEVA: return "AVOIDABILITY : " + sign + value;
                //case GearPropType.incEVAr: return "AVOIDABILITY : " + sign + value + "%";
                case GearPropType.incSpeed: return "Speed: " + sign + value;
                case GearPropType.incJump: return "Jump: " + sign + value;
                case GearPropType.incCraft: return "Diligence: " + sign + value;
                case GearPropType.damR:
                case GearPropType.incDAMr: return "Damage: " + sign + value + "%";
                case GearPropType.incCr: return "Critical Rate: " + sign + value + "%";
                case GearPropType.incCDr: return "Critical Damage: " + sign + value + "%";
                case GearPropType.knockback: return "Knockback Chance: " + value + "%";
                //case GearPropType.incPVPDamage: return "Battle Mode ATT " + sign + " " + value;
                case GearPropType.incPQEXPr: return "Party Quest EXP: +" + value + "%";
                case GearPropType.incEXPr: return "Party EXP: +" + value + "%";
                case GearPropType.incBDR:
                case GearPropType.bdR: return "Boss Damage: +" + value + "%";
                case GearPropType.incIMDR:
                case GearPropType.imdR: return "Ignored Enemy DEF: +" + value + "%";
                case GearPropType.limitBreak: return "Damage Cap: " + value.ToString("N0");
                case GearPropType.reduceReq: return "Required Level: -" + value;
                case GearPropType.nbdR: return "Damage Against Normal Monsters: +" + value + "%"; //KMST 1069

                case GearPropType.only: return value == 0 ? null : "One-of-a-kind Item";
                case GearPropType.tradeBlock: return value == 0 ? null : "Untradable";
                case GearPropType.equipTradeBlock: return value == 0 ? null : "Cannot be Traded when equipped";
                case GearPropType.accountSharable: return value == 0 ? null : "Account-bound. Transferable within world."; //v218 Transferable within world
                case GearPropType.sharableOnce: return value == 0 ? null : "Tradable once within the same world.\n(Cannot be traded after transfer)"; //old "Can be traded once within account"
                case GearPropType.onlyEquip: return value == 0 ? null : "Unique Equipped Item";
                case GearPropType.notExtend: return value == 0 ? null : "Duration cannot be extended.";
                case GearPropType.accountSharableAfterExchange: return value == 0 ? null : "Tradable once\nOnly tradable to your chars in this world after exch.";
                case GearPropType.mintable: return value == 0 ? null : "Mintable";
                case GearPropType.tradeAvailable:
                    switch (value)
                    {
                        case 1: return "#cUse the Scissors of Karma to enable this item to be traded one time.#";
                        case 2: return "#cUse the Platinum Scissors of Karma to\n\renable this item to be traded one time.#";
                        default: return null;
                    }
                case GearPropType.accountShareTag:
                    switch (value)
                    {
                        case 1: return "#cUse the Sharing Tag to move an item to another character on the same account once.#";
                        default: return null;
                    }
                //case GearPropType.noPotential: return value == 0 ? null : "This item cannot gain Potential.";
                case GearPropType.fixedPotential: return value == 0 ? null : "Potential Reset Not Allowed";
                case GearPropType.superiorEqp: return value == 0 ? null : "Allows you to gain even higher stats with successful item enhancement.";
                case GearPropType.nActivatedSocket: return value == 0 ? null : "#cYou can mount a Nebulite on this item.#";
                case GearPropType.jokerToSetItem: return value == 0 ? null : "\n#cThis lucky item counts towards any set, so long\n\ras you have at least 3 set pieces equipped!\n\r(However, the effect will apply to only one set if\n\ryou have 2 or more lucky items equipped.)#";//\n\r#cThis lucky...
                case GearPropType.plusToSetItem: return value == 0 ? null : "#cWhen equipped, the item set will count as having equipped two.#";
                case GearPropType.abilityTimeLimited: return value == 0 ? null : "Limited Time Stats";
                case GearPropType.blockGoldHammer: return value == 0 ? null : "Golden Hammer cannot be used.";
                case GearPropType.colorvar: return value == 0 ? null : "#cThis item can be dyed using a Dye.#";
                case GearPropType.cantRepair: return value == 0 ? null : "Cannot be repaired";
                case GearPropType.noLookChange: return value == 0 ? null : "Cannot use Fusion Anvil";

                case GearPropType.incAllStat_incMHP25: return "All Stats: " + sign + value + ", MaxHP : " + sign + (value * 25);// check once Lv 250 set comes out in GMS
                case GearPropType.incAllStat_incMHP50_incMMP50: return "All Stats: " + sign + value + ", MaxHP / MaxMP : " + sign + (value * 50);
                case GearPropType.incMHP_incMMP: return "MaxHP / MaxMP: " + sign + value;
                case GearPropType.incMHPr_incMMPr: return "MaxHP / MaxMP: " + sign + value + "%";
                case GearPropType.incPAD_incMAD:
                case GearPropType.incAD: return "Attack Power & Magic ATT: " + sign + value;
                case GearPropType.incPDD_incMDD: return "Defense: " + sign + value;
                //case GearPropType.incACC_incEVA: return "ACC/AVO :" + sign + value;

                case GearPropType.incARC: return "ARC : " + sign + value;
                case GearPropType.incAUT: return "SAC : " + sign + value;

                case GearPropType.Etuc: return "Exceptional Enhancement is possible.  (Max" + "\ntime(s):  " + value + ")";
                case GearPropType.CuttableCount: return "Scissors Usages Available : " + value;
                default: return null;
            }
        }

        public static string[] GetGearPropString22(GearPropType propType, long value, int signFlag)
        {
            string[] res = new string[2];
            string sign;
            switch (signFlag)
            {
                default:
                case 0: //默认处理符号
                    sign = value > 0 ? "+" : null;
                    break;

                case 1: //固定加号
                    sign = "+";
                    break;

                case 2: //无特别符号
                    sign = "";
                    break;
            }
            switch (propType)
            {
                case GearPropType.incSTR:
                    res[0] = "STR";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incSTRr:
                    res[0] = "STR";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incDEX:
                    res[0] = "DEX";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incDEXr:
                    res[0] = "DEX";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incINT:
                    res[0] = "INT";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incINTr:
                    res[0] = "INT";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incLUK:
                    res[0] = "LUK";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incLUKr:
                    res[0] = "LUK";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incAllStat:
                    res[0] = "All Stats";
                    res[1] = sign + value;
                    return res;
                case GearPropType.statR:
                    res[0] = "All Stats";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incMHP:
                    res[0] = "MaxHP";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incMHPr:
                    res[0] = "MaxHP";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incMMP:
                    res[0] = "MaxMP";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incMMPr:
                    res[0] = "MaxMP";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incMDF:
                    res[0] = "MaxDF";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incPAD:
                    res[0] = "Attack Power";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incPADr:
                    res[0] = "Attack Power";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incMAD:
                    res[0] = "Magic Attack";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incMADr:
                    res[0] = "Magic Attack";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incPDD:
                    res[0] = "Defense";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incPDDr:
                    res[0] = "Defense";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incSpeed:
                    res[0] = "Speed";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incJump:
                    res[0] = "Jump";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incCraft:
                    res[0] = "Diligence";
                    res[1] = sign + value;
                    return res;
                case GearPropType.damR:
                case GearPropType.incDAMr:
                    res[0] = "Damage";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incCr:
                    res[0] = "Critical Rate";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.incCDr:
                    res[0] = "Critical Damage";
                    res[1] = sign + value + "%";
                    return res;
                case GearPropType.knockback:
                    res[0] = "Knockback Chance: " + value + " % ";
                    return res;
                case GearPropType.incPQEXPr:
                    res[0] = "Party Quest EXP";
                    res[1] = "+" + value + "%";
                    return res;
                case GearPropType.incBDR:
                case GearPropType.bdR:
                    res[0] = "Boss Damage";
                    res[1] = "+" + value + "%";
                    return res;
                case GearPropType.incIMDR:
                case GearPropType.imdR:
                    res[0] = "Ignored Enemy DEF";
                    res[1] = "+" + value + "%";
                    return res;
                /*
            case GearPropType.attackSpeed:
                if (2 <= value && value <= 9)
                {
                    res[0] = "공격 속도";
                    res[1] = $"{10 - value}단계";
                }
                return res;
                */
                case GearPropType.nbdR:
                    res[0] = "Damage Against Normal Monsters";
                    res[1] = "+" + value + "%";
                    return res;
                case GearPropType.incARC:
                    res[0] = "ARC";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incAUT:
                    res[0] = "SAC";
                    res[1] = sign + value;
                    return res;
                case GearPropType.incCHUC:
                    res[0] = "Star Force";
                    res[1] = sign + value;
                    return res;

                case GearPropType.tradeBlock:
                    res[0] = value == 0 ? null : "#$r교환 불가#";
                    return res;
                case GearPropType.accountSharable:
                    res[0] = value == 0 ? null : "#$r월드 내 나의 캐릭터 간 이동만 가능#";
                    return res;
                case GearPropType.sharableOnce:
                    res[0] = value == 0 ? null : "#$r월드 내 나의 캐릭터 간 1회 이동 가능 (이동 후 교환불가)#";
                    return res;
                case GearPropType.only:
                    res[0] = value == 0 ? null : "#$r중복 소지 불가#";
                    return res;
                case GearPropType.onlyEquip:
                    res[0] = value == 0 ? null : "#$r중복 장착 불가#";
                    return res;
                case GearPropType.equipTradeBlock:
                    res[0] = value == 0 ? null : "#$r장착 시 교환 불가#";
                    return res;
                case GearPropType.notExtend:
                    res[0] = value == 0 ? null : " (연장 불가)";
                    return res;
                case GearPropType.accountSharableAfterExchange:
                    res[0] = value == 0 ? null : "#$r1회 교환 가능 (거래 후 월드 내 나의 캐릭터 간 이동만 가능)#";
                    return res;
                case GearPropType.timeLimited:
                    res[0] = value == 0 ? null : "유효 기간";
                    return res;
                case GearPropType.abilityTimeLimited:
                    res[0] = value == 0 ? null : "능력치 유효 기간";
                    return res;
                case GearPropType.noLookChange:
                    res[0] = value == 0 ? null : "#$r훈장 신비의 모루 사용 불가#";
                    return res;
                case GearPropType.mintable:
                    res[0] = value == 0 ? null : "#$r민팅 가능#";
                    return res;
                case GearPropType.tradeAvailable:
                    switch (value)
                    {
                        case 1:
                            res[0] = "#$g카르마의 가위 또는 실버 카르마의 가위 사용 시 1회 교환 가능#";
                            return res;
                        case 2:
                            res[0] = "#$g플래티넘 카르마의 가위 사용 시 1회 교환 가능#";
                            return res;
                        default: return res;
                    }
                case GearPropType.accountShareTag:
                    switch (value)
                    {
                        case 1:
                            res[0] = "#$g쉐어 네임 텍을 사용하면 월드 내 나의 캐릭터 간 1회 이동할 수 있습니다.#";
                            return res;
                        default: return res;
                    }
                //case GearPropType.noPotential: return value == 0 ? null : "잠재능력 설정 불가";
                //case GearPropType.fixedPotential: return value == 0 ? null : "잠재능력 재설정 불가";
                case GearPropType.superiorEqp:
                    res[0] = value == 0 ? null : "아이템 강화 성공시 더욱 높은 효과를 받을 수 있습니다.";
                    return res;
                //case GearPropType.jokerToSetItem: return value == 0 ? null : "#c3개 이상 착용하고 있는 모든 세트 아이템에 포함되는 럭키 아이템! (단, 2개 이상의 럭키 아이템 착용 시 1개만 효과 적용.)#";
                //case GearPropType.cantRepair: return value == 0 ? null : "수리 불가";

                case GearPropType.incAllStat_incMHP25:
                    res[0] = "All Stats  " + sign + value + ", MaxHP  " + sign + (value * 25);
                    return res;
                case GearPropType.incAllStat_incMHP50_incMMP50:
                    res[0] = "All Stats  " + sign + value + ", MaxHP / MaxMP  " + sign + (value * 50);
                    return res;
                case GearPropType.incMHP_incMMP:
                    res[0] = "MaxHP / MaxMP  " + sign + value;
                    return res;
                case GearPropType.incMHPr_incMMPr:
                    res[0] = "MaxHP / MaxMP  " + sign + value + "%";
                    return res;
                case GearPropType.incPAD_incMAD:
                case GearPropType.incAD:
                    res[0] = "Attack Power / Magic Attack  " + sign + " " + value;
                    return res;
                case GearPropType.incPDD_incMDD:
                    res[0] = "Defense  " + sign + value;
                    return res;

                case GearPropType.Etuc:
                    res[0] = $"#$d익셉셔널 : 없음 (최대 {value}회)#";
                    return res;
                case GearPropType.CuttableCount:
                    res[0] = $" #$r(가위 사용 잔여 횟수：{value} / {value})#";
                    return res;

                case GearPropType.incEXPr:
                default: return res;
            }
        }

        public static string GetGearPropDiffString(GearPropType propType, int value, int standardValue)
        {
            var propStr = GetGearPropString(propType, value);
            if (value > standardValue)
            {
                string suffix = null;
                switch (propType)
                {
                    case GearPropType.incSTR:
                    case GearPropType.incDEX:
                    case GearPropType.incINT:
                    case GearPropType.incLUK:
                    case GearPropType.incMHP:
                    case GearPropType.incMMP:
                    case GearPropType.incMDF:
                    case GearPropType.incARC:
                    case GearPropType.incAUT:
                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                    case GearPropType.incPDD:
                    case GearPropType.incMDD:
                    case GearPropType.incSpeed:
                    case GearPropType.incJump:
                        suffix = $"({standardValue} #$e+{value - standardValue}#)"; break;
                    case GearPropType.bdR:
                    case GearPropType.incBDR:
                    case GearPropType.imdR:
                    case GearPropType.incIMDR:
                    case GearPropType.damR:
                    case GearPropType.incDAMr:
                    case GearPropType.statR:
                        suffix = $"({standardValue}% #$y+{value - standardValue}%#)"; break;
                }
                propStr = "#$y" + propStr + "# " + suffix;
            }
            return propStr;
        }

        public static string[] GetGearPropDiffString22(GearPropType propType, int value, int standardValue)
        {
            string[] res = new string[3];

            var propStr = GetGearPropString22(propType, value, 0);
            res[0] = propStr[0];
            res[1] = propStr[1];

            if (value > standardValue)
            {
                string suffix = null;
                switch (propType)
                {
                    case GearPropType.incSTR:
                    case GearPropType.incDEX:
                    case GearPropType.incINT:
                    case GearPropType.incLUK:
                    case GearPropType.incMHP:
                    case GearPropType.incMMP:
                    case GearPropType.incMDF:
                    case GearPropType.incARC:
                    case GearPropType.incAUT:
                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                    case GearPropType.incPDD:
                    case GearPropType.incMDD:
                    case GearPropType.incSpeed:
                    case GearPropType.incJump:
                        suffix = $"({standardValue} #$e+{value - standardValue}#)"; break;
                    case GearPropType.bdR:
                    case GearPropType.incBDR:
                    case GearPropType.imdR:
                    case GearPropType.incIMDR:
                    case GearPropType.damR:
                    case GearPropType.incDAMr:
                    case GearPropType.statR:
                        suffix = $"({standardValue}% #$e+{value - standardValue}%#)"; break;
                }
                res[2] = suffix;
            }
            return res;
        }

        /// <summary>
        /// 获取gearGrade所对应的字符串。
        /// </summary>
        /// <param Name="rank">表示装备的潜能等级GearGrade。</param>
        /// <returns></returns>
        public static string GetGearGradeString(GearGrade rank)
        {
            switch (rank)
            {
                //case GearGrade.C: return "C级(一般物品)";
                case GearGrade.B: return "(Rare Item)";
                case GearGrade.A: return "(Epic Item)";
                case GearGrade.S: return "(Unique Item)";
                case GearGrade.SS: return "(Legendary Item)";
                case GearGrade.Special: return "(Special Item)";
                default: return null;
            }
        }

        /// <summary>
        /// 获取gearType所对应的字符串。
        /// </summary>
        /// <param Name="Type">表示装备类型GearType。</param>
        /// <returns></returns>
        public static string GetGearTypeString(GearType type)
        {
            switch (type)
            {
                //case GearType.body: return "Avatar (Body)";
                case GearType.head: return "Avatar (Head)";
                case GearType.head_n: return "Head";
                case GearType.face:
                case GearType.face2:
                case GearType.face_n: return "Face";
                case GearType.hair:
                case GearType.hair2:
                case GearType.hair3:
                case GearType.hair_n: return "Hair";
                case GearType.faceAccessory: return "Face Accessory";
                case GearType.eyeAccessory: return "Eye Accessory";
                case GearType.earrings: return "Earrings";
                case GearType.pendant: return "Pendant";
                case GearType.belt: return "Belt";
                case GearType.medal: return "Medal";
                case GearType.shoulderPad: return "Shoulder";
                case GearType.cap: return "Hat";
                case GearType.cape: return "Cape";
                case GearType.coat: return "Top";
                case GearType.dragonMask: return "Dragon Hat";
                case GearType.dragonPendant: return "Dragon Pendant";
                case GearType.dragonWings: return "Dragon Wing Accessory";
                case GearType.dragonTail: return "Dragon Tail Accessory";
                case GearType.glove: return "Gloves";
                case GearType.longcoat: return "Outfit";
                case GearType.machineEngine: return "Mechanic Engine";
                case GearType.machineArms: return "Mechanic Arm";
                case GearType.machineLegs: return "Mechanic Leg";
                case GearType.machineBody: return "Mechanic Frame";
                case GearType.machineTransistors: return "Mechanic Transistor";
                case GearType.pants: return "Bottom";
                case GearType.ring: return "Ring";
                case GearType.shield: return "Shield";
                case GearType.shoes: return "Shoes";
                case GearType.shiningRod: return "Shining Rod";
                case GearType.soulShooter: return "Soul Shooter";
                case GearType.ohSword: return "One-handed Sword";
                case GearType.ohAxe: return "One-handed Axe";
                case GearType.ohBlunt: return "One-handed Blunt Weapon";
                case GearType.dagger: return "Dagger";
                case GearType.katara: return "Katara";
                case GearType.magicArrow: return "Magic Arrow";
                case GearType.card: return "Card";
                case GearType.box: return "Core";
                case GearType.orb: return "Orb";
                case GearType.novaMarrow: return "Dragon Essence";
                case GearType.soulBangle: return "Soul Ring";
                case GearType.mailin: return "Magnum";
                case GearType.cane: return "Cane";
                case GearType.wand: return "Wand";
                case GearType.staff: return "Staff";
                case GearType.thSword: return "Two-handed Sword";
                case GearType.thAxe: return "Two-handed Axe";
                case GearType.thBlunt: return "Two-handed Blunt Weapon";
                case GearType.spear: return "Spear";
                case GearType.polearm: return "Polearm";
                case GearType.bow: return "Bow";
                case GearType.crossbow: return "Crossbow";
                case GearType.throwingGlove: return "Claw";
                case GearType.knuckle: return "Knuckle";
                case GearType.gun: return "Gun";
                case GearType.android: return "Android";
                case GearType.machineHeart: return "Mechanical Heart";
                case GearType.pickaxe: return "Mining Tool";
                case GearType.shovel: return "Herbalism Tool";
                case GearType.pocket: return "Pocket Item";
                case GearType.dualBow: return "Dual Bowguns";
                case GearType.handCannon: return "Hand Cannon";
                case GearType.badge: return "Badge";
                case GearType.emblem: return "Emblem";
                case GearType.soulShield: return "Soul Shield";
                case GearType.demonShield: return "Demon Aegis";
                //case GearType.totem: return "Totem";
                case GearType.petEquip: return "Pet Equipment";
                case GearType.taming:
                case GearType.taming2:
                case GearType.taming3:
                case GearType.tamingChair: return "Tamed Monster";
                case GearType.saddle: return "Saddle";
                case GearType.katana: return "Katana";
                case GearType.fan: return "Fan";
                case GearType.swordZB: return "Heavy Sword";
                case GearType.swordZL: return "Long Sword";
                case GearType.weapon: return "Weapon";
                case GearType.subWeapon: return "Secondary Weapon";
                case GearType.heroMedal: return "Medallions";
                case GearType.rosario: return "Rosary";
                case GearType.chain: return "Iron Chain";
                case GearType.book1:
                case GearType.book2:
                case GearType.book3: return "Magic Book";
                case GearType.bowMasterFeather: return "Arrow Fletching";
                case GearType.crossBowThimble: return "Bow Thimble";
                case GearType.shadowerSheath: return "Dagger Scabbard";
                case GearType.nightLordPoutch: return "Charm";
                case GearType.viperWristband: return "Wrist Band";
                case GearType.captainSight: return "Far Sight";
                case GearType.cannonGunPowder:
                case GearType.cannonGunPowder2: return "Powder Keg";
                case GearType.aranPendulum: return "Mass";
                case GearType.evanPaper: return "Document";
                case GearType.battlemageBall: return "Magic Marble";
                case GearType.wildHunterArrowHead: return "Arrowhead";
                case GearType.cygnusGem: return "Jewel";
                case GearType.controller: return "Controller";
                case GearType.foxPearl: return "Fox Marble";
                case GearType.chess: return "Chess Piece";
                case GearType.powerSource: return "Power Source";
                case GearType.energySword: return "Whip Blade";
                case GearType.desperado: return "Desperado";
                case GearType.memorialStaff: return "Memorial Staff";
                case GearType.magicStick: return "Beast Tamer Scepter";
                case GearType.leaf:
                case GearType.leaf2: return "Leaf";
                case GearType.boxingClaw: return "Fist";
                case GearType.kodachi:
                case GearType.kodachi2: return "Kodachi";
                case GearType.espLimiter: return "Psy-limiter";

                case GearType.gauntletBuster: return "Arm Cannon";
                case GearType.explosivePill: return "Charge";

                case GearType.chain2: return "Chain";
                case GearType.magicGauntlet: return "Lucent Gauntlet";
                case GearType.transmitter: return "Warp Forge";
                case GearType.magicWing: return "Lucent Wings";
                case GearType.pathOfAbyss: return "Abyssal Path";

                case GearType.relic: return "Relic";
                case GearType.ancientBow: return "Ancient Bow";

                case GearType.handFan: return "Ritual Fan";
                case GearType.fanTassel: return "Fan Tassel";

                case GearType.tuner: return "Bladecaster";
                case GearType.bracelet: return "Bladebinder";

                case GearType.breathShooter: return "Whispershot";
                case GearType.weaponBelt: return "Weapon Belt";

                case GearType.ornament: return "Ornament";

                case GearType.chakram: return "Chakram";
                case GearType.hexSeeker: return "Hex Seeker";

                case GearType.boxingCannon: return "Martial Brace";
                case GearType.boxingSky: return "Brace Band";

                case GearType.arcaneSymbol: return "Arcane Symbol";
                case GearType.authenticSymbol: return "Sacred Symbol";
                case GearType.grandAuthenticSymbol: return "Grand Sacred Symbol";

                case GearType.jewel: return "宝玉";

                case GearType.celestialLight: return "Celestial Light";
                case GearType.compass: return "Compass";

                // case GearType.keir: return "Keir";
                // case GearType.gram: return "Gram";

                case GearType.longSword: return "장검";
                case GearType.yeouiGem: return "여의보주";
                default: return null;
            }
        }

        /// <summary>
        /// 获取武器攻击速度所对应的字符串。
        /// </summary>
        /// <param Name="attackSpeed">表示武器的攻击速度，通常为2~9的数字。</param>
        /// <returns></returns>
        public static string GetAttackSpeedString(int attackSpeed)
        {
            switch (attackSpeed)
            {
                case 2:
                case 3: return "Very Fast";
                case 4:
                case 5: return "Fast";
                case 6: return "Average";
                case 7:
                case 8: return "Slow";
                case 9: return "Very Slow";
                default:
                    return attackSpeed.ToString();
            }
        }

        /// <summary>
        /// 获取套装装备类型的字符串。
        /// </summary>
        /// <param Name="Type">表示套装装备类型的GearType。</param>
        /// <returns></returns>
        public static string GetSetItemGearTypeString(GearType type)
        {
            return GetGearTypeString(type);
        }

        /// <summary>
        /// 获取装备额外职业要求说明的字符串。
        /// </summary>
        /// <param Name="Type">表示装备类型的GearType。</param>
        /// <returns></returns>
        public static string GetExtraJobReqString(GearType type)
        {
            switch (type)
            {
                //0xxx
                case GearType.heroMedal: return "Hero only";
                case GearType.rosario: return "Paladin only";
                case GearType.chain: return "Dark Knight only";
                case GearType.book1: return "Fire/Poison Magician only";
                case GearType.book2: return "Ice/Lightning Magician only";
                case GearType.book3: return "Bishop Magician only";
                case GearType.bowMasterFeather: return "Bow Master only";
                case GearType.crossBowThimble: return "Marksman only";
                case GearType.relic: return "Pathfinder only";
                case GearType.shadowerSheath: return "Shadower only";
                case GearType.nightLordPoutch: return "Night Lord only";
                case GearType.katara: return "Dual Blade only";
                case GearType.viperWristband: return "Buccaneer only";
                case GearType.captainSight: return "Corsair only";
                case GearType.cannonGunPowder:
                case GearType.cannonGunPowder2: return "Cannoneer only";
                case GearType.box:
                case GearType.boxingClaw: return "Jett only";

                //1xxx
                case GearType.cygnusGem: return "Cygnus Knights only";

                //2xxx
                case GearType.aranPendulum: return GetExtraJobReqString(21);
                case GearType.dragonMask:
                case GearType.dragonPendant:
                case GearType.dragonWings:
                case GearType.dragonTail:
                case GearType.evanPaper: return GetExtraJobReqString(22);
                case GearType.magicArrow: return GetExtraJobReqString(23);
                case GearType.card: return GetExtraJobReqString(24);
                case GearType.foxPearl: return GetExtraJobReqString(25);
                case GearType.orb:
                case GearType.shiningRod: return GetExtraJobReqString(27);

                //3xxx
                case GearType.demonShield: return GetExtraJobReqString(31);
                case GearType.desperado: return "Demon Avenger only";
                case GearType.battlemageBall: return "Battle Mage only";
                case GearType.wildHunterArrowHead: return "Wild Hunter only";
                case GearType.machineEngine:
                case GearType.machineArms:
                case GearType.machineLegs:
                case GearType.machineBody:
                case GearType.machineTransistors:
                case GearType.mailin: return "Mechanic only";
                case GearType.controller:
                case GearType.powerSource:
                case GearType.energySword: return GetExtraJobReqString(36);
                case GearType.gauntletBuster:
                case GearType.explosivePill: return GetExtraJobReqString(37);

                //4xxx
                case GearType.katana:
                case GearType.kodachi:
                case GearType.kodachi2: return GetExtraJobReqString(41);
                case GearType.fan: return "Kanna only"; //Haku only?

                //5xxx
                case GearType.soulShield: return "Mihile only";

                //6xxx
                case GearType.novaMarrow: return GetExtraJobReqString(61);
                case GearType.weaponBelt:
                case GearType.breathShooter: return GetExtraJobReqString(63);
                case GearType.chain2:
                case GearType.transmitter: return GetExtraJobReqString(64);
                case GearType.soulBangle:
                case GearType.soulShooter: return GetExtraJobReqString(65);

                //10xxx
                case GearType.swordZB:
                case GearType.swordZL: return GetExtraJobReqString(101);

                case GearType.leaf:
                case GearType.leaf2:
                case GearType.memorialStaff: return GetExtraJobReqString(172);
                case GearType.magicStick: return GetExtraJobReqString(112);

                case GearType.espLimiter:
                case GearType.chess: return GetExtraJobReqString(142);

                case GearType.magicGauntlet:
                case GearType.magicWing: return GetExtraJobReqString(152);

                case GearType.pathOfAbyss: return GetExtraJobReqString(155);
                case GearType.handFan:
                case GearType.fanTassel: return GetExtraJobReqString(164);

                case GearType.tuner:
                case GearType.bracelet: return GetExtraJobReqString(151);

                case GearType.boxingCannon:
                case GearType.boxingSky: return GetExtraJobReqString(175);

                case GearType.ornament: return GetExtraJobReqString(162);

                case GearType.chakram:
                case GearType.hexSeeker: return GetExtraJobReqString(154);

                case GearType.longSword:
                case GearType.yeouiGem: return GetExtraJobReqString(161);
                default: return null;
            }
        }

        /// <summary>
        /// 获取装备额外职业要求说明的字符串。
        /// </summary>
        /// <param Name="specJob">表示装备属性的reqSpecJob的值。</param>
        /// <returns></returns>
        public static string GetExtraJobReqString(int specJob)
        {
            switch (specJob)
            {
                case 21: return "Aran only";
                case 22: return "Evan only";
                case 23: return "Mercedes only";
                case 24: return "Phantom only";
                case 25: return "Shade only";
                case 27: return "Luminous only";
                case 31: return "Demon only";
                case 36: return "Xenon only";
                case 37: return "Blaster only";
                case 41: return "Hayato only";
                case 42: return "Kanna only";
                case 51: return "Mihile only";
                case 61: return "Kaiser only";
                case 63: return "Kain only";
                case 64: return "Cadena only";
                case 65: return "Angelic Buster only";
                case 101: return "Zero only";
                case 112: return "Beast Tamer only";
                case 142: return "Kinesis only";
                case 151: return "Adele only";
                case 152: return "Illium only";
                case 154: return "Khali only";
                case 155: return "Ark only";
                case 161: return "Len only";
                case 162: return "Lara only";
                case 164: return "Hoyoung only";
                case 172: return "Lynn only";
                case 175: return "Mo Xuan only";
                default: return null;
            }
        }

        public static string GetExtraJobReqString(IEnumerable<int> specJobs)
        {
            return string.Join(", ", GetExtraJobReqStringList(specJobs)) + " only";
        }

        public static List<string> GetExtraJobReqStringList(IEnumerable<int> specJobs)
        {
            List<string> extraJobNames = new List<string>();
            foreach (int specJob in specJobs)
            {
                switch (specJob)
                {
                    case 1: extraJobNames.AddRange(new[] { "Hero", "Paladin" }); break;
                    case 2: extraJobNames.AddRange(new[] { "Arch Mage (Ice, Lightning)", "Arch Mage (Fire, Poison)", "Bishop" }); break;
                    case 4: extraJobNames.Add("Shadower"); break;
                    case 11: extraJobNames.Add("Dawn Warrior"); break;
                    case 12: extraJobNames.Add("Blaze Wizard"); break;
                    case 22: extraJobNames.Add("Evan"); break;
                    case 32: extraJobNames.Add("Battle Mage"); break;
                    case 172: extraJobNames.Add("Lynn"); break;
                    default: extraJobNames.Add(specJob.ToString()); break;
                }
            }
            if (extraJobNames.Count == 0)
            {
                return null;
            }
            return extraJobNames;
        }

        public static string GetItemPropString(ItemPropType propType, long value)
        {
            switch (propType)
            {
                case ItemPropType.tradeBlock:
                    return GetGearPropString(GearPropType.tradeBlock, value);
                case ItemPropType.useTradeBlock:
                    return value == 0 ? null : "Cannot be traded after use";
                case ItemPropType.tradeAvailable:
                    return GetGearPropString(GearPropType.tradeAvailable, value);
                case ItemPropType.only:
                    return GetGearPropString(GearPropType.only, value);
                case ItemPropType.accountSharable:
                    return GetGearPropString(GearPropType.accountSharable, value);
                case ItemPropType.sharableOnce:
                    return GetGearPropString(GearPropType.sharableOnce, value);
                case ItemPropType.exchangeableOnce:
                    return value == 0 ? null : "Tradable once (untradable after using or trading)";
                case ItemPropType.accountSharableAfterExchange:
                    return GetGearPropString(GearPropType.accountSharableAfterExchange, value);
                case ItemPropType.quest:
                    return value == 0 ? null : "Quest Item";
                case ItemPropType.pquest:
                    return value == 0 ? null : "Party Quest Item";
                case ItemPropType.permanent:
                    return value == 0 ? null : "PERMANENT";//GMS PLACEHOLDER?
                case ItemPropType.multiPet://GMS string for: "Normal Pet (Cannot be used with other pets)" and "Multi Pet (Can use up to 3 pets at once)"
                    return value == 0 ? "" : "";
                case ItemPropType.mintable:
                    return GetGearPropString(GearPropType.mintable, value);
                default:
                    return null;
            }
        }

        public static string GetItemCoreSpecString(ItemCoreSpecType coreSpecType, int value, string desc)
        {
            bool hasCoda = false;
            if (desc?.Length > 0)
            {
                char lastCharacter = desc.Last();
                hasCoda = lastCharacter >= '가' && lastCharacter <= '힣' && (lastCharacter - '가') % 28 != 0;
            }
            switch (coreSpecType)
            {
                case ItemCoreSpecType.Ctrl_mobLv:
                    return value == 0 ? null : "Monster Level " + "+" + value;
                case ItemCoreSpecType.Ctrl_mobHPRate:
                    return value == 0 ? null : "Monster HP " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_mobRate:
                    return value == 0 ? null : "Monster Population " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_mobRateSpecial:
                    return value == 0 ? null : "Monster Population " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_change_Mob:
                    return desc == null ? null : "Change monster skins for " + desc;
                case ItemCoreSpecType.Ctrl_change_BGM:
                    return desc == null ? null : "Change music for " + desc;
                case ItemCoreSpecType.Ctrl_change_BackGrnd:
                    return desc == null ? null : "Change background image for " + desc;
                case ItemCoreSpecType.Ctrl_partyExp:
                    return value == 0 ? null : "Party EXP " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_partyExpSpecial:
                    return value == 0 ? null : "Party EXP " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_addMob:
                    return value == 0 || desc == null ? null : desc + ", Link " + value + " added to area";
                case ItemCoreSpecType.Ctrl_dropRate:
                    return value == 0 ? null : "Drop Rate " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_dropRateSpecial:
                    return value == 0 ? null : "Drop Rate " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_dropRate_Herb:
                    return value == 0 ? null : "Herb Drop Rate " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_dropRate_Mineral:
                    return value == 0 ? null : "Mineral Drop Rate " + "+" + value + "%";
                case ItemCoreSpecType.Ctrl_dropRareEquip:
                    return value == 0 ? null : "Rare Equipment Drop";
                case ItemCoreSpecType.Ctrl_reward:
                case ItemCoreSpecType.Ctrl_addMission:
                    return desc;
                default:
                    return null;
            }
        }

        public static string GetSkillReqAmount(int skillID, int reqAmount)
        {
            switch (skillID / 10000)
            {
                case 11200: return "[Required Bear Skill Point(s): " + reqAmount + "]";
                case 11210: return "[Required Leopard Skill Point(s): " + reqAmount + "]";
                case 11211: return "[Required Hawk Skill Point(s): " + reqAmount + "]";
                case 11212: return "[Required Cat Skill Point(s): " + reqAmount + "]";
                default: return "[Required ?? Skill Point(s): " + reqAmount + "]";
            }
        }

        public static string GetJobName(int jobCode)
        {
            switch (jobCode)
            {
                case 0: return "Beginner";
                case 100: return "Swordsman";
                case 110: return "Fighter";
                case 111: return "Crusader";
                case 112: return "Hero";
                case 113: return "Hero(5)";
                case 114: return "Hero(6)";
                case 120: return "Page";
                case 121: return "White Knight";
                case 122: return "Paladin";
                case 123: return "Paladin(5)";
                case 124: return "Paladin(6)";
                case 130: return "Spearman";
                case 131: return "Dragon Knight";
                case 132: return "Dark Knight";
                case 133: return "Dark Knight(5)";
                case 134: return "Dark Knight(6)";
                case 200: return "Magician";
                case 210: return "Wizard (Fire,Poison)";
                case 211: return "Mage (Fire, Poison)";
                case 212: return "Arch Mage (Fire,Poison)";
                case 213: return "Arch Mage (Fire,Poison)(5)";
                case 214: return "Arch Mage (Fire,Poison)(6)";
                case 220: return "Wizard (Ice,Lightning)";
                case 221: return "Mage (Ice,Lightning)";
                case 222: return "Arch Mage (Ice,Lightning)";
                case 223: return "Arch Mage (Ice,Lightning)(5)";
                case 224: return "Arch Mage (Ice,Lightning)(6)";
                case 230: return "Cleric";
                case 231: return "Priest";
                case 232: return "Bishop";
                case 233: return "Bishop(5)";
                case 234: return "Bishop(6)";
                case 300: return "Archer";
                case 301: return "Archer";
                case 310: return "Hunter";
                case 311: return "Ranger";
                case 312: return "Bowmaster";
                case 313: return "Bowmaster(5)";
                case 314: return "Bowmaster(6)";
                case 320: return "Crossbowman";
                case 321: return "Sniper";
                case 322: return "Marksman";
                case 323: return "Marksman(5)";
                case 324: return "Marksman(6)";
                case 330: return "Ancient Archer";
                case 331: return "Soulchaser";
                case 332: return "Pathfinder";
                case 333: return "Pathfinder(5)";
                case 334: return "Pathfinder(6)";
                case 400: return "Rogue";
                case 410: return "Assassin";
                case 411: return "Hermit";
                case 412: return "Night Lord";
                case 413: return "Night Lord(5)";
                case 414: return "Night Lord(6)";
                case 420: return "Thief";
                case 421: return "Chief Bandit";
                case 422: return "Shadower";
                case 423: return "Shadower(5)";
                case 424: return "Shadower(6)";
                case 430: return "Blade Recruit";
                case 431: return "Blade Acolyte";
                case 432: return "Blade Specialist";
                case 433: return "Blade Loard";
                case 434: return "Blade Master";
                case 435: return "Blade Master(5)";
                case 436: return "Blade Master(6)";
                case 500: return "Pirate";
                case 501: return "Pirate";
                case 510: return "Brawler";
                case 511: return "Marauder";
                case 512: return "Buccaneer";
                case 513: return "Buccaneer(5)";
                case 514: return "Buccaneer(6)";
                case 520: return "Gunslinger";
                case 521: return "Outlaw";
                case 522: return "Corsair";
                case 523: return "Corsair(5)";
                case 524: return "Corsair(6)";
                case 530: return "Cannoneer";
                case 531: return "Cannon Trooper";
                case 532: return "Cannon Master";
                case 533: return "Cannon Master(5)";
                case 534: return "Cannon Master(6)";
                case 508: return "Jett(1)";
                case 570: return "Jett(2)";
                case 571: return "Jett(3)";
                case 572: return "Jett(4)";

                case 800:
                case 900: return "Operator";

                case 1000: return "Noblesse";
                case 1100: return "Dawn Warrior(1)";
                case 1110: return "Dawn Warrior(2)";
                case 1111: return "Dawn Warrior(3)";
                case 1112: return "Dawn Warrior(4)";
                case 1113: return "Dawn Warrior(5)";
                case 1114: return "Dawn Warrior(6)";
                case 1200: return "Blaze Wizard(1)";
                case 1210: return "Blaze Wizard(2)";
                case 1211: return "Blaze Wizard(3)";
                case 1212: return "Blaze Wizard(4)";
                case 1213: return "Blaze Wizard(5)";
                case 1214: return "Blaze Wizard(6)";
                case 1300: return "Wind Archer(1)";
                case 1310: return "Wind Archer(2)";
                case 1311: return "Wind Archer(3)";
                case 1312: return "Wind Archer(4)";
                case 1313: return "Wind Archer(5)";
                case 1314: return "Wind Archer(6)";
                case 1400: return "Night Walker(1)";
                case 1410: return "Night Walker(2)";
                case 1411: return "Night Walker(3)";
                case 1412: return "Night Walker(4)";
                case 1413: return "Night Walker(5)";
                case 1414: return "Night Walker(6)";
                case 1500: return "Thunder Breaker(1)";
                case 1510: return "Thunder Breaker(2)";
                case 1511: return "Thunder Breaker(3)";
                case 1512: return "Thunder Breaker(4)";
                case 1513: return "Thunder Breaker(5)";
                case 1514: return "Thunder Breaker(6)";

                case 2000: return "Legend";
                case 2001: return "Evan";
                case 2002: return "Mercedes";
                case 2003: return "Phantom";
                case 2004: return "Luminous";
                case 2005: return "Shade";
                case 2100: return "Aran(1)";
                case 2110: return "Aran(2)";
                case 2111: return "Aran(3)";
                case 2112: return "Aran(4)";
                case 2113: return "Aran(5)";
                case 2114: return "Aran(6)";
                case 2200:
                case 2210: return "Evan(1)";
                case 2211:
                case 2212:
                case 2213: return "Evan(2)";
                case 2214:
                case 2215:
                case 2216: return "Evan (3)";
                case 2217:
                case 2218: return "Evan(4)";
                case 2219: return "Evan(5)";
                case 2220: return "Evan(6)";
                case 2300: return "Mercedes(1)";
                case 2310: return "Mercedes(2)";
                case 2311: return "Mercedes(3)";
                case 2312: return "Mercedes(4)";
                case 2313: return "Mercedes(5)";
                case 2314: return "Mercedes(6)";
                case 2400: return "Phantom(1)";
                case 2410: return "Phantom(2)";
                case 2411: return "Phantom(3)";
                case 2412: return "Phantom(4)";
                case 2413: return "Phantom(5)";
                case 2414: return "Phantom(6)";
                case 2500: return "Shade(1)";
                case 2510: return "Shade(2)";
                case 2511: return "Shade(3)";
                case 2512: return "Shade(4)";
                case 2513: return "Shade(5)";
                case 2514: return "Shade(6)";
                case 2700: return "Luminous(1)";
                case 2710: return "Luminous(2)";
                case 2711: return "Luminous(3)";
                case 2712: return "Luminous(4)";
                case 2713: return "Luminous(5)";
                case 2714: return "Luminous(6)";

                case 3000: return "Citizen";
                case 3001: return "Demon";
                case 3100: return "Demon Slayer(1)";
                case 3110: return "Demon Slayer(2)";
                case 3111: return "Demon Slayer(3)";
                case 3112: return "Demon Slayer(4)";
                case 3113: return "Demon Slayer(5)";
                case 3114: return "Demon Slayer(6)";
                case 3101: return "Demon Avenger(1)";
                case 3120: return "Demon Avenger(2)";
                case 3121: return "Demon Avenger(3)";
                case 3122: return "Demon Avenger(4)";
                case 3123: return "Demon Avenger(5)";
                case 3124: return "Demon Avenger(6)";
                case 3200: return "Battle Mage(1)";
                case 3210: return "Battle Mage(2)";
                case 3211: return "Battle Mage(3)";
                case 3212: return "Battle Mage(4)";
                case 3213: return "Battle Mage(5)";
                case 3214: return "Battle Mage(6)";
                case 3300: return "Wild Hunter(1)";
                case 3310: return "Wild Hunter(2)";
                case 3311: return "Wild Hunter(3)";
                case 3312: return "Wild Hunter(4)";
                case 3313: return "Wild Hunter(5)";
                case 3314: return "Wild Hunter(6)";
                case 3500: return "Mechanic(1)";
                case 3510: return "Mechanic(2)";
                case 3511: return "Mechanic(3)";
                case 3512: return "Mechanic(4)";
                case 3513: return "Mechanic(5)";
                case 3514: return "Mechanic(6)";
                case 3002: return "Xenon";
                case 3600: return "Xenon(1)";
                case 3610: return "Xenon(2)";
                case 3611: return "Xenon(3)";
                case 3612: return "Xenon(4)";
                case 3613: return "Xenon(5)";
                case 3614: return "Xenon(6)";
                case 3700: return "Blaster(1)";
                case 3710: return "Blaster(2)";
                case 3711: return "Blaster(3)";
                case 3712: return "Blaster(4)";
                case 3713: return "Blaster(5)";
                case 3714: return "Blaster(6)";

                case 4001: return "Hayato";
                case 4002: return "Kanna";
                case 4100: return "Hayato(1)";
                case 4110: return "Hayato(2)";
                case 4111: return "Hayato(3)";
                case 4112: return "Hayato(4)";
                case 4113: return "Hayato(5)";
                case 4114: return "Hayato(6)";
                case 4200: return "Kanna(1)";
                case 4210: return "Kanna(2)";
                case 4211: return "Kanna(3)";
                case 4212: return "Kanna(4)";
                case 4213: return "Kanna(5)";
                case 4214: return "Kanna(6)";

                case 5000: return "Mihile";
                case 5100: return "Mihile(1)";
                case 5110: return "Mihile(2)";
                case 5111: return "Mihile(3)";
                case 5112: return "Mihile(4)";
                case 5113: return "Mihile(5)";
                case 5114: return "Mihile(6)";

                case 6000: return "Kaiser";
                case 6001: return "Angelic Buster";
                case 6002: return "Cadena";
                case 6003: return "Kain";
                case 6100: return "Kaiser(1)";
                case 6110: return "Kaiser(2)";
                case 6111: return "Kaiser(3)";
                case 6112: return "Kaiser(4)";
                case 6113: return "Kaiser(5)";
                case 6114: return "Kaiser(6)";
                case 6300: return "Kain(1)";
                case 6310: return "Kain(2)";
                case 6311: return "Kain(3)";
                case 6312: return "Kain(4)";
                case 6313: return "Kain(5)";
                case 6314: return "Kain(6)";
                case 6400: return "Cadena(1)";
                case 6410: return "Cadena(2)";
                case 6411: return "Cadena(3)";
                case 6412: return "Cadena(4)";
                case 6413: return "Cadena(5)";
                case 6414: return "Cadena(6)";
                case 6500: return "Angelic Buster(1)";
                case 6510: return "Angelic Buster(2)";
                case 6511: return "Angelic Buster(3)";
                case 6512: return "Angelic Buster(4)";
                case 6513: return "Angelic Buster(5)";
                case 6514: return "Angelic Buster(6)";

                case 10000: return "Zero";
                case 10100: return "Zero(1)";
                case 10110: return "Zero(2)";
                case 10111: return "Zero(3)";
                case 10112: return "Zero(4)";
                case 10113: return "Zero(5)";
                case 10114: return "Zero(6)";

                case 11000: return "Chase";
                case 11200: return "Beast Tamer(1)";
                case 11210: return "Beast Tamer(2)";
                case 11211: return "Beast Tamer(3)";
                case 11212: return "Beast Tamer(4)";

                case 12005:
                case 12100: return "Tanjiro Kamado";

                case 13000: return "Pink Bean";
                case 13001: return "Yetihood";
                case 13100: return "Pink Bean";
                case 13500: return "Yeti";

                case 14000: return "Kinesis";
                case 14200: return "Kinesis(1)";
                case 14210: return "Kinesis(2)";
                case 14211: return "Kinesis(3)";
                case 14212: return "Kinesis(4)";
                case 14213: return "Kinesis(5)";
                case 14214: return "Kinesis(6)";

                case 15000: return "Illium";
                case 15001: return "Ark";
                case 15002: return "Adele";
                case 15003: return "Khali";
                case 15100: return "Adele(1)";
                case 15110: return "Adele(2)";
                case 15111: return "Adele(3)";
                case 15112: return "Adele(4)";
                case 15113: return "Adele(5)";
                case 15114: return "Adele(6)";
                case 15200: return "Illium(1)";
                case 15210: return "Illium(2)";
                case 15211: return "Illium(3)";
                case 15212: return "Illium(4)";
                case 15213: return "Illium(5)";
                case 15214: return "Illium(6)";
                case 15400: return "Khali(1)";
                case 15410: return "Khali(2)";
                case 15411: return "Khali(3)";
                case 15412: return "Khali(4)";
                case 15413: return "Khali(5)";
                case 15414: return "Khali(6)";
                case 15500: return "Ark(1)";
                case 15510: return "Ark(2)";
                case 15511: return "Ark(3)";
                case 15512: return "Ark(4)";
                case 15513: return "Ark(5)";
                case 15514: return "Ark(6)";

                case 16000: return "Anima Thief";
                case 16001: return "Lara";
                case 16002: return "Len";
                case 16100: return "Len(1)";
                case 16110: return "Len(2)";
                case 16111: return "Len(3)";
                case 16112: return "Len(4)";
                case 16114: return "Len(6)";
                case 16200: return "Lara(1)";
                case 16210: return "Lara(2)";
                case 16211: return "Lara(3)";
                case 16212: return "Lara(4)";
                case 16213: return "Lara(5)";
                case 16214: return "Lara(6)";
                case 16400: return "Hoyoung(1)";
                case 16410: return "Hoyoung(2)";
                case 16411: return "Hoyoung(3)";
                case 16412: return "Hoyoung(4)";
                case 16413: return "Hoyoung(5)";
                case 16414: return "Hoyoung(6)";

                case 17000: return "Mo Xuan";
                case 17001: return "Lynn";
                case 17200: return "Lynn(1)";
                case 17210: return "Lynn(2)";
                case 17211: return "Lynn(3)";
                case 17212: return "Lynn(4)";
                case 17213: return "Lynn(5)";
                case 17214: return "Lynn(6)";
                case 17500: return "Mo Xuan(1)";
                case 17510: return "Mo Xuan(2)";
                case 17511: return "Mo Xuan(3)";
                case 17512: return "Mo Xuan(4)";
                case 17513: return "Mo Xuan(5)";
                case 17514: return "Mo Xuan(6)";

                case 18000: return "Sia Astelle";
                case 18001: return "Erel Light";
                case 18100: return "Erel Light(1)";
                case 18110: return "Erel Light(2)";
                case 18111: return "Erel Light(3)";
                case 18112: return "Erel Light(4)";
                case 18114: return "Erel Light(6)";
                case 18200: return "Sia Astelle(1)";
                case 18210: return "Sia Astelle(2)";
                case 18211: return "Sia Astelle(3)";
                case 18212: return "Sia Astelle(4)";
                case 18214: return "Sia Astelle(6)";

                case 40000: return "5th";
                case 40001: return "5th (Warrior)";
                case 40002: return "5th (Magician)";
                case 40003: return "5th (Bowman)";
                case 40004: return "5th (Thief)";
                case 40005: return "5th (Pirate)";

                case 50000: return "6th";
                case 50006: return "6th (Enhanced Node)";
                case 50007: return "6th (HEXA Stat)";
            }
            return null;
        }

        /* Not required for GMS
        private static string ToChineseNumberExpr(long value)
        {
            var sb = new StringBuilder(16);
            bool firstPart = true;
            if (value < 0)
            {
                sb.Append("-");
                value = -value; // just ignore the exception -2147483648
            }
            if (value >= 1_0000_0000)
            {
                long part = value / 1_0000_0000;
                sb.AppendFormat("{0}亿", part);
                value -= part * 1_0000_0000;
                firstPart = false;
            }
            if (value >= 1_0000)
            {
                long part = value / 1_0000;
                sb.Append(firstPart ? null : " ");
                sb.AppendFormat("{0}万", part);
                value -= part * 1_0000;
                firstPart = false;
            }
            if (value > 0)
            {
                sb.Append(firstPart ? null : " ");
                sb.AppendFormat("{0}", value);
            }

            return sb.Length > 0 ? sb.ToString() : "0";
        }
        */

        public static string GetMobSkillName(int id)
        {
            switch (id)
            {
                case 100: return "ATT increase";
                case 101: return "Magic ATT increase";
                case 102: return "DEF increase";
                case 103: return "Magic DEF increase";
                case 105: return "HP absorption";
                case 110: return "Area ATT increase";
                case 111: return "Area Magic ATT increase";
                case 112: return "Area DEF increase";
                case 113: return "Area Magic DEF increase";
                case 114: return "HP recovery";
                case 115: return "Movement speed increase";
                case 120: return "Seal";
                case 121: return "Miss";
                case 122: return "Weak";
                case 123: return "Faint";
                case 124: return "Curse";
                case 125: return "Poisoning";
                case 126: return "Slow";
                case 127: return "Remove buff";
                case 128: return "Temptation";
                case 129: return "Banish";
                case 131: return "Area continuous damage";
                case 132: return "Confusion";
                case 133: return "Undead";
                case 134: return "Potion seal";
                case 135: return "Do not stop";
                case 136: return "Darkness";
                case 137: return "Pronounced";
                case 138: return "Nullify potential";
                case 140: return "Ignore physical attacks";
                case 141: return "Ignore magic attacks";
                case 142: return "Hard skin";
                case 143: return "Physical attack reflection";
                case 144: return "Magic attack reflection";
                case 145: return "Attack reflection";
                case 146: return "Invincible";

                case 150: return "ATT increase";
                case 151: return "Magic ATT increase";
                case 152: return "DEF increase";
                case 153: return "Magic DEF increase";
                case 154: return "Accuracy rate increase";
                case 155: return "Evasion rate increase";
                case 156: return "Movement speed increase";

                case 170: return "Teleport";
                case 171: return "Explosion";
                case 172: return "Mutation";
                case 173: return "Airborne";
                case 174: return "Petrification";

                case 200: return "Summon";
                case 201: return "Summon";

                default: return null;
            }
        }
    }
}
