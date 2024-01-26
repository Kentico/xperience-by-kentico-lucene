using System.Linq;

namespace DancingGoat.Models
{
    public record CafeViewModel(string Name, string PhotoPath, string PhotoShortDescription, string Street, string City, string Country, string ZipCode, string Phone)
    {
        public static CafeViewModel GetViewModel(Cafe cafe)
        {
            var cafePhoto = cafe.CafePhoto?.FirstOrDefault();
            return new CafeViewModel(
                cafe.CafeName,
                cafePhoto?.ImageFile.Url,
                cafePhoto?.ImageShortDescription,
                cafe.CafeStreet,
                cafe.CafeCity,
                cafe.CafeCountry,
                cafe.CafeZipCode,
                cafe.CafePhone);
        }
    }
}
