﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public enum GearType
    {
        body = 0,
        head = 1,
        face = 2,
        hair = 3,
        hair2 = 4,
        face2 = 5,
        hair3 = 6,
        /// <summary>
        /// MSU support
        /// </summary>
        face_n = 1790,
        hair_n = 1791,
        head_n = 1792,
        /// <summary>
        /// Face Accessory 101
        /// </summary>
        faceAccessory = 101,
        /// <summary>
        /// Eye Accessory 102
        /// </summary>
        eyeAccessory = 102,
        /// <summary>
        /// Earrings 103
        /// </summary>
        earrings = 103,
        /// <summary>
        /// Pendant 112
        /// </summary>
        pendant = 112,
        /// <summary>
        /// Belt 113
        /// </summary>
        belt = 113,
        /// <summary>
        /// Medal 114
        /// </summary>
        medal = 114,
        /// <summary>
        /// Shoulder 115
        /// </summary>
        shoulderPad = 115,
        /// <summary>
        /// Hat 100
        /// </summary>
        cap = 100,
        /// <summary>
        /// Cape 110
        /// </summary>
        cape = 110,
        /// <summary>
        /// Top 104
        /// </summary>
        coat = 104,
        /// <summary>
        /// Dragon Hat 194
        /// </summary>
        dragonMask = 194,
        /// <summary>
        /// Dragon Pendant 195
        /// </summary>
        dragonPendant = 195,
        /// <summary>
        /// Dragon Wing Accessory 196
        /// </summary>
        dragonWings = 196,
        /// <summary>
        /// Dragon Tail Accessory 197
        /// </summary>
        dragonTail = 197,
        /// <summary>
        /// Gloves 108
        /// </summary>
        glove = 108,
        /// <summary>
        /// Outfit 105
        /// </summary>
        longcoat = 105,
        /// <summary>
        /// Mechanic Engine 161
        /// </summary>
        machineEngine = 161,
        /// <summary>
        /// Mechanic Arm 162
        /// </summary>
        machineArms = 162,
        /// <summary>
        /// Mechanic Leg 163
        /// </summary>
        machineLegs = 163,
        /// <summary>
        /// Mechanic Frame 164
        /// </summary>
        machineBody = 164,
        /// <summary>
        /// Mechanic Transistor 165
        /// </summary>
        machineTransistors = 165,
        /// <summary>
        /// Android 166
        /// </summary>
        android = 166,
        /// <summary>
        /// Mechanical Heartz 167
        /// </summary>
        machineHeart = 167,
        /// <summary>
        /// Pocket Item 116
        /// </summary>
        pocket = 116,
        /// <summary>
        /// Badge 118
        /// </summary>
        badge = 118,
        /// <summary>
        /// Emblem / Power Source 119
        /// </summary>
        emblem = 119,
        powerSource = 119020,
        /// <summary>
        /// Bottom 106
        /// </summary>
        pants = 106,
        /// <summary>
        /// Ring 111
        /// </summary>
        ring = 111,
        /// <summary>
        /// Shield 109
        /// </summary>
        shield = 109,
        /// <summary>
        /// Soul Shield 1098xxx
        /// </summary>
        soulShield = 1098,
        /// <summary>
        /// Demon Aegis 1099xxx
        /// </summary>
        demonShield = 1099,
        /// <summary>
        /// Shoes 107
        /// </summary>
        shoes = 107,
        /// <summary>
        /// Shining Rod 1212
        /// </summary>
        shiningRod = 1212,
        /// <summary>
        /// Bladecaster 1213
        /// </summary>
        tuner = 1213,
        /// <summary>
        /// Whispershot 1214
        /// </summary>
        breathShooter = 1214,
        /// <summary>
        /// Long Sword 1215
        /// </summary>
        longSword = 1215,
        /// <summary>
        /// Sould SHooter 122
        /// </summary>
        soulShooter = 122,
        /// <summary>
        /// Desperado 123
        /// </summary>
        desperado = 123,
        /// <summary>
        /// Whip Blade 124
        /// </summary>
        energySword = 124,
        /// <summary>
        /// Memorial Staff 1252
        /// </summary>
        memorialStaff = 1252,
        /// <summary>
        /// Celestial Light 1253
        /// </summary>
        celestialLight = 1253,
        /// <summary>
        /// Beast Tamer Scepter 1259
        /// </summary>
        magicStick = 1259,
        /// <summary>
        /// Psy-limiter
        /// </summary>
        espLimiter = 126,
        /// <summary>
        /// Chain 127
        /// </summary>
        chain2 = 127,
        /// <summary>
        /// Lucent Gauntlet 128
        /// </summary>
        magicGauntlet = 128,
        /// <summary>
        /// Ritual Fan 129
        /// </summary>
        handFan = 129,
        /// <summary>
        /// One-handed Sword 130
        /// </summary>
        ohSword = 130,
        /// <summary>
        /// One-handed Axe 131
        /// </summary>
        ohAxe = 131,
        /// <summary>
        /// One-handed Blunt Weapon 132
        /// </summary>
        ohBlunt = 132,
        /// <summary>
        /// Dagger 133
        /// </summary>
        dagger = 133,
        /// <summary>
        /// Katara 134
        /// </summary>
        katara = 134,
        /// <summary>
        /// Magic Arrow 135_00
        /// </summary>
        magicArrow = 135200,
        /// <summary>
        /// Card 135_10
        /// </summary>
        card = 135210,
        /// <summary>
        /// Medallions 135_20
        /// </summary>
        heroMedal = 135220,
        /// <summary>
        /// Rosary 135_21
        /// </summary>
        rosario = 135221,
        /// <summary>
        /// Iron Chain 135_22
        /// </summary>
        chain = 135222,
        /// <summary>
        /// Magic Book (Fire,Poison) 135_23
        /// </summary>
        book1 = 135223,
        /// <summary>
        /// Magic Book (Ice,Lightning) 135_24
        /// </summary>
        book2 = 135224,
        /// <summary>
        /// Magic Book (Bishop) 135_25
        /// </summary>
        book3 = 135225,
        /// <summary>
        /// Arrow Fletching 135_26
        /// </summary>
        bowMasterFeather = 135226,
        /// <summary>
        /// Bow Thimble 135_27
        /// </summary>
        crossBowThimble = 135227,
        /// <summary>
        /// Dagger Scabbard 135_28
        /// </summary>
        shadowerSheath = 135228,
        /// <summary>
        /// Charm 135_29
        /// </summary>
        nightLordPoutch = 135229,
        /// <summary>
        /// Core 135_30
        /// </summary>
        box = 135230,
        /// <summary>
        /// Orb 135_40
        /// </summary>
        orb = 135240,
        /// <summary>
        /// Dragon Essence 135_50
        /// </summary>
        novaMarrow = 135250,
        /// <summary>
        /// Soul Ring 135_60
        /// </summary>
        soulBangle = 135260,
        /// <summary>
        /// Magnum 135_70
        /// </summary>
        mailin = 135270,
        /// <summary>
        /// Kodachi 135_80
        /// </summary>
        kodachi = 135280,
        /// <summary>
        /// Leaf 135_81
        /// </summary>
        leaf = 135281,
        /// <summary>
        /// Fist 135_82
        /// </summary>
        boxingClaw = 135282,
        /// <summary>
        /// Kodachi 135_83
        /// </summary>
        kodachi2 = 135283,
        /// <summary>
        /// Leaf 135_84
        /// </summary>
        leaf2 = 135284,
        /// <summary>
        /// 拳天 135_86
        /// </summary>
        boxingSky = 135286,
        /// <summary>
        /// compass 135_87
        /// </summary>
        compass = 135287,
        /// <summary>
        /// Wrist Band 135_90
        /// </summary>
        viperWristband = 135290,
        /// <summary>
        /// Far Sight 135_91
        /// </summary>
        captainSight = 135291,
        /// <summary>
        /// Powder Keg 135_92
        /// </summary>
        cannonGunPowder = 135292,
        /// <summary>
        /// Mass 135_93
        /// </summary>
        aranPendulum = 135293,
        /// <summary>
        /// Document 135_94
        /// </summary>
        evanPaper = 135294,
        /// <summary>
        /// Magic Marble 135_95
        /// </summary>
        battlemageBall = 135295,
        /// <summary>
        /// Arrowhead 135_96
        /// </summary>
        wildHunterArrowHead = 135296,
        /// <summary>
        /// Jewel 135_97
        /// </summary>
        cygnusGem = 135297,
        /// <summary>
        /// Powder Keg 135_98
        /// </summary>
        cannonGunPowder2 = 135298,
        /// <summary>
        /// Controller 135300
        /// </summary>
        controller = 135300,
        /// <summary>
        /// Fox Marble 135310
        /// </summary>
        foxPearl = 135310,
        /// <summary>
        /// Chess Piece 135320
        /// </summary>
        chess = 135320,
        /// <summary>
        /// Warp Forge 135330
        /// </summary>
        transmitter = 135330,
        /// <summary>
        /// Charge 135340
        /// </summary>
        explosivePill = 135340,
        /// <summary>
        /// Lucent Wings 135350
        /// </summary>
        magicWing = 135350,
        /// <summary>
        /// Abyssal Path 135360
        /// </summary>
        pathOfAbyss = 135360,
        /// <summary>
        /// Relic 135370x
        /// </summary>
        relic = 135370,
        /// <summary>
        /// Fan Tassel 135380x
        /// </summary>
        fanTassel = 135380,
        /// <summary>
        /// Bladebinder 135400x
        /// </summary>
        bracelet = 135400,
        /// <summary>
        /// Weapon Belt 135401x
        /// </summary>
        weaponBelt = 135401,
        /// <summary>
        /// Ornament 135402x
        /// </summary>
        ornament = 135402,
        /// <summary>
        /// Hex Seeker 135403x
        /// </summary>
        hexSeeker = 135403,
        /// <summary>
        /// yeouiGem 135404
        /// </summary>
        yeouiGem = 135404,
        /// <summary>
        /// Cane 136
        /// </summary>
        cane = 136,
        /// <summary>
        /// Wand 137
        /// </summary>
        wand = 137,
        /// <summary>
        /// Staff 138
        /// </summary>
        staff = 138,
        /// <summary>
        /// Bare Hands 139
        /// </summary>
        barehand = 139,
        /// <summary>
        /// Two-handed Sword 140
        /// </summary>
        thSword = 140,
        /// <summary>
        /// 拳封 140_3xxx
        /// </summary>
        boxingCannon = 1403,
        /// <summary>
        /// Chakram 140_4xxx
        /// </summary>
        chakram = 1404,
        /// <summary>
        /// Two-handed Axe 141
        /// </summary>
        thAxe = 141,
        /// <summary>
        /// Two-handed Blunt Weapon 142
        /// </summary>
        thBlunt = 142,
        /// <summary>
        /// Spear 143
        /// </summary>
        spear = 143,
        /// <summary>
        /// Polearm 144
        /// </summary>
        polearm = 144,
        /// <summary>
        /// Bow 145
        /// </summary>
        bow = 145,
        /// <summary>
        /// Crossbow 146
        /// </summary>
        crossbow = 146,
        /// <summary>
        /// Claw 147
        /// </summary>
        throwingGlove = 147,
        /// <summary>
        /// Knuckle 148
        /// </summary>
        knuckle = 148,
        /// <summary>
        /// Gun 149
        /// </summary>
        gun = 149,
        /// <summary>
        /// Herbalism Tool 150
        /// </summary>
        shovel = 150,
        /// <summary>
        /// Mining Tool 151
        /// </summary>
        pickaxe = 151,
        /// <summary>
        /// Dual Bowguns 152
        /// </summary>
        dualBow = 152,
        /// <summary>
        /// Hand Cannon 153
        /// </summary>
        handCannon = 153,
        /// <summary>
        /// Katana 154
        /// </summary>
        katana = 154,
        /// <summary>
        /// Fan 155
        /// </summary>
        fan = 155,
        /// <summary>
        /// Heavy Sword 156
        /// </summary>
        swordZB = 156,
        /// <summary>
        /// Long Sword 157
        /// </summary>
        swordZL = 157,
        /// <summary>
        /// Arm Cannon 158
        /// </summary>
        gauntletBuster = 158,
        /// <summary>
        /// Ancient Bow 159
        /// </summary>
        ancientBow = 159,
        /// <summary>
        /// Bit 168
        /// </summary>
        bit = 168,
        /// <summary>
        /// Cash Weapon 170
        /// </summary>
        cashWeapon = 170,
        /// <summary>
        /// Weapon -1
        /// </summary>
        weapon = -1,
        /// <summary>
        /// Secondary Weapon -1
        /// </summary>
        subWeapon = -2,
        /// <summary>
        /// Totem 120
        /// </summary>
        totem = 120,
        /// <summary>
        /// Arcane Symbol 1712
        /// </summary>
        arcaneSymbol = 1712,
        /// <summary>
        /// Sacred Symbol 1713
        /// </summary>
        authenticSymbol = 1713,
        /// <summary>
        /// Grand Sacred Symbol 1714
        /// </summary>
        grandAuthenticSymbol = 1714,
        /// <summary>
        /// Jade 178
        /// </summary>
        jewel = 178,
        /// <summary>
        /// Pet Equipment 180
        /// </summary>
        petEquip = 180,
        /// <summary>
        /// Tamed Monster 190
        /// </summary>
        taming = 190,
        /// <summary>
        /// Tamed Monster 191
        /// </summary>
        saddle = 191,
        /// <summary>
        /// Tamed Monster 193
        /// </summary>
        taming2 = 193,
        /// <summary>
        /// Tamed Monster 198
        /// </summary>
        tamingChair = 198,
        /// <summary>
        /// Tamed Monster 199
        /// </summary>
        taming3 = 199
    }
}