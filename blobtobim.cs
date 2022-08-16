using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Azure.Storage.Blobs;
using Azure.Storage;
using System.Collections.Generic;
using Azure.Storage.Blobs.Models;
using System.Diagnostics;
using Azure;
using Amazon.S3;
using System.Web;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net;

namespace sendafiletohttp
{
    public static class blobtobim
    {
        public class PresignedPost
        {
            public string url { get; set; }
            public Fields fields { get; set; }
        }
        public class Fields
        {
            public string AWSAccessKeyId { get; set; }
            public string acl { get; set; }
            public string key { get; set; }
            public string policy { get; set; }
            public string signature { get; set; }
        }
        public class s3url
        {
            public string url { get; set; }
            public string uploadId { get; set; }
            public string partNumber { get; set; }
            public string X_Amz_Security_Token { get; set; }
            public string X_Amz_Algorithm { get; set; }
            public string X_Amz_Date { get; set; }
            public string X_Amz_SignedHeaders { get; set; }
            public string X_Amz_Expire { get; set; }
            public string X_Amz_Credential { get; set; }
            public string X_Amz_Signature { get; set; }
        }

        public class completupload
        {
            public string bucketKey { get; set; }
            public string objectId { get; set; }
            public string objectKey { get; set; }
            public string size { get; set; }
            public string contentType { get; set; }
            public string location { get; set; }
        }

      
        //blobtobim
        //bimtoblob

        [FunctionName("blobtobim")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var formdata = await req.ReadFormAsync();

            string[] posturl;            
            string blobname = string.Empty;
            string uploadkey = string.Empty;
            string bucketkey = string.Empty;
            string objectkey = string.Empty;          
            string projectid = string.Empty;   
            string upload_file_name = string.Empty;
            string upload_folder_id = string.Empty;
            string upload_object_id = string.Empty;
            string stgconnectionstring = string.Empty; 
            string containername = string.Empty;
            string callbackurl = string.Empty;
            try
            {
                string url = formdata["url"];
                posturl = url.Split(',');
                callbackurl = formdata["callbackurl"];
                blobname = formdata["blobname"];
                uploadkey = formdata["uploadkey"];
                bucketkey = formdata["bucketkey"];
                objectkey = formdata["objectkey"];
          
                projectid = formdata["projectid"];
                stgconnectionstring = formdata["stgconnectionstring"];
                containername = formdata["containername"];
                upload_file_name = formdata["upload_file_name"];
                upload_folder_id = formdata["upload_folder_id"];
                upload_object_id = formdata["upload_object_id"];

                log.LogError("posturl leng {}\n", posturl.Length);
                for (int i = 0; i < posturl.Length; i++)
                    log.LogError("posturl{0} {1}\n", i, posturl[i]);

                log.LogError("blobname {}\n",blobname);
                log.LogError("uploadkey {}]n",uploadkey);
                log.LogError("bucketkey {}\n",bucketkey);
                log.LogError("objectkey {}\n",objectkey);          
                log.LogError("projectid {}\n",projectid);
                log.LogError("upload_file_name {}\n", upload_file_name);
                log.LogError("upload_folder_id {}\n", upload_folder_id);
                log.LogError("upload_object_id {}\n", upload_object_id);
                log.LogError("stgconnectionstring {}\n", stgconnectionstring);
                log.LogError("containername {}\n", containername);

            } 
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult("missing parameter");
            }

            log.LogInformation("Downloading from Blob...\n");

            CloudStorageAccount mycloudStorageAccount = CloudStorageAccount.Parse(stgconnectionstring);
            CloudBlobClient blobClient = mycloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containername);
            CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(blobname);



            //MemoryStream mem = new MemoryStream();
            //await cloudBlockBlob.DownloadToStreamAsync(mem);
            //byte[] content = mem.ToArray();


