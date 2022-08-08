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

      
        //blobtobim
        //bimtoblob

        [FunctionName("blobtobim")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var formdata = await req.ReadFormAsync();

            string posturl=string.Empty;
            string filename = string.Empty;
            string uploadkey = string.Empty;
            string bucketkey = string.Empty;
            string objectkey = string.Empty;
            string token = string.Empty;
            string projectid = string.Empty;   
            string upload_file_name = string.Empty;
            string upload_folder_id = string.Empty;
            string upload_object_id = string.Empty;

            try
            {
                posturl = formdata["url"];
                filename = formdata["filename"];
                uploadkey = formdata["uploadkey"];
                bucketkey = formdata["bucketkey"];
                objectkey = formdata["objectkey"];
                token = formdata["token"];
                projectid = formdata["projectid"];
                upload_file_name = formdata["upload_file_name"];
                upload_folder_id = formdata["upload_folder_id"];
                upload_object_id = formdata["upload_object_id"];

                log.LogError("posturl {}\n", posturl);
                log.LogError("filename {}\n",filename);
                log.LogError("uploadkey {}]n",uploadkey);
                log.LogError("bucketkey {}\n",bucketkey);
                log.LogError("objectkey {}\n",objectkey);
                log.LogError("token {}\n",token);
                log.LogError("projectid {}\n",projectid);
                log.LogError("upload_file_name {}\n", upload_file_name);
                log.LogError("upload_folder_id {}\n", upload_folder_id);
                log.LogError("upload_object_id {}\n", upload_object_id);

            } 
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult("Bad form-data");
            }

            log.LogInformation("Downloading from Blob...\n");

            string storageAccount_connectionString = "DefaultEndpointsProtocol=https;AccountName=storagecde;AccountKey=qk9t+wsINASsXfC+W9+e9crkwhYZ1x+I2PFtawdGMLbw8y32YBRL7Lul4QjolCoANLMEMwJC01rR+AStkrFBQw==;EndpointSuffix=core.windows.net";
            string azure_ContainerName = "cde";           
            CloudStorageAccount mycloudStorageAccount = CloudStorageAccount.Parse(storageAccount_connectionString);
            CloudBlobClient blobClient = mycloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(azure_ContainerName);
            CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(filename);
           
            MemoryStream mem = new MemoryStream();
            await cloudBlockBlob.DownloadToStreamAsync(mem);
            log.LogInformation("download completed");
           
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(60);

            StreamContent strm = new StreamContent(mem);
            strm.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            try
            {
                log.LogInformation("Uploading to BIM.... \n");
                // upload file
                var response = await httpClient.PutAsync(posturl, strm);
                var contents = await response.Content.ReadAsStringAsync();
                log.LogError(response.ToString());
                log.LogError(contents.ToString());

                var httpClient2 = new HttpClient();

                log.LogInformation("upload completed\n");

                JObject payload2 = new JObject(new JProperty("uploadKey", uploadkey));

                var httpContent2 = new StringContent(payload2.ToString(), Encoding.UTF8, "application/json");

                httpClient2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string url2 = string.Format(@"https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}/signeds3upload", bucketkey, objectkey);
                log.LogError("url2 {}\n", url2);
                var response2 = await httpClient2.PostAsync(url2, httpContent2);
                var contents2 = await response2.Content.ReadAsStringAsync();
                log.LogError(response2.ToString());
                log.LogError(contents2.ToString());


                log.LogInformation("create the first version of the file\n");

                // create the first version of the file

                
                JObject payload3 = new JObject(
                    new JProperty("jsonapi", new JObject(new JProperty("version", "1.0")),
                    new JProperty("data", new JObject(new JProperty("type", "items"),
                                          new JProperty("attributes", new JObject(new JProperty("displayName", upload_file_name),
                                                                      new JProperty("extensioin", new JObject(new JProperty("type", "items:autodesk.bim360:File"),
                                                                                                  new JProperty("version", "1.0"))))))),
                                          new JProperty("relationships", new JObject(new JProperty("tip", new JObject(new JProperty("data", new JObject(new JProperty("type", "versions"),
                                                                                                                    new JProperty("id", "1")))),
                                                                         new JObject(new JProperty("parent", new JObject(new JProperty("data", new JObject(new JProperty("type", "folders"),
                                                                                                   new JProperty("id", upload_folder_id))))))))),
                    new JProperty("included", new JArray(new JObject(new JProperty("type", "versions"),
                                              new JProperty("id", 1),
                                              new JProperty("attributes", new JObject(new JProperty("name", upload_file_name),
                                                                          new JProperty("extension", new JObject(new JProperty("type", "versions:autodesk.bim360:File"),
                                                                                                     new JProperty("version", "1.0"))),
                                              new JProperty("relationships", new JObject(new JProperty("storage", new JObject(new JProperty("data", new JObject(new JProperty("type", "objects"),
                                                                                                                            new JProperty("id", upload_object_id))))))))))))));


                //JObject payload3 = new JObject
                //{
                //    {"jsonapi", {"version", "1.0" }},
                //    {"data", { "type" , "items"},
                //             {"attributes", new JProperty("displayName", upload_file_name),
                //                                                      new JProperty("extensioin", new JProperty("type", "items:autodesk.bim360:File"),
                //                                                                                  new JProperty("version", "1.0"))),
                //                          new JProperty("relationships", new JProperty("tip", new JProperty("data", new JProperty("type", "versions"),
                //                                                                                                    new JProperty("id", "1"))),
                //                                                         new JProperty("parent", new JProperty("data", new JProperty("type", "folders"),
                //                                                                                   new JProperty("id", upload_folder_id))))),
                //    new JArray("included", new JProperty("type", "versions"),
                //                              new JProperty("id", 1),
                //                              new JProperty("attributes", new JProperty("name", upload_file_name),
                //                                                          new JProperty("extension", new JProperty("type", "versions:autodesk.bim360:File"),
                //                                                                                     new JProperty("version", "1.0"))),
                //                              new JProperty("relationships", new JProperty("storage", new JProperty("data", new JProperty("type", "objects"),
                //                                                                                                            new JProperty("id", upload_object_id)))))
                //};

                //JObject payload3 = new JObject
                //{
                //    {"jsonapi", {"version", "1.0" }},
                //    {"data", {"type", "items"},{ "attributes" ,{ "displayName", }

                //        {
                //            "attributes",{ "displayName", upload_file_name },
                //                              {
                //                "extension", { "type","items:autodesk.bim360:File"},
                //                                             { "version","1.0"}
                //            }
                //        }
                //    }
                //};


                var httpContent3 = new StringContent(payload3.ToString(), Encoding.UTF8, "application/json");
                log.LogError(httpContent3.ToString());
                string url3 = string.Format(@"https://developer.api.autodesk.com/data/v1/projects/{0}/items",projectid);
                var response3 = await httpClient.PostAsync(url3, httpContent3);
                var contents3 = await response3.Content.ReadAsStringAsync();
                log.LogError(response3.ToString());
                log.LogError(contents3.ToString());


                if (response.ReasonPhrase == "OK")
                    return new OkObjectResult(response.ReasonPhrase);
                else
                    return new BadRequestObjectResult(response.ReasonPhrase);
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new BadRequestObjectResult("Upload Failed");
            }
           
            
        }
    }
}
