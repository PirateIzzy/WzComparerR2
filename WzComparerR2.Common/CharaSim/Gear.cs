﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Gear : ItemBase
    {
        public Gear()
        {
            Props = new Dictionary<GearPropType, int>();
            VariableStat = new Dictionary<GearPropType, float>();
            AbilityTimeLimited = new Dictionary<GearPropType, int>();
            ReqSpecJobs = new List<int>();
            Options = new Potential[3];
            AdditionalOptions = new Potential[3];
            Additions = new List<Addition>();
        }
        public GearGrade Grade { get; set; }
        public GearGrade AdditionGrade { get; set; }
        public GearType type;
        public GearState State { get; set; }

        public int diff
        {
            get { return Compare(this); }
            set { }
        }

        public Potential[] Options { get; private set; }
        public Potential[] AdditionalOptions { get; private set; }
        public AlienStone AlienStoneSlot { get; set; }

        public int Star { get; set; }
        public int ScrollUp { get; set; }
        public int Hammer { get; set; }
        public bool HasTuc { get; internal set; }
        public int PlatinumHammer { get; set; }
        public bool CanPotential { get; internal set; }
        public string EpicHs { get; internal set; }
        public BitmapOrigin ToolTIpPreview { get; set; }
        public Bitmap AndroidBitmap { get; set; }
        public string LabelGradeTooltip { get; internal set; }

        public bool FixLevel { get; internal set; }
        public List<GearLevelInfo> Levels { get; internal set; }
        public List<GearSealedInfo> Seals { get; internal set; }

        public List<Addition> Additions { get; private set; }
        public bool AdditionHideDesc { get; set; }
        public Dictionary<GearPropType, int> Props { get; private set; }
        public Dictionary<GearPropType, float> VariableStat { get; private set; }
        public Dictionary<GearPropType, int> AbilityTimeLimited { get; private set; }
        public List<int> ReqSpecJobs { get; private set; }

        /// <summary>
        /// 获取或设置装备的标准属性。
        /// </summary>
        public Dictionary<GearPropType, int> StandardProps { get; private set; }


        public bool Epic
        {
            get { return GetBooleanValue(GearPropType.epicItem); }
        }

        public bool TimeLimited
        {
            get { return GetBooleanValue(GearPropType.timeLimited); }
        }

        public bool Cash
        {
            get { return GetBooleanValue(GearPropType.cash); }
        }

        public bool GetBooleanValue(GearPropType type)
        {
            int value;
            return this.Props.TryGetValue(type, out value) && value != 0;
        }

        public IEnumerable<KeyValuePair<GearPropType, int>> PropsV5
        {
            get
            {
                return this.Props.Where(kv => IsV5SupportPropType(kv.Key));
            }
        }

        public int GetMaxStar(bool isPostNEXTClient = false)
        {
            if (!this.HasTuc)
            {
                return 0;
            }
            if (this.Cash)
            {
                return 0;
            }
            if (this.GetBooleanValue(GearPropType.onlyUpgrade))
            {
                return 0;
            }
            if (this.type == GearType.machineEngine || this.type == GearType.machineArms || this.type == GearType.machineLegs || this.type == GearType.machineBody || this.type == GearType.machineTransistors || this.type == GearType.dragonMask || this.type == GearType.dragonPendant || this.type == GearType.dragonWings || this.type == GearType.dragonTail)
            {
                return 0;
            }

            int reqLevel;
            this.Props.TryGetValue(GearPropType.reqLevel, out reqLevel);
            int[] data = null;
            if (isPostNEXTClient)
            {
                foreach (int[] item in starDataPostNEXT)
                {
                    if (reqLevel >= item[0])
                    {
                        data = item;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                foreach (int[] item in starData)
                {
                    if (reqLevel >= item[0])
                    {
                        data = item;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (data == null)
            {
                return 0;
            }

            return data[this.GetBooleanValue(GearPropType.superiorEqp) ? 2 : 1];
        }

        private static readonly int[][] starData = new int[][] {
            new[]{ 0, 5, 3 },
            new[]{ 95, 8, 5 },
            new[]{ 108, 10, 8 },
            new[]{ 118, 15, 10 },
            new[]{ 128, 20, 12 },
            new[]{ 138, 25, 15 },
        };

        private static readonly int[][] starDataPostNEXT = new int[][] {
            new[]{ 0, 5, 3 },
            new[]{ 95, 8, 5 },
            new[]{ 108, 10, 8 },
            new[]{ 118, 15, 10 },
            new[]{ 128, 20, 12 },
            new[]{ 138, 30, 15 },
        };

        public override object Clone()
        {
            Gear gear = (Gear)this.MemberwiseClone();
            gear.Props = new Dictionary<GearPropType, int>(this.Props.Count);
            foreach (KeyValuePair<GearPropType, int> p in this.Props)
            {
                gear.Props.Add(p.Key, p.Value);
            }
            gear.Options = (Potential[])this.Options.Clone();
            gear.Additions = new List<Addition>(this.Additions);
            return gear;
        }

        public void MakeTimeLimitedPropAvailable()
        {
            if (AbilityTimeLimited.Count > 0 && !this.GetBooleanValue(GearPropType.abilityTimeLimited))
            {
                int diff = 0;
                foreach (var kv in AbilityTimeLimited)
                {
                    this.Props.TryGetValue(kv.Key, out int oldValue);
                    this.Props[kv.Key] = oldValue + kv.Value;
                    diff += kv.Value / Gear.GetPropTypeWeight(kv.Key);
                }
                this.Props[GearPropType.abilityTimeLimited] = 1;
                this.diff += diff;
            }
        }

        public void RestoreStandardProperties()
        {
            if (this.StandardProps != null)
            {
                this.Props.Clear();
                foreach (var kv in this.StandardProps)
                {
                    this.Props[kv.Key] = kv.Value;
                }
                this.diff = 0;
            }
        }

        public bool IsGenesisWeapon
        {
            get
            {
                // There's no better way to determine if a weapon is a Genesis weapon, the game itself also uses a hard-coded list to check it.
                if (IsWeapon(this.type)
                    && this.Props.TryGetValue(GearPropType.setItemID, out var setItemID)
                    && 886 <= setItemID && setItemID <= 890)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsDestinyWeapon
        {
            get
            {
                if (IsGenesisWeapon &&
                    this.Props.TryGetValue(GearPropType.reqLevel, out var equipLevel)
                    && equipLevel == 250)
                {
                    return true;
                }
                return false;
            }
        }

        public void Upgrade(Wz_Node infoNode, int count)
        {
            this.ScrollUp += count;
            foreach (Wz_Node subNode in infoNode.Nodes)
            {
                GearPropType type;
                if (!int.TryParse(subNode.Text, out _) && Enum.TryParse(subNode.Text, out type) && (int)type < 100)
                {
                    try
                    {
                        if (this.Props.ContainsKey(type))
                        {
                            this.Props[type] += Convert.ToInt32(subNode.Value) * count;
                        }
                        else
                        {
                            this.Props.Add(type, Convert.ToInt32(subNode.Value) * count);
                        }
                    }
                    finally
                    {
                    }
                }
            }
            if (this.Props.ContainsKey(GearPropType.tuc))
            {
                this.Props[GearPropType.tuc] -= count;
            }
        }

        public static bool IsFace(GearType type)
        {
            string gearTypeName = Enum.GetName(typeof(GearType), type);
            return gearTypeName != null && Regex.IsMatch(gearTypeName, @"^face\d*$");
        }

        public static bool IsHair(GearType type)
        {
            string gearTypeName = Enum.GetName(typeof(GearType), type);
            return gearTypeName != null && Regex.IsMatch(gearTypeName, @"^hair\d*$");
        }

        public static bool IsWeapon(GearType type)
        {
            return IsLeftWeapon(type)
                || IsDoubleHandWeapon(type);
        }

        public static bool IsCashWeapon(GearType type)
        {
            return type == GearType.cashWeapon;
        }

        /// <summary>
        /// 获取一个值，指示装备类型是否为主手武器。
        /// </summary>
        /// <param name="type">装备类型。</param>
        /// <returns></returns>
        public static bool IsLeftWeapon(GearType type)
        {
            return (int)type >= 121 && (int)type <= 139 && type != GearType.katara
                || ((int)type / 10) == 121 || ((int)type / 10) == 125;
        }

        public static bool IsSubWeapon(GearType type)
        {
            switch (type)
            {
                case GearType.katara:
                //case GearType.shield:
                case GearType.demonShield:
                case GearType.soulShield:
                    return true;

                default:
                    if ((int)type / 1000 == 135)
                    {
                        return true;
                    }
                    return false;
            }
        }

        public static bool IsEmblem(GearType type)
        {
            if (type == GearType.emblem || type == GearType.powerSource)
            {
                return true;
            }
            return false;
        }

        public static bool IsArmor(GearType type)
        {
            switch(type)
            {
                case GearType.cap:
                case GearType.coat:
                case GearType.longcoat:
                case GearType.pants:
                case GearType.shoes:
                case GearType.glove:
                case GearType.cape:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsAccessory(GearType type)
        {
            switch (type)
            {
                case GearType.faceAccessory:
                case GearType.eyeAccessory:
                case GearType.earrings:
                case GearType.ring:
                case GearType.pendant:
                case GearType.belt:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSymbol(GearType type)
        {
            switch (type)
            {
                case GearType.arcaneSymbol:
                case GearType.authenticSymbol:
                case GearType.grandAuthenticSymbol:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsTamingMob(GearType type)
        {
            if ((int)type >= 190 && (int)type < 200)
                return true;

            return false;
        }

        public static bool IsEnhanceable(GearType type)
        {
            switch (type)
            {
                case GearType.body:
                case GearType.head:
                case GearType.face:
                case GearType.hair:
                case GearType.hair2:
                case GearType.face2:
                case GearType.hair3:
                case GearType.medal:
                case GearType.android:
                case GearType.shovel:
                case GearType.pickaxe:
                case GearType.arcaneSymbol:
                case GearType.authenticSymbol:
                case GearType.grandAuthenticSymbol:
                case GearType.petEquip:
                    return false;
                default:
                    return true;
            }
        }

        public static bool CanEnhanceBonusStat(GearType type)
        {
            switch (type)
            {
                case GearType.faceAccessory:
                case GearType.eyeAccessory:
                case GearType.earrings:
                case GearType.pendant:
                case GearType.belt:
                case GearType.cap:
                case GearType.cape:
                case GearType.coat:
                case GearType.glove:
                case GearType.longcoat:
                case GearType.pocket:
                case GearType.pants:
                case GearType.shoes:
                case GearType.totem:
                    return true;
                default:
                    return IsWeapon(type) ? true : false;
            }
        }

        /// <summary>
        /// 获取一个值，指示装备类型是否为双手武器。
        /// </summary>
        /// <param name="type">装备类型。</param>
        /// <returns></returns>
        public static bool IsDoubleHandWeapon(GearType type)
        {
            int _type = (int)type;
            return (_type >= 140 && _type <= 149)
                || (_type >= 152 && _type <= 159)
                || type == GearType.boxingCannon
                || type == GearType.chakram;
        }

        public static bool IsMechanicGear(GearType type)
        {
            return (int)type >= 161 && (int)type <= 165;
        }

        public static bool IsDragonGear(GearType type)
        {
            return (int)type >= 194 && (int)type <= 197;
        }

        public static int Compare(Gear gear, Gear originGear)
        {
            if (gear.ItemID != originGear.ItemID)
                return 0;
            int diff = 0;
            int tempValue;
            foreach (KeyValuePair<GearPropType, int> prop in gear.Props)
            {
                originGear.Props.TryGetValue(prop.Key, out tempValue);//在原装备中寻找属性 若没有找到 视为0
                diff += (int)Math.Round((prop.Value - tempValue) / (double)GetPropTypeWeight(prop.Key));
            }
            foreach (KeyValuePair<GearPropType, int> prop in originGear.Props)
            {
                if (!gear.Props.TryGetValue(prop.Key, out tempValue))//寻找装备原属性里新装备没有的
                {
                    diff -= (int)Math.Round(prop.Value / (double)GetPropTypeWeight(prop.Key));
                }
            }
            return diff;
        }

        public static int Compare(Gear gear)
        {
            int diff = 0;
            int tempValue;
            foreach (KeyValuePair<GearPropType, int> prop in gear.Props)
            {
                gear.StandardProps.TryGetValue(prop.Key, out tempValue);//在原装备中寻找属性 若没有找到 视为0
                diff += (int)Math.Round((prop.Value - tempValue) / (double)GetPropTypeWeight(prop.Key));
            }
            foreach (KeyValuePair<GearPropType, int> prop in gear.StandardProps)
            {
                if (!gear.Props.TryGetValue(prop.Key, out tempValue))//寻找装备原属性里新装备没有的
                {
                    diff -= (int)Math.Round(prop.Value / (double)GetPropTypeWeight(prop.Key));
                }
            }
            return diff;
        }

        private static int GetPropTypeWeight(GearPropType type)
        {
            if ((int)type < 100)
            {
                switch (type)
                {
                    case GearPropType.incSTR:
                    case GearPropType.incDEX:
                    case GearPropType.incINT:
                    case GearPropType.incLUK:
                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                    case GearPropType.incSpeed:
                    case GearPropType.incJump:
                        return 1;
                    case GearPropType.incMHP:
                    case GearPropType.incMMP:
                        return 100;
                    case GearPropType.incPDD_incMDD:
                    case GearPropType.incPDD:
                        return 10;
                    case GearPropType.incPAD_incMAD:
                    case GearPropType.incAD:
                        return 2;
                    case GearPropType.incMHP_incMMP:
                        return 200;
                }
            }
            return int.MaxValue;
        }

        public static bool IsEpicPropType(GearPropType type)
        {
            switch (type)
            {
                case GearPropType.incPAD:
                case GearPropType.incMAD:
                case GearPropType.incSTR:
                case GearPropType.incDEX:
                case GearPropType.incINT:
                case GearPropType.incLUK:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsV5SupportPropType(GearPropType type)
        {
            switch (type)
            {
                case GearPropType.incMDD:
                case GearPropType.incMDDr:
                case GearPropType.incACC:
                case GearPropType.incACCr:
                case GearPropType.incEVA:
                case GearPropType.incEVAr:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 获取装备类型。
        /// </summary>
        /// <param Name="gearCode"></param>
        /// <returns></returns>
        public static GearType GetGearType(int code)
        {
            switch (code / 1000)
            {
                case 1098:
                    return GearType.soulShield;
                case 1099:
                    return GearType.demonShield;
                case 1212:
                    return GearType.shiningRod;
                case 1213:
                    return GearType.tuner;
                case 1214:
                    return GearType.breathShooter;
                case 1215:
                    return GearType.longSword;
                case 1252:
                case 1253:
                case 1259:
                    return (GearType)(code / 1000);
                case 1403:
                    return GearType.boxingCannon;
                case 1404:
                    return GearType.chakram;
                case 1712:
                    return GearType.arcaneSymbol;
                case 1713:
                    return GearType.authenticSymbol;
                case 1714:
                    return GearType.grandAuthenticSymbol;
            }
            if (code / 10000 == 135)
            {
                switch (code / 100)
                {
                    case 13522:
                    case 13528:
                    case 13529:
                    case 13540:
                        return (GearType)(code / 10);

                    default:
                        return (GearType)(code / 100 * 10);
                }
            }
            if (code / 10000 == 119)
            {
                switch(code / 100)
                {
                    case 11902:
                        return (GearType)(code / 10);
                }
            }
            // MSN support
            if (code / 10000 == 179)
            {
                switch (code / 1000)
                {
                    case 1790:
                    case 1791:
                    case 1792:
                        return (GearType)(code / 1000);
                    default:
                        return (GearType)(code / 100 * 10);
                }
            }
            return (GearType)(code / 10000);
        }

        public static int GetGender(int code)
        {
            GearType type = GetGearType(code);
            switch (type)
            {
                case GearType.emblem:
                case GearType.powerSource:
                case GearType.bit:
                case GearType.jewel:
                    return 2;
                case GearType.hair:
                case GearType.hair2:
                case GearType.hair3:
                case GearType.face:
                case GearType.face2:
                    return GetCosmeticGender(code) - 1;
            }

            return code / 1000 % 10;
        }

        public static int GetCosmeticGender(int code)
        {
            var check = code / 1000;

            switch (check / 10)
            {
                case 2: // face
                case 5:
                    switch (check % 10)
                    {
                        case 0:
                        case 3:
                        case 5:
                        case 7:
                            return 1; // 남

                        case 1:
                        case 4:
                        case 6:
                        case 8:
                            return 2; // 여

                        default:
                            return 3; // 공용
                    }

                case 3: // hair
                case 4:
                case 6:
                    switch (check % 10)
                    {
                        case 0:
                        case 3:
                        case 5:
                        case 6:
                            return 1; // 남

                        case 1:
                        case 4:
                        case 7:
                        case 8:
                            return 2; // 여

                        default:
                            return 3; // 공용
                    }

                default:
                    return 3; // 공용
            }
        }

        public static bool SpecialCanPotential(GearType type)
        {
            switch (type)
            {
                case GearType.soulShield:
                case GearType.demonShield:
                case GearType.katara:
                case GearType.magicArrow:
                case GearType.card:
                case GearType.box:
                case GearType.orb:
                case GearType.novaMarrow:
                case GearType.soulBangle:
                case GearType.mailin:
                case GearType.emblem:
                    return true;
                default:
                    return false;
            }
        }

        public static IEnumerable<KeyValuePair<GearPropType, int>> CombineProperties(IEnumerable<KeyValuePair<GearPropType, int>> props)
        {
            var wrappedProp = props.Select(kv => new KeyValuePair<GearPropType, object>(kv.Key, kv.Value));
            var combinedProp = CombineProperties(wrappedProp);
            return combinedProp.Select(kv => new KeyValuePair<GearPropType, int>(kv.Key, Convert.ToInt32(kv.Value)));
        }

        public static IEnumerable<KeyValuePair<GearPropType, object>> CombineProperties(IEnumerable<KeyValuePair<GearPropType, object>> props)
        {
            var combinedProps = new SortedDictionary<GearPropType, object>();
            var propCache = new SortedDictionary<GearPropType, object>();
            foreach (var kv in props)
            {
                propCache.Add(kv.Key, kv.Value);
            }

            object obj;
            foreach (var prop in propCache)
            {
                switch (prop.Key)
                {
                    case GearPropType.incAllStat:
                        if (combinedProps.ContainsKey(GearPropType.incAllStat_incMHP25) || combinedProps.ContainsKey(GearPropType.incAllStat_incMHP50_incMMP50))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(GearPropType.incMHP, out obj)
                            && object.Equals((int)prop.Value * 25, obj)
                            && !propCache.ContainsKey(GearPropType.incMMP))
                        {
                            combinedProps.Add(GearPropType.incAllStat_incMHP25, prop.Value);
                            break;
                        }
                        else if (propCache.TryGetValue(GearPropType.incMHP, out obj)
                            && object.Equals((int)prop.Value * 50, obj)
                            && propCache.TryGetValue(GearPropType.incMMP, out obj)
                            && object.Equals((int)prop.Value * 50, obj))
                        {
                            combinedProps.Add(GearPropType.incAllStat_incMHP50_incMMP50, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incMHP:
                        if (combinedProps.ContainsKey(GearPropType.incAllStat_incMHP25))
                        {
                            break;
                        }
                        goto case GearPropType.incMMP;

                    case GearPropType.incMMP:
                        if (combinedProps.ContainsKey(GearPropType.incMHP_incMMP) || combinedProps.ContainsKey(GearPropType.incAllStat_incMHP50_incMMP50))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incMHP ? GearPropType.incMMP : GearPropType.incMHP, out obj)
                            && object.Equals(prop.Value, obj)
                            && !combinedProps.ContainsKey(GearPropType.incAllStat_incMHP50_incMMP50))
                        {
                            combinedProps.Add(GearPropType.incMHP_incMMP, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incMHPr:
                    case GearPropType.incMMPr:
                        if (combinedProps.ContainsKey(GearPropType.incMHPr_incMMPr))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incMHPr ? GearPropType.incMMPr : GearPropType.incMHPr, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incMHPr_incMMPr, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                        if (combinedProps.ContainsKey(GearPropType.incPAD_incMAD))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incPAD ? GearPropType.incMAD : GearPropType.incPAD, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incPAD_incMAD, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incPDD:
                    case GearPropType.incMDD:
                        if (combinedProps.ContainsKey(GearPropType.incPDD_incMDD))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incPDD ? GearPropType.incMDD : GearPropType.incPDD, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incPDD_incMDD, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incACC:
                    case GearPropType.incEVA:
                        if (combinedProps.ContainsKey(GearPropType.incACC_incEVA))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incACC ? GearPropType.incEVA : GearPropType.incACC, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incACC_incEVA, prop.Value);
                            break;
                        }
                        goto default;

                    default:
                        combinedProps.Add(prop.Key, prop.Value);
                        break;
                }
            }
            return combinedProps;
        }

        public static Gear CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            int gearID;
            Match m = Regex.Match(node.Text, @"^(\d{8})\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out gearID)))
            {
                return null;
            }
            Gear gear = new Gear();
            gear.ItemID = gearID;
            gear.type = Gear.GetGearType(gear.ItemID);
            Wz_Node infoNode = node.FindNodeByPath("info").ResolveUol();

            if (infoNode != null)
            {
                foreach (Wz_Node subNode in infoNode.Nodes)
                {
                    switch (subNode.Text)
                    {
                        case "icon":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                gear.Icon = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "iconRaw":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                gear.IconRaw = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "sample":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                gear.Sample = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "toolTipPreview":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                gear.ToolTIpPreview = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "addition": //附加属性信息
                            foreach (Wz_Node addiNode in subNode.Nodes)
                            {
                                if (addiNode.Text == "hideDesc")
                                {
                                    gear.AdditionHideDesc = true;
                                }
                                else
                                {
                                    Addition addi = Addition.CreateFromNode(addiNode);
                                    if (addi != null)
                                        gear.Additions.Add(addi);
                                }
                            }
                            gear.Additions.Sort((add1, add2) => (int)add1.Type - (int)add2.Type);
                            break;

                        case "option": //附加潜能信息
                            Wz_Node itemWz = findNode !=null? findNode("Item\\ItemOption.img"):null;
                            if (itemWz == null)
                                break;
                            int optIdx = 0;
                            foreach (Wz_Node optNode in subNode.Nodes)
                            {
                                int optId = 0, optLevel = 0;
                                foreach (Wz_Node optArgNode in optNode.Nodes)
                                {
                                    switch (optArgNode.Text)
                                    {
                                        case "option": optId = Convert.ToInt32(optArgNode.Value); break;
                                        case "level": optLevel = Convert.ToInt32(optArgNode.Value); break;
                                    }
                                }

                                Potential opt = Potential.CreateFromNode(itemWz.FindNodeByPath(optId.ToString("d6")), optLevel);
                                if (opt != null)
                                    gear.Options[optIdx++] = opt;
                            }
                            break;

                        case "level": //可升级信息
                            if (subNode.Nodes["fixLevel"].GetValueEx<int>(0) != 0)
                            {
                                gear.FixLevel = true;
                            }

                            Wz_Node levelInfo = subNode.Nodes["info"];
                            gear.Levels = new List<GearLevelInfo>();
                            if (levelInfo != null)
                            {
                                for (int i = 1; ; i++)
                                {
                                    Wz_Node levelInfoNode = levelInfo.Nodes[i.ToString()];
                                    if (levelInfoNode != null)
                                    {
                                        GearLevelInfo info = GearLevelInfo.CreateFromNode(levelInfoNode);
                                        int lv;
                                        Int32.TryParse(levelInfoNode.Text, out lv);
                                        info.Level = lv;
                                        gear.Levels.Add(info);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            Wz_Node levelCase = subNode.Nodes["case"];
                            if (levelCase != null)
                            {
                                int probTotal = 0;
                                foreach (Wz_Node caseNode in levelCase.Nodes)
                                {
                                    int prob = caseNode.Nodes["prob"].GetValueEx(0);
                                    probTotal += prob;
                                    for (int i = 0; i < gear.Levels.Count; i++)
                                    {
                                        GearLevelInfo info = gear.Levels[i];
                                        Wz_Node caseLevel = caseNode.Nodes[info.Level.ToString()];
                                        if (caseLevel != null)
                                        {
                                            //desc
                                            Wz_Node caseHS = caseLevel.Nodes["hs"];
                                            if (caseHS != null)
                                            {
                                                info.HS = caseHS.GetValue<string>();
                                            }

                                            //随机技能
                                            Wz_Node caseSkill = caseLevel.Nodes["Skill"];
                                            if (caseSkill != null)
                                            {
                                                foreach (Wz_Node skillNode in caseSkill.Nodes)
                                                {
                                                    int id = skillNode.Nodes["id"].GetValueEx(-1);
                                                    int level = skillNode.Nodes["level"].GetValueEx(-1);
                                                    if (id >= 0 && level >= 0)
                                                    {
                                                        info.Skills[id] = level;
                                                    }
                                                }
                                            }

                                            //装备技能
                                            Wz_Node equipSkill = caseLevel.Nodes["EquipmentSkill"];
                                            if (equipSkill != null)
                                            {
                                                foreach (Wz_Node skillNode in equipSkill.Nodes)
                                                {
                                                    int id = skillNode.Nodes["id"].GetValueEx(-1);
                                                    int level = skillNode.Nodes["level"].GetValueEx(-1);
                                                    if (id >= 0 && level >= 0)
                                                    {
                                                        info.EquipmentSkills[id] = level;
                                                    }
                                                }
                                            }
                                            info.Prob = prob;
                                        }
                                    }
                                }

                                foreach (var info in gear.Levels)
                                {
                                    info.ProbTotal = probTotal;
                                }
                            }
                            gear.Props.Add(GearPropType.level, 1);
                            break;

                        case "sealed": //封印解除信息
                            Wz_Node sealedInfo = subNode.Nodes["info"];
                            gear.Seals = new List<GearSealedInfo>();
                            if (sealedInfo != null)
                            {
                                foreach (Wz_Node levelInfoNode in sealedInfo.Nodes)
                                {
                                    GearSealedInfo info = GearSealedInfo.CreateFromNode(levelInfoNode, findNode);
                                    int lv;
                                    Int32.TryParse(levelInfoNode.Text, out lv);
                                    info.Level = lv;
                                    gear.Seals.Add(info);
                                }
                            }
                            gear.Props.Add(GearPropType.@sealed, 1);
                            break;

                        case "variableStat": //升级奖励属性
                            foreach (Wz_Node statNode in subNode.Nodes)
                            {
                                GearPropType type;
                                if (Enum.TryParse(statNode.Text, out type))
                                {
                                    try
                                    {
                                        gear.VariableStat.Add(type, Convert.ToSingle(statNode.Value));
                                    }
                                    finally
                                    {
                                    }
                                }
                            }
                            break;

                        case "abilityTimeLimited": //限时属性
                            foreach (Wz_Node statNode in subNode.Nodes)
                            {
                                GearPropType type;
                                if (Enum.TryParse(statNode.Text, out type))
                                {
                                    try
                                    {
                                        gear.AbilityTimeLimited.Add(type, Convert.ToInt32(statNode.Value));
                                    }
                                    finally
                                    {
                                    }
                                }
                            }
                            break;

                        case "onlyUpgrade":
                            int upgradeItemID = subNode.Nodes["0"]?.GetValueEx(0) ?? 0;
                            gear.Props.Add(GearPropType.onlyUpgrade, upgradeItemID);
                            break;

                        case "epic":
                            Wz_Node hsNode = subNode.Nodes["hs"];
                            if (hsNode != null)
                            {
                                gear.EpicHs = Convert.ToString(hsNode.Value);
                            }
                            break;

                        case "gatherTool":
                            foreach (Wz_Node gatherNode in subNode.Nodes)
                            {
                                GearPropType type;
                                if (Enum.TryParse(subNode.Text + "_" + gatherNode.Text, out type))
                                {
                                    try
                                    {
                                        gear.Props.Add(type, Convert.ToInt32(gatherNode.Value));
                                    }
                                    finally
                                    {
                                    }
                                }
                            }
                            break;

                        case "reqSpecJobs":
                            foreach (Wz_Node jobNode in subNode.Nodes)
                            {
                                gear.ReqSpecJobs.Add(jobNode.GetValue<int>());
                            }
                            break;

                        case "limitedLabelGradeTooltip":
                            gear.LabelGradeTooltip = Convert.ToString(subNode.Value);
                            break;

                        default:
                            {
                                GearPropType type;
                                if (!int.TryParse(subNode.Text, out _) && Enum.TryParse(subNode.Text, out type))
                                {
                                    try
                                    {
                                        gear.Props.Add(type, Convert.ToInt32(subNode.Value));
                                    }
                                    finally
                                    {
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            int value;

            //读取默认可升级状态
            if (gear.Props.TryGetValue(GearPropType.tuc, out value) && value > 0)
            {
                gear.HasTuc = true;
                gear.CanPotential = true;
            }
            else if (Gear.SpecialCanPotential(gear.type) || Gear.IsSubWeapon(gear.type) || (gear.Props.TryGetValue(GearPropType.tucIgnoreForPotential, out value) && value > 0))
            {
                gear.CanPotential = true;
            }
            if (Gear.IsMechanicGear(gear.type) || Gear.IsDragonGear(gear.type))
            {
                gear.CanPotential = false;
            }
            else if (gear.Props.TryGetValue(GearPropType.noPotential, out value) && value > 0)
            {
                gear.CanPotential = false;
            }

            //读取默认gearGrade
            if (gear.Props.TryGetValue(GearPropType.fixedGrade, out value))
            {
                //gear.Grade = (GearGrade)(value - 1);
                switch (value)
                {
                    case 2: gear.Grade = GearGrade.B; break;
                    case 3: gear.Grade = GearGrade.A; break;
                    case 5: gear.Grade = GearGrade.S; break;
                    case 7: gear.Grade = GearGrade.SS; break;
                    default: gear.Grade = (GearGrade)(value - 1); break;
                }
            }

            //自动填充Grade
            if (gear.Options.Any(opt => opt != null) && gear.Grade == GearGrade.C)
            {
                gear.Grade = GearGrade.B;
            }

            //添加默认装备要求
            GearPropType[] types = new GearPropType[]{
                GearPropType.reqJob,GearPropType.reqLevel,GearPropType.reqSTR,GearPropType.reqDEX,
            GearPropType.reqINT,GearPropType.reqLUK};
            foreach (GearPropType type in types)
            {
                if (!gear.Props.ContainsKey(type))
                {
                    gear.Props.Add(type, 0);
                }
            }

            //修复恶魔盾牌特殊属性
            if (gear.type == GearType.demonShield)
            {
                if (gear.Props.TryGetValue(GearPropType.incMMP, out value))
                {
                    gear.Props.Remove(GearPropType.incMMP);
                    gear.Props.Add(GearPropType.incMDF, value);
                }
            }

            //检查道具默认的剪刀次数
            var cuttableCountOverride = findNode?.Invoke(@$"Etc\KarmaScissor_WZ2.img\ItemList\{gear.ItemID}")?.GetValueEx<int>();
            if (cuttableCountOverride != null && cuttableCountOverride > 0)
            {
                gear.Props[GearPropType.CuttableCount] = cuttableCountOverride.Value;
            }

            //备份标准属性
            gear.StandardProps = new Dictionary<GearPropType, int>(gear.Props);

            //追加限时属性
            gear.MakeTimeLimitedPropAvailable();

            if (Gear.IsFace(gear.type))
            {
                gear.Icon = BitmapOrigin.CreateFromNode(findNode(@"Item\Install\0380.img\03801284\info\icon"), findNode);
                gear.IconRaw = BitmapOrigin.CreateFromNode(findNode(@"Item\Install\0380.img\03801284\info\iconRaw"), findNode);
            }
            if (Gear.IsHair(gear.type))
            {
                gear.Icon = BitmapOrigin.CreateFromNode(findNode(@"Item\Install\0380.img\03801283\info\icon"), findNode);
                gear.IconRaw = BitmapOrigin.CreateFromNode(findNode(@"Item\Install\0380.img\03801283\info\iconRaw"), findNode);
            }
            if (gear.type == GearType.head)
            {
                gear.Icon = BitmapOrigin.CreateFromNode(findNode(@"Item\Install\0380.img\03801577\info\icon"), findNode);
                gear.IconRaw = BitmapOrigin.CreateFromNode(findNode(@"Item\Install\0380.img\03801577\info\iconRaw"), findNode);
            }

            /*
            if (gear.Props.TryGetValue(GearPropType.incCHUC, out value))
            {
                gear.Star = value;
            }
            */

            return gear;
        }
    }
}