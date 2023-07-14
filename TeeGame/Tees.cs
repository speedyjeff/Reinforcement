using System;

namespace TeeGame
{
    public enum Tees : short
    {
        _00 = 0b00000_0000_000_00_1,
        _01 = 0b00000_0000_000_10_0,
        _02 = 0b00000_0000_000_01_0,
        _03 = 0b00000_0000_100_00_0,
        _04 = 0b00000_0000_010_00_0,
        _05 = 0b00000_0000_001_00_0,
        _06 = 0b00000_1000_000_00_0,
        _07 = 0b00000_0100_000_00_0,
        _08 = 0b00000_0010_000_00_0,
        _09 = 0b00000_0001_000_00_0,
        _10 = 0b10000_0000_000_00_0,
        _11 = 0b01000_0000_000_00_0,
        _12 = 0b00100_0000_000_00_0,
        _13 = 0b00010_0000_000_00_0,
        _14 = 0b00001_0000_000_00_0,
        All = 0b11111_1111_111_11_1,
    }

    public static class TeeSupport
    {
        public static char TeeToChar(Tees tee)
        {
            if (tee == Tees._00) return 'a';
            if (tee == Tees._01) return 'b';
            if (tee == Tees._02) return 'c';
            if (tee == Tees._03) return 'd';
            if (tee == Tees._04) return 'e';
            if (tee == Tees._05) return 'f';
            if (tee == Tees._06) return 'g';
            if (tee == Tees._07) return 'h';
            if (tee == Tees._08) return 'i';
            if (tee == Tees._09) return 'j';
            if (tee == Tees._10) return 'k';
            if (tee == Tees._11) return 'l';
            if (tee == Tees._12) return 'm';
            if (tee == Tees._13) return 'n';
            if (tee == Tees._14) return 'o';
            throw new Exception("invalid id");
        }

        public static Tees CharToTee(char c)
        {
            if (c == 'a') return Tees._00;
            if (c == 'b') return Tees._01;
            if (c == 'c') return Tees._02;
            if (c == 'd') return Tees._03;
            if (c == 'e') return Tees._04;
            if (c == 'f') return Tees._05;
            if (c == 'g') return Tees._06;
            if (c == 'h') return Tees._07;
            if (c == 'i') return Tees._08;
            if (c == 'j') return Tees._09;
            if (c == 'k') return Tees._10;
            if (c == 'l') return Tees._11;
            if (c == 'm') return Tees._12;
            if (c == 'n') return Tees._13;
            if (c == 'o') return Tees._14;
            throw new Exception("invalid id");
        }
    }
}