            //log.LogInformation("download completed length {}\n",mem.Length);



            
            try
            {
                log.LogInformation("Uploading to BIM.... \n");
                int offset = 0;
                int idx = 0;
                int bytesize = 50000000;
                byte[] buffer = new byte[bytesize];
                do
                {
                    var httpClient = new HttpClient();
                    // httpClient.Timeout = TimeSpan.FromMinutes(60);

                    MemoryStream memStream = new MemoryStream();

                    //await cloudBlockBlob.DownloadToStreamAsync(memStream);
                    await cloudBlockBlob.DownloadRangeToStreamAsync(memStream, offset, bytesize);
                    //await cloudBlockBlob.DownloadRangeToByteArrayAsync(buffer,0,offset, 3000);
                    //await cloudBlockBlob.DownloadToByteArrayAsync(buffer, 0);

                    log.LogInformation("memStream {}\n", memStream.Length);               
                    
                    StreamContent strmcontent = new StreamContent(memStream);


              //      strmcontent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                    var content = new MultipartFormDataContent();
                 //   content.Headers.ContentType.MediaType = "multipart/form-data";
                    var byte_content = new ByteArrayContent(strmcontent.ReadAsByteArrayAsync().Result);                   
                 //   byte_content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    content.Add(byte_content);
                  //  httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

                    var response = await httpClient.PutAsync(posturl[idx], content);

                    var contents = await response.Content.ReadAsStringAsync();


                    log.LogError("response {}\n", response.ToString());
                    log.LogError("contents {}\n", contents.ToString());                  

                    offset = offset + bytesize;
                    idx++;

                } while (posturl.Length > idx);

                log.LogInformation("exit do while\n");
                var httpClient2 = new HttpClient();

                string payload = string.Format(@"{{ ""blobname"":""{0}"" }}", blobname);
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                var response2 = await httpClient2.PostAsync(callbackurl, httpContent);

                //var httpClient2 = new HttpClient();
                /*
                log.LogInformation("upload completed\n");
                JObject payload3 = new JObject(new JProperty("uploadKey", uploadkey));
                var httpContent3 = new StringContent(payload3.ToString(), Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                string url3 = string.Format(@"https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}/signeds3upload", bucketkey, objectkey);
                
                var response3 = await httpClient.PostAsync(url3, httpContent3);
                var contents3 = await response3.Content.ReadAsStringAsync();
                log.LogError("httpclient-response--- {}\n",response3.ToString());
                log.LogError("response {}\n",contents3.ToString());
                completupload cu = JsonConvert.DeserializeObject<completupload>(contents3);



                log.LogInformation("create the first version of the file\n");
                // create the first version of the file           

                string payload4 = string.Format(@"{{""jsonapi"":{{""version"":""1.0""}},""data"":{{""type"":""items"",""attributes"":{{""displayName"":""{0}"",""extension"":{{""type"":""items:autodesk.bim360:File"",""version"":""1.0""}}}},""relationships"":{{""tip"":{{""data"":{{""type"":""versions"",""id"":""1""}}}},""parent"":{{""data"":{{""type"":""folders"",""id"":""{1}""}}}}}}}},""included"":[{{""type"":""versions"",""id"":""1"",""attributes"":{{""name"":""{2}"",""extension"":{{""type"":""versions:autodesk.bim360:File"",""version"":""1.0""}}}},""relationships"":{{""storage"":{{""data"":{{""type"":""objects"",""id"":""{3}""}}}}}}}}]}}", upload_file_name, upload_folder_id,upload_file_name, cu.objectId);
                var httpContent4 = new StringContent(payload4, Encoding.UTF8, "application/json");  
                string url4 = string.Format(@"https://developer.api.autodesk.com/data/v1/projects/{0}/items",projectid);
                httpClient.DefaultRequestHeaders.Authorization =  new AuthenticationHeaderValue("Bearer", token);
               
                var response4 = await httpClient.PostAsync(url4, httpContent4);
                var contents4 = await response4.Content.ReadAsStringAsync();
                log.LogError("response {}\n",response4.ToString());
                log.LogError("content {}\n",contents4.ToString());

                */
                //if (response.ReasonPhrase == "OK")
                //    return new OkObjectResult(response.ReasonPhrase);
                //else
                //    return new BadRequestObjectResult(response.ReasonPhrase);
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new BadRequestObjectResult("Upload Failed");
            }
            return new OkObjectResult("Ok");

        }
    }
}
