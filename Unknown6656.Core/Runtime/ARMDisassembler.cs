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
    private const uint MASK_OPERAND2 = 0b_00000000_00000000_00001111_11111111;
    private const uint MASK_Rn_CRn = 0b_00000000_00001111_00000000_00000000;
    private const uint MASK_Rd_CRd = 0b_00000000_00000000_11110000_00000000;
    private const uint MASK_Rs_CP = 0b_00000000_00000000_00001111_00000000;
    private const uint MASK_CP = 0b_00000000_00000000_00000000_11110000;
    private const uint MASK_Rm_CRm = 0b_00000000_00000000_00000000_00001111;
    private const uint MASK_SDT_OFFSET = MASK_OPERAND2;
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
                instructions.Add($"NOP  ; undefined behaviour: 0x{*ptr:x8}");
            else
            {
                string suffix = cond switch
                {
                    0b0000 => "EQ",
                    0b0001 => "NE",
                    0b0010 => "CS",
                    0b0011 => "CC",
                    0b0100 => "MI",
                    0b0101 => "PL",
                    0b0110 => "VS",
                    0b0111 => "VC",
                    0b1000 => "HI",
                    0b1001 => "LS",
                    0b1010 => "GE",
                    0b1011 => "LT",
                    0b1100 => "GT",
                    0b1101 => "LE",
                    0b1110 => "",
                };

                if ((matches(*ptr, MASK_DATA_PROCESSING, MATCH_DATA_PROCESSING) ||
                    matches(*ptr, MASK_MULTIPLY, MATCH_MULTIPLY) ||
                    matches(*ptr, MASK_MULTIPLY_LONG, MATCH_MULTIPLY_LONG)) &&
                    (*ptr & MASK_BIT_SL) != 0)
                    suffix += 'S';


                // TODO : S suffix for DATAPROC, MUL, MULLONG







            }
        }

        throw new NotImplementedException(); // TODO

        return instructions;
    }

    private static bool matches(uint value, uint mask, uint match) => (value & mask) == (match & mask);
}
