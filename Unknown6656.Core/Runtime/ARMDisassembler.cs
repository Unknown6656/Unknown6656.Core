using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unknown6656.Runtime;


public static class ARMDisassembler
{
    // see https://iitd-plos.github.io/col718/ref/arm-instructionset.pdf
    //     http://cs107e.github.io/readings/armisa.pdf

    private const uint MASK_COND = 0b_11110000_00000000_00000000_00000000;
    private const int RSHIFT_COND = 28;
    private const uint MASK_BIT_I = 0b_00000010_00000000_00000000_00000000;
    private const uint MASK_BIT_PL = 0b_00000001_00000000_00000000_00000000;
    private const uint MASK_BIT_U = 0b_00000000_10000000_00000000_00000000;
    private const uint MASK_BIT_UBSN = 0b_00000000_01000000_00000000_00000000;
    private const uint MASK_BIT_AW = 0b_00000000_00100000_00000000_00000000;
    private const uint MASK_BIT_SL = 0b_00000000_00010000_00000000_00000000;
    private const uint MASK_OPCODE = 0b_00000001_11100000_00000000_00000000;
    private const int RSHIFT_OPCODE = 21;
    private const uint MASK_RMSHIFT = 0b_00000000_00000000_00001111_11110000;
    private const int RSHIFT_RMSHIFT = 4;
    private const uint MASK_IMM = 0b_00000000_00000000_00000000_11111111;
    private const uint MASK_Rn_CRn = 0b_00000000_00001111_00000000_00000000;
    private const int RSHIFT_Rn_CRn = 16;
    private const uint MASK_Rd_CRd = 0b_00000000_00000000_11110000_00000000;
    private const int RSHIFT_Rd_CRd = 12;
    private const uint MASK_Rs_CP_Rotate = 0b_00000000_00000000_00001111_00000000;
    private const int RSHIFT_Rs_CP_Rotate = 8;
    private const uint MASK_CP = 0b_00000000_00000000_00000000_11110000;
    private const uint MASK_Rm_CRm = 0b_00000000_00000000_00000000_00001111;
    private const uint MASK_SDT_OFFSET = 0b_00000000_00000000_11111111_11111111;
    private const uint MASK_BDT_REGISTER = 0b_00000000_00000000_11111111_11111111;
    private const uint MASK_BR_OFFSET = 0b_00000000_11111111_11111111_11111111;

    private const uint MASK_INTERRUPT = 0b__00001111_00000000_00000000_00000000;
    private const uint MATCH_INTERRUPT = 0b_00001111_00000000_00000000_00000000;
    private const uint MASK_COPROC_REGISTER_TRANSFER = 0b__00001111_00000000_00000000_00010000;
    private const uint MATCH_COPROC_REGISTER_TRANSFER = 0b_00001110_00000000_00000000_00010000;
    private const uint MASK_COPROC_DATA_OPERATION = 0b__00001111_00000000_00000000_00010000;
    private const uint MATCH_COPROC_DATA_OPERATION = 0b_00001110_00000000_00000000_00000000;
    private const uint MASK_COPROC_DATA_TRANSFER = 0b__00001110_00000000_00000000_00000000;
    private const uint MATCH_COPROC_DATA_TRANSFER = 0b_00001100_00000000_00000000_00000000;
    private const uint MASK_BRANCH = 0b__00001110_00000000_00000000_00000000;
    private const uint MATCH_BRANCH = 0b_00001010_00000000_00000000_00000000;
    private const uint MASK_DATA_BLOCK_TRANSFER = 0b__00001110_00000000_00000000_00000000;
    private const uint MATCH_DATA_BLOCK_TRANSFER = 0b_00001000_00000000_00000000_00000000;
    private const uint MASK_UNDEFINED = 0b__00001110_00000000_00000000_00010000;
    private const uint MATCH_UNDEFINED = 0b_00000110_00000000_00000000_00010000;
    private const uint MASK_SINGLE_DATA_TRANSFER = 0b__00001100_00000000_00000000_00000000;
    private const uint MATCH_SINGLE_DATA_TRANSFER = 0b_00000100_00000000_00000000_00000000;
    private const uint MASK_HWORD_DATA_TRANSFER_IMMEDIATE = 0b__00001110_01000000_00000000_10010000;
    private const uint MATCH_HWORD_DATA_TRANSFER_IMMEDIATE = 0b_00000000_01000000_00000000_10010000;
    private const uint MASK_HWORD_DATA_TRANSFER_REGISTER = 0b__00001110_01000000_00001111_10010000;
    private const uint MATCH_HWORD_DATA_TRANSFER_REGISTER = 0b_00000000_00000000_00000000_10010000;
    private const uint MASK_BRANCH_EXCHANGE = 0b__00001111_11111111_11111111_11110000;
    private const uint MATCH_BRANCH_EXCHANGE = 0b_00000001_00101111_11111111_00010000;
    private const uint MASK_SINGLE_DATA_SWAP = 0b__00001111_10110000_00001111_11110000;
    private const uint MATCH_SINGLE_DATA_SWAP = 0b_00000001_00100000_00000000_10010000;
    private const uint MASK_MULTIPLY_LONG = 0b__00001111_10000000_00000000_11110000;
    private const uint MATCH_MULTIPLY_LONG = 0b_00000000_10000000_00000000_10010000;
    private const uint MASK_MULTIPLY = 0b__00001111_11000000_00000000_11110000;
    private const uint MATCH_MULTIPLY = 0b_00000000_00000000_00000000_10010000;
    private const uint MASK_DATA_PROCESSING = 0b__00001100_00000000_00000000_00000000;
    private const uint MATCH_DATA_PROCESSING = 0b_00000000_00000000_00000000_00000000;
    private const uint MASK_PSR_MRS = 0b__00001111_10111111_00001111_11111111;
    private const uint MATCH_PSR_MRS = 0b_00000001_00001111_00000000_00000000;
    private const uint MASK_PSR_MSR = 0b__00001111_10111111_11111111_11110000;
    private const uint MATCH_PSR_MSR = 0b_00000001_00101001_11110000_00000000;
    private const uint MASK_PSR_MSR2 = 0b__00001101_10111111_11110000_00000000;
    private const uint MATCH_PSR_MSR2 = 0b_00000001_00101000_11110000_00000000;

