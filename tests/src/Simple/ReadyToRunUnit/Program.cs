// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

internal class Program
{
    // [ThreadStatic]
    private static string TextFileName = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\clientexclusionlist.xml";

    //[ThreadStatic]
    private static int LineCount = 0x12345678;

    private static List<string> _passedTests;

    private static List<string> _failedTests;

    private static bool NewString()
    {
        string s = new string('x', 10);
        return s.Length == 10;
    }

    private static bool WriteLine()
    {
        Console.WriteLine("Hello CoreRT R2R running on CoreCLR!");
        return true;
    }
    
    private static bool IsInstanceOf()
    {
        object obj = TextFileName;
        if (obj is string str)
        {
            Console.WriteLine($@"Object is string: {str}");
            return true;
        }
        else
        {
            Console.Error.WriteLine($@"Object is not a string: {obj}");
            return false;
        }
    }

    private static bool IsInstanceOfValueType()
    {
        object obj = LineCount;
        if (obj is int i)
        {
            Console.WriteLine($@"Object {obj:X8} is int: {i:X8}");
            return true;
        }
        else
        {
            Console.Error.WriteLine($@"Object is not an int: {obj}");
            return false;
        }
    }
    
    private static bool ChkCast()
    {
        object obj = TextFileName;
        string objString = (string)obj;
        Console.WriteLine($@"String: {objString}");
        return objString == TextFileName;
    }

    private static bool ChkCastValueType()
    {
        object obj = LineCount;
        int objInt = (int)obj;
        Console.WriteLine($@"Int: {objInt:X8}");
        return objInt == LineCount;
    }
    
    private static bool BoxUnbox()
    {
        bool success = true;
        object intAsObject = LineCount;
        int unboxedInt = (int)intAsObject;
        if (unboxedInt == LineCount)
        {
            Console.WriteLine($@"unbox == box: original {LineCount}, boxed {intAsObject:X8}, unboxed {unboxedInt:X8}");
        }
        else
        {
            Console.Error.WriteLine($@"unbox != box: original {LineCount}, boxed {intAsObject:X8}, unboxed {unboxedInt:X8}");
            success = false;
        }
        int? nullableInt = LineCount;
        object nullableIntAsObject = nullableInt;
        int? unboxedNullable = (int?)nullableIntAsObject;
        if (unboxedNullable == nullableInt)
        {
            Console.WriteLine($@"unbox_nullable == box_nullable: original {nullableInt:X8}, boxed {nullableIntAsObject:X8}, unboxed {unboxedNullable:X8}");
        }
        else
        {
            Console.Error.WriteLine($@"unbox_nullable != box_nullable: original {nullableInt:X8}, boxed {nullableIntAsObject:X8}, unboxed {unboxedNullable:X8}");
            success = false;
        }
        return success;
    }
    
    private static bool TypeHandle()
    {
        Console.WriteLine(TextFileName.GetType().ToString());
        Console.WriteLine(LineCount.GetType().ToString());
        return true;
    }

    private static bool RuntimeTypeHandle()
    {
        Console.WriteLine(typeof(string).ToString());
        return true;
    }

    private static bool ReadAllText()
    {
        Console.WriteLine($@"Dumping file: {TextFileName}");
        string textFile = File.ReadAllText(TextFileName);
        if (textFile.Length > 100)
        {
            textFile = textFile.Substring(0, 100) + "...";
        }
        Console.WriteLine(textFile);

        return textFile.Length > 0;
    }

    private static bool StreamReaderReadLine()
    {
        Console.WriteLine($@"Dumping file: {TextFileName}");
        using (StreamReader reader = new StreamReader(TextFileName, System.Text.Encoding.UTF8))
        {
            Console.WriteLine("StreamReader created ...");
            string line1 = reader.ReadLine();
            Console.WriteLine($@"Line 1: {line1}");
            string line2 = reader.ReadLine();
            Console.WriteLine($@"Line 2: {line2}");
            return line2 != null;
        }
    }
    
    private static bool ConstructListOfInt()
    {
        List<int> listOfInt = new List<int>();
        if (listOfInt.Count == 0)
        {
            Console.WriteLine("Successfully constructed empty List<int>!");
            return true;
        }
        else
        {
            Console.WriteLine($@"Invalid element count in List<int>: {listOfInt.Count}");
            return false;
        }
    }
    
    private static bool ManipulateListOfInt()
    {
        List<int> listOfInt = new List<int>();
        const int ItemCount = 100;
        for (int index = ItemCount; index > 0; index--)
        {
            listOfInt.Add(index);
        }
        listOfInt.Sort();
        for (int index = 0; index < listOfInt.Count; index++)
        {
            Console.Write($@"{listOfInt[index]} ");
            if (index > 0 && listOfInt[index] <= listOfInt[index - 1])
            {
                // The list should be monotonically increasing now
                return false;
            }
        }
        Console.WriteLine();
        return listOfInt.Count == ItemCount;
    }

