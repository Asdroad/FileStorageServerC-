using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace lab_3_zad_2
{
    public class Client
    {

        static void Main()
        {
            TcpClient client = new TcpClient("localhost", 33333);

            string requestType;

            while (true)
            {
                Console.Write("Enter action (1 - get a file, 2 - save a file, 3 - delete a file): > ");
                requestType = Console.ReadLine();
                if (requestType.Equals("1") || requestType.Equals("2") || requestType.Equals("3") || requestType.Equals("exit"))
                {
                    break;
                }
            }

            switch (requestType)    
            {
                case "1":
                    GetFile(client);
                    break;
                case "2":
                    SaveFile(client);
                    break;
                case "3":
                    DeleteFile(client);
                    break;
                default:
                    Console.WriteLine("Exiting...");
                    break;
                    
            }
            
            client.Close(); 
            
        }

        static void GetFile(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            
            Console.Write("Do you want to get the file by name or by id (1 - name, 2 - id): > ");
            string method = "";
            while (true)
            {
                method = Console.ReadLine();
                if (method == "1" || method == "2")
                {
                    break;
                }
            }

            string fileNameOrId = "";
            if (method == "1")
            {
                Console.Write("Enter name of the file: > ");
            }
            else
            {
                Console.Write("Enter id: > ");
            }
            fileNameOrId = Console.ReadLine();
            
            string request = "GET|" + fileNameOrId + "| ";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            stream.Write(requestBytes, 0, requestBytes.Length);
            
            // byte[] buffer = new byte[client.ReceiveBufferSize];
            // int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
            // string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            string filePath = @"..\..\..\..\client\data\";
            string responseStatus = getPart(stream);
            Console.WriteLine(responseStatus);    
            if (responseStatus == "200")
            {
                string fileName = getPart(stream);

                if (File.Exists(filePath + fileName))
                {
                    Console.WriteLine("This file is already on your PC.");
                }
                else
                {
                    long fileSize = long.Parse(getPart(stream));
                    readAndWriteToFile(stream, fileName, fileSize);
                    Console.WriteLine("The file was downloaded! Specify a name for it: > " + fileName);
                    Console.WriteLine("File saved on the hard drive!");
                }
            }
            
            stream.Close();
        }

        static void SaveFile(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            
            Console.Write("Enter name of the file: > ");
            string fileName = Console.ReadLine();
            
            Console.Write("Enter name of the file to be saved on server: > ");
            string fileToSave = Console.ReadLine();

            string filepath = @"..\..\..\..\client\data\";

            if (File.Exists(filepath + fileName))
            {
                byte[] fileBytes = File.ReadAllBytes(filepath + fileName);
                // Console.WriteLine("(" + fileBytes.Length + ")"); 

                FileInfo info = new FileInfo(filepath + fileName);
                string request = "SAVE|" + fileToSave + "|" + info.Length + "|";
                // Console.WriteLine(info.Length);
                
                byte[] requestBytes = Encoding.UTF8.GetBytes(request).Concat(fileBytes).ToArray();
                stream.Write(requestBytes, 0, requestBytes.Length);
                
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.Write(response);
            }
            else
            {
                Console.Write("Enable to found file with name: " + fileName);
            }
            
            stream.Close();
        }

        static void DeleteFile(TcpClient client)    
        {
            NetworkStream stream = client.GetStream();
            
            Console.Write("Do you want to get the file by name or by id (1 - name, 2 - id): > ");
            string method = "";
            while (true)
            {
                method = Console.ReadLine();
                if (method == "1" || method == "2")
                {
                    break;
                }
            }

            string fileNameOrId = "";
            if (method == "1")
            {
                Console.Write("Enter name of the file: > ");
            }
            else
            {
                Console.Write("Enter id: > ");
            }
            fileNameOrId = Console.ReadLine();

            string request = "DELETE|" + fileNameOrId + "| ";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            stream.Write(requestBytes, 0, requestBytes.Length);
            
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.Write(response);
            
            stream.Close();
        }
        
        static string getPart(NetworkStream stream) 
        {
            byte[] buffer = new byte[1];
            StringBuilder dataReceived = new StringBuilder();
        
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
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
            string filePath = @"..\..\..\..\client\data\";
            
            byte[] buffer = new byte[1];
            List<byte> allBytes = new List<byte>();
            Console.WriteLine(fileSize);
            if (!File.Exists(filePath + fileName))
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
}