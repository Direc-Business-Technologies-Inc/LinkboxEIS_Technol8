using ClosedXML.Excel;
using DataAccessLayer.Class;
using DomainLayer.Models;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using Remotion.Data.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Http;

namespace LinkBoxUI.Services.Shopify
{
    public class ShopifyImageService
    {
        NsInventoryItemToShopifyService nsInventoryItemToShopifyService = new NsInventoryItemToShopifyService();
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        NetsuiteHelperMethod nsHelperMethods = new NetsuiteHelperMethod();
        private readonly LinkboxDb _context = new LinkboxDb();

        public async Task<IHttpActionResult> SyncImageToShopify()
        {
            var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NSQueryItemData")).Select(x => x.APIURL).FirstOrDefault();

            try
            {
                var get_jsonresult = await nsInventoryItemToShopifyService.GetParentHeader_Images(677, "Shopify", "");
                var folderData = Newtonsoft.Json.JsonConvert.DeserializeObject<FolderData>(get_jsonresult.ToString());

                var get_parent_heads_cmd = $@"SELECT DISTINCT
	                                            i.id,
	                                            i.itemid,
	                                            p.custrecord_dbti_ecom_platid as product_id
                                            FROM
	                                            item i
	                                            JOIN customrecord_dbti_platform p ON p.custrecord_dbti_ecom_item = i.id
                                            WHERE
	                                            1=1
	                                            AND i.custitem_dbti_ecom_reviseditem IS NOT NULL
	                                            AND i.custitem_dbti_ecom_reviseditem = 'T'
	                                            AND p.custrecord_dbti_ecom_customer = 348 
	                                            AND p.custrecord_dbti_ecom_platid is not null
	                                            AND i.parent IS  NULL";

                var getparent_data = await nsHelper.GetNetsuiteDataTable(QueryEndpoint, get_parent_heads_cmd);

                foreach(var Cabinet_Folder in folderData.Folders)
                {
                    var ProductID = getparent_data.AsEnumerable().FirstOrDefault(x => x.Field<string>("SKU") == Cabinet_Folder.FolderName.ToString());

                    if (ProductID != null)
                    {
                        // If a matching row is found, extract the ProductID
                        var _ProductID = ProductID.Field<string>("ProductId");

                        var SKU_List = _context.FileCabinetImages.Where(x => x.SKUFolder == Cabinet_Folder.FolderName && !x.status.Contains("Deleted")).ToList(); // Lsit of FileID Existed or Uploaded

                        //if(SKU_List.Count > 0)
                        //{
                            var filesToDelete = SKU_List
                                .Where(img => !Cabinet_Folder.Files
                                    .Select(f => f.ToString())
                                    .Contains(img.FileID))
                                .ToList();

                            // Files that exist in the folder but not in the database (Files to upload to platform)
                            var filesToUpload = Cabinet_Folder.Files
                                .Select(f => f.ToString())
                                .Except(SKU_List.Select(img => img.FileID))
                                .ToList();

                            if (filesToUpload.Count > 0)
                            {
                                foreach (var upload in filesToUpload)
                                {
                                    var imageDetails = await nsHelperMethods.GetImageDetails(upload);
                                    var base64String = imageDetails?.base64 ?? "";
                                    var imageName = imageDetails?.name ?? "";

                                    var posting_result = await nsHelperMethods.PostImagetoShopify(_ProductID, base64String, imageName);

                                if (!posting_result.Contains("error"))
                                {

                                    _context.FileCabinetImages.Add(new FileCabinetImage
                                    {
                                        SKUFolder = Cabinet_Folder.FolderName,
                                        FileID = upload,
                                        Platform = "Shopify",
                                        ImageID = _ProductID,
                                        ImageName = imageName,
                                        Type = "Header",
                                        ImageBinary = Convert.FromBase64String(base64String.ToString()),
                                        CreatedDate = DateTime.Now,
                                        status = posting_result.ToString(),
                                    });

                                    _context.SaveChanges();
                                }
                                }
                            }
                                    
                            

                            if (filesToDelete.Count > 0)
                            {
                                foreach (var delete in filesToDelete)
                                {
                                    var Json_Image = _context.FileCabinetImages.Where(x => x.FileID == delete.FileID.ToString()).FirstOrDefault();

                                    long imageId = 0;
                                    using (JsonDocument doc = JsonDocument.Parse(Json_Image.status))
                                    {
                                        imageId = doc.RootElement
                                                         .GetProperty("image")
                                                         .GetProperty("id")
                                                         .GetInt64();
                                    }
                                    var res = await nsHelperMethods.DeleteShopifyImage(_ProductID.ToString(), imageId.ToString());

                                    if (res.IsSuccess)
                                    {
                                        Json_Image.status = $"Deleted - {imageId}";
                                        Json_Image.UpdatedDate = DateTime.Now;
                                         _context.SaveChanges();

                                     }


                                 }
                            }

                           
                        //}

                    }
                }

                return null; 
            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = $"Error - Update Master Item with ProductId Script Call",
                    ApiUrl = "SyncImageToShopify",
                    ErrorMsg = $"Error  - {DateTime.Now} - {ex.Message}",
                    CreateDate = DateTime.Now,
                });
                _context.SaveChanges();
                throw;
            }
        }

    }
    public class FolderData
    {
        public bool Ok { get; set; }
        public ParentFolder ParentFolder { get; set; }
        public List<Folder> Folders { get; set; }
    }

    public class ParentFolder
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; }
    }

    public class Folder
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; }
        public List<int> Files { get; set; }
    }

}