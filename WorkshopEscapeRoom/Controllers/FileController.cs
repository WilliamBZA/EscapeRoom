using nanoFramework.WebServer;
using System.Collections;
using System.Net;
using System.Text;
using System.IO;
using System.Diagnostics;
using nanoFramework.System.IO.FileSystem;
using System;

namespace _0.ValidatePrerequisites
{
    public class FileController
    {
        public static SDCard fileSystem = new SDCard();

        public const string DirectoryPath = "D:\\";

        [Route("api/files")]
        [Method("GET")]
        public void GetFiles(WebServerEventArgs e)
        {
            string output = $"{{\"files\": [{ GetFilesInDirectoryIncludingSubdirectories("I:\\") }]}}";
            
            e.Context.Response.ContentType = "application/json";
            WebServer.OutPutStream(e.Context.Response, output);
        }

        [Route("api/files/deleteallfiles")]
        [Method("DELETE")]
        public void DeleteAllFiles(WebServerEventArgs e)
        {
            var directories = Directory.GetDirectories("I:\\");
            foreach (var dir in directories)
            {
                Directory.Delete(dir, true);
            }

            var files = Directory.GetFiles("D:\\");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        [Route("api/files")]
        [Method("DELETE")]
        public void DeleteFile(WebServerEventArgs e)
        {
            var parameters = WebServer.DecodeParam(e.Context.Request.RawUrl);
            foreach (var parameter in parameters)
            {
                if (parameter.Name.ToLower() == "file")
                {
                    File.Delete(parameter.Value);
                    WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.Accepted);
                    return;
                }
            }

            WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
        }

        [Route("api/files")]
        [Method("POST")]
        public void AddFile(WebServerEventArgs e)
        {
            /*
            var filename = "I:\\text.txt";//GetHeaderValue(e.Context.Request.Headers, "filename");
            if (filename.Length == 0)
            {
                //WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
                //return;
            }

            

            var length = e.Context.Request.ContentLength64;
            var body = new byte[1024];
            var amountRead = e.Context.Request.InputStream.Read(body, 0, body.Length);

            using (var file = File.Create(filename))
            {
                int position = 0;
                while (amountRead != 0)
                {
                    file.Write(body, 0, amountRead);

                    position += amountRead;

                    amountRead = e.Context.Request.InputStream.Read(body, 0, body.Length);
                }

                file.Close();
            }

            */

            //var data = UTF8Encoding.UTF8.GetString(body, 0, body.Length);
            //Debug.WriteLine(data);

            //string fullPath = $"{DirectoryPath}\\{(directory.Length > 0 ? directory + "\\" : "")}{filename}";
            //Debug.WriteLine($"Uploading file to '{fullPath}'");

            //File.WriteAllBytes(fullPath, body);




            //var form = e.Context.Request.StreamFilePartsToStorage("I:\\");
            //var files = form.Files;

            //var subDirectory = "";
            //foreach (var formParam in form.Parameters)
            //{
            //    if (formParam.Name == "subdir")
            //    {
            //        subDirectory = formParam.Data;
            //    }
            //}

            //foreach (var file in files)
            //{
            //    string fullPath = $"{DirectoryPath}\\{(subDirectory.Length > 0 ? subDirectory + "\\" : "")}{file.FileName}";
            //    Debug.WriteLine($"Uploading file to '{fullPath}'");

            //    using (var writeFile = File.Create(fullPath))
            //    {
            //        file.Data.CopyTo(writeFile);
            //    }
            //}

            WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.Accepted);
        }

        private string GetHeaderValue(WebHeaderCollection headers, string headername)
        {
            foreach (var header in headers.AllKeys)
            {
                if (header.ToLower() == headername)
                {
                    return headers[header];
                }
            }

            return "";
        }

        private string GetFilesInDirectoryIncludingSubdirectories(string directory)
        {
            var directories = Directory.GetDirectories(directory);
            var subDirResult = "";

            foreach(var dir in directories)
            {
                var currentSubDirFiles = GetFilesInDirectoryIncludingSubdirectories(dir + "\\");

                if (subDirResult.Length > 0 && currentSubDirFiles.Length > 0)
                {
                    subDirResult += ", ";
                }

                subDirResult += currentSubDirFiles;
            }

            var files = Directory.GetFiles(directory);
            var fileResult = "";

            foreach(var file in files)
            {
                if (fileResult.Length > 0)
                {
                    fileResult += ", ";
                }

                fileResult += $"\"{file}\"";
            }

            foreach(var dir in directories)
            {
                if (fileResult.Length > 0)
                {
                    fileResult += ", ";
                }

                fileResult += $"\"'{dir}'\"";
            }

            var finalResult = "";
            for (var x = 0; x < fileResult.Length; x++)
            {
                if (fileResult[x] == '\\')
                {
                    finalResult += "\\\\";
                } else
                {
                    finalResult += fileResult[x];
                }
            }
            fileResult = finalResult;

            if (fileResult.Length > 0 && subDirResult.Length > 0)
            {
                return fileResult + ", " + subDirResult;
            }

            return fileResult + subDirResult;
        }

        public static bool MountSDCard()
        {
            try
            {
                fileSystem.Mount();
                Debug.WriteLine("Card Mounted");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Card failed to mount : {ex.Message}");
                Debug.WriteLine($"IsMounted {fileSystem.IsMounted}");
            }

            return false;
        }
    }
}