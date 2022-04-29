using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unknown6656.Generics;

namespace Unknown6656.Runtime;


// TODOs:
// - allow specification of endianness
// - BKPT
// - BX


public unsafe class ARMDisassembler
    : Disassembler
{
    // see https://iitd-plos.github.io/col718/ref/arm-instructionset.pdf
    //     http://cs107e.github.io/readings/armisa.pdf

    internal const uint MASK_COND = 0b_11110000_00000000_00000000_00000000;
    internal const int RSHIFT_COND = 28;
    internal const uint MASK_BIT_I = 0b_00000010_00000000_00000000_00000000;
    internal const uint MASK_BIT_PL = 0b_00000001_00000000_00000000_00000000;
    internal const uint MASK_BIT_U = 0b_00000000_10000000_00000000_00000000;
    internal const uint MASK_BIT_UBSN = 0b_00000000_01000000_00000000_00000000;
    internal const uint MASK_BIT_AW = 0b_00000000_00100000_00000000_00000000;
    internal const uint MASK_BIT_SL = 0b_00000000_00010000_00000000_00000000;
    internal const uint MASK_OPCODE = 0b_00000001_11100000_00000000_00000000;
    internal const int RSHIFT_OPCODE = 21;
    internal const uint MASK_CPOP = 0b_00000000_11110000_00000000_00000000;
    internal const int RSHIFT_CPOP = 21;
    internal const uint MASK_RMSHIFT = 0b_00000000_00000000_00001111_11110000;
    internal const int RSHIFT_RMSHIFT = 4;
    internal const uint MASK_IMM = 0b_00000000_00000000_00000000_11111111;
    internal const uint MASK_Rn_CRn = 0b_00000000_00001111_00000000_00000000;
    internal const int RSHIFT_Rn_CRn = 16;
    internal const uint MASK_Rd_CRd = 0b_00000000_00000000_11110000_00000000;
    internal const int RSHIFT_Rd_CRd = 12;
    internal const uint MASK_Rs_CP_Rotate = 0b_00000000_00000000_00001111_00000000;
    internal const int RSHIFT_Rs_CP_Rotate = 8;
    internal const uint MASK_SH = 0b_00000000_00000000_00000000_01100000;
    internal const int RSHIFT_SH = 5;
    internal const uint MASK_CP = 0b_00000000_00000000_00000000_11100000;
    internal const int RSHIFT_CP = 5;
    internal const uint MASK_CPN = 0b_00000000_00000000_00001111_00000000;
    internal const int RSHIFT_CPN = 8;
    internal const uint MASK_Rm_CRm = 0b_00000000_00000000_00000000_00001111;
    internal const uint MASK_SDT_OFFSET = 0b_00000000_00000000_11111111_11111111;
    internal const uint MASK_BDT_REGISTER = 0b_00000000_00000000_11111111_11111111;
    internal const uint MASK_BR_OFFSET = 0b_00000000_11111111_11111111_11111111;

    internal const uint MASK_INTERRUPT = 0b__00001111_00000000_00000000_00000000;
    internal const uint MATCH_INTERRUPT = 0b_00001111_00000000_00000000_00000000;
    internal const uint MASK_COPROC_REGISTER_TRANSFER = 0b__00001111_00000000_00000000_00010000;
    internal const uint MATCH_COPROC_REGISTER_TRANSFER = 0b_00001110_00000000_00000000_00010000;
    internal const uint MASK_COPROC_DATA_OPERATION = 0b__00001111_00000000_00000000_00010000;
    internal const uint MATCH_COPROC_DATA_OPERATION = 0b_00001110_00000000_00000000_00000000;
    internal const uint MASK_COPROC_DATA_TRANSFER = 0b__00001110_00000000_00000000_00000000;
    internal const uint MATCH_COPROC_DATA_TRANSFER = 0b_00001100_00000000_00000000_00000000;
    internal const uint MASK_BRANCH = 0b__00001110_00000000_00000000_00000000;
    internal const uint MATCH_BRANCH = 0b_00001010_00000000_00000000_00000000;
    internal const uint MASK_DATA_BLOCK_TRANSFER = 0b__00001110_00000000_00000000_00000000;
    internal const uint MATCH_DATA_BLOCK_TRANSFER = 0b_00001000_00000000_00000000_00000000;
    internal const uint MASK_UNDEFINED = 0b__00001110_00000000_00000000_00010000;
    internal const uint MATCH_UNDEFINED = 0b_00000110_00000000_00000000_00010000;
    internal const uint MASK_SINGLE_DATA_TRANSFER = 0b__00001100_00000000_00000000_00000000;
    internal const uint MATCH_SINGLE_DATA_TRANSFER = 0b_00000100_00000000_00000000_00000000;
    internal const uint MASK_HWORD_DATA_TRANSFER_IMMEDIATE = 0b__00001110_01000000_00000000_10010000;
    internal const uint MATCH_HWORD_DATA_TRANSFER_IMMEDIATE = 0b_00000000_01000000_00000000_10010000;
    internal const uint MASK_HWORD_DATA_TRANSFER_REGISTER = 0b___00001110_01000000_00001111_10010000;
    internal const uint MATCH_HWORD_DATA_TRANSFER_REGISTER = 0b__00000000_00000000_00000000_10010000;
    internal const uint MASK_BRANCH_EXCHANGE = 0b__00001111_11111111_11111111_11110000;
    internal const uint MATCH_BRANCH_EXCHANGE = 0b_00000001_00101111_11111111_00010000;
    internal const uint MASK_SINGLE_DATA_SWAP = 0b__00001111_10110000_00001111_11110000;
    internal const uint MATCH_SINGLE_DATA_SWAP = 0b_00000001_00100000_00000000_10010000;
    internal const uint MASK_MULTIPLY_LONG = 0b__00001111_10000000_00000000_11110000;
    internal const uint MATCH_MULTIPLY_LONG = 0b_00000000_10000000_00000000_10010000;
    internal const uint MASK_MULTIPLY = 0b__00001111_11000000_00000000_11110000;
    internal const uint MATCH_MULTIPLY = 0b_00000000_00000000_00000000_10010000;
    internal const uint MASK_DATA_PROCESSING = 0b__00001100_00000000_00000000_00000000;
    internal const uint MATCH_DATA_PROCESSING = 0b_00000000_00000000_00000000_00000000;
    internal const uint MASK_PSR_MRS = 0b__00001111_10111111_00001111_11111111;
    internal const uint MATCH_PSR_MRS = 0b_00000001_00001111_00000000_00000000;
    internal const uint MASK_PSR_MSR = 0b__00001111_10111111_11111111_11110000;
    internal const uint MATCH_PSR_MSR = 0b_00000001_00101001_11110000_00000000;
    internal const uint MASK_PSR_MSR2 = 0b__00001101_10111111_11110000_00000000;
    internal const uint MATCH_PSR_MSR2 = 0b_00000001_00101000_11110000_00000000;
    internal const string REGISTER_PREFIX = "R";
    internal const string COPROCESSOR_REGISTER_PREFIX = "c";


    public override unsafe List<string> DisassembleIntoLines(byte* ptr, int length, __empty _) =>
        (length % sizeof(uint)) != 0 ? throw new ArgumentException($"The number of bytes must be divisable by {sizeof(uint)}.", nameof(length))
                                     : Disassemble((uint*) ptr, length / sizeof(uint));

    public List<string> Disassemble(uint[] instructions)
    {
        fixed (uint* ptr = instructions)
            return Disassemble(ptr, instructions.Length);
    }

    public List<string> Disassemble(uint* ptr, int count)
    {
        List<string> instructions = new();

        while (count --> 0)
        {
            uint cond = (*ptr & MASK_COND) >> RSHIFT_COND;

            if (cond >= 0b1111)
                instructions.Add($"NOP  ; undefined behaviour in COND: 0x{*ptr:x8}");
            else
            {
                string rn = get_register(ptr, ARMRegisterType.Rn);
                string rd = get_register(ptr, ARMRegisterType.Rd);
                string rs = get_register(ptr, ARMRegisterType.Rs);
                string rm = get_register(ptr, ARMRegisterType.Rm);
                string suffix = (ARMCondition)cond switch
                {
                    ARMCondition.AL or
                    ARMCondition.RV => "",
                    ARMCondition c => c.ToString(),
                };

                if ((matches(*ptr, MASK_DATA_PROCESSING, MATCH_DATA_PROCESSING) ||
                    matches(*ptr, MASK_MULTIPLY, MATCH_MULTIPLY) ||
                    matches(*ptr, MASK_MULTIPLY_LONG, MATCH_MULTIPLY_LONG)) &&
                    (*ptr & MASK_BIT_SL) != 0)
                    suffix += 'S';

                if (matches(*ptr, MASK_BRANCH, MATCH_BRANCH))
                {
                    string instr = get_bit(ptr, ARMInstructionBit.Link) ? "BL" : "B";
                    int offs = (int)((*ptr & MASK_BR_OFFSET) << 8);

                    instructions.Add($"{instr}{suffix} 0x{offs >> 6:x8}");
                }
                else if (matches(*ptr, MASK_DATA_PROCESSING, MATCH_DATA_PROCESSING))
                {
                    ARMDataProcessingOpCode opcode = (ARMDataProcessingOpCode)((*ptr & MASK_OPCODE) >> RSHIFT_OPCODE);
                    string instr = $"{opcode}{suffix} {opcode switch
                    {
                        ARMDataProcessingOpCode.MOV or
                        ARMDataProcessingOpCode.MVN => rd,
                        ARMDataProcessingOpCode.CMP or
                        ARMDataProcessingOpCode.CMN or
                        ARMDataProcessingOpCode.TEQ or
                        ARMDataProcessingOpCode.TST => rn,
                        _ => $"{rd}, {rn}",
                    }}, ";
                    string operand2;

                    if (get_bit(ptr, ARMInstructionBit.Immediate))
                    {
                        uint rot = (*ptr & MASK_Rs_CP_Rotate) >> RSHIFT_Rs_CP_Rotate;
                        uint imm = *ptr & MASK_IMM;

                        operand2 = $"#{imm << (int)rot}";
                    }
                    else
                        operand2 = $"{rm}, {process_shift_rm(ptr)}";

                    string P = get_bit(ptr, ARMInstructionBit.PSR) ? "CPSR" : "SPSR";

                    if (matches(*ptr, MASK_PSR_MRS, MATCH_PSR_MRS))
                        instr = $"MRS{suffix} {rd}, {P}";
                    else if (matches(*ptr, MASK_PSR_MSR, MATCH_PSR_MSR))
                        instr = $"MSR{suffix} {P}, {rm}";
                    else if (matches(*ptr, MASK_PSR_MSR2, MATCH_PSR_MSR2))
                        instr = $"MSR{suffix}_flg {P}, {operand2}";
                    else
                        instr += operand2;

                    instructions.Add(instr);
                }
                else if (matches(*ptr, MASK_MULTIPLY, MATCH_MULTIPLY))
                    instructions.Add(get_bit(ptr, ARMInstructionBit.Accumulate) ? $"MUL{suffix} {rd}, {rm}, {rs}"
                                                                                : $"MULA{suffix} {rd}, {rm}, {rs}, {rn}");
                else if (matches(*ptr, MASK_MULTIPLY_LONG, MATCH_MULTIPLY_LONG))
                {
                    string accumulate = get_bit(ptr, ARMInstructionBit.Accumulate) ? "L" : "AL";
                    char unsigned = get_bit(ptr, ARMInstructionBit.MultiplyUnsigned) ? 'S' : 'U';

                    instructions.Add($"{unsigned}MUL{accumulate}{suffix} {rd}, {rn}, {rm}, {rs}");
                }
                else if (matches(*ptr, MASK_SINGLE_DATA_TRANSFER, MATCH_SINGLE_DATA_TRANSFER))
                {
                    bool immediate = get_bit(ptr, ARMInstructionBit.Immediate);
                    bool up = get_bit(ptr, ARMInstructionBit.Up);
                    bool preindexed = get_bit(ptr, ARMInstructionBit.PreIndexing);
                    bool writeback = get_bit(ptr, ARMInstructionBit.WriteBack);
                    string instr = get_bit(ptr, ARMInstructionBit.Load) ? "LDR" : "STR";
                    uint offs = *ptr & MASK_SDT_OFFSET;

                    instr += suffix;

                    if (get_bit(ptr, ARMInstructionBit.Byte))
                        instr += 'B';

                    if (preindexed && writeback)
                        instr += 'T';

                    instr += $", {rd}, [{rn}";

                    if (!preindexed)
                        instr += ']';

                    if (!immediate)
                        instr += $", {(up ? '+' : '-')}{rm}, {process_shift_rm(ptr)}";
                    else if (offs != 0)
                        instr += $", #{offs}";

                    if (preindexed)
                        instr += ']';

                    if (preindexed && writeback && offs != 0)
                        instr += '!';
                }
                else if (matches(*ptr, MASK_HWORD_DATA_TRANSFER_REGISTER, MATCH_HWORD_DATA_TRANSFER_REGISTER)
                     || matches(*ptr, MASK_HWORD_DATA_TRANSFER_IMMEDIATE, MATCH_HWORD_DATA_TRANSFER_IMMEDIATE))
                {
                    bool immediate = matches(*ptr, MASK_HWORD_DATA_TRANSFER_IMMEDIATE, MATCH_HWORD_DATA_TRANSFER_IMMEDIATE);
                    bool preindexed = get_bit(ptr, ARMInstructionBit.PreIndexing);
                    bool up = get_bit(ptr, ARMInstructionBit.Up);
                    bool writeback = get_bit(ptr, ARMInstructionBit.WriteBack);
                    uint offset = (*ptr & MASK_Rm_CRm) | ((*ptr & MASK_Rs_CP_Rotate) >> 4);
                    string instr = get_bit(ptr, ARMInstructionBit.Load) ? "LDR" : "STR";

                    instr += $"{suffix}{((*ptr & MASK_SH) >> RSHIFT_SH) switch
                    {
                        00 => "",
                        01 => "H",
                        10 => "SB",
                        11 => "SH",
                    }} {rd}, [{rn}";

                    if (preindexed)
                    {
                        if (immediate & offset == 0)
                            instr += ']';
                        else if (immediate)
                            instr += $", {offset}]";
                        else
                            instr += $", {(up ? '+' : '-')}{rm}]";

                        if (writeback)
                            instr += '!';
                    }
                    else if (immediate)
                        instr += $"], {offset}";
                    else
                        instr += $"], {(up ? '+' : '-')}{rm}]";

                    instructions.Add(instr);
                }
                else if (matches(*ptr, MASK_DATA_BLOCK_TRANSFER, MATCH_DATA_BLOCK_TRANSFER))
                {
                    bool preindexed = get_bit(ptr, ARMInstructionBit.PreIndexing);
                    bool up = get_bit(ptr, ARMInstructionBit.Up);
                    bool writeback = get_bit(ptr, ARMInstructionBit.WriteBack);
                    bool psr = get_bit(ptr, ARMInstructionBit.PSR);
                    bool load = get_bit(ptr, ARMInstructionBit.Load);
                    string instr = load ? "LDM" : "STM";
                    string mode = (preindexed, up) switch
                    {
                        (true, true) => "IB",
                        (false, true) => "IA",
                        (true, false) => "DB",
                        (false, false) => "DA",
                    };
                    string registers = Enumerable.Range(0, 16)
                                                 .Where(i => (*ptr & (1 << i)) != 0)
                                                 .Select(i => get_register((uint)i, false))
                                                 .StringJoin(", ");

                    instr += $"{suffix}{mode} {rn}{(writeback ? "!" : "")}, {{{registers}}}{(psr ? "^" : "")}";

                    if (instr.StartsWith("ldm sp!, ", StringComparison.CurrentCultureIgnoreCase))
                        instr = "POP " + instr[9..];
                    else if (instr.StartsWith("stmdb sp!, ", StringComparison.CurrentCultureIgnoreCase))
                        instr = "PUSH " + instr[11..];

                    instructions.Add(instr);
                }
                else if (matches(*ptr, MASK_SINGLE_DATA_SWAP, MATCH_SINGLE_DATA_SWAP))
                    instructions.Add($"SWP{suffix}{(get_bit(ptr, ARMInstructionBit.Byte) ? "B" : "")} {rd}, {rm}, [{rn}]");
                else if (matches(*ptr, MASK_INTERRUPT, MATCH_INTERRUPT))
                    instructions.Add($"SWI{suffix} ; 0x{*ptr:x8}");
                else if (matches(*ptr, MASK_COPROC_DATA_OPERATION, MATCH_COPROC_DATA_OPERATION))
                {
                    uint cop = (*ptr & MASK_CPOP) >> RSHIFT_CPOP;
                    uint cp = (*ptr & MASK_CP) >> RSHIFT_CP;
                    uint cpn = (*ptr & MASK_CPN) >> RSHIFT_CPN;

                    rn = get_register(ptr, ARMRegisterType.CRn);
                    rd = get_register(ptr, ARMRegisterType.CRd);
                    rm = get_register(ptr, ARMRegisterType.CRm);

                    instructions.Add($"CDP{suffix} p{cpn}, {cop}, {rd}, {rn}, {rm}, {cp}");
                }
                else if (matches(*ptr, MASK_COPROC_DATA_TRANSFER, MATCH_COPROC_DATA_TRANSFER))
                {
                    bool up = get_bit(ptr, ARMInstructionBit.Up);
                    uint cpn = (*ptr & MASK_CPN) >> RSHIFT_CPN;
                    int offs = (int)(*ptr & MASK_IMM) * (up ? 1 : -1);
                    string instr = get_bit(ptr, ARMInstructionBit.Load) ? "LDC" : "STC";

                    instr += suffix;

                    if (get_bit(ptr, ARMInstructionBit.TransferLength))
                        instr += 'L';

                    instr += $" p{cpn} {get_register(ptr, ARMRegisterType.CRd)}, [{rn}";

                    if (get_bit(ptr, ARMInstructionBit.PreIndexing))
                    {
                        if (offs != 0)
                            instr += $", {offs}";

                        instr += ']';

                        if (get_bit(ptr, ARMInstructionBit.WriteBack))
                            instr += '!';
                    }
                    else
                        instr += $"], {offs}";

                    instructions.Add(instr);
                }
                else if (matches(*ptr, MASK_COPROC_REGISTER_TRANSFER, MATCH_COPROC_REGISTER_TRANSFER))
                {
                    string instr = get_bit(ptr, ARMInstructionBit.Load) ? "MRC" : "MCR";
                    uint cpn = (*ptr & MASK_CPN) >> RSHIFT_CPN;
                    uint cp = (*ptr & MASK_CP) >> RSHIFT_CP;
                    uint cpop = (*ptr & MASK_CPOP) >> RSHIFT_CPOP;
                    string cn = get_register(ptr, ARMRegisterType.CRn);
                    string cm = get_register(ptr, ARMRegisterType.CRm);

                    instructions.Add($"{instr}{suffix} p{cpn}, {cpop}, {rd}, {cn}, {cm}, {cp}");
                }
                else
                    instructions.Add($"; undefined/unknown instruction: 0x{*ptr:x8}");
            }

            ++ptr;
        }

        return instructions;
    }

    private static string process_shift_rm(uint* ptr)
    {
        uint shift = (*ptr & MASK_RMSHIFT) >> RSHIFT_RMSHIFT;
        ARMShiftType type = (ARMShiftType)((shift >> 1) & 0b11);
        string operand2 = $"{type} ";

        if ((shift & 1) != 0)
            operand2 += REGISTER_PREFIX + (shift >> 4);
        else
            operand2 += $"#{shift >> 3}";

        return operand2;
    }

    private static string get_register(uint index, bool coproc) =>
        coproc ? COPROCESSOR_REGISTER_PREFIX + index : index switch
        {
            <= 10 => REGISTER_PREFIX + index,
            11 => "FP",
            12 => "IP",
            13 => "SP",
            14 => "LR",
            15 => "PC",
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    private static string get_register(uint* ptr, ARMRegisterType register)
    {
        (uint mask, int shift) = register switch
        {
            ARMRegisterType.Rd or ARMRegisterType.CRd => (MASK_Rd_CRd, RSHIFT_Rd_CRd),
            ARMRegisterType.Rn or ARMRegisterType.CRn => (MASK_Rn_CRn, RSHIFT_Rn_CRn),
            ARMRegisterType.Rs or ARMRegisterType.CRs => (MASK_Rs_CP_Rotate, RSHIFT_Rs_CP_Rotate),
            ARMRegisterType.Rm or ARMRegisterType.CRm => (MASK_Rm_CRm, 0),
        };

        return get_register((*ptr & mask) >> shift, register is ARMRegisterType.CRd
                                                             or ARMRegisterType.CRn
                                                             or ARMRegisterType.CRs
                                                             or ARMRegisterType.CRm);
    }

    private static bool get_bit(uint* ptr, ARMInstructionBit bit) => (*ptr & (uint)bit) != 0;

    private static bool matches(uint value, uint mask, uint match) => (value & mask) == (match & mask);
}

