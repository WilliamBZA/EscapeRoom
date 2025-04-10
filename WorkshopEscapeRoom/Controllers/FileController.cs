using nanoFramework.WebServer;
using System.Collections;
using System.Net;
using System.Text;
using System.IO;
using System;
using System.Diagnostics;

namespace Deployment
{
    public class FileController
    {
        public const string DirectoryPath = "D:\\";

        [Route("api/files")]
        [Method("GET")]
        public void GetFiles(WebServerEventArgs e)
        {
            string output = $"{{\"files\": [{ GetFilesInDirectoryIncludingSubdirectories(DirectoryPath) }]}}";
            
            e.Context.Response.ContentType = "application/json";
            WebServer.OutPutStream(e.Context.Response, output);
        }

        [Route("api/files/deleteallfiles")]
        [Method("DELETE")]
        public void DeleteAllFiles(WebServerEventArgs e)
        {
            var directories = Directory.GetDirectories(DirectoryPath);
            foreach (var dir in directories)
            {
                Directory.Delete(dir, true);
            }

            var files = Directory.GetFiles(DirectoryPath);
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
            var subDirectory = "";
            if (e.Context.Request.RawUrl.ToLower().IndexOf("?") >= 0)
            {
                var parameters = WebServer.DecodeParam(e.Context.Request.RawUrl);
                foreach (var param in parameters)
                {
                    if (param.Name.ToLower() == "subdir")
                    {
                        subDirectory = param.Value;
                    }
                }
            }

            WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.Accepted);
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
    }
}