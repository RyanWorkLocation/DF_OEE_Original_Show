using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class ImageDefine
    {
        public ImageDefine(string imageUrl, List<ImageInfo> imageInfos)
        {
            ImageUrl = imageUrl;
            ImageInfos = imageInfos;
        }

        public string ImageUrl { get; set; }

        public virtual List<ImageInfo> ImageInfos { get; set; }
    }

    public class ImageInfo
    {
        public ImageInfo(string imageName, double px, double py, string status)
        {
            ImageName = imageName;
            Px = px;
            Py = py;
            Status = status;

        }

        //可標註重要機台名稱
        public string ImageName { get; set; }
        //X位置
        public double Px { get; set; }
        //Y位置
        public double Py { get; set; }
        //機台狀態
        public string Status { get; set; }
    }
}
