using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Xml.Linq;

namespace GhidraSplit
{
    public class FileProcessor
    {
        public static string FilePath;
        public static Dictionary<string, List<CodePiece>>? Functions;
        public static List<CodePiece>? Structs;
        public static List<CodePiece>? Enums;

        public static void processFile(string filepath)
        {
            if (!File.Exists(filepath))
                return;

            FilePath = filepath;

            Functions = new Dictionary<string, List<CodePiece>>();
            Structs = new List<CodePiece>();
            Enums = new List<CodePiece>();

            string name = null;
            List<string> lineBuffer = new List<string>();
            bool inAFunction = false;
            bool inAStruct = false;
            bool inAnEnum = false;

            using (StreamReader fileReader = new StreamReader(FilePath))
            {
                string currentLine;
                while ((currentLine = fileReader.ReadLine()) != null)
                {
                    if (inAFunction)
                    {
                        lineBuffer.Add(currentLine);
                        if (currentLine.StartsWith("}"))
                        {
                            inAFunction = false;

                            string funClass = getFunctionClass(name);
                            if (!Functions.ContainsKey(funClass))
                                Functions.Add(funClass, new List<CodePiece>());

                            Functions[funClass].Add(new CodePiece(name, new List<string>(lineBuffer)));

                            lineBuffer.Clear();
                        }
                    }
                    else if (inAStruct)
                    {
                        lineBuffer.Add(currentLine);
                        if (currentLine.StartsWith("}"))
                        {
                            inAStruct = false;
                            Structs.Add(new CodePiece(name, new List<string>(lineBuffer)));
                            lineBuffer.Clear();
                        }
                    }
                    else if (inAnEnum)
                    {
                        lineBuffer.Add(currentLine);
                        if (currentLine.StartsWith("}"))
                        {
                            inAnEnum = false;
                            Enums.Add(new CodePiece(name, new List<string>(lineBuffer)));
                            lineBuffer.Clear();
                        }
                    }
                    else
                    {
                        LineType lineType = detectLineType(currentLine);

                        if(lineType == LineType.Function)
                        {
                            inAFunction = true;

                            name = getFunctionName(currentLine);

                            string mergeBuffer = String.Join(" ", lineBuffer);
                            currentLine = mergeBuffer + " " + currentLine;
                            if(currentLine.StartsWith(" "))
                                currentLine = currentLine.Substring(1);

                            lineBuffer.Clear();
                            lineBuffer.Add(currentLine);
                        }
                        else if (lineType == LineType.FunctionMulti)
                        {
                            lineBuffer.Add(currentLine);
                        }
                        else if (lineType == LineType.Struct)
                        {
                            inAStruct = true;
                            name = getStructName(currentLine);
                            lineBuffer.Add(currentLine);
                        }
                        else if (lineType == LineType.Enum)
                        {
                            inAnEnum = true;
                            name = getEnumName(currentLine);
                            lineBuffer.Add(currentLine);
                        }
                    }
                }
            }

            Debug.WriteLine("Finished reading!");

            writeData();
        }

        public static void processFile2(string filepath)
        {
            if (!File.Exists(filepath))
                return;

            FilePath = filepath;

            Functions = new Dictionary<string, List<CodePiece>>();
            Structs = new List<CodePiece>();
            Enums = new List<CodePiece>();

            List<string> lineBuffer = new List<string>();
            bool inAPiece = false;

            using (StreamReader fileReader = new StreamReader(FilePath))
            {
                string currentLine;
                while ((currentLine = fileReader.ReadLine()) != null)
                {
                    if (!inAPiece && (currentLine == "" || currentLine.StartsWith("//")))
                        continue;

                    currentLine = currentLine.Replace("::(", "::[");
                    currentLine = currentLine.Replace(")::", "]::");

                    lineBuffer.Add((currentLine));

                    // Open piece
                    if (currentLine.StartsWith("{"))
                        inAPiece = true;

                    if ((currentLine.StartsWith("struct ") || currentLine.StartsWith("typedef enum ")) && currentLine.Contains("{"))
                    {
                        lineBuffer.Clear();
                        lineBuffer.Add((currentLine));
                        inAPiece = true;
                    }

                    // Close piece
                    if (inAPiece && currentLine.StartsWith("}"))
                    {
                        inAPiece = false;

                        // Struct
                        if (lineBuffer[0].StartsWith("struct "))
                        {
                            Structs.Add(new CodePiece(getStructName(lineBuffer[0]), new List<string>(lineBuffer)));
                            lineBuffer.Clear();
                        }
                        // Enum
                        else if (lineBuffer[0].StartsWith("typedef enum "))
                        {
                            Enums.Add(new CodePiece(getEnumName(lineBuffer[0]), new List<string>(lineBuffer)));
                            lineBuffer.Clear();
                        }
                        // Function
                        else
                        {
                            // First we have to clean everything that came up before the start of the function, because sometimes the function definition is split in multiple lines.

                            // The { always starts in a new line
                            int pieceStartIndex = 0;
                            for(int i = lineBuffer.Count - 1; i >= 0; i--)
                            {
                                if (lineBuffer[i].StartsWith("{"))
                                {
                                    pieceStartIndex = i;
                                    break;
                                }
                            }

                            // Join everything before the {
                            List<string> sublist = new List<string>();
                            for (int i = 0; i < pieceStartIndex; i++)
                            {
                                sublist.Add(lineBuffer[i]);
                            }
                            string merger = String.Join(" ", sublist);
                            // Get everything before the function parameters
                            string[] parenthesisSplit = merger.Split("(");
                            string startSplit = parenthesisSplit[parenthesisSplit.Length - 2];
                            // Get the last 3 words
                            string[] splits = startSplit.Split(" ");
                            List<string> actualBufferStart = new List<string>();
                            if(splits.Length > 2)
                                actualBufferStart.Add(splits[splits.Length - 3]);
                            actualBufferStart.Add(splits[splits.Length - 2]);
                            actualBufferStart.Add(splits[splits.Length - 1]);
                            string name = actualBufferStart.Last();
                            if(name.StartsWith("."))
                                name = name.Substring(1);

                            // If it has thiscall and a pointer it's 4 words
                            if (actualBufferStart.Contains("*") && actualBufferStart.Contains("__thiscall"))
                                actualBufferStart.Insert(0, splits[splits.Length - 4]);

                            // Add the calculated function definition and the code
                            List<string> realBuffer = new List<string>();
                            string functionHeader = String.Join(" ", actualBufferStart);
                            functionHeader = functionHeader + "(" + parenthesisSplit.Last();
                            functionHeader = Regex.Replace(functionHeader, @"\s+", " "); // Remove multiple spaces
                            realBuffer.Add(functionHeader);
                            for (int i = pieceStartIndex; i < lineBuffer.Count; i++)
                            {
                                realBuffer.Add(lineBuffer[i]);
                            }

                            //string mergedBuffer = String.Join(" ", lineBuffer);
                            //string[] splits1 = mergedBuffer.Split("(");
                            //string[] splits2 = splits1[0].Split(" ");
                            //string name = splits2.Last();

                            string funClass = getFunctionClass(name);
                            if (!Functions.ContainsKey(funClass))
                                Functions.Add(funClass, new List<CodePiece>());

                            Functions[funClass].Add(new CodePiece(name, new List<string>(realBuffer)));

                            lineBuffer.Clear();
                        }
                    }
                }
            }

            Debug.WriteLine("Finished reading!");

            writeData();
        }