public enum ARMDataProcessingOpCode
    : byte
{
    AND = 0b0000,
    EOR = 0b0001,
    SUB = 0b0010,
    RSB = 0b0011,
    ADD = 0b0100,
    ADC = 0b0101,
    SBC = 0b0110,
    RSC = 0b0111,
    TST = 0b1000,
    TEQ = 0b1001,
    CMP = 0b1010,
    CMN = 0b1011,
    ORR = 0b1100,
    MOV = 0b1101,
    BIC = 0b1110,
    MVN = 0b1111,
}

public enum ARMCondition
    : byte
{
    EQ = 0b0000,
    NE = 0b0001,
    CS = 0b0010,
    CC = 0b0011,
    MI = 0b0100,
    PL = 0b0101,
    VS = 0b0110,
    VC = 0b0111,
    HI = 0b1000,
    LS = 0b1001,
    GE = 0b1010,
    LT = 0b1011,
    GT = 0b1100,
    LE = 0b1101,
    AL = 0b1110,
    RV = 0b1111,
}

public enum ARMShiftType
    : byte
{
    LSL = 0b00, // <<
    LSR = 0b01, // unsigned >>
    ASR = 0b10, // signed >>
    ROR = 0b11, // >>>
}

public enum ARMRegisterType
{
    Rd,
    Rn,
    Rs,
    Rm,
    CRd,
    CRn,
    CRs,
    CRm,
}