    private const string REGISTER_PREFIX = "R";


    public static unsafe List<string> Disassemble(byte[] bytes)
    {
        if ((bytes.Length % sizeof(uint)) != 0)
            throw new ArgumentException($"The number of bytes must be divisable by {sizeof(uint)}.", nameof(bytes));
        else
            fixed (byte* ptr = bytes)
                return Disassemble((uint*)ptr, bytes.Length / sizeof(uint));
    }

    public static unsafe List<string> Disassemble(uint[] instructions)
    {
        fixed (uint* ptr = instructions)
            return Disassemble(ptr, instructions.Length);
    }

    public static unsafe List<string> Disassemble(uint* ptr, int count)
    {
        List<string> instructions = new();

        while (count --> 0)
        {
            uint cond = (*ptr & MASK_COND) >> RSHIFT_COND;

            if (cond >= 0b1111)
                instructions.Add($"NOP  ; undefined behaviour in COND: 0x{*ptr:x8}");
            else
            {
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
                    string instr = (*ptr & MASK_BIT_PL) != 0 ? "BL" : "B";
                    int offs = (int)((*ptr & MASK_BR_OFFSET) << 8);

                    instructions.Add($"{instr}{suffix} 0x{offs >> 6:x8}");
                }
                else if (matches(*ptr, MASK_DATA_PROCESSING, MATCH_DATA_PROCESSING))
                {
                    ARMDataProcessingOpCode opcode = (ARMDataProcessingOpCode)((*ptr & MASK_OPCODE) >> RSHIFT_OPCODE);
                    bool immediate = (*ptr & MASK_BIT_I) != 0;
                    string rn = REGISTER_PREFIX + ((*ptr & MASK_Rn_CRn) >> RSHIFT_Rn_CRn);
                    string rd = REGISTER_PREFIX + ((*ptr & MASK_Rd_CRd) >> RSHIFT_Rd_CRd);
                    string rm = REGISTER_PREFIX + (*ptr & MASK_Rm_CRm);
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

                    if (immediate)
                    {
                        uint rot = (*ptr & MASK_Rs_CP_Rotate) >> RSHIFT_Rs_CP_Rotate;
                        uint imm = *ptr & MASK_IMM;

                        operand2 = $"#{imm << (int)rot}";
                    }
                    else
                    {
                        uint shift = (*ptr & MASK_RMSHIFT) >> RSHIFT_RMSHIFT;
                        ARMShiftType type = (ARMShiftType)((shift >> 1) & 0b11);

                        operand2 = $"{rm}, {type} ";

                        if ((shift & 1) != 0)
                            operand2 += REGISTER_PREFIX + (shift >> 4);
                        else
                            operand2 += $"#{shift >> 3}";
                    }

                    string P = (*ptr & MASK_BIT_UBSN) == 0 ? "CPSR" : "SPSR";

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
                {
                    string instr = (*ptr & MASK_BIT_AW) != 0 ? "MULA" : "MUL";

                }

                // TODO


                else
                    instructions.Add($"; undefined/unknown instruction: 0x{*ptr:x8}");
            }
        }

        throw new NotImplementedException(); // TODO

        return instructions;
    }

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