    private static bool ConstructListOfString()
    {
        List<string> listOfString = new List<string>();
        return listOfString.Count == 0;
    }

    private static bool ManipulateListOfString()
    {
        List<string> listOfString = new List<string>();
        const int ItemCount = 100;
        for (int index = ItemCount; index > 0; index--)
        {
            listOfString.Add(index.ToString());
        }
        listOfString.Sort();
        for (int index = 0; index < listOfString.Count; index++)
        {
            Console.Write($@"{listOfString[index]} ");
            if (index > 0 && listOfString[index].CompareTo(listOfString[index - 1]) <= 0)
            {
                // The list should be monotonically increasing now
                return false;
            }
        }
        Console.WriteLine();
        return listOfString.Count == ItemCount;
    }

    private static bool EmptyArray()
    {
        int[] emptyIntArray = Array.Empty<int>();
        Console.WriteLine("Successfully constructed Array.Empty<int>!");
        return emptyIntArray.Length == 0;
    }

    private delegate char CharFilterDelegate(char inputChar);

    private static bool CharFilterDelegateTest()
    {
        string transformedString = TransformStringUsingCharFilter(TextFileName, CharFilterUpperCase);
        Console.WriteLine(transformedString);
        return transformedString.Length == TextFileName.Length;
    }

    private static string TransformStringUsingCharFilter(string inputString, CharFilterDelegate charFilter)
    {
        StringBuilder outputBuilder = new StringBuilder(inputString.Length);
        foreach (char c in inputString)
        {
            char filteredChar = charFilter(c);
            if (filteredChar != '\0')
            {
                outputBuilder.Append(filteredChar);
            }
        }
        return outputBuilder.ToString();
    }

    private static char CharFilterUpperCase(char c)
    {
        return Char.ToUpperInvariant(c);
    }

    private static bool EnumerateEmptyArray()
    {
        foreach (int element in Array.Empty<int>())
        {
            Console.Error.WriteLine($@"Error: Array.Empty<int> has an element {element}!");
            return false;
        }
        foreach (string element in Array.Empty<string>())
        {
            Console.Error.WriteLine($@"Error: Array.Empty<string> has an element {element}");
            return false;
        }
        return true;
    }
    
    private static bool CreateLocalClassInstance()
    {
        var testClass = new TestClass(1234);
        Console.WriteLine("Successfully constructed TestClass");
        return testClass.A == 1234;
    }

    private class TestClass
    {
        private int _a;

        public TestClass(int a)
        {
            _a = a;
        }

        public int A => _a;
    }

    public static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            TextFileName = args[0];
        }

        _passedTests = new List<string>();
        _failedTests = new List<string>();

        RunTest("NewString", NewString());
        RunTest("WriteLine", WriteLine());
        RunTest("IsInstanceOf", IsInstanceOf());
        RunTest("IsInstanceOfValueType", IsInstanceOfValueType());
        RunTest("ChkCast", ChkCast());
        RunTest("ChkCastValueType", ChkCastValueType());
        RunTest("BoxUnbox", BoxUnbox());
        RunTest("TypeHandle", TypeHandle());
        RunTest("RuntimeTypeHandle", RuntimeTypeHandle());
        RunTest("ReadAllText", ReadAllText());
        RunTest("StreamReaderReadLine", StreamReaderReadLine());
        // TODO: RunTest("CharFilterDelegateTest", CharFilterDelegateTest());
        
        RunTest("ConstructListOfInt", ConstructListOfInt());
        RunTest("ManipulateListOfInt", ManipulateListOfInt());
        RunTest("ConstructListOfString", ConstructListOfString());
        RunTest("ManipulateListOfString", ManipulateListOfString());

        RunTest("EmptyArray", EmptyArray());
        RunTest("CreateLocalClassInstance", CreateLocalClassInstance());
        
        // TODO: RunTest("EnumerateEmptyArray", EnumerateEmptyArray());

        Console.WriteLine($@"{_passedTests.Count} tests pass:");
        // TODO: enumerator - foreach (string testName in _passedTests)
        for (int i = 0; i < _passedTests.Count; i++)
        {
            string testName = _passedTests[i];
            Console.WriteLine($@"    {testName}");
        }

        if (_failedTests.Count == 0)
        {
            Console.WriteLine($@"All {_passedTests.Count} tests pass!");
            return 100;
        }
        else
        {
            Console.Error.WriteLine($@"{_failedTests.Count} test failed:");
            // TODO: enumerator - foreach (string testName in _failedTests)
            for (int i = 0; i < _failedTests.Count; i++)
            {
                string testName = _failedTests[i];
                Console.Error.WriteLine($@"    {testName}");
            }
            return 1;
        }
    }

    private static void RunTest(string name, bool result)
    {
        if (result)
        {
            _passedTests.Add(name);
        }
        else
        {
            _failedTests.Add(name);
        }
    }
}