public enum ARMInstructionBit
    : uint
{
    Immediate = ARMDisassembler.MASK_BIT_I,
    PreIndexing = ARMDisassembler.MASK_BIT_PL,
    Accumulate = ARMDisassembler.MASK_BIT_AW,
    Up = ARMDisassembler.MASK_BIT_UBSN,
    Link = ARMDisassembler.MASK_BIT_PL,
    SetCondition = ARMDisassembler.MASK_BIT_SL,
    PSR = ARMDisassembler.MASK_BIT_UBSN,
    MultiplyUnsigned = ARMDisassembler.MASK_BIT_UBSN,
    Unsigned = ARMDisassembler.MASK_BIT_U,
    Load = ARMDisassembler.MASK_BIT_SL,
    WriteBack = ARMDisassembler.MASK_BIT_AW,
    Byte = ARMDisassembler.MASK_BIT_UBSN,
    TransferLength = ARMDisassembler.MASK_BIT_UBSN,
}

// MASK_BIT_I =     0000'0010 0000'0000 0000'0000 0000'0000;
// MASK_BIT_PL =    0000'0001 0000'0000 0000'0000 0000'0000;
// MASK_BIT_U =     0000'0000 1000'0000 0000'0000 0000'0000;
// MASK_BIT_UBSN    0000'0000 0100'0000 0000'0000 0000'0000;
// MASK_BIT_AW =    0000'0000 0010'0000 0000'0000 0000'0000;
// MASK_BIT_SL =    0000'0000 0001'0000 0000'0000 0000'0000;
//                         |  |  |
//                         25 23 20
//