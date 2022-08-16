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
using Newtonsoft.Json.Linq;

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
            string s3url;
            string bimfilename;
            string stgconnectionstring;
            string containername;
            string callbackurl;
            try
            {
                s3url = formdata["s3url"];
                bimfilename = formdata["bimfilename"];
                stgconnectionstring = formdata["stgconnectionstring"];
                containername = formdata["containername"];
                callbackurl = formdata["callbackurl"];

                log.LogInformation("s3url {}\n", s3url);
                log.LogInformation("blobname {}\n", bimfilename);
                log.LogInformation("stgconnectionstring {}\n", stgconnectionstring);
                log.LogInformation("containername {}\n", containername);

            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult("missing parameter");
            }
          
            try
            {
                                         
                CloudStorageAccount mycloudStorageAccount = CloudStorageAccount.Parse(stgconnectionstring);
                CloudBlobClient blobClient = mycloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(containername);
                CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(bimfilename);

                
                int bytesize = 50000000;
                // local variable to track the current number of bytes read into buffer
                int bytesRead;

                // track the current block number as the code iterates through the file
                int blockNumber = 0;
                int offset= 0;
                // Create list to track blockIds, it will be needed after the loop
                List<string> blockList = new List<string>();
                byte[] buffer = new byte[bytesize];
               
                log.LogInformation("start downloading from M360...\n");
                do
                {

                    var httpClient = new HttpClient();                

                    string range = string.Format("bytes={0}-{1}", offset, offset + bytesize-1);

                    httpClient.DefaultRequestHeaders.Add("range", range);
                  
                    var response = httpClient.GetAsync(s3url).Result;                          
                    var contents = Task.Run(() => response.Content.ReadAsStreamAsync()).Result;
                   
                    if (contents.Length != bytesize)
                    {
                        log.LogError(response.ToString());
                        log.LogError(contents.ToString());
                    }
                    // increment block number by 1 each iteration
                    blockNumber++;                    
                    // set block ID as a string and convert it to Base64 which is the required format
                    string blockId = $"{blockNumber:0000000}";
                    string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));
                   
                    // create buffer and retrieve chunk
                   //byte[] buffer = new byte[bytesize];

                    //MemoryStream mem = new MemoryStream();
                    bytesRead = await contents.ReadAsync(buffer,0, bytesize);
                    log.LogInformation("{0,2} blocknumber {1,30} content length {2,12} byteread {3,12}\n", blockNumber, range, contents.Length,bytesRead);
                    offset = bytesize * blockNumber;

                    
                    // Upload buffer chunk to Azure
                    //await cloudBlockBlob.PutBlockAsync(base64BlockId, mem, null);

                    await cloudBlockBlob.PutBlockAsync(base64BlockId, new MemoryStream(buffer, 0, bytesRead), null);

                    // add the current blockId into our list
                    blockList.Add(base64BlockId);
                
                    // contents.Dispose();
                    // While bytesRead == size it means there is more data left to read and process

                } while (bytesRead == bytesize);
           
                // add the blockList to the Azure which allows the resource to stick together the chunks
               await cloudBlockBlob.PutBlockListAsync(blockList);

                // make sure to dispose the stream once your are done


                // await cloudBlockBlob.UploadFromStreamAsync(contents);
                var httpClient2 = new HttpClient();
               
                
                string payload = string.Format(@"{{ ""bimfilename"":""{0}"", ""size"":{1} }}",bimfilename, offset + bytesRead);
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");                
                var response2 = await httpClient2.PostAsync(callbackurl, httpContent);
                //var contents = Task.Run(() => response.Content.ReadAsStreamAsync()).Result;

            } catch(Exception e)
            {
                log.LogError(e.Message);
                return new BadRequestObjectResult(e.Message);
            }

            return new OkObjectResult("OK");
        }
    }
}
