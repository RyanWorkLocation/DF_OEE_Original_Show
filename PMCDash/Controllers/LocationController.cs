using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PMCDash.Models;
namespace PMCDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : BaseApiController
    {
        public LocationController()
        {

        }

        /// <summary>
        /// 取回圖檔與座標描述
        /// </summary>
        /// <param name="request">輸入需要哪個廠區或產線瀏覽圖(產線可空白廠區不行)</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResponse<ImageDefine> Post(RequestProductionLine request)
        {
            //判斷是要廠區圖還是產線圖
            var choice = string.IsNullOrEmpty(request.FactoryName) ? "404" :  
                string.IsNullOrEmpty(request.ProductionName) ? "factory" :"productionline";
            switch (choice)
            {
                case "factory":
                    return new ActionResponse<ImageDefine>
                    {
                        Data = new ImageDefine(imageUrl: "http://" + Request.Host.Value + "/Images/Factory.png",
                        imageInfos: new List<ImageInfo>
                        {
                            //new ImageInfo("全產線", 65.0d, 50.0d),
                        })
                    };              
                case "productionline":
                    return new ActionResponse<ImageDefine>
                    {
                        Data = new ImageDefine(imageUrl: "http://" + Request.Host.Value + "/Images/ProductionLine.png",
                        imageInfos: new List<ImageInfo>
                        {
                            //new ImageInfo("DFL-001", 33.61d, 24.48d),
                            //new ImageInfo("DFD-001", 42.96d, 24.48d)
                        })
                    };
                default:
                    return new ActionResponse<ImageDefine>
                    {
                        Data = new ImageDefine(imageUrl: "Not Find",
                        imageInfos: new List<ImageInfo>
                        {
                        })
                    };
            }
            
           
        }

    }
}
