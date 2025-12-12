using System;
using System.IO;
using System.Collections.Generic;

namespace AokanaUnpacker
{
    class Program
    {
        // 默认路径
        static string currentOutputPath = @"D:\Downloads\Game_CG\test";

        static void Main(string[] args)
        {
            Console.Title = "Sprite Extract";

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Sprite社steam版本解包工具");
                Console.WriteLine("------------------------------------------------");
                Console.WriteLine($"当前输出路径: [{currentOutputPath}]");
                Console.WriteLine("------------------------------------------------");
                Console.WriteLine("1. 解包");
                Console.WriteLine("2. 修改默认输出路径");
                Console.WriteLine("------------------------------------------------");
                Console.Write("请输入选项: ");

                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    RunUnpackLoop();
                }
                else if (choice == "2")
                {
                    ChangeOutputPath();
                }
            }
        }

        static void ChangeOutputPath()
        {
            Console.WriteLine("\n请输入新的输出文件夹路径 (支持拖入文件夹):");
            string input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                currentOutputPath = input.Trim('"');
            }
        }

        static void RunUnpackLoop()
        {
            Console.Clear();
            Console.WriteLine(">>> 输入 'edit' 返回主菜单");
            Console.WriteLine($">>> 文件解包至: {currentOutputPath}");

            while (true)
            {
                Console.WriteLine("\n请拖入游戏文件(.dat):");

                string inputRaw = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(inputRaw)) continue;

                if (inputRaw.Trim().ToLower() == "edit")
                {
                    return; 
                }

                Console.Clear();

                string datPath = inputRaw.Trim('"');

                if (!File.Exists(datPath))
                {
                    Console.WriteLine("文件不存在");
                    continue;
                }

                if (!Directory.Exists(currentOutputPath))
                {
                    try { Directory.CreateDirectory(currentOutputPath); }
                    catch { Console.WriteLine("路径错误"); continue; }
                }

                try
                {
                    PRead pRead = new PRead(datPath);

                    if (pRead.ti == null || pRead.ti.Count == 0)
                    {
                        Console.WriteLine("失败：文件中未发现资源");
                    }
                    else
                    {
                        int totalFiles = pRead.ti.Count;
                        Console.WriteLine($"成功：发现 {totalFiles} 个文件。");
                        Console.WriteLine("正在解包...\n");

                        int currentCount = 0;
                        int successCount = 0;

                        foreach (string fileName in pRead.ti.Keys)
                        {
                            currentCount++;

                            string fileExt = Path.GetExtension(fileName).Replace(".", "").ToLower();
                            if (string.IsNullOrEmpty(fileExt)) fileExt = "未知";

                            double progress = (double)currentCount / totalFiles * 100;

                            Console.Write($"\r {currentCount}/{totalFiles} | 文件类型: {fileExt,-5} | 进度: {progress:F1}%   ");

                            try
                            {
                                byte[] fileData = pRead.Data(fileName);

                                if (fileData != null)
                                {
                                    string fullSavePath = Path.Combine(currentOutputPath, fileName);
                                    string dirName = Path.GetDirectoryName(fullSavePath);

                                    if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);

                                    File.WriteAllBytes(fullSavePath, fileData);
                                    successCount++;
                                }
                            }
                            catch
                            {
                                
                            }
                        }

                        Console.WriteLine($"{Path.GetFileName(datPath)} 处理完毕");
                        Console.WriteLine($"共导出 {successCount}/{totalFiles} 个文件。");
                        Console.WriteLine("请拖入游戏文件(.dat)...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n错误: {ex.Message}");
                }
            }
        }
    }
}