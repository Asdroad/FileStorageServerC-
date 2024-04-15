using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;

namespace lab_3_zad_2
{
    public class Server3
    {
        private static readonly object fileLock = new object();
        private static readonly object dictLock = new object();
        private static Dictionary<int, string> idsWithNames;

        static async Task Main()
        {
            idsWithNames = GetFilesIds();

            TcpListener listener = new TcpListener(IPAddress.Any, 33333);
            listener.Start();
            Console.WriteLine("Сервер начал работу");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Клиент подключен");

                Task.Run(() => HandleClient(client));
            }
        }

        static void HandleClient(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                string methodType = getMethodType(stream).Result;
                Console.WriteLine(methodType);

                if (methodType.Equals("SAVE"))
                {
                    SaveFileCommand(stream);
                }
                else if (methodType.Equals("GET"))
                {
                    GetFileCommand(stream);
                }
                else if (methodType.Equals("DELETE"))
                {
                    DeleteFileCommand(stream);
                }
                Thread.Sleep(100000000);
                stream.Close();
            }
        }
        
        static Dictionary<int, string> GetFilesIds()
        {
            string fileWithIdsPath = @"..\..\..\..\server\ids.txt";
            if (File.Exists(fileWithIdsPath))
            {
                string[] lines = File.ReadAllLines(fileWithIdsPath);
                
                Dictionary<int, string> idsNames = new Dictionary<int, string>();
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split('|');
                    if (parts.Count() == 2)
                        idsNames.Add(Int32.Parse(parts[0]), parts[1]);
                }

                return idsNames;
            }
            else
            {
                return null;
            }
        }

        static void AddNewFileId(string fileName, int id)
        {
            lock (dictLock)
            {
                string fileWithIdsPath = @"..\..\..\..\server\ids.txt";
                if (File.Exists(fileWithIdsPath))
                {
                    File.AppendAllText(fileWithIdsPath, id + "|" + fileName + "\n");
                }    
            }
        }

        static void DeleteById(Dictionary<int, string> ids)
        {
            lock (dictLock)
            {
                string fileWithIdsPath = @"..\..\..\..\server\ids.txt";
                if (File.Exists(fileWithIdsPath))
                {
                    File.WriteAllText(fileWithIdsPath, "");
                    int[] idArr = ids.Keys.ToArray();
                    for (int i = 0; i < ids.Count; i++)
                    {
                        File.AppendAllText(fileWithIdsPath, idArr[i] + "|" + ids[idArr[i]] + "\n");
                    }
                }   
            }
        }

        static async Task<string> getMethodType(NetworkStream stream) 
        {
            byte[] buffer = new byte[1];
            StringBuilder dataReceived = new StringBuilder();
        
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                char receivedChar = (char)buffer[0];
                if (receivedChar == '|')
                {
                    break;
                }
                dataReceived.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }
        
            return dataReceived.ToString();
        }

        static void readAndWriteToFile(NetworkStream stream, string fileName, long fileSize)
        {
            string filePath = @"..\..\..\..\server\data\";
            lock (fileLock)
            {
                byte[] buffer = new byte[1];
                List<byte> allBytes = new List<byte>();
                Console.WriteLine(fileSize);
                if (File.Exists(filePath + fileName))
                {
                    for (int i = 0; i < fileSize; i++)
                    {
                        int bytesRead =  stream.Read(buffer, 0, buffer.Length);
                        allBytes.Add(buffer[0]);
                    }

                    Console.WriteLine("(" + allBytes.Count + ")");
                    File.WriteAllBytes(filePath + fileName, allBytes.ToArray());
                }    
            }
        }

        static async void SaveFileCommand(NetworkStream stream)
        {
            string filePath = @"..\..\..\..\server\data\";
            string fileName = await getMethodType(stream);
            Console.WriteLine(fileName);
            if (File.Exists(filePath + fileName))
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes("403"));
            }
            else
            {
                int newKey = 0;
                using (var mutex = new Mutex(false, "FileIdMutex"))
                {
                    Dictionary<int, string>.KeyCollection keys = idsWithNames.Keys;
                    if (keys.Count != 0)
                    {
                        newKey = keys.Max() + 1;  
                    }
                        
                    AddNewFileId(fileName, newKey);
                    idsWithNames = GetFilesIds();

                    long fileSize = long.Parse(getMethodType(stream).Result);
                            
                    FileStream fileStream = File.Create(filePath + fileName);
                    fileStream.Close();
                            
                    readAndWriteToFile(stream, fileName, fileSize);
                }
                        
                Console.WriteLine(1234567890);
                await stream.WriteAsync(Encoding.UTF8.GetBytes("200, " + newKey));
            }    
        }

        static async void GetFileCommand(NetworkStream stream)
        {
            string filePath = @"..\..\..\..\server\data\";
            string fileName = await getMethodType(stream);
            bool isId = !fileName.Contains('.');
            if (isId)
            {
                try
                {
                    fileName = idsWithNames[Int32.Parse(fileName)];
                }
                catch (Exception e)
                {
                    fileName = "..";
                }
            }

            if (File.Exists(filePath + fileName))
            {
                using (var mutex = new Mutex(false, "FileIdMutex"))
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath + fileName);
                    FileInfo info = new FileInfo(filePath + fileName);
                    string responce = "200|" + fileName + "|" + info.Length + "|";
                    Console.WriteLine(responce);
                    byte[] responseBytes = Encoding.ASCII.GetBytes(responce).Concat(fileBytes).ToArray();
                    stream.Write(responseBytes);
                }
            }
            else
            {
                stream.Write(Encoding.ASCII.GetBytes("404|"));
            }
        }

        static async void DeleteFileCommand(NetworkStream stream)
        {
            string filePath = @"..\..\..\..\server\data\";
            string fileName = await getMethodType(stream);
            bool isId = !fileName.Contains('.');
            int fileId = -1;
            if (isId)
            {
                try
                {
                    fileId = Int32.Parse(fileName);
                    fileName = idsWithNames[Int32.Parse(fileName)];
                }
                catch (Exception e)
                {
                    fileName = "..";
                }
            }
            if (File.Exists(filePath + fileName))
            {
                using (var mutex = new Mutex(false, "FileIdMutex"))
                {
                    idsWithNames.Remove(fileId);
                    DeleteById(idsWithNames);
                    idsWithNames = GetFilesIds();
                    File.Delete(filePath + fileName);
                }
                        
                stream.Write(Encoding.UTF8.GetBytes("200"));
            }
            else
            {
                stream.Write(Encoding.UTF8.GetBytes("404"));
            }
        }
    }
}