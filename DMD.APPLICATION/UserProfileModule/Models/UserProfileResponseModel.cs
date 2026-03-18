namespace DMD.APPLICATION.UserProfileModule.Models
{
    public class UserProfileResponseModel
    {
        public List<UserProfileModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
