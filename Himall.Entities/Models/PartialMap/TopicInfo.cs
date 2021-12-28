using PetaPoco;
using System.Configuration;

namespace Himall.Entities
{
    public partial class TopicInfo
    {
        protected string ImageServerUrl = "";
        [ResultColumn]
        public string TopImageUrl
        {
            get { return Core.HimallIO.GetImagePath(TopImage); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(ImageServerUrl))
                    TopImage = value.Replace(ImageServerUrl, "");
                else
                    TopImage = value;
            }
        }

        [ResultColumn]
        public string BackgroundImageUrl
        {
            get { return  Core.HimallIO.GetImagePath(BackgroundImage); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(ImageServerUrl))
                    BackgroundImage = value.Replace(ImageServerUrl, "");
                else
                    BackgroundImage = value;
            }
        }

        [ResultColumn]
        public string FrontCoverImageUrl
        {
            get { return  Core.HimallIO.GetImagePath(FrontCoverImage); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(ImageServerUrl))
                    FrontCoverImage = value.Replace(ImageServerUrl, "");
                else
                    FrontCoverImage = value;
            }
        }


    }
}
