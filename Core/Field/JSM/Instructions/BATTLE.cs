﻿using System;

namespace OpenVIII.Fields.Scripts.Instructions
{
    /// <summary>
    /// trigger Battle encounter
    /// </summary>
    /// <see cref="http://wiki.ffrtt.ru/index.php?title=FF8/Field/Script/Opcodes/069_BATTLE"/>
    public sealed class BATTLE : JsmInstruction
    {
        #region Fields

        private readonly ushort _encounter;
        private readonly BFlags _flags;

        #endregion Fields

        #region Constructors

        public BATTLE(ushort encounter, BFlags flags)
        {
            _encounter = encounter;
            _flags = flags;
        }

        public BATTLE(Int32 parameter, IStack<IJsmExpression> stack)
            : this(
                flags: (BFlags)((IConstExpression)stack.Pop()).Int32(),
                encounter: (ushort)((IConstExpression)stack.Pop()).Int32())
        {
        }

        #endregion Constructors

        #region Enums

        [Flags]
        public enum BFlags : byte
        {
            Regular_battle = 0x0,
            No_escape = 0x1,

            /// <summary>
            /// (battle music keeps playing after win/loss)
            /// </summary>
            Disable_victory_fanfare = 0x2,

            Inherit_countdown_timer_from_field = 0x4,
            No_Item_XP_Gain = 0x8,
            Use_current_music_as_battle_music = 0x10,
            Force_preemptive_attacked = 0x20,
            Force_back_attack = 0x40,
            Unknown = 0x80
        }

        #endregion Enums

        #region Properties

        public ushort Encounter => _encounter;
        public BFlags Flags => _flags;

        #endregion Properties

        #region Methods

        public override String ToString() => $"{nameof(BATTLE)}({nameof(_encounter)}: {_encounter}, {nameof(_flags)}: {_flags})";

        #endregion Methods
    }
}