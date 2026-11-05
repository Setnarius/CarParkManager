using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CarParkManager
{
    public static class Validator
    {
        private const string PlateLetters = "АВЕКМНОРСТУХ";
        private const string LatinLetters = "ABEKMHOPCTYX";

        public static string NormalizePlate(string plate)
        {
            plate = (plate ?? "").Replace(" ", "").ToUpper();

            char[] chars = plate.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                int index = LatinLetters.IndexOf(chars[i]);

                if (index >= 0)
                {
                    chars[i] = PlateLetters[index];
                }
            }

            return new string(chars);
        }

        public static bool IsPlate(string plate)
        {
            return Regex.IsMatch(plate, "^[" + PlateLetters + "][0-9]{3}[" + PlateLetters + "]{2}[0-9]{2,3}\\z");
        }

        public static bool IsEmail(string email)
        {
            return email.Length <= 100 && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+\z");
        }

        public static string NormalizePhone(string phone)
        {
            return Regex.Replace(phone ?? "", @"[\s\-()]", "");
        }

        public static bool IsPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^\+?[0-9]{10,15}\z");
        }

        public static bool IsLicenseNumber(string number)
        {
            return Regex.IsMatch(number, @"^[0-9]{10}\z");
        }

        public static string CheckPassword(string password)
        {
            string error = "";

            if (password.Length < 8 || password.Length > 128)
            {
                error += "От 8 до 128 символов. ";
            }
            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                error += "Нужна заглавная буква. ";
            }
            if (!Regex.IsMatch(password, "[a-z]"))
            {
                error += "Нужна строчная буква. ";
            }
            if (!Regex.IsMatch(password, "[0-9]"))
            {
                error += "Нужна цифра.";
            }

            return error.Trim();
        }
    }
}
