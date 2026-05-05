using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class FontConverter
{
    public static void Main()
    {
        string input = " :\1\1\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0" +
                       "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0" +
                       "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\10*>\34\34>*\10\0\10\10>" +
                       ">\10\10\0\0\0\200\340`\0\0\0\0\10\10\10\10\10\10\0\0\0\0``\0\0\0`\60\30\14" +
                       "\6\3\1\0>\177QIE\177>\0\0@B\177\177@@\0\0r{IIof\0\0\42aI" +
                       "I\177\66\0\30\24R\177\177P\20\0\0'oIIy\63\0\0>\177II{\62\0\0\3\1q" +
                       "}\17\7\0\0\66\177II\177\66\0\0&oII\177>\0\0\0\0ll\0\0";

        var bytes = ConvertToByteArray(input);
        
        Console.WriteLine("Total bytes: " + bytes.Length);
        Console.WriteLine("\nC# array format:");
        PrintCSharpArray(bytes);
    }

    static byte[] ConvertToByteArray(string input)
    {
        var result = new List<byte>();
        
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            
            // Handle escape sequences
            if (c == '\\' && i + 1 < input.Length)
            {
                char next = input[i + 1];
                
                // Octal escape (\0, \10, \200, etc)
                if (next >= '0' && next <= '7')
                {
                    int octalValue = 0;
                    int digits = 0;
                    i++; // Skip backslash
                    
                    while (i < input.Length && input[i] >= '0' && input[i] <= '7' && digits < 3)
                    {
                        octalValue = octalValue * 8 + (input[i] - '0');
                        i++;
                        digits++;
                    }
                    i--; // Back one since loop will increment
                    
                    result.Add((byte)octalValue);
                }
                else
                {
                    // Other escapes (not present in this font, but for completeness)
                    i++; // Skip backslash
                    result.Add((byte)next);
                }
            }
            else
            {
                // Regular ASCII character
                result.Add((byte)c);
            }
        }
        
        return result.ToArray();
    }

    static void PrintCSharpArray(byte[] bytes)
    {
        Console.WriteLine("byte[] fontData = {");
        
        for (int i = 0; i < bytes.Length; i += 16)
        {
            Console.Write("    ");
            for (int j = 0; j < 16 && i + j < bytes.Length; j++)
            {
                Console.Write($"0x{bytes[i + j]:X2}");
                if (i + j < bytes.Length - 1)
                    Console.Write(", ");
            }
            Console.WriteLine();
        }
        
        Console.WriteLine("};");
        
        // Print as glyphs (8 bytes per character)
        Console.WriteLine("\n\nAs glyphs (8 bytes each):");
        int glyphCount = bytes.Length / 8;
        for (int g = 0; g < glyphCount; g++)
        {
            Console.Write($"Glyph {g}: {{ ");
            for (int b = 0; b < 8 && g * 8 + b < bytes.Length; b++)
            {
                Console.Write($"0x{bytes[g * 8 + b]:X2}");
                if (b < 7) Console.Write(", ");
            }
            Console.WriteLine(" }");
        }
    }
}