        public static void writeData()
        {
            string folder = Path.GetDirectoryName(FilePath);
            string enumsFile = Path.Combine(folder, "_ENUMS.c");
            string structsFile = Path.Combine(folder, "_STRUCTS.c");
            string libraryDirectory = Path.Combine(folder, "Libraries");
            Directory.CreateDirectory(libraryDirectory);

            foreach (CodePiece piece in Enums)
            {
                File.AppendAllLines(enumsFile, piece.Lines);
            }

            foreach (CodePiece piece in Structs)
            {
                File.AppendAllLines(structsFile, piece.Lines);
            }

            foreach (string funClass in Functions.Keys)
            {
                Functions[funClass] = Functions[funClass].OrderBy(o => o.Name).ToList();
                foreach (CodePiece piece in Functions[funClass])
                {
                    string classFile = Path.Combine(libraryDirectory, getFunctionClass(piece.Name));
                    classFile += ".c";
                    File.AppendAllLines(classFile, piece.Lines);
                }
            }

            Debug.WriteLine("Finished writing!");
        }

        public static LineType detectLineType(string line)
        {
            if (line == "" || line.StartsWith(" "))
                return LineType.None;

            bool swtd = line.StartsWith("typedef ");

            if (line.StartsWith("typedef "))
            {
                if (line.StartsWith("typedef enum") && line.Contains("{"))
                    return LineType.Enum;
                else
                    return LineType.Definition;
            }

            if (line.StartsWith("//"))
                return LineType.Comment;


            if (line.StartsWith("struct") && line.Contains("{"))
                return LineType.Struct;

            if (line.Contains("("))
                return LineType.Function;
            else
                return LineType.FunctionMulti;

            return LineType.ERROR;
        }

        public enum LineType
        {
            None,
            Function,
            FunctionMulti,
            Struct,
            Enum,
            Definition,
            Comment,
            ERROR
        }

        /*
         * BOOL Axa::dbgButtonDown(INT userId,ULONG buttons,INT padId)

           {
           }
         */
        public static string getFunctionName(string line)
        {
            string[] splits1 = line.Split("(");
            string[] splits2 = splits1[0].Split(" ");
            string name = splits2.Last();
            if(name.StartsWith("."))
                name = name.Substring(1);
            return name;
        }

        public static string getFunctionClass(string functionName)
        {
            if (!functionName.Contains("::"))
                return "_MISC";

            string[] splits = functionName.Split("::");
            return splits[0];
        }

        // struct VTABLE<gb::PLAYER> { // PlaceHolder Class Structure
        // };
        public static string getStructName(string line)
        {
            string[] splits = line.Split(" ");
            return splits[1];
        }

        /*
         * typedef enum THREAD_PRI {
               PRI_MOVIE=11,
               PRI_VSYNC=40,
               PRI_CD_READ_CALLBACK=10,
               PRI_MAIN=30
           } THREAD_PRI;
        */
        public static string getEnumName(string line)
        {
            string[] splits = line.Split(" ");
            return splits[2];
        }

        public class CodePiece
        {
            public string Name;
            public List<string> Lines;

            public CodePiece(string name, List<string> lines)
            {
                Name = name;
                Lines = lines;
            }
        }
    }
}
