using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Diagnostics;
namespace JavaLabRelplacer
{
    class Program
    {
        static string documentDir = @".";
        static string documentPath = @"doc.txt";
        const string PATTERN_BLANK = @"【代码\d*】(?=[^：\n])";
        const string PATTERN_ANSWER = @"(?<=【代码\d*】：).+";
        const string CODEBLOCK_START = "答案";
        const string CODEMOD_START = "模板代码";
        const string CODEBLOCK_END = "编译并运行该程序";
        private static int indexOfReplace = -1;
        private static ArrayList answerSet = new ArrayList();
        private static ArrayList LabSet = new ArrayList();
        static void Main(string[] args)
        {
            if (!File.Exists(documentPath))
            {
                documentDir = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Path.GetDirectoryName(Path.GetDirectoryName(typeof(Program).Assembly.Location))));
                documentPath = documentDir + @"\doc.txt";
                if (!File.Exists(documentPath))
                {
                    Console.WriteLine("[Error] 未能找到 " + Path.GetDirectoryName(typeof(Program).Assembly.Location) + "\\doc.txt 或 " + documentPath);
                    Console.WriteLine("按任意键退出...");
                    Console.ReadKey();
                    return;
                }
            }

            // 时间记录
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            // DateTime startTime = DateTime.Now;

            // 读取文件流
            string Oridocument;
            using (StreamReader streamReader = new StreamReader(documentPath))
                Oridocument = streamReader.ReadToEnd();

            // 自动提取有效代码块
            Console.WriteLine("[JavaLabReplacer] 正在扫描文档...");
            Console.WriteLine("[JavaLabReplacer] 正在分离实验代码...");
            Console.WriteLine("-----------------------------------------");
            int labIndex = 0, startIndex = 0, endIndex = 0;
            startIndex = Oridocument.IndexOf(CODEBLOCK_START, startIndex);
            endIndex = Oridocument.IndexOf(CODEBLOCK_END, endIndex);
            while (startIndex != -1)
            {
                string labContent = "实验" + (++labIndex) + "：" + System.Environment.NewLine;
                labContent += Oridocument.Substring(startIndex + CODEBLOCK_START.Length, endIndex - startIndex - CODEBLOCK_START.Length).Trim();
                labContent += System.Environment.NewLine + System.Environment.NewLine + "===============================" + System.Environment.NewLine + System.Environment.NewLine;
                LabSet.Add(labContent);
                startIndex = Oridocument.IndexOf(CODEBLOCK_START, startIndex + CODEBLOCK_START.Length);
                endIndex = Oridocument.IndexOf(CODEBLOCK_END, endIndex + CODEBLOCK_END.Length);
            }

            int blankCount = 0;
            string outputString = "";
            for (int i = 0; i < LabSet.Count; i++)
            {
                string document = LabSet[i].ToString();
                
                int indexOfLab = i + 1;
                Console.WriteLine("[JavaLabReplacer] 正在处理实验" + indexOfLab + "...");
                // 匹配待填空与答案
                MatchCollection blanks = Regex.Matches(document, PATTERN_BLANK);
                MatchCollection answers = Regex.Matches(document, PATTERN_ANSWER);
                Console.WriteLine("[JavaLabReplacer] 已找到填空" + blanks.Count + "个");
                Console.WriteLine("[JavaLabReplacer] 已找到答案" + answers.Count + "个");
                if (0 == blanks.Count + answers.Count) { Console.WriteLine("-----------------------------------------"); continue; }

                // 将答案收入答案集中
                blankCount += blanks.Count;
                Console.WriteLine("[JavaLabReplacer] 正在处理多行答案...");
                string tempAns = "";
                int indexOfAnswer = 0;
                foreach (Match m in answers)
                {
                    indexOfAnswer++;
                    tempAns = m.Value.Trim();
                    if (!tempAns.Equals(""))
                    {
                        string answer = m.Value.Trim();
                        //if(answer.IndexOf("/") == -1)
                        answerSet.Add(answer);
                        //else
                        //    continue;
                    }
                    else
                    {
                        // 此处表明答案有多行代码，正则表达式不方便匹配，故单独处理
                        string tempCodeTag_Start = "【代码" + (indexOfAnswer) + "】：";
                        string tempCodeTag_End = "【代码" + (indexOfAnswer + 1) + "】：";
                        int tempStartIndex = document.IndexOf(tempCodeTag_Start) + tempCodeTag_Start.Length;
                        int tempEndIndex = document.IndexOf(tempCodeTag_End);
                        if (tempEndIndex == -1) tempEndIndex = document.IndexOf("模板代码") - 2;
                        answerSet.Add(document.Substring(tempStartIndex, tempEndIndex - tempStartIndex).Trim());
                    }
                    // Console.WriteLine("【答案" + indexOfAnswer + "】" + answerSet[answerSet.Count - 1]);
                }

                // Print blanks' tag
                /*
                foreach(Match m in blanks)
                {
                    Console.WriteLine(m.Value.Trim());
                } */

                // 开始填空
                Console.WriteLine("[JavaLabReplacer] 正在填写答案...");
                MatchEvaluator evaluator = new MatchEvaluator(CodeReplacer);
                string replaceResult = Regex.Replace(document, PATTERN_BLANK, CodeReplacer);
                string CODEMODE_END = "=======";
                int codeStart = replaceResult.IndexOf(CODEMOD_START);
                int codeEnd = replaceResult.IndexOf(CODEMODE_END);
                outputString += "实验" + indexOfLab + "：";
                outputString += replaceResult.Substring(codeStart + CODEMOD_START.Length, codeEnd - codeStart - CODEMODE_END.Length) + System.Environment.NewLine;
                outputString += "=============================================" + System.Environment.NewLine;

                Console.WriteLine("-----------------------------------------");
            }

            // 输出填空结果
            //Console.WriteLine("Replace Result:");
            //Console.WriteLine(replaceResult);

            // 写出最终结果到文本文件
            Console.WriteLine("[JavaLabReplacer] 正在写出处理结果...");
            using (StreamWriter streamWriter = new StreamWriter(documentDir + @"\output.txt", false))
                streamWriter.WriteLine(outputString);

            // 打印执行报告
            stopwatch.Stop();
            Console.WriteLine("[JavaLabReplacer] 程序执行完毕，共处理 " + labIndex + " 个实验的 " + blankCount + " 个填空，");
            Console.WriteLine("[JavaLabReplacer] 结果已写出至 " + documentDir + "\\output.txt，全程耗时 " + stopwatch.ElapsedMilliseconds /*DateTime.Now.Subtract(startTime).TotalMilliseconds*/ + " ms");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static string CodeReplacer(Match match)
        {
            indexOfReplace++;
            string result = "";
            try
            {
                // 答案集与空白具体有对应关系
                result = answerSet[indexOfReplace].ToString();
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine("CodeReplacer: " + e.Message);
            }
            return result;
        }
    }
}
