using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace MeetingRoomBot.Helpers
{
    public class iCSGenerator
    {
        static string GetBlobSasUri(CloudBlob blob)
        {
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

            //Generate the shared access signature on the blob, setting the constraints directly on the signature.
            string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

            //Return the URI string for the container, including the SAS token.
            return blob.Uri + sasBlobToken;
        }
        static string GetContainerSasUri(CloudBlobContainer container)
        {
            //Set the expiry time and permissions for the container.
            //In this case no start time is specified, so the shared access signature becomes valid immediately.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Write;

            //Generate the shared access signature on the container, setting the constraints directly on the signature.
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            //Return the URI string for the container, including the SAS token.
            return container.Uri + sasContainerToken;
        }
        private static Uri Upload(string content)
        {
            var fn = Guid.NewGuid().ToString() + ".ics";
            var key = ConfigurationManager.AppSettings["StorageKey"];
            var name = ConfigurationManager.AppSettings["StorageName"];
            CloudStorageAccount acct = new CloudStorageAccount(new StorageCredentials(name, key), true);
            var bc = acct.CreateCloudBlobClient();
            var cRef = bc.GetContainerReference("tmmrbot");
            cRef.CreateIfNotExists();
            CloudBlockBlob blockBlob = cRef.GetBlockBlobReference(fn);
            var buffer = Encoding.UTF8.GetBytes(content);
            blockBlob.UploadFromByteArray(buffer, 0, buffer.Count());
            return new Uri(GetBlobSasUri((CloudBlob)blockBlob));
        }
        public static Uri Save(DateTimeOffset start, DateTimeOffset end, string roomId,string tzOffset = "0000", string desc = "Booked by Cortana")
        {
            var template = File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/ics.txt"));
            var ics = template
                        .Replace("{START}", start.ToUniversalTime().ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{END}", end.ToUniversalTime().ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{TIMESTAMP}", new DateTimeOffset(DateTime.UtcNow, start.Offset).ToUniversalTime().ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{UUID}", Guid.NewGuid().ToString())
                        .Replace("{CREATETIME}", DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{TIMESTAMP}", DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{ROOMID}", roomId)
                        .Replace("{SUMMARY}", desc ?? "Booked by Cortana")
                        .Replace("{DESC}", desc?? "Booked by Cortana");
            var fn = Guid.NewGuid().ToString() + ".ics";
            return Upload(ics);
        }
        public static Uri Save(DateTimeOffset start, DateTimeOffset end, string roomId, string desc = "Booked by Cortana")
        {
            var template = File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/ics.txt"));
            
            var ics = template
                        .Replace("{START}", start.ToUniversalTime().ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{END}", end.ToUniversalTime().ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{TIMESTAMP}", new DateTimeOffset(DateTime.UtcNow).ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{UUID}", Guid.NewGuid().ToString())
                        .Replace("{CREATETIME}", DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{TIMESTAMP}", DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"))
                        .Replace("{ROOMID}", roomId)
                        .Replace("{SUMMARY}", desc ?? "Booked by Cortana")
                        .Replace("{DESC}", desc ?? "Booked by Cortana");
            var fn = Guid.NewGuid().ToString() + ".ics";
            return Upload(ics);
        }
    }
}