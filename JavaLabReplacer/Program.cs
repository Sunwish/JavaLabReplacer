using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
namespace JavaLabRelplacer
{
    class Program
    {
        static string documentDir;
        static string documentPath;
        const string PATTERN_BLANK = @"【代码\d*】(?=[^：\n])";
        const string PATTERN_ANSWER = @"(?<=【代码\d*】：).+";
        const string CODEBLOCK_START = "模板代码";
        const string CODEBLOCK_END = "编译并运行该程序";
        private static int indexOfReplace = -1;
        private static ArrayList answerSet = new ArrayList();
        static void Main(string[] args)
        {
            documentDir = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Path.GetDirectoryName(Path.GetDirectoryName(typeof(Program).Assembly.Location))));
            documentPath = documentDir + @"\doc.txt";
            if (!File.Exists(documentPath))
            {
                Console.WriteLine("[Error] 未能找到 " + documentPath);
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            // 时间记录
            DateTime startTime = DateTime.Now;

            // 读取文件流
            StreamReader streamReader = new StreamReader(documentPath);
            string document = streamReader.ReadToEnd();

            // 匹配待填空与答案
            Console.WriteLine("[JavaLabReplacer] 正在扫描文档...");
            MatchCollection blanks = Regex.Matches(document, PATTERN_BLANK);
            MatchCollection answers = Regex.Matches(document, PATTERN_ANSWER);
            Console.WriteLine("[JavaLabReplacer] 已找到填空" + blanks.Count + "个");
            Console.WriteLine("[JavaLabReplacer] 已找到答案" + answers.Count + "个");

            // 将答案收入答案集中
            Console.WriteLine("[JavaLabReplacer] 正在处理多行答案...");
            string tempAns = "";
            foreach (Match m in answers)
            {
                tempAns = m.Value.Trim();
                if (!tempAns.Equals(""))
                    answerSet.Add(m.Value.Trim());
                else
                {
                    // 此处表明答案有多行代码，正则表达式不方便匹配，故单独处理
                    string tempCodeTag_Start = "【代码" + (answerSet.Count + 1) + "】：";
                    string tempCodeTag_End = "【代码" + (answerSet.Count + 2) + "】：";
                    int tempStartIndex = document.IndexOf(tempCodeTag_Start) + tempCodeTag_Start.Length;
                    int tempEndIndex = document.IndexOf(tempCodeTag_End);
                    answerSet.Add(document.Substring(tempStartIndex, tempEndIndex - tempStartIndex).Trim());
                }
                // Console.WriteLine("【答案" + answerSet.Count + "】" + answerSet[answerSet.Count-1]);
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

            // 输出填空结果
            //Console.WriteLine("Replace Result:");
            //Console.WriteLine(replaceResult);

            // 自动提取有效代码块
            Console.WriteLine("[JavaLabReplacer] 正在分离实验代码...");
            int labIndex = 0, startIndex = 0, endIndex = 0;
            string outputString = "";
            startIndex = replaceResult.IndexOf(CODEBLOCK_START, startIndex);
            endIndex = replaceResult.IndexOf(CODEBLOCK_END, endIndex);
            while (startIndex != -1)
            {
                outputString += "实验" + (++labIndex) + "：" + System.Environment.NewLine;
                outputString += replaceResult.Substring(startIndex + CODEBLOCK_START.Length, endIndex - startIndex - CODEBLOCK_START.Length).Trim();
                outputString += System.Environment.NewLine + System.Environment.NewLine + "===============================" + System.Environment.NewLine + System.Environment.NewLine;
                startIndex = replaceResult.IndexOf(CODEBLOCK_START, startIndex + CODEBLOCK_START.Length);
                endIndex = replaceResult.IndexOf(CODEBLOCK_END, endIndex + CODEBLOCK_END.Length);
            }

            // 写出最终结果到文本文件
            Console.WriteLine("[JavaLabReplacer] 正在写出处理结果...");
            using (StreamWriter streamWriter = new StreamWriter(documentDir + @"\output.txt", false))
                streamWriter.WriteLine(outputString);

            // 打印执行报告
            Console.WriteLine("[JavaLabReplacer] 程序执行完毕，共处理 " + labIndex + " 个实验的 " + blanks.Count + " 个填空，");
            Console.WriteLine("[JavaLabReplacer] 结果已写出至 " + documentDir + "\\output.txt，全程耗时 " + DateTime.Now.Subtract(startTime).TotalMilliseconds + " ms");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static string CodeReplacer(Match match)
        {
            indexOfReplace++;
            string result = "";
            try
            {
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
