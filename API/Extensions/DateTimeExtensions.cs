namespace API.Extensions
{
    public static class DateTimeExtensions
    {
        public static int CalculateAge(this DateOnly dob){
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var age = today.Year - dob.Year; //2023 - 1996 = 27

            if (dob > today.AddYears(-age)) age--;// I was born in Jan so my dob

            return age;
        }
    }
}