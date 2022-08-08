using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;

namespace sendafiletohttp
{
    public static class bimtoblob
    {
        [FunctionName("bimtoblob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var formdata = await req.ReadFormAsync();
            string url;
            string filename;
            string uploadkey;
            string bucketkey;
            string objectkey;
            string token;
            string projectid;
            string upload_file_name = string.Empty;
            string upload_folder_id = string.Empty;
            string upload_object_id = string.Empty;
            int size = 0;
            try
            {
                url = formdata["url"];
                filename = formdata["filename"];
                uploadkey = formdata["uploadkey"];
                bucketkey = formdata["bucketkey"];
                objectkey = formdata["objectkey"];
                token = formdata["token"];
                projectid = formdata["projectid"];
                upload_file_name = formdata["upload_file_name"];
                upload_folder_id = formdata["upload_folder_id"];
                upload_object_id = formdata["upload_object_id"];
                size = Int32.Parse(formdata["size"]);
                log.LogError("url {}\n", url);
                log.LogError("filename {}\n", filename);
                log.LogError("uploadkey {}]n", uploadkey);
                log.LogError("bucketkey {}\n", bucketkey);
                log.LogError("objectkey {}\n", objectkey);
                log.LogError("token {}\n", token);
                log.LogError("projectid {}\n", projectid);
                log.LogError("upload_file_name {}\n", upload_file_name);
                log.LogError("upload_folder_id {}\n", upload_folder_id);
                log.LogError("upload_object_id {}\n", upload_object_id);
                log.LogError("size {}\n", size);



            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult("no parameter");
            }
            log.LogInformation("downloading from BIM360...\n");
            try
            {
                //var httpClient = new HttpClient();
                //httpClient.Timeout = TimeSpan.FromMinutes(60);
               
                //string range = string.Format("bytes={0}-{1}", start, end);
                //httpClient.DefaultRequestHeaders.Add("range", range);
                //var response = await httpClient.GetAsync(url); 
                //var contents = await response.Content.ReadAsStreamAsync();
               
                 
                log.LogInformation("uploading to Blob...\n");
                string storageAccount_connectionString = "DefaultEndpointsProtocol=https;AccountName=storagecde;AccountKey=qk9t+wsINASsXfC+W9+e9crkwhYZ1x+I2PFtawdGMLbw8y32YBRL7Lul4QjolCoANLMEMwJC01rR+AStkrFBQw==;EndpointSuffix=core.windows.net";
                string azure_ContainerName = "cde";
                CloudStorageAccount mycloudStorageAccount = CloudStorageAccount.Parse(storageAccount_connectionString);
                CloudBlobClient blobClient = mycloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(azure_ContainerName);
                CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(filename);




                int bytesize = 8000000;
                // local variable to track the current number of bytes read into buffer
                int bytesRead;

                // track the current block number as the code iterates through the file
                int blockNumber = 0;
                int offset= 0;
                // Create list to track blockIds, it will be needed after the loop
                List<string> blockList = new List<string>();

              

                do
                {

                    var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromMinutes(60);
                    string range = string.Format("bytes={0}-{1}", offset, offset + bytesize);

                    log.LogInformation("range {0}\n", range);
                    httpClient.DefaultRequestHeaders.Add("range", range);
     
                    var response = httpClient.GetAsync(url).Result;  
                        
                    var contents = Task.Run(() => response.Content.ReadAsStreamAsync()).Result;
                    
                    log.LogError("content length {}\n", contents.Length);

                    // increment block number by 1 each iteration
                    blockNumber++;
                    
                    // set block ID as a string and convert it to Base64 which is the required format
                    string blockId = $"{blockNumber:0000000}";
                    string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

                    // create buffer and retrieve chunk
                    byte[] buffer = new byte[bytesize];


                    //MemoryStream mem = new MemoryStream();
                    bytesRead = await contents.ReadAsync(buffer,0,3333skesk bytesize);
                    log.LogInformation("offset {0} size {1} byteread {2}\n", offset, offset + bytesize, bytesRead);
                    offset = bytesize * blockNumber + blockNumber;


                    // Upload buffer chunk to Azure
                    //await cloudBlockBlob.PutBlockAsync(base64BlockId, mem, null);

                    await cloudBlockBlob.PutBlockAsync(base64BlockId, new MemoryStream(buffer, 0, bytesRead), null);

                    // add the current blockId into our list
                    blockList.Add(base64BlockId);
                   // contents.Dispose();
                    // While bytesRead == size it means there is more data left to read and process
                } while (offset < size);

                // add the blockList to the Azure which allows the resource to stick together the chunks
                await cloudBlockBlob.PutBlockListAsync(blockList);

                // make sure to dispose the stream once your are done
              





               // await cloudBlockBlob.UploadFromStreamAsync(contents);
            } catch(Exception e)
            {
                log.LogError(e.Message);
                return new BadRequestObjectResult(e.Message);
            }

            return new OkObjectResult("OK");
        }
    }
